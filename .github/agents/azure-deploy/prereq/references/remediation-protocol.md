<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Remediation Protocol â€” Step 6

## Pre-check

> **ðŸ›‘ Hard Halt?** Skip remediation. Set `overallHealth: "blocked"`, write artifacts, go to Step 7.
> **ðŸ”¶ Major Migration?** If user chose "Attempt migration," enter loop. Warn changes may need review.

## Remediation Scope

â›” **Fix ONLY Azure-deployment blockers:** missing dependencies causing build/startup failure, missing entry point, startup crashes, Azure-required configuration (port binding, env var externalization, managed-DB client TLS/SSL â€” e.g. `W-MYSQL-SSL`). Everything else â†’ `postDeployRecommendations[]`.

## Remediation Loop

If any âŒ FAIL, ðŸ”§ Fix, or âš ï¸ WARN with `fixPhase: "prereq"` exist:
1. Present ALL together in one batch. Lead with: "Found {N} blockers, {M} fixes, {P} prereq-phase warnings. Fix all?"
   > â›” `fixPhase: "prereq"` is a remediation trigger regardless of severity. If left unfixed â†’ deploy-time failures. Warnings with `fixPhase: "scaffold"/"deploy-gate"/"post-deploy"` are NOT included.
2. After fixes, â›” **re-run full Step 3 evaluation (all 3 axes)** on affected components. Re-read each reference file and re-evaluate inline.
3. â›” **Verify fixes via static analysis only.** File exists â†’ exports match imports (grep) â†’ no syntax errors â†’ config values present. Do NOT run install/build/test commands.
4. â›” **Re-read instructions.md before applying fixes** if it hasn't been read this turn.
5. â›” **Print after re-evaluation:** `ðŸ”„ Re-evaluation complete â€” âœ… N issues resolved, âŒ M remaining.` If M > 0, loop back.
6. **Build-validation gate (agent-modified code only).** After re-evaluation passes AND agent modified >2 source files, ask via `ask_user`: "I've fixed {N} files. Want me to install, build, and test? (Yes / Skip)". â›” **General prior consent** (e.g., "fix my issues", "yes", "go ahead", "fix them") **does NOT constitute consent to run install/build/test.** The user must answer THIS specific question. If they say Skip, proceed to Step 7 without running commands. Max 3 build-fix attempts.
7. â›” **Max 3 cycles total** (re-evaluations + build-fix retries). Then STOP and ask: "Keep trying" (grants 3 more) / "Guide fixes" / "Stop â€” accept remaining issues".

## Post-Remediation Artifact Updates

â›” Re-evaluation = full 3-axis re-scan, NOT just reading fixed files. After ANY successful remediation, update all 3 artifacts per [prereq-artifacts.md](prereq-artifacts.md):
1. `context.json`: `readiness.status: "fixesApplied"`, updated verdicts, `fixes[]`, updated `statusSummary`
2. `prereq-output.json`: Rewrite completely via `create` tool. Recompute `overallHealth` from updated verdicts.
3. `readiness-report.md`: Append "Post-Fix Re-evaluation" section.

â›” Re-derive verdicts from findings â€” do NOT carry forward pre-fix values. Per axis: worst finding verdict wins. Then: any axis FAIL â†’ `"blocked"`, else any WARN â†’ `"readyWithCaveats"`, else `"ready"`.

## Write Final State (Step 7)

â›” Write components to BOTH `prereq-output.json.components[]` AND `context.json.components[]`. `context.json.components[]` is authoritative for downstream.

â›” **`fixesApplied` requires re-evaluation evidence** â€” verify `ðŸ”„ Re-evaluation complete` was printed.

Set `readiness.status` per component: All PASS â†’ `ready` | Fixes applied + re-eval passed â†’ `fixesApplied` | Unresolved FAILs â†’ `needsFixes`.

Update `context.json`: append `"prereq"` to `completedPhases`, set `currentPhase: null`, update `lastModifiedUtc`.

> â›” `overallHealth` MUST be one of: `"ready"`, `"readyWithCaveats"`, `"blocked"`.
> â›” `repo.lastScanCommit` is required â€” run `git rev-parse HEAD`.

