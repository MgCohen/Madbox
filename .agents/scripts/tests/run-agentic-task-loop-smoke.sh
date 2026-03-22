#!/usr/bin/env bash
# Smoke test for run-agentic-task-loop.sh (no Cursor CLI required).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOOP="$ROOT/run-agentic-task-loop.sh"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

cat >"$TMP/tasks.txt" <<'EOF'
First task body
line two

Second task
EOF

cat >"$TMP/eval.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
COUNTER_FILE="${EVAL_COUNTER_FILE:?}"
if [[ ! -f "$COUNTER_FILE" ]]; then
  echo 0 >"$COUNTER_FILE"
fi
n="$(cat "$COUNTER_FILE")"
n=$((n + 1))
echo "$n" >"$COUNTER_FILE"
if [[ "$n" -lt 2 ]]; then
  exit 1
fi
exit 0
EOF
chmod +x "$TMP/eval.sh"

export EVAL_COUNTER_FILE="$TMP/counter"
export AGENT_RUNNER='printf "mock:%s\n" "$AGENT_ROLE"'

out="$("$LOOP" -w "$TMP" -e "$TMP/eval.sh" -m 5 "$TMP/tasks.txt" 2>&1)" || true

echo "$out" | grep -q "Task 1: OK after 2 iteration" || {
  echo "Expected task 1 to pass on iteration 2. Output:" >&2
  echo "$out" >&2
  exit 1
}

echo "$out" | grep -q "Task 2: OK after 1 iteration" || {
  echo "Expected task 2 to pass on iteration 1. Output:" >&2
  echo "$out" >&2
  exit 1
}

echo "run-agentic-task-loop-smoke: OK"
