#!/usr/bin/env bash
set -euo pipefail

# ── Configuration ──────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESULTS_DIR="$SCRIPT_DIR/docs/todo/code-audit-results"
LOG_FILE="$SCRIPT_DIR/docs/todo/audit-loop.log"
MAX_ITERATIONS=${MAX_ITERATIONS:-20}
BUILD_CMD="${BUILD_CMD:-dotnet build isma-ui-dotnet.slnx}"
OPENCODE_TIMEOUT=${OPENCODE_TIMEOUT:-300}  # seconds per opencode2 call
OPENCODE_RETRIES=${OPENCODE_RETRIES:-2}
OPENCODE_MODEL="my-own/qwen-3.6"

# ── Helpers ────────────────────────────────────────────────────────────────
timestamp() { date '+%Y-%m-%d %H:%M:%S'; }

log() {
  echo "[$(timestamp)] $*" | tee -a "$LOG_FILE"
}

count_findings() {
  find "$RESULTS_DIR" -name 'p*.md' -type f 2>/dev/null | wc -l
}

# Run opencode2 with timeout and retry
run_opencode() {
  local agent="$1"
  local prompt="$2"
  local output=""
  local attempt=0

  while [ "$attempt" -lt "$OPENCODE_RETRIES" ]; do
    attempt=$((attempt + 1))
    log "  opencode2 call (attempt $attempt/$OPENCODE_RETRIES)..."

    output=$(timeout "$OPENCODE_TIMEOUT" bash -c "cd '$SCRIPT_DIR' && opencode2 run --agent '$agent' --model '$OPENCODE_MODEL' '$prompt'" 2>&1) || {
      local exit_code=$?
      if [ "$exit_code" -eq 124 ]; then
        log "  TIMEOUT after ${OPENCODE_TIMEOUT}s"
      else
        log "  FAILED (exit code $exit_code)"
      fi
      if [ "$attempt" -lt "$OPENCODE_RETRIES" ]; then
        log "  Retrying..."
        continue
      fi
      echo "OPENCODE_ERROR: command failed after $OPENCODE_RETRIES attempts"
      return 1
    }

    # Check for opencode errors in output
    if echo "$output" | grep -q "OPENCODE_ERROR\|Error:"; then
      log "  Error detected in output, retrying..."
      if [ "$attempt" -lt "$OPENCODE_RETRIES" ]; then
        continue
      fi
    fi

    echo "$output"
    return 0
  done

  echo "OPENCODE_ERROR: all attempts failed"
  return 1
}

# Pick highest priority finding (P0 > P1 > P2 > P3 > P4)
pick_next_finding() {
  local finding=""
  # Priority 0-4: look for p0-, p1-, etc. in filenames
  for priority in p0 p1 p2 p3 p4; do
    finding=$(find "$RESULTS_DIR" -name "${priority}-*.md" -type f | sort | head -1)
    if [ -n "$finding" ]; then
      echo "$finding"
      return 0
    fi
  done
  # Fallback: any remaining .md file
  finding=$(find "$RESULTS_DIR" -name 'p*.md' -type f | sort | head -1)
  if [ -n "$finding" ]; then
    echo "$finding"
    return 0
  fi
  return 1
}

# ── Pre-flight ─────────────────────────────────────────────────────────────
mkdir -p "$RESULTS_DIR"

log "=== Audit Loop Started ==="
log "Results dir: $RESULTS_DIR"
log "Max iterations: $MAX_ITERATIONS"
log "Build command: $BUILD_CMD"
log "Opencode timeout: ${OPENCODE_TIMEOUT}s"
log "Opencode retries: $OPENCODE_RETRIES"

# Check if build currently works
log "Pre-flight build check..."
if ! $BUILD_CMD --verbosity quiet > /dev/null 2>&1; then
  log "ERROR: Project does NOT compile before starting loop."
  log "Fix build issues first, then re-run this script."
  exit 1
fi
log "Pre-flight build: OK"

# ── Main Loop ──────────────────────────────────────────────────────────────
iteration=0
while [ "$iteration" -lt "$MAX_ITERATIONS" ]; do
  iteration=$((iteration + 1))
  echo ""
  echo "═══════════════════════════════════════════════════"
  log "ITERATION $iteration / $MAX_ITERATIONS"
  echo "═══════════════════════════════════════════════════"

  # ── Phase 0: Audit (skip on first iteration if findings exist) ─────────
  # Only run audit if there are no findings to fix yet
  if [ "$(count_findings)" -eq 0 ]; then
    log "Phase 0: Running audit (no findings to fix)..."
    AUDIT_OUTPUT=$(run_opencode "auditor" "Run audit on blueprint editor. Scope: BlueprintEditorViewModel.cs ArrowLine.cs StateBox.cs LoopArrow.cs EditArrowPopOverViewModel.cs BlueprintEditorView.axaml.cs BlueprintToLismaConverter.cs NameChangingMonitor.cs BlueprintProjectViewModel.cs BlueprintModel.cs BlueprintTransactionModel.cs BlueprintLoopTransactionModel.cs BlueprintStateModel.cs BlueprintStateViewModel.cs BlueprintLoopTransactionViewModel.cs BlueprintTransitionViewModel.cs BlueprintEditorClipboardService.cs BlueprintValidationService.cs IBlueprintValidationService.cs. Write new findings to $RESULTS_DIR. Do NOT write findings that already exist. Read existing findings first to avoid duplicates. Name files as p<priority>-<short-name>.md where priority is 0=correctness, 1=architecture, 2=quality, 3=performance. Output 'NEW_FINDINGS: N' at the end.") || {
      log "Audit failed. Check log for details."
      exit 1
    }
    new_findings=$(echo "$AUDIT_OUTPUT" | grep -oP 'NEW_FINDINGS: \K[0-9]+' || echo "0")
    log "New findings written: $new_findings"

    if [ "$new_findings" -eq 0 ]; then
      log "CONVERGENCE: No findings and no new issues. Nothing to fix."
      break
    fi
  fi

  # ── Phase 1: Pick highest priority finding ─────────────────────────────
  next_finding=$(pick_next_finding) || {
    log "No findings to fix. Loop complete."
    break
  }

  finding_name=$(basename "$next_finding" .md)
  log "Next finding to fix: $finding_name"

  # ── Phase 2: Fix ───────────────────────────────────────────────────────
  log "Phase 2: Fixing $finding_name..."
  FIX_OUTPUT=$(run_opencode "fixer" "Fix the finding in $RESULTS_DIR/$finding_name.md. Read the finding file first, then apply the fix. After fixing, run '$BUILD_CMD' to verify the build. Commit the fix with a descriptive message. Output 'FIX_STATUS: OK|FAILED|BUILD_BROKE' and 'FIX_FILE: <path to fixed file>' and 'FIXED_FINDING: <finding-name>' at the end.") || {
    log "Fixer failed. Check log for details."
    continue
  }

  fix_status=$(echo "$FIX_OUTPUT" | grep -oP 'FIX_STATUS: \K[A-Z_]+' || echo "UNKNOWN")
  fix_file=$(echo "$FIX_OUTPUT" | grep -oP 'FIX_FILE: \K.+' || echo "unknown")
  fixed_finding=$(echo "$FIX_OUTPUT" | grep -oP 'FIXED_FINDING: \K.+' || echo "")

  log "Fix status: $fix_status"
  log "Fixed file: $fix_file"
  log "Fixed finding: $fixed_finding"

  if [ "$fix_status" = "BUILD_BROKE" ]; then
    log "ERROR: Fix broke the build!"
    log "Fix the compilation errors manually, then re-run this script."
    exit 1
  fi

  if [ "$fix_status" = "OK" ] && [ -n "$fixed_finding" ]; then
    finding_file="$RESULTS_DIR/$fixed_finding.md"
    if [ -f "$finding_file" ]; then
      rm "$finding_file"
      log "Removed finding file: $finding_file"
    fi
  fi

  if [ "$fix_status" = "FAILED" ]; then
    log "Fix failed. This finding may need manual intervention."
    log "Continuing to next finding..."
  fi

  # ── Phase 3: Build Verification ────────────────────────────────────────
  log "Verifying build..."
  if ! $BUILD_CMD --verbosity quiet > /dev/null 2>&1; then
    log "ERROR: Build failed after fix!"
    log "Run '$BUILD_CMD' manually to see errors."
    exit 1
  fi
  log "Build: OK"

  # ── Phase 4: Run Tests ─────────────────────────────────────────────────
  log "Running tests..."
  if ! dotnet test --verbosity quiet > /dev/null 2>&1; then
    log "WARNING: Some tests failed after fix."
    log "Run 'dotnet test' manually to investigate."
  else
    log "Tests: ALL PASS"
  fi

  log "Iteration $iteration complete."
done

# ── Summary ────────────────────────────────────────────────────────────────
echo ""
echo "═══════════════════════════════════════════════════"
log "=== Audit Loop Finished ==="
log "Iterations: $iteration"
log "Remaining findings: $(count_findings)"
echo "═══════════════════════════════════════════════════"
echo ""
echo "Log file: $LOG_FILE"
echo "Findings: $RESULTS_DIR/"
