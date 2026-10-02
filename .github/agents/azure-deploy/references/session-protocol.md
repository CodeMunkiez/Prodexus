<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Session Protocol â€” Step 1

## All Prompts Are Actionable

> â›” **ALL prompts that activate this agent are actionable â€” go directly to Step 1.** Do NOT answer the user's question, give an overview of capabilities, or describe what AppOnboard can do before starting the pipeline. "Can Azure figure out my app?" and "Deploy my app" are the same action: Step 1 â†’ Step 2 â†’ scan. The user's phrasing (question vs command) does NOT change the workflow.

## Session Check

Resolve active session via pointer file.

> â›” **YOU MUST CREATE A SESSION BEFORE DOING ANY WORK â€” INCLUDING SCANNING**
>
> 1. **STOP** â€” Do not answer the user's question, scan code, or plan architecture yet
> 2. **CHECK** â€” Read `.copilot-azure/sessions/active-session.json`.
>    - â›” **First, ensure the repo's `.gitignore` contains `.copilot-azure/`** (append if missing, create the file if absent) â€” this runs on EVERY path below, BEFORE any branch writes a session file, since session artifacts may hold deploy secrets.
>    - **Pointer exists** â†’ â›” **You MUST read [`session-schemas.ts`](session-schemas.ts)** to get the exact field names and types for `AppOnboardContext`. Do not guess field names. Then read the pointed-to session's `context.json`. Display: "Found session from [lastModifiedUtc] â€” {statusSummary}." â›” **You MUST ask the user via `ask_user`: "Resume this session or start fresh?" Do NOT auto-resume.** This gate is mandatory â€” stale sessions from prior tests cause the agent to skip phase instructions.md reads and miss artifact writes.
>      - Resume â†’ â›” **Refresh the deployment inventory baseline before any more provisioning.** Preserve the current `deploy-result.json.createdResources[]` and `orphanedResourceGroups[]`, then call `capture_deployment_inventory` with `phase: "baseline"`, using `context.json.sessionId` and `context.json.azure.subscriptionId`. If the subscription is missing or auth must be refreshed, complete the Azure login gate below first, but do not run `az deployment`, `az group create`, `az webapp deploy`, `az acr build`, or any other command that can create resources until the baseline call succeeds. This new baseline starts a resumed inventory segment: union later capture output with the preserved inventory by normalized resource ID and case-insensitive resource-group name; never discard cleanup evidence from before the resume. Then â›” **read the phase instructions.md for the NEXT phase** (derive from `completedPhases`). E.g., prereq done â†’ read `prepare/instructions.md`, then continue from that phase.
>        - â›” **If `context.json.routeToSkill` is set:** The previous session was halted for migration (e.g., `azure-cloud-migrate`). The code has likely changed since then. **Do NOT skip prereq** â€” start fresh: clear `routeToSkill`, `routeReason`, remove `"prereq"` from `completedPhases`, and re-run from Step 2. This ensures the migrated codebase gets a clean 3-axis evaluation.
>        - â›” **If `completedPhases` includes `"prereq"` (and no `routeToSkill`):** Prereq already wrote `prereq-output.json` and `context.json.components[]`. Proceed to Step 2 (scope triage) â€” prereq may have been invoked standalone, so the user still needs to confirm the full pipeline. Skip Step 3 (prereq invocation), then continue to Step 4 (scan-informed intent gathering).
>      - Start fresh â†’ generate a new UUID via `[guid]::NewGuid().ToString()`, create a new session folder, update `active-session.json` to point to the new session. Old session folder is never touched again.
>    - **Pointer missing but session folders exist** â†’ list folders under `.copilot-azure/sessions/`. If 1 folder: adopt it (read its `context.json`, write `active-session.json` pointing to it, show summary). If 2+: show a numbered list with `statusSummary` + `lastModifiedUtc` from each, ask user to pick one or start fresh. Write pointer for the chosen session.
>    - **No sessions at all** â†’ generate a UUID by running `[guid]::NewGuid().ToString()` in the terminal. â›” **You MUST generate the UUID via a terminal command â€” do NOT hardcode a placeholder like `a1b2c3d4-e5f6-7890-abcd-ef1234567890`.** Create the session directory: `New-Item -ItemType Directory -Path ".copilot-azure/sessions/{uuid}" -Force`. Then write a **minimal** `context.json` using the `create` tool â€” only these 3 fields are known immediately: `{ "sessionId": "{uuid}", "createdUtc": "{ISO 8601 now}", "intent": { "userPrompt": "{user's first message verbatim}" } }`. Write `active-session.json` with `activeSessionId: {uuid}` using the `create` tool.
> 3. **PRUNE** â€” After resolving the active session, check remaining session folders. Delete any where `context.json.lastModifiedUtc` is >7 days ago. **Never delete the active session** (the one `active-session.json` points to).
> 4. **VERIFY** â€” Confirm `context.json` exists and is valid JSON. If missing or malformed, halt and retry creation â€” do NOT continue to Step 2 without a verified session.
> 5. **CONFIRM** â€” Begin your first response with: "Started session at `.copilot-azure/sessions/{uuid}/`" (new) or "Resuming session from [date] â€” {statusSummary}" (existing)
> 6. **THEN** proceed to Step 2
>
> â›” **Ordering: session FIRST, scanning SECOND.** If you scan the workspace or read project files before writing `context.json`, you have violated the session-first rule. The session must exist before ANY code analysis.
>
> â›” **Shell fallback:** If PowerShell/terminal hangs on first attempt (no output after 10s), use the `create` tool directly for session directory and file writes. Do NOT retry shell commands more than once.
>
> â›” **Path scoping: ALL `create` tool calls for session artifacts MUST target `.copilot-azure/sessions/{active-session-id}/`.** Writing to any other session folder is forbidden.

## CLI Availability

Call `mcp_azure_mcp_extension_cli_install` with `cli-type: "az"` to verify Azure CLI is available. If missing, surface installation instructions before proceeding. Downstream phases (prepare, deploy) require it. Fallback: skip if MCP tool unavailable.

## Azure Login Gate

**Azure login gate (mandatory):** Run `az account show --query "{id:id, name:name, tenantId:tenantId}" -o json` with a **15-second timeout** (PowerShell: `Start-Process` with `-Wait` or inline timeout; if command hangs beyond 15s, treat as failure). Also run `az ad signed-in-user show --query displayName -o tsv` (15-second timeout). After BOTH commands complete, merge ALL azure fields into `context.json.azure` in a **SINGLE update** â€” `subscriptionId`, `subscriptionName`, `tenantId`, and `userDisplayName`. Do NOT write separate updates for subscription and identity.

> â›” **If `az account show` fails or hangs:** â›” **You MUST read [`subscription-resolution.md`](subscription-resolution.md)** and follow its fallback procedure. Do NOT proceed to Step 2 without a resolved subscription. Do NOT leave `context.json.azure` empty and continue. Every downstream phase (prepare, scaffold validation, deploy) requires Azure auth â€” proceeding without it produces incomplete results.

## User Identity Detection

**User identity detection (for `deployed-by` tag):** Run `az ad signed-in-user show --query displayName -o tsv` (15-second timeout) alongside `az account show`. Fallback if `az ad` fails: use `az account show --query user.name -o tsv` (returns UPN/email). If both fail, leave empty â€” prepare phase will resolve. This value becomes the `deployed-by` tag on ALL resources â€” resolving it once here prevents inconsistent tag values across resources. **Merge into the SAME `context.json` update as the azure login gate â€” do NOT write separately.**

## Subscription Detection Method

> â›” **`az account show` is the ONLY subscription detection method in Step 1.** Do NOT call `mcp_azure_mcp_subscription_list` here â€” that tool returns ALL subscriptions across ALL tenants and causes a lengthy picker detour. `az account show` returns the CLI's active subscription in <1 second. MCP subscription list is reserved for prepare Step 1 when the user explicitly wants a different subscription.

## Artifact Locations

| Location | Artifacts |
|----------|-----------|
| `.copilot-azure/sessions/{uuid}/` | `context.json`, `prereq-output.json`, `prepare-plan.json`, `scaffold-manifest.json`, `deploy-result.json` |
| `.copilot-azure/sessions/{uuid}/replaced-files/` | User files displaced by scaffold (existing IaC), stored at their original relative path (**mirror path** = same directory structure as the repo). Never overwritten or deleted â€” moved here so the original is preserved. |

## Phase-gated Reference Loading

> â›” **Phase-gated reference loading.** Do NOT pre-read reference files for downstream phases. Read each phase's references only when entering that step. Scaffold references (bicep-patterns, self-review) are irrelevant during deploy; prepare references (service-mapping, pricing-guide) are irrelevant during scaffold. Each phase instructions.md specifies its own required reads.

