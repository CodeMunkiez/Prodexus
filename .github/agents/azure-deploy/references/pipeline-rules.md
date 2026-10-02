<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Pipeline Rules â€” Reference

Cross-cutting rules enforced across all workflow steps. Referenced from [instructions.md](../instructions.md) `## Pipeline Rules`.

## Approval gates

â›” **Two separate approval gates are required â€” never merge them.**

1. **Scaffold gate (Step 6):** "âœ… Ready to proceed with scaffolding? (Yes / Edit plan / Cancel)" â€” approves IaC generation only. â›” When the plan includes PostgreSQL/MySQL, add `Private access` as a selectable choice â€” see approval-gates.md for the exact variant.
2. **Deploy gate (Step 8):** "ðŸš€ Ready to deploy? (Yes / Run manually / Edit plan / Cancel)" â€” approves resource provisioning.

The scaffold gate does NOT grant deploy permission. After scaffold completes, you MUST present the deploy gate as a SEPARATE response. Never go from scaffold approval directly to `az group create` or `az deployment`.

â›” **BOTH gates MUST show Subscription (name + ID), Resource Group, and Region** as standalone lines above the service table â€” users must see WHERE resources will be created before approving.

â›” **NEVER create, write, or modify infrastructure files before the user explicitly says "Yes" to the scaffold gate.** No exceptions â€” not for simple apps, trivial plans, free-tier deployments, or single-component repos.

â›” **Modifying existing IaC files (not AppOnboard-generated) requires explicit user approval.** Present: "I need to modify {file}: {change description}. Approve? (Yes / Edit / Cancel)". This applies to repos with existing Bicep/Terraform where AppOnboard adjusts SKUs, regions, or settings.

Each gate is the LAST content in its response â€” do NOT continue past a gate in the same turn.

> âŒ BAD: Writes Bicep without approval Â· scaffolds and deploys in same response Â· skips deploy gate after scaffold approval
> âœ… GOOD: Shows plan â†’ user says Yes â†’ scaffold â†’ show validation summary â†’ deploy gate â†’ user says Yes â†’ deploy

## Phase lifecycle

Update `context.json` at phase boundaries â€” combine `completedPhases` update with `currentPhase` for the next phase in a single write. Write the phase artifact before marking complete. The orchestrator instructions.md specifies exact write points (after prereq, after scaffold, after deploy).

`currentPhase` must NEVER appear in `completedPhases` â€” if invariant violated, halt and report.

`context.json` is NOT write-once â€” each phase boundary MUST update it on completion:
- Write `intent` after Step 2
- `components` after Step 3
- `azure.resourceGroup` after Step 7 (also written to `deploy-result.json.resourceGroupName`)
- Push to `completedPhases` at phase boundaries
- Update `statusSummary` at every phase exit â€” 1-line description. Templates:
  - prereq: `"{N} components, stack: {detectedStack}, health: {overallHealth}"`
  - prepare: `"{N} services, ~${monthlyUsd}/mo, region: {region}"` (if `quotaValidation.checkedRegions` >1, append fallback reason: `"region: westus2 (eastus quota full)"`)
  - scaffold: `"{N} files, self-review: {VERIFIED|FLAGGED count}"`
  - deploy: `"{healthStatus}, RG: {resourceGroupName}"`
  - cancel: `"Paused at {phase} â€” {reason}"`

## Session artifacts

**Session file writes: `New-Item -ItemType Directory` for directories, `create` tool for file content.** Create session directory via `New-Item -ItemType Directory -Path ".copilot-azure/sessions/{uuid}" -Force`, then `create` tool for all JSON/md content. Never use `Out-File`, `Set-Content`, or shell commands for file content.

## Phase transition rule

> â›” **Before executing the FIRST command of any new phase, re-read that phase instructions.md.** After prereq â†’ read `prepare/instructions.md`. After prepare â†’ `scaffold/instructions.md`. After scaffold gate â†’ `deploy/instructions.md`. This applies at EVERY transition.

## Post-compaction recovery

> â›” After ANY compaction, re-read current phase instructions.md + this file. Check `scaffold-manifest.json` and `completedPhases` exist if mid-scaffold/deploy.

Begin responses with: "Started session at `.copilot-azure/sessions/{uuid}/`" or "Resuming session from [date] â€” {statusSummary}".

â›” **Session immutability:** NEVER write to any session folder other than the active session (the one `.copilot-azure/sessions/active-session.json` points to). Old sessions are read-only â€” no updates, no backfills, no status changes.

**Session TTL:** 7 days. Non-active sessions where `context.json.lastModifiedUtc` is >7 days ago are deleted on next invocation. The active session is never pruned.

## fastTrackEligible

Set by prereq: (1) auto-approves readiness gate, (2) simplifies prepare Step 3 alternatives. Does NOT skip phases, reads, gates, self-review, validation, or preflight.

## Deploy as-is

â›” Do NOT refactor or upgrade working application code. Deploy what works. Fixing broken code IS allowed (build errors, missing deps) through the approval gate. Upgrade suggestions â†’ `prepare-plan.json.postDeployRecommendations[]`. Infrastructure changes = allowed; code rewrites = forbidden; Azure compatibility changes (TLS, SSL, port) = allowed when detected by prereq AND approved. Never prompt for secrets â€” databases/caches/storage use managed identity (no passwords exist); auto-generate only app-internal secrets (e.g. `SECRET_KEY`) and store them on the compute resource (App Service app settings / Container Apps native secrets) via an `@secure()` param â€” no Key Vault.

## Known Platform Bugs

See [`pipeline-rules-runtime.md`](pipeline-rules-runtime.md) Â§ Known Platform Bugs for the full bug table and workarounds.

## No top-level agent invocation

â›” **NEVER call external agents** (`azure-validate`, `azure-deploy`, `azure-prepare`, etc.) during the AppOnboard pipeline. Only `azure-app-onboard-prereq` and `azure-app-onboard` orchestrator are allowed. Use direct CLI commands for validation and deployment.

## Structured sub-agent delegation

â›” Use ONLY `subagent-*.md` templates â€” no ad-hoc prompts. Pass template content verbatim. Destructive commands (`az deployment`, `az webapp deploy`, `az acr build`) execute in main thread only.

## Security baseline

See [iac-generation-rules.md](../scaffold/references/iac-generation-rules.md) Â§ Security Patterns and [bicep-patterns-security.md](../scaffold/references/bicep-patterns-security.md). Flag `AllowAzureServices` firewall rule as a security warning.

## azure.yaml prohibition

â›” **NEVER generate `azure.yaml`. NEVER use `azd up`/`azd provision`/`azd deploy`.** AppOnboard deploys via `az deployment sub create` (Bicep) or `terraform apply` (Terraform). Repos with existing `azure.yaml` â†’ route per [`azd-template-routing.md`](azd-template-routing.md).


