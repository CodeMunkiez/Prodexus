<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Readiness Gate â€” Step 4

## Write Artifacts

â›” **You MUST read [`prereq-artifacts.md`](prereq-artifacts.md)** for complete artifact write procedures and phase exit checklist.

---

## Severity Tiers

| Verdict | Icon | Meaning |
|---------|------|---------|
| Hard Halt | ðŸ›‘ | App is intentionally vulnerable â€” pipeline stops, no fix possible |
| Major Migration | ðŸ”¶ | Large-scope change (EOL runtime, cloud SDK migration, >5 files) â€” redirect or warn |
| Critical | âŒ FAIL | Deployment will fail â€” agent can fix (â‰¤5 files, config-level) |
| Recommended Fix | ðŸ”§ | App deploys but has quality/security issues â€” agent offers fix |
| Warning | âš ï¸ WARN | Informational, non-blocking â€” can proceed with caveats |
| Pass | âœ… PASS | No issues |

---

## Overall Health Gate

**Compute `overallHealth`:** ALL âœ… PASS â†’ `"ready"` | Any âš ï¸ WARN no âŒ FAIL â†’ `"readyWithCaveats"` | ANY âŒ FAIL â†’ `"blocked"`

**Component `readiness.status` alignment:**
- `"ready"` â†’ `readiness.status: "ready"`
- `"readyWithCaveats"` â†’ `readiness.status: "ready"` (WARNs aren't "needs fixes")
- `"blocked"` â†’ `readiness.status: "needsFixes"`
- After remediation â†’ `readiness.status: "fixesApplied"`

â›” `readiness.status: "needsFixes"` requires at least one âŒ FAIL. If all âš ï¸/âœ…, use `"ready"`.

---

## Critical Readiness Gate

â›” **Verdict propagation cross-check** before computing `overallHealth`:
1. Any finding with `verdict: "FAIL"` â†’ axis verdict MUST be `"FAIL"`.
2. Any finding with `verdict: "WARN"` + `fixPhase: "prereq"` â†’ escalate to `"FAIL"` (prevents wasting a deploy cycle). â›” Escalate only WARNs that would actually break THIS deploy (build/startup failure, or a health probe wired to a route the app lacks). Issues that deploy and run fine â€” missing trust proxy, README, in-memory sessions â€” stay `fixPhase: "postdeploy"`/`"scaffold"`; `engines`/health-endpoint escalate only on a real version/probe mismatch (see [completeness-check.md](completeness-check.md) Â§ Stack-Specific Checks).

| Tiers | Reference file |
|-------|---------------|
| ðŸ›‘ ðŸ”¶ ðŸ”§ âš ï¸ | [`dependency-compatibility.md`](dependency-compatibility.md) |
| âŒ ðŸ”§ | [`completeness-check.md`](completeness-check.md) |
| âŒ | [`build-check.md`](build-check.md) |

**Post-evaluation HALT cross-check:** Intentionally vulnerable apps (â‰¥2 code signals from dependency-compatibility.md) â†’ `overallHealth: "blocked"` MUST be written to `prereq-output.json` on disk (Step 4) BEFORE any halt message. If the artifact is not on disk when you reach the halt, write it NOW via the `create` tool and read it back â€” do NOT present the halt until it exists.

---

## Batch-Then-Approve Flow

â›” **Artifacts before message.** Write AND read back all 3 artifacts (`prereq-output.json`, `context.json`, `readiness-report.md`) to confirm they exist on disk BEFORE presenting any findings, cloud-SDK stop prompt, or ðŸ›‘ hard-halt message. Those messages can end the turn, so every artifact MUST already be persisted â€” NEVER batch artifact writes after the message.

1. **Detect ALL issues first** â€” full 3-axis scan, all components.
2. **Present ALL findings at once** â€” summary: "ðŸ” Readiness: 2 critical, 1 recommended fix, 3 warnings". Group: ðŸ›‘ â†’ ðŸ”¶ â†’ âŒ â†’ ðŸ”§ â†’ âš ï¸.
3. **Fix plan** â€” for âŒ, ðŸ”§, ðŸ”¶, âš ï¸ with `fixPhase: "prereq"`: describe WHAT and WHY. â›” Exclude ðŸ”¶ with `routeToSkill` set. Never include ðŸ›‘.
4. **User choice** (based on highest severity):
   - **ðŸ›‘:** Pipeline stops. No formal gate.
   - **ðŸ”¶ + others:** "Fix {N} issues including {M} migration(s) â€” scope warning" / "Fix blockers only" / "Continue with risks" / "Cancel"
   - **ðŸ”¶ only:** "Attempt migration" / "Continue as-is" / "Cancel"
   - **âŒ/ðŸ”§/âš ï¸ with fixPhase prereq:** "Fix {N} deployment issues" / "Continue with risks" / "Cancel"
5. **After approval** â†’ apply fixes per [remediation-protocol.md](remediation-protocol.md).

> â›” **Two-gate rule:** Intent approval â‰  fix execution approval. Present the fix prompt here even if user agreed earlier.

---

## Fast-Track

Single-component + no DB + no auth + **no Dockerfile** â†’ `fastTrackEligible: true`.

---

## Present Findings (Step 5)

â›” Do NOT skip â€” user must see scan results before pipeline continues.

**Part 1 â€” Summary:** "ðŸ” Readiness: {N} critical, {M} fixes, {K} warnings" (or "âœ… Ready").
**Part 2 â€” Per-axis reasoning:** Verdict icon + 1â€“2 sentence summary per axis.
**Part 3 â€” Findings table:** Grouped by severity. Include warning ID and actionable detail.

**Data-loss warnings** (SQLite, in-memory sessions, local file storage) require explicit acknowledgment. Other âš ï¸ are informational.

End with: "ðŸ“„ Full evaluation saved to `readiness-report.md`."

### Remediation Decision Gate

â›” **STOP after presenting findings.** Options:
1. **"Fix deployment issues"** â€” fix all actionable items (âŒ, ðŸ”§, âš ï¸ with `fixPhase: "prereq"`). Re-evaluate after.
2. **"I have context â€” let me guide the fixes"**
3. **"Continue without fixing â€” I accept the risks"**

Wait for explicit choice. Generic "Yes"/"Go ahead" â‰  remediation consent â€” clarify if ambiguous.

> â›” **Remediation budget:** Max 3 cycles. See [remediation-protocol.md](remediation-protocol.md) step 7.

> â›” **ARTIFACT CHECKPOINT.** After presenting findings, verify all 3 artifacts exist: `context.json`, `prereq-output.json`, `readiness-report.md`. Write any missing ones NOW.

