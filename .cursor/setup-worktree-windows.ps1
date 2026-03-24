$ErrorActionPreference = "Stop"

function Get-CurrentBranch {
    $branch = git rev-parse --abbrev-ref HEAD 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return $branch.Trim()
}

function Test-BranchExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BranchName
    )

    git show-ref --verify --quiet "refs/heads/$BranchName" 2>$null
    if ($LASTEXITCODE -eq 0) {
        return $true
    }

    git show-ref --verify --quiet "refs/remotes/origin/$BranchName" 2>$null
    return $LASTEXITCODE -eq 0
}

$currentBranch = Get-CurrentBranch
if ([string]::IsNullOrWhiteSpace($currentBranch) -or $currentBranch -eq "HEAD") {
    Write-Host "Skipping branch rename: current checkout is detached or unavailable."
    exit 0
}

# Cursor-created branches commonly follow feat-<n>-<id>.
# Only rename those auto-generated names to avoid touching explicit user branches.
$cursorAutoBranchPattern = "^feat-\d+-[A-Za-z0-9]+$"
if ($currentBranch -notmatch $cursorAutoBranchPattern) {
    Write-Host "Keeping existing branch '$currentBranch' (not a Cursor auto-generated name)."
    exit 0
}

$username = $env:USERNAME
if ([string]::IsNullOrWhiteSpace($username)) {
    $username = "user"
}
$username = $username.ToLowerInvariant()

$timestamp = Get-Date -Format "yyyyMMdd-HHmm"
$shortId = ($currentBranch -split "-")[-1].ToLowerInvariant()

$baseBranchName = "wt/$username/$timestamp-$shortId"
$targetBranchName = $baseBranchName
$suffix = 1

while (Test-BranchExists -BranchName $targetBranchName) {
    $suffix++
    $targetBranchName = "$baseBranchName-$suffix"
}

if ($targetBranchName -eq $currentBranch) {
    Write-Host "Branch already matches target format: '$currentBranch'."
    exit 0
}

git branch -m "$targetBranchName"
if ($LASTEXITCODE -ne 0) {
    throw "Failed to rename branch '$currentBranch' to '$targetBranchName'."
}

Write-Host "Renamed branch '$currentBranch' -> '$targetBranchName'."
