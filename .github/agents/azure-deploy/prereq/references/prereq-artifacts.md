<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

Artifact write procedures for the prereq phase exit. Read at Step 4 of the [readiness gate](readiness-gate.md).

## Write Artifacts

> â›” **Write all 3 artifacts before exiting prereq.** Downstream phases read these.
> â›” **Use `create` tool for ALL session files â€” NEVER `powershell`.** Terminal is OK ONLY for read-only ops: UUID generation, git info, file existence checks.
> â›” **Path scoping:** ALL `create` calls target `.copilot-azure/sessions/{uuid}/`. After writing, read back to confirm correct path.

1. **`prereq-output.json`** â€” â›” Read [`prereq-schemas.ts`](prereq-schemas.ts) for `PrereqOutput` interface.

   > â›” **Per-component verdicts MUST persist.** Every entry in `components[]` MUST include a `verdicts` object: `{ "build", "completeness", "deployability" }` with values `PASS`/`WARN`/`FAIL` (`build` may be `SKIPPED` when build validation was skipped; `completeness`/`deployability` are never `SKIPPED`). Downstream readiness scoring and the prepare phase read these â€” never omit them.
   >
   > â›” **Warnings MUST persist.** Every âš ï¸ WARN â†’ `warnings[]`: `{ "id": "W-{ID}", "component", "axis", "summary", "detail", "fix", "fixPhase" }`. Both `fix` and `fixPhase` are required â€” validate before writing.
   >
   > â›” **Health endpoint:** Write detected path to `healthEndpoint` (e.g., `"/api/v1/health/"`). If none â†’ `null` + `W-HEALTH` warning with `fixPhase: "scaffold"`.
   >
   > â›” **Entry point:** Write to `entryPoint` (e.g., `"index.js"`). `null` for .NET/Go/Java (Oryx handles startup).
   >
   > â›” **`postDeployRecommendations[]`:** For each âš ï¸ WARN, write per `PostDeployRecommendation` schema from [`session-schemas.ts`](session-schemas.ts): `{ "title", "reason", "effort": "low|medium|high", "services": [] }`.

2. **`context.json`** â€” â›” Use `edit` (not `create` â€” Step 1 already created it). Populate `components[]`, `repo`, `detectedInfra[]`, `detectedServices[]`, `app.name` (from primary component's project manifest or workspace root dir name). Append `"prereq"` to `completedPhases` NOW (before presenting), set `currentPhase: null`, update `lastModifiedUtc`.

3. **`readiness-report.md`** â€” Summary table (Build/Completeness/Deployability verdicts), detected stack, all warnings with actionable detail. Clean markdown, no rigid template.

> â›” **Phase exit gate:** All 3 artifacts must exist. If any missing, write NOW.

