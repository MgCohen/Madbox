#!/usr/bin/env bash
# Reads tasks from a file and runs a tight iteration loop per task:
#   code agent -> optional input -> deterministic eval -> git agent OR fix agent -> repeat until pass or max iterations.
#
# Requires Cursor CLI (`agent`) on PATH unless AGENT_RUNNER is set (see below).
#
# Usage:
#   EVAL_SCRIPT=/path/to/eval.sh ./run-agentic-task-loop.sh [options] TASKS_FILE
#
# Options:
#   -w DIR       Repository root (default: cwd)
#   -e SCRIPT    Evaluation script (required for real runs unless AGENTIC_LOOP_SKIP_EVAL=1)
#   -m N         Max iterations per task (default: 10)
#   -i           Prompt for extra input on stdin each iteration (interactive)
#
# Environment:
#   CURSOR_AGENT_BIN     Agent executable (default: agent)
#   AGENT_RUNNER         If set, invoked instead of agent for every agent step. Receives env:
#                        AGENT_ROLE=(code|git|fix), AGENT_PROMPT, REPO_ROOT, TASK_BODY, ITERATION
#   CODE_AGENT_ARGS      Extra args passed to agent for the code step (e.g. --mode=plan)
#   GIT_AGENT_ARGS       Extra args for git step
#   FIX_AGENT_ARGS       Extra args for fix step
#   GET_INPUT_CMD        Optional shell snippet; stdout appended as user input (runs each iter after code agent)
#   AGENTIC_LOOP_STATE_DIR  Where run logs are written (default: REPO_ROOT/.agents/agentic-task-loop)
#   AGENTIC_LOOP_SKIP_EVAL If 1 and no -e, eval is treated as pass (for dry runs only)
#
# Evaluation contract (EVAL_SCRIPT):
#   Exit 0  -> task iteration succeeded; move to next task
#   Exit 1  -> run fix agent with failure context
#   Exit 2  -> run git agent (e.g. commit/push flow)
#   Other   -> abort task with error
#
# Eval script environment:
#   TASK_BODY, ITERATION, LAST_CODE_OUTPUT, LAST_AGENT_ROLE, REPO_ROOT

set -euo pipefail

WORKDIR="$(pwd)"
EVAL_SCRIPT=""
MAX_ITERS=10
INTERACTIVE_INPUT=0
TASKS_FILE=""

usage() {
  sed -n '1,40p' "$0" | sed -n '/^# /s/^# //p'
  echo
  echo "Usage: $(basename "$0") [-w DIR] [-e EVAL_SCRIPT] [-m N] [-i] TASKS_FILE"
}

while getopts ":w:e:m:ih" opt; do
  case "$opt" in
    w) WORKDIR="$(cd "$OPTARG" && pwd)" ;;
    e) EVAL_SCRIPT="$OPTARG" ;;
    m) MAX_ITERS="$OPTARG" ;;
    i) INTERACTIVE_INPUT=1 ;;
    h) usage; exit 0 ;;
    :) echo "Option -$OPTARG requires an argument." >&2; exit 2 ;;
    \?) echo "Invalid option: -$OPTARG" >&2; usage >&2; exit 2 ;;
  esac
done
shift $((OPTIND - 1)) || true

if [[ $# -lt 1 ]]; then
  echo "Error: TASKS_FILE is required." >&2
  usage >&2
  exit 2
fi

TASKS_FILE="$1"
if [[ ! -f "$TASKS_FILE" ]]; then
  echo "Error: tasks file not found: $TASKS_FILE" >&2
  exit 2
fi

if [[ -z "$EVAL_SCRIPT" && "${AGENTIC_LOOP_SKIP_EVAL:-0}" != "1" ]]; then
  echo "Error: set -e EVAL_SCRIPT or AGENTIC_LOOP_SKIP_EVAL=1 for dry runs." >&2
  exit 2
fi

if [[ -n "$EVAL_SCRIPT" && ! -f "$EVAL_SCRIPT" ]]; then
  echo "Error: EVAL_SCRIPT not found: $EVAL_SCRIPT" >&2
  exit 2
fi

STATE_DIR="${AGENTIC_LOOP_STATE_DIR:-$WORKDIR/.agents/agentic-task-loop}"
mkdir -p "$STATE_DIR"

CURSOR_AGENT_BIN="${CURSOR_AGENT_BIN:-agent}"

run_agent_role() {
  local role="$1"
  local prompt="$2"
  local extra_args=()
  case "$role" in
    code) read -r -a _extra <<< "${CODE_AGENT_ARGS:-}"; extra_args=("${_extra[@]:-}") ;;
    git)  read -r -a _extra <<< "${GIT_AGENT_ARGS:-}";  extra_args=("${_extra[@]:-}") ;;
    fix)  read -r -a _extra <<< "${FIX_AGENT_ARGS:-}";  extra_args=("${_extra[@]:-}") ;;
  esac

  if [[ -n "${AGENT_RUNNER:-}" ]]; then
    AGENT_ROLE="$role" AGENT_PROMPT="$prompt" REPO_ROOT="$WORKDIR" TASK_BODY="$TASK_BODY" ITERATION="$ITERATION" \
      eval "$AGENT_RUNNER"
    return
  fi

  if ! command -v "$CURSOR_AGENT_BIN" >/dev/null 2>&1; then
    echo "Error: '$CURSOR_AGENT_BIN' not on PATH. Install Cursor CLI or set AGENT_RUNNER." >&2
    return 127
  fi

  # Non-interactive agent run (see Cursor CLI docs: -p / --print).
  "$CURSOR_AGENT_BIN" -p --output-format text "${extra_args[@]}" "$prompt"
}

read_tasks() {
  local file="$1"
  awk '
    BEGIN { RS=""; ORS="\0" }
    {
      gsub(/^\n+|\n+$/, "", $0)
      if ($0 ~ /^[[:space:]]*$/) next
      out=""
      n = split($0, lines, /\n/)
      for (i = 1; i <= n; i++) {
        line = lines[i]
        if (line ~ /^[[:space:]]*#/) continue
        if (line ~ /^[[:space:]]*$/) continue
        if (out != "") out = out "\n"
        out = out line
      }
      if (out != "") print out
    }
  ' "$file" | while IFS= read -r -d '' block || [[ -n "${block:-}" ]]; do
    printf '%s\n' "$block"
  done
}

TASK_INDEX=0
SUMMARY=()

while IFS= read -r TASK_BODY || [[ -n "${TASK_BODY:-}" ]]; do
  [[ -z "${TASK_BODY// }" ]] && continue
  TASK_INDEX=$((TASK_INDEX + 1))
  RUN_ID="task${TASK_INDEX}_$(date +%Y%m%d_%H%M%S)"
  RUN_DIR="$STATE_DIR/$RUN_ID"
  mkdir -p "$RUN_DIR"

  echo "=== Task $TASK_INDEX ==="
  printf '%s\n' "$TASK_BODY" | tee "$RUN_DIR/task.txt" >/dev/null
  export TASK_INDEX

  context=""
  last_code_output=""
  done_task=0
  aborted_task=0

  for ((ITERATION = 1; ITERATION <= MAX_ITERS; ITERATION++)); do
    echo "--- Iteration $ITERATION ---"

    code_prompt="You are working in repository: $WORKDIR

Task:
$TASK_BODY

Additional context from prior iterations:
$context

Implement or advance this task. Follow project AGENTS.md and architecture rules."

    LAST_AGENT_ROLE="code"
    set +e
    last_code_output="$(run_agent_role code "$code_prompt" 2>&1)"
    code_status=$?
    set -e
    printf '%s\n' "$last_code_output" > "$RUN_DIR/iter_${ITERATION}_code.txt"
    if [[ $code_status -ne 0 ]]; then
      SUMMARY+=("Task $TASK_INDEX: code agent failed (exit $code_status). Logs: $RUN_DIR")
      echo "Code agent exited with $code_status"
      aborted_task=1
      break
    fi

    extra_input=""
    if [[ -n "${GET_INPUT_CMD:-}" ]]; then
      extra_input="$(cd "$WORKDIR" && TASK_BODY="$TASK_BODY" ITERATION="$ITERATION" LAST_CODE_OUTPUT="$last_code_output" eval "$GET_INPUT_CMD")" || true
    elif [[ "$INTERACTIVE_INPUT" == "1" ]]; then
      read -r -p "Additional input (empty to skip): " extra_input || true
    fi
    if [[ -n "$extra_input" ]]; then
      printf '%s\n' "$extra_input" > "$RUN_DIR/iter_${ITERATION}_input.txt"
      context+=$'\n'"User input: $extra_input"
    fi

    if [[ -z "$EVAL_SCRIPT" ]]; then
      SUMMARY+=("Task $TASK_INDEX: completed (eval skipped). Run: $RUN_DIR")
      done_task=1
      break
    fi

    export TASK_BODY TASK_INDEX ITERATION LAST_CODE_OUTPUT="$last_code_output" LAST_AGENT_ROLE REPO_ROOT="$WORKDIR"
    set +e
    (cd "$WORKDIR" && bash "$EVAL_SCRIPT") >"$RUN_DIR/iter_${ITERATION}_eval.stdout" 2>"$RUN_DIR/iter_${ITERATION}_eval.stderr"
    eval_code=$?
    set -e

    if [[ $eval_code -eq 0 ]]; then
      echo "Eval passed."
      SUMMARY+=("Task $TASK_INDEX: OK after $ITERATION iteration(s). Run: $RUN_DIR")
      done_task=1
      break
    fi

    if [[ $eval_code -ne 1 && $eval_code -ne 2 ]]; then
      SUMMARY+=("Task $TASK_INDEX: eval aborted with exit $eval_code. Run: $RUN_DIR")
      echo "Eval script exited with unexpected code $eval_code"
      aborted_task=1
      break
    fi

    eval_tail="$(tail -n 20 "$RUN_DIR/iter_${ITERATION}_eval.stderr" 2>/dev/null || true)"
    [[ -z "$eval_tail" ]] && eval_tail="$(tail -n 20 "$RUN_DIR/iter_${ITERATION}_eval.stdout" 2>/dev/null || true)"

    if [[ $eval_code -eq 2 ]]; then
      LAST_AGENT_ROLE="git"
      git_prompt="Repository: $WORKDIR

Task:
$TASK_BODY

The automated check indicated git operations are needed (eval exit 2). Stage, commit with a clear English message, and push if that is appropriate for this repo. Recent eval output:
$eval_tail"

      set +e
      git_out="$(run_agent_role git "$git_prompt" 2>&1)"
      git_status=$?
      set -e
      printf '%s\n' "$git_out" > "$RUN_DIR/iter_${ITERATION}_git.txt"
      context+=$'\n'"Git agent (exit $git_status): ${git_out:0:2000}"
    else
      LAST_AGENT_ROLE="fix"
      fix_prompt="Repository: $WORKDIR

Task:
$TASK_BODY

Deterministic evaluation failed with exit code $eval_code. Fix the issues. Recent eval output:
$eval_tail

Prior code agent output (truncated):
${last_code_output:0:4000}"

      set +e
      fix_out="$(run_agent_role fix "$fix_prompt" 2>&1)"
      fix_status=$?
      set -e
      printf '%s\n' "$fix_out" > "$RUN_DIR/iter_${ITERATION}_fix.txt"
      context+=$'\n'"Fix agent (exit $fix_status): ${fix_out:0:2000}"
    fi
  done

  if [[ $done_task -ne 1 && $aborted_task -ne 1 ]]; then
    SUMMARY+=("Task $TASK_INDEX: stopped after $MAX_ITERS iterations (no eval pass). Run: $RUN_DIR")
  fi
done < <(read_tasks "$TASKS_FILE")

echo
echo "========== Summary =========="
if [[ "${#SUMMARY[@]}" -gt 0 ]]; then
  for line in "${SUMMARY[@]}"; do
    echo "$line"
  done
else
  echo "(no tasks processed)"
fi
echo "Done."
