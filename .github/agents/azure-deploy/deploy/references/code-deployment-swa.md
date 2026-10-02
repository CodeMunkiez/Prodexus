<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Code Deployment â€” Static Web Apps

## Static Web Apps â€” Content Deployment (Step 6c)

Deploy content using the SWA CLI with deployment token.

**Pre-check:** Verify `swa` CLI is installed: `npx --yes @azure/static-web-apps-cli --version`. If not available, install: `npm install -g @azure/static-web-apps-cli`.

> â›” **Pre-deploy: Build the frontend if SPA source (not pre-built HTML).** Check if the SWA component directory has a `package.json` (or equivalent manifest) with a `build` script. If yes, detect the package manager from the lockfile (`package-lock.json` â†’ npm, `yarn.lock` â†’ yarn, `pnpm-lock.yaml` â†’ pnpm, `bun.lock`/`bun.lockb` â†’ bun; default to npm if no lockfile) and run:
> ```powershell
> cd {component-path}   # e.g., web/
> {pm} install           # npm install, yarn install, pnpm install, etc.
> {pm} run build         # npm run build, yarn build, pnpm build, etc.
> ```
> The deploy path should point to the **build output directory** â€” read the framework config to determine the output dir (`vite.config.*` â†’ `build.outDir`, `next.config.*` â†’ `.next/` or `out/`, CRA â†’ `build/`; default to `dist/`). If no `package.json` exists (plain HTML/CSS/JS), deploy the source directory directly â€” no build needed.
>
> â›” **This is NOT optional for SPA frameworks.** Raw JSX/TSX/Vue/Svelte source files cannot be served by SWA â€” the app must be compiled to static HTML/JS/CSS first. Skipping the build produces a broken deployment.

> â›” **Pre-deploy: Update frontend config with deployed backend URLs.** If `prereq-output.json.cloudSdkSwaps[]` mapped cloud SDK endpoints (AWS API Gateway, GCP Cloud Functions) to Azure equivalents, the frontend config file (`config.ts`, `.env`, `environment.ts`) still references the original cloud URLs. After IaC deploy (Step 6), read `deploy-result.json.endpoints[]` to get the deployed Azure backend URLs. Update the frontend config with these URLs before running `swa deploy`. This is a string replacement in the config file â€” NOT a code rewrite. Do NOT defer as a post-deploy step â€” the SWA will show a broken page if the frontend calls non-existent AWS/GCP endpoints.

| Step | Command | Notes |
|------|---------|-------|
| 1. Get token | `$token = az staticwebapp secrets list --name {swa} -g {rg} --query "properties.apiKey" -o tsv 2>$null` | Store in variable first â€” do NOT pass inline. â›” **On Windows, use `2>$null`** (not `2>&1`) â€” Azure CLI Python warnings corrupt `-o tsv` output |
| 2. Set env var | `$env:SWA_CLI_DEPLOYMENT_TOKEN = $token` | â›” Use env var ONLY â€” do NOT pass `--deployment-token $token` as CLI arg (leaks token in process args / transcript) |
| 3. Copy to temp | `$tempDir = "C:\temp\swa-deploy"; Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Path $tempDir -Force \| Out-Null; Copy-Item -Path .\* -Destination $tempDir -Recurse -Exclude @('.git','.copilot-azure','node_modules','.azure','infra')` | â›” **MANDATORY on Windows** â€” `$env:TEMP` often contains spaces (e.g., `C:\Users\Jane Doe\AppData\Local\Temp`). Always use a short, space-free path on the FIRST attempt â€” do NOT use `$env:TEMP` and retry. On macOS/Linux, use `/tmp/swa-deploy` instead. **If a build step ran:** copy the build output directory (e.g., `{component}/dist/`) instead of the entire workspace â€” `Copy-Item -Path {component}\dist\* -Destination $tempDir -Recurse`. |
| 4. Deploy | `swa deploy $tempDir --app-name {swaName} --env production` | â›” **`--app-name` is MANDATORY** â€” without it, the SWA CLI launches an interactive "create new project?" prompt that fails in automation. `{swaName}` = the SWA resource name from `prepare-plan.json.naming.resources[]`. SWA CLI reads `SWA_CLI_DEPLOYMENT_TOKEN` from env var automatically â€” do NOT pass `--deployment-token` flag (leaks token in command args) |
| 5. Clean up | `Remove-Item -Recurse -Force $tempDir` | Clean temp dir after successful deploy |

â›” **`az staticwebapp deploy` does NOT exist** â€” the correct CLI is `swa deploy` (from `@azure/static-web-apps-cli`).

## Fallback: Direct StaticSitesClient Upload

If `swa deploy` fails (binary crash, path errors on Windows), use the underlying `StaticSitesClient.exe` directly:

```powershell
# Find the binary bundled with @azure/static-web-apps-cli
$swaCliPath = (Get-Command swa).Source | Split-Path -Parent
$client = Get-ChildItem -Path $swaCliPath -Recurse -Filter "StaticSitesClient*" | Select-Object -First 1

# Upload from PARENT directory with relative paths (avoids "identical to artifact folder" error)
Push-Location (Split-Path $tempDir -Parent)
& $client.FullName upload --app (Split-Path $tempDir -Leaf) --apiToken $token --skipAppBuild true
Pop-Location
```

> â›” **Run from a PARENT directory** with a relative `--app` path. Running from inside the app directory causes `StaticSitesClient` to error with "Current directory cannot be identical to or contained within artifact folders."

## Finalize deploy-result.json (after `swa deploy` succeeds)

â›” **`swa deploy` succeeding is NOT the end of the deploy phase.** SWA finalizes through the **generic Step 8** (see [`deploy-checklist-template.md`](deploy-checklist-template.md) Â§"Before handoff (Step 8)") â€” overwrite the `deploy-result.json` skeleton IN PLACE with the full `DeployResult` contract, not a `status`+`subscriptionId` stub. Only the values Step 8 can't derive on the SWA path are below:

- **Hostname** â€” `swa deploy` produces no ARM endpoint output, so fetch it: `$swaHost = az staticwebapp show -n {swa} -g {rg} --query defaultHostname -o tsv`
- `endpoints[]` â€” `[{ name, url: "https://$swaHost", healthStatus: "healthy" }]` (HTTP GET `https://$swaHost/` â†’ 2xx confirms healthy)
- `resourceIds[]` â€” include the `Microsoft.Web/staticSites` resource id

