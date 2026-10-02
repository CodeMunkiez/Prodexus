---
name: azure-app-onboard-prereq
description: "Assess whether source code is ready to deploy to Azure â€” the check BEFORE infrastructure work. Evaluates build health, app completeness, dependencies and local services, stack compatibility, and deployment feasibility. Answers questions about what your app needs before it can be deployed â€” frameworks, dependencies, and configuration. Checks whether dependencies are compatible and identifies deployment blockers and unsupported frameworks. WHEN: \"evaluate my repo\", \"is my app ready to deploy\", \"what does my app need to deploy\", \"what do I need before deploying\", \"does my app need\", \"can I ship this to Azure\", \"scan my repo for issues\", \"is this app deployable\", \"check if my app is ready for Azure\", \"do I need a Dockerfile\", \"what's blocking my deployment\", \"are there any blockers\", \"are my dependencies compatible\", \"does Azure support my framework\", \"what needs to change before deploying\", \"check my app configuration\"."
license: MIT
metadata:
  author: Microsoft
  version: "1.2.1"
---

<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.


# Azure App Onboard Prereq â€” Repository Evaluation

Evaluate a user's repository for build health, app completeness, and Azure deployment feasibility â€” before infrastructure planning. Produces per-component verdicts (PASS/WARN/FAIL) consumed by downstream phases.

> **Orchestrator relationship:** Called by the deploy agent's [`instructions.md`](../instructions.md) at Step 3. Return control to `instructions.md` (continue at Step 4) after writing artifacts â€” do NOT invoke downstream phases directly.

Phase 1 of 4 in AppOnboard pipeline. Session: `.copilot-azure/sessions/{session-id}/`. Reads `context.json`. Writes `components[]`, `repo{}`, `detectedInfra[]`. Produces `prereq-output.json`. Schema: [`prereq-schemas.ts`](references/prereq-schemas.ts) â€” `PrereqOutput`, `BuildRequirements`. Direct entry supported.

## When NOT to Use

| Signal | Redirect |
|--------|----------|
| Validate infrastructure (Bicep/TF/azure.yaml) | **azure-validate** |
| Generate IaC | **azure-prepare** |
| End-to-end idea-to-production | **azure-app-onboard** |
| Run `azd up` or deploy | **azure-deploy** |

## Rules

> â›” **ABSOLUTE PROHIBITION â€” `npm install`, `npm test`, `npx jest`, `pytest`, and ALL install/build/test commands are NEVER allowed.**
> Under NO circumstances may you run `npm install`, `npm test`, `npx jest`, `pip install`, `pytest`, `dotnet build`, `dotnet restore`, `dotnet test`, `go mod download`, `cargo build`, or ANY package-manager install, build, or test command during the prereq phase. Do NOT run test suites to verify code â€” check for test config files statically instead. The prereq phase is read-only evaluation + static-only verification.
> **ONLY exception â€” two sanctioned contexts, both consent-gated:** (a) code the agent **modified** during migration/remediation (see [remediation-protocol.md](references/remediation-protocol.md) step 6), or (b) code the agent **wrote** from scratch on the zero-code path (see [zero-code-path.md](references/zero-code-path.md)). In either case, install/build/test runs ONLY via the user-confirmed build-validation gate ([build-check.md](references/build-check.md) Step 3), after the user answers that specific per-command consent prompt. General prior consent never counts.

1. â›” **Full pipeline (Steps 1â€“8), no exceptions.** All prompts â†’ Step 1 directly. Answer specific questions AS PART OF findings (Step 5), not before.
2. â›” **No sub-agents for evaluation.** 3-axis evaluation is inline. **Exception**: zero-code-path scaffolding (Step 2).
3. Code/destructive modifications require `ask_user`. Max 3 questions before results. Direct entry: don't repeat orchestrator's intent questions.

## MCP Tools

| Tool | Purpose |
|------|---------|
| `mcp_azure_mcp_get_azure_bestpractices` | Validate detected stack patterns against Azure best practices |
| `mcp_azure_mcp_extension_cli_install` | Check/install required CLI tools (az, azd, func) |

## Workflow

### Step 1: Session Check

**Orchestrator entry:** Session exists â€” read `context.json`, proceed to Step 2.

**Direct entry:** Check `.copilot-azure/sessions/active-session.json`:
- **Exists** â†’ â›” read [session-protocol.md](references/session-protocol.md) for resume/fresh gate. Do NOT proceed until user answers.
- **Missing** â†’ create session: generate UUID, `New-Item -ItemType Directory -Path ".copilot-azure/sessions/{uuid}" -Force`, write `context.json` + `active-session.json` via `create` tool.

Then: `az account show` â†’ merge `{id, name, tenantId}` into `context.json.azure`. â›” Session MUST exist on disk before any scanning.

### Step 2: Scan Workspace

Scan for project files. Detect components, `repo{}`, `detectedInfra[]`, `detectedServices[]`. Classify Terraform providers. Check CLI availability. Stack detection conflicts: user explicit statement wins (write to `context.json`, mark scan as override); scan-only â†’ confirm with user; multiple stacks â†’ show all and ask (see [component-mapping.md](references/component-mapping.md)); no code â†’ [zero-code-path.md](references/zero-code-path.md).

> â›” **Preserve Azure Functions service roots.** A directory with `host.json` plus an Azure Functions SDK or
> worker signal (`@azure/functions`, `Microsoft.NET.Sdk.Functions`, Functions Python decorators, or a
> `FUNCTIONS_WORKER_RUNTIME` setting) is an Azure Functions component. Record its exact path and identify its
> framework as Azure Functions in both `context.json.components[]` and `prereq-output.json.components[]`.
> Do not classify HTTP-triggered Functions as a generic REST API. When a SPA and Functions root both exist,
> record two components even if the frontend calls the backend through `/api`.

> If no project files, no Dockerfile, AND no index.html â†’ â›” read [zero-code-path.md](references/zero-code-path.md).

> â›” **Cloud SDK early gate.** Grep for `aws-sdk|@aws-sdk|boto3|google-cloud|@google-cloud|firebase`. If functional deps found â†’ read [cloud-sdk-migration.md](references/cloud-sdk-migration.md), then `ask_user`: **"Redirect to Azure Cloud Migrate"** (set `routeToSkill: "azure-cloud-migrate"`) Â· **"Continue evaluation anyway"** (finish readiness eval + SDKâ†’Azure mapping, then STOP at Step 8 â€” no plan until the deps are swapped) Â· **"Cancel"**.

### Step 3: Per-Component Evaluation

| Sub-step | Action | Reference |
|----------|--------|-----------|
| 3.1 | **Build check** | â›” **You MUST read [build-check.md](references/build-check.md)** |
| 3.2 | **Completeness check** | â›” **You MUST read [completeness-check.md](references/completeness-check.md)** |
| 3.3 | **Deployability check** | â›” **You MUST read [deployability-check.md](references/deployability-check.md)** |
| 3.3a | **Component mapping** (conditional) | Read [component-mapping.md](references/component-mapping.md) ONLY IF >1 project manifest found (monorepo) |

Populate `buildRequirements` per component after evaluation. Verdict propagation, tier rules, and f1Viable aggregation are in [readiness-gate.md](references/readiness-gate.md) and the individual check references.

### Step 4: Write Artifacts + Readiness Gate

â›” Verify `context.json` exists on disk. Read [readiness-gate.md](references/readiness-gate.md) (verdicts, tiers, batch-then-approve, fast-track) then [prereq-artifacts.md](references/prereq-artifacts.md) (write procedures, schemas).

### Step 5: Present Findings

Per [readiness-gate.md Â§ Present Findings](references/readiness-gate.md) â€” show verdicts grouped by severity before proceeding.

### Step 6: Remediation (conditional)

â›” **You MUST read [remediation-protocol.md](references/remediation-protocol.md)** IF any âŒ FAIL verdict, ðŸ”§ Recommended Fix, or âš ï¸ WARN with `fixPhase: "prereq"` exists. Contains remediation loop, static verification, re-eval mandate, post-remediation artifact updates, and the build-validation consent gate. If all verdicts are âœ… PASS or âš ï¸ WARN without `fixPhase: "prereq"`, skip to Step 7.

### Step 7: Write Final State

`completedPhases` already has `"prereq"` + `currentPhase: null` (from Step 4). Then:

> â›” **Write `lastScanCommit`.** Run `git rev-parse HEAD` and store the full 40-character SHA as `context.json.repo.lastScanCommit`. Required â€” staleness guard in Step 1 compares to HEAD on resume to detect changes.

### Step 8: Route

â›” **Mandatory â€” do NOT skip this step.**

> **Routing fields:** All routing writes `routeToSkill` and `routeReason` to `context.json`.

> **Post-remediation context:** If Step 6 ran, lead the routing prompt with: "Remediation complete â€” {N} issues fixed, your app is now {overallHealth}."

> â›” **Evaluate rows top to bottom â€” first match wins.**

| # | Condition | Action |
|---|-----------|--------|
| 1 | `routeToSkill` set (any entry) | `ask_user`: "Redirect to {routeToSkill}" / "Not now". â›” Pipeline stops â€” do NOT proceed to architecture planning. |
| 2 | `cloudSdkFindings[]` non-empty (user chose "Continue evaluation anyway") | Present the cloud-SDK â†’ Azure swap mapping as ðŸ”¶ blockers, then `ask_user` with this exact prompt: **"ðŸ”¶ Cloud SDK migration required â€” these dependencies must be swapped before this app can deploy to Azure. (Redirect to azure-cloud-migrate / Stop â€” swap manually and re-run)"** â€” Redirect sets `routeToSkill: "azure-cloud-migrate"`, Stop halts. â›” Pipeline stops â€” do NOT proceed to architecture planning, and do NOT offer a "continue to prepare" option; the app can't deploy until the deps are swapped. |
| 3 | Orchestrator + no `routeToSkill` | Tell the user: "âœ… Your app has been evaluated and is ready â€” let's plan your Azure deployment." Then return to the deploy agent's [`instructions.md`](../instructions.md) and continue at Step 4 (Gather intent). â›” Do NOT stop, do NOT wait for user input, do NOT narrate internal handoffs. The user already consented to the full pipeline at scope triage. |
| 4 | Direct + ready/readyWithCaveats + no Azure infra | `ask_user`: "Deploy to Azure (full pipeline)" â†’ invoke `azure-app-onboard` / "Not now" |
| 5 | Direct + ready/readyWithCaveats + existing Azure infra | `ask_user`: "Start fresh" â†’ invoke `azure-app-onboard` / "Use existing infra" â†’ invoke `azure-prepare` / "Not now" |
| 6 | Direct + blocked | Report blocker summary + "Fix and re-run." |

Severity tiers (ðŸ›‘ðŸ”¶âŒðŸ”§âš ï¸âœ…) are defined in [readiness-gate.md](references/readiness-gate.md).

## Outputs

| Artifact | Location | Consumer |
|----------|----------|----------|
| Session context | `context.json` â†’ `components[]`, `repo{}`, `detectedInfra[]`, `detectedServices[]` | All downstream phases |
| Prereq output | `prereq-output.json` | prepare phase (via `azure-app-onboard`) |
| Readiness report | `.copilot-azure/sessions/{uuid}/readiness-report.md` | User (offline reference) |
