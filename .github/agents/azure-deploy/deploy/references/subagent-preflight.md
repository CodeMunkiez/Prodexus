<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Subagent Template â€” Deploy Preflight & Checklist Generation

Read deploy reference files and distill deployment-specific rules into `deploy-checklist.md`. This is the main agent's ONLY source of deploy rules.

## Critical Rules

- â›” Do NOT invoke any agents, run `az` commands, or modify IaC/app code. Read-only sub-agent.
- â›” Do NOT read `deploy-schemas.ts` or `error-classification.md` â€” main agent reads those on-demand.

## Input (provided by caller)

| Field | Source |
|-------|--------|
| `prepare-plan.json` content | services[], naming, region, costEstimate, deploymentVariables |
| `scaffold-manifest.json` content | deployCommand, validationResult, files[] |
| `context.json` content | azure.subscriptionId, subscriptionName, resourceGroup, sessionId, intent |
| `prereq-output.json` content | buildRequirements, warnings[], components[] |
| Session folder path + working directory | Required |

## Output

| Artifact | Location |
|----------|----------|
| `deploy-checklist.md` | Session folder |
| `deploy-result.json` skeleton (if missing) | Session folder |
| Summary (â‰¤300 tokens) | Return to caller |

## Workflow

### Step 1 â€” Read session artifacts + write skeleton

Read all 4 session artifacts. Extract: services[], naming, costEstimate, deployCommand, validationResult, deploymentVariables, subscriptionId, sessionId, buildRequirements, warnings[], quotaValidation.

If `deploy-result.json` missing, write skeleton with these EXACT field names (do NOT rename): `{ sessionId, subscriptionId, resourceGroupName, deploymentNames: ["app-onboard-deploy-{first 8 of sessionId}"], status: "in-progress", startedUtc, resourceIds: [], endpoints: [], healthStatus: "unknown", resourceResults: [], healingAttempts: [] }`.

### Step 2 â€” Read safety + blocked patterns refs

Read [deploy-safety.md](deploy-safety.md) and [blocked-patterns.md](blocked-patterns.md). Bake into checklist sections: deploy-result.json rules, blocked commands table, shell rules (sync shells, secrets persistence, no --track-status, az rest headers on Windows), 403 scope fallback (4-step procedure with real values), post-deploy tag verification, deployment operation polling (conditional: >5 resources), re-approval gates, antipatterns, artifact reconciliation.

### Step 3 â€” Read preflight + approval gate refs

Read [preflight-checks.md](preflight-checks.md) and [approval-gate-template.md](approval-gate-template.md).

Bake preflight into checklist: auth token check, resource name availability (real names), RBAC scope pre-check, â›” MANDATORY what-if command (pre-filled with real deploymentName, region, subscriptionId), RG existence check, offer restriction check (conditional: if `offerRestrictionsVerified` false AND DB services in plan).

Bake approval gate VERBATIM with real values: subscription, RG, region, service table, cost table, validation status, files list, response handlers with exact CLI commands. If F1/D1 detected, append warning.

### Step 4 â€” Read code-deployment + health refs

Read ONLY the code-deployment ref(s) matching the compute types in `prepare-plan.json.services[]`. **Do NOT read** references for compute types absent from the plan:
- If `App Service` or `Functions` in plan â†’ read [code-deployment-appservice.md](code-deployment-appservice.md)
- If `Container Apps` in plan â†’ read [code-deployment-container-apps.md](code-deployment-container-apps.md)
- If `Static Web Apps` in plan â†’ read [code-deployment-swa.md](code-deployment-swa.md)

Bake per-service-type `## Code deploy` sections (one per compute service â€” do NOT merge).

Read [health-check-patterns.md](health-check-patterns.md). Bake health check section: HTTP checks (timeout 30s, 3 retries), status interpretation, Azure default page detection strings, non-HTTP resource checks, functional verification (conditional).

### Step 5 â€” Read conditional refs + handoff + write checklist

**Database (conditional):** If DB services in plan â†’ read [database-post-deploy.md](database-post-deploy.md). Bake migration discovery, execution commands, PG-specific checks.

Read [../../references/handoff-protocol.md](../../references/handoff-protocol.md). Bake cleanup commands (with real rg, sessionId), orphan listing, healing summary, post-deploy recommendations, agent-based next steps, auth-aware handoff.

**Write `deploy-checklist.md`** to session folder. If it already exists, replace all content via `edit`; if not, use `create`:
```
# Deploy Checklist for {appName}
# RG: {rgName} | Sub: {subscriptionId} | Session: {sessionId}
# âš ï¸ If compaction recently occurred: re-read deploy/instructions.md Steps 6-8
```

Delete inapplicable sections (e.g., remove App Service section for Container Apps deploys).

> â›” **Copy `## Before handoff (Step 8)` and `## Artifact verification (Step 8 â€” MANDATORY)` from the template VERBATIM.** Do NOT paraphrase, merge, or weaken `â›”` markers. These sections are the compaction-safe finalization anchor â€” diluting them causes artifact writes to be skipped after compaction.

### Step 6 â€” Return summary

Return â‰¤300 tokens: preflight warnings, deploy command, service types, confirmation of checklist write, database migration note if applicable.

