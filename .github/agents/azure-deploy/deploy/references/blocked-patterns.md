<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Blocked Patterns

Commands the agent must NEVER execute. Block decisions are non-negotiable â€” user must run blocked commands manually outside AppOnboard.

| Pattern | Action | Reason |
|---------|--------|--------|
| `rm -rf` (any path outside a fresh temp dir) | â›” Block | Prevents accidental deletion of IaC, app code, or session artifacts â€” especially `infra/`, `.azure/`, `.copilot-azure/`. |
| `git reset --hard`, `git checkout -- <path>`, `git restore`, `git clean` | â›” Block | Discards uncommitted work. During region-fallback healing the agent edits Bicep/app config; these wipe the user's unstaged changes irrecoverably. |
| `git push --force` / `--force-with-lease` (any branch) | â›” Block | Prevents force-push of generated code over remote history. |
| `--no-verify` (on `git commit` / `git push`) | â›” Block | Bypasses hooks (secret-scan, lint) that guard the commit. |
| `DROP TABLE` / `DROP DATABASE` | â›” Block | Prevents data loss |
| `terraform destroy` | â›” Block | Prevents accidental teardown (user must run manually) |
| `az group delete` | â›” HARD BLOCK | **NEVER delete resource groups yourself.** During healing: if switching regions/RGs, add the old RG to your `orphanedResourceGroups[]` list (per `OrphanResourceGroup` in [`deploy-schemas.ts`](deploy-schemas.ts)) instead of deleting it. At handoff: emit `az group delete` commands in the handoff message for the USER to run â€” the agent never executes them. If you are about to type `az group delete` into a terminal command, STOP â€” you are violating this rule. Track it in `orphanedResourceGroups[]` instead. |
| `az containerapp up --source` / `az containerapp create` | â›” Block | Creates ACR + CA Environment + Log Analytics imperatively â€” orphan resources invisible to `terraform destroy`, `az deployment sub delete`, and session tag-based bulk cleanup. State drift from IaC is unrecoverable. The Container App MUST be created via Bicep `az deployment sub create` â€” for code deploy on an existing CA use `az containerapp update --source` (Step 6d) |
| `az appservice plan update` | â›” Block | Imperative SKU change â€” edit Bicep + redeploy |
| `az webapp update` | â›” Block | Imperative resource modification â€” all changes via IaC |
| `az functionapp update` | â›” Block | Imperative resource modification â€” all changes via IaC |
| `az webapp deployment source config-zip` | â›” Block | Requires SCM basic auth â€” use `az webapp deploy` (Entra auth). â›” **This block is the `webapp` command only.** `az functionapp deployment source config-zip` is the **ALLOWED** deploy channel for Flex Consumption function apps (blob-container package, not SCM basic auth) â€” see [`code-deployment-functions-flex.md`](code-deployment-functions-flex.md). |
| `az webapp deploy --track-status` | â›” Block | `--track-status` flag does not exist. Remove it. |
| `az webapp up` / `az webapp create` / `az appservice plan create` | â›” Block | Creates App Service Plan + App imperatively â€” bypasses IaC entirely |
| `az containerapp update` (config changes) | â›” Block | Imperative resource modification â€” all changes via IaC |
| `az containerapp update --revision-suffix` (no config changes) | âš ï¸ ALLOWED | KV secret rotation only â€” when KV secrets were updated post-deploy and a new revision is needed to pick up cached values |
| `az webapp delete` | â›” Block | Imperative resource deletion â€” destroys resources outside IaC |
| `az appservice plan delete` | â›” Block | Imperative plan deletion â€” remove from Bicep + redeploy instead |
| `az containerapp update --image` | â›” Block (during healing) | Imperative image swap causes IaC drift â€” update Bicep + redeploy |
| Inline secret values in CLI args | â›” Block | `--parameters password=MyP@ss$word!` breaks shell escaping and leaks secrets in terminal history. Pass app-internal secrets via `main.parameters.json`, `terraform.tfvars`, or an `@secure()` param from a variable reloaded from `deploy-secrets.env` (no Key Vault). |
| Writing secrets to temp files on disk | â›” Block | â›” NEVER write rendered secret values to arbitrary temp files. App-internal secrets are passed as `@secure()` params and stored on the compute resource (App Service app settings / CA native secrets); the only persisted copy is the git-ignored `deploy-secrets.env` reload cache under `.copilot-azure/`. Temp files risk exposure in crash dumps, logs, and unprotected storage. |
| `az group create` (during healing) | â›” HARD BLOCK â€” **one sanctioned exception** | **NEVER create resource groups imperatively during healing.** All RG creation must go through `az deployment sub create` with Bicep `targetScope = 'subscription'`. If you need a new RG for region fallback, update the Bicep region parameter and redeploy. **Sole exception:** the documented 403 scope-fallback in [`deploy-safety.md`](deploy-safety.md) Â§ 403 Scope Fallback â€” when `az deployment sub create` returns 403, rescoping Bicep to RG-scope requires `az group create` (with all 5 AppOnboard tags). That path is allowed. |
| `az rest --method put/patch` (for individual resource creation) | â›” HARD BLOCK | **NEVER create individual Azure resources via REST API as a fallback for Bicep failures.** After a deployment failure, the ONLY allowed remediation is: fix the Bicep parameters/template â†’ re-run `az deployment sub create`. Compiling Bicepâ†’ARM and deploying via REST is still imperative resource creation. |
| Disabling a security control to unblock â€” `require_secure_transport`/TLS â†’ OFF, HTTPS-only off, KV purge protection off, auth off (via `az ... parameter set` OR editing the Bicep) | â›” HARD BLOCK | **NEVER weaken a security control to make a failing deploy pass.** A DB TLS handshake failure means the *client* lacks SSL config â€” fix the client (prereq `W-MYSQL-SSL`/`W-PG-SSL`) or surface the tradeoff to the user. Downgrading the server control is forbidden. |
| `Compress-Archive -Path $files.FullName` | â›” Block | Absolute paths flatten directory structure â€” app crashes on `./src/app` not found. Use `System.IO.Compression.ZipFile` with relative paths from workspace root. On Windows, normalize: `$entryName = $relativePath.Replace('\', '/')`. |

> **Repos with existing `azure.yaml`:** See [`pipeline-rules.md`](../../references/pipeline-rules.md) Â§ azure.yaml prohibition. Deploy via `az deployment sub create` â€” do NOT run `azd up`.

