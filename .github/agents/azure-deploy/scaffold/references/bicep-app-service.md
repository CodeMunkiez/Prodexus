<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Bicep â€” App Service Patterns

App Service-specific Bicep patterns. For shared patterns (skeleton, naming, tags, security defaults, data modules), see [bicep-patterns.md](bicep-patterns.md).

> â›” **Azure Functions on Flex Consumption (the default Functions floor) is NOT an App Service â€” do NOT use this module for it.** Flex uses a different resource shape (`functionAppConfig`) and a different deploy channel; see [bicep-functions-flex.md](bicep-functions-flex.md). This module applies to App Service web apps and to Functions only on **Premium (EP1)**.

## Module Template

Standard App Service module with managed identity and SCM/FTP auth disabled. Use this as the base for ALL App Service resources.

```bicep
param location string
param tags object
param appServicePlanId string
param appServiceName string

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      // Only when this App Service is an API called by a browser frontend on a DIFFERENT hostname.
      // Omit entirely for a full-stack app that serves its own UI â€” that is same-origin and needs no CORS.
      // See bicep-patterns.md Â§ Cross-Origin (CORS).
      cors: {
        allowedOrigins: [ 'https://${staticWebApp.properties.defaultHostname}' ]
        supportCredentials: false
      }
    }
  }
}

// SCM basic auth â€” enabled in IaC for deploy convenience. Deploy phase re-disables via REST API after code upload.
resource scmAuth 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2023-12-01' = {
  parent: appService
  name: 'scm'
  properties: {
    allow: true
  }
}

// â›” MANDATORY â€” disable FTP basic auth
resource ftpAuth 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2023-12-01' = {
  parent: appService
  name: 'ftp'
  properties: {
    allow: false
  }
}

output appServiceId string = appService.id
output principalId string = appService.identity.principalId
```

> â›” **Every App Service module MUST include:** (1) `identity: { type: 'SystemAssigned' }`, (2) `scm` basicPublishingCredentialsPolicies with `allow: true` (IaC sets enabled for deploy convenience â€” deploy phase re-disables via REST API after code upload), (3) `ftp` basicPublishingCredentialsPolicies with `allow: false`. Missing any of these â†’ self-review L1 `FLAGGED`.

## Native Module Deploy Strategy

When `prepare-plan.json.deployStrategy` exists with `codeDeployPattern: "startup-install"`, apply these patterns to the App Service Bicep:

```bicep
resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  tags: tags
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'NODE|${nodeVersion}' // â›” Use exact version tag (18-lts, 20-lts) â€” NEVER tilde (~18). Tilde works for WEBSITE_NODE_DEFAULT_VERSION but NOT linuxFxVersion.
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      // Startup command: Oryx build is primary, this is the safety-net fallback.
      // Runs npm install only if node_modules doesn't exist (Oryx missed it).
      // MUST be inline â€” never a .sh file (Windows CRLF â†’ bash exit code 2).
      appCommandLine: '${deployStrategy.startupCommand}'
      appSettings: [
        // Primary: tell Oryx to run npm install during zip deploy
        { name: 'SCM_DO_BUILD_DURING_DEPLOYMENT', value: 'true' }
        { name: 'ENABLE_ORYX_BUILD', value: 'true' }
        // Extended timeout for native module compilation (max 1800, default 230)
        { name: 'WEBSITES_CONTAINER_START_TIME_LIMIT', value: '1800' }
        // App-specific settings
        { name: 'NODE_ENV', value: 'production' }
      ]
    }
  }
}
```

**Rules:**
- â›” **Inline `appCommandLine` only** â€” never generate a `.sh` startup script file. Files created on Windows have CRLF line endings â†’ bash exit code 2 on Linux
- â›” **Entry point from manifest** â€” read `package.json.scripts.start` or `.main`, never hardcode `index.js`
- â›” **`WEBSITES_CONTAINER_START_TIME_LIMIT` = 1800** (the maximum). Native compilation takes 2-5 min; Python with scipy can take longer
- When `deployStrategy` is absent (no native modules), do NOT set `appCommandLine` â€” let Oryx use its default startup
- â›” **Never prefix startup with `cd /home/site/wwwroot`** â€” Oryx extracts build output to a temp directory and sets the working directory automatically. Hardcoding `cd /home/site/wwwroot` causes `MODULE_NOT_FOUND` / `Could not import` because the app files aren't there

**Self-review check (L2 Pattern):** If `hasNativeModules == true`, verify Bicep has BOTH `appCommandLine` and `WEBSITES_CONTAINER_START_TIME_LIMIT`. If `prereq-output.json.initCommands[]` has `required: true` entries, verify `appCommandLine` includes them before the app start command. **FLAGGED** if either check fails.

## Identity Output â€” SystemAssigned vs UserAssigned

> â›” **`appService.identity.principalId` only exists for `SystemAssigned` identity.** When using `UserAssigned`, output the managed identity MODULE's `principalId` instead â€” `identity.principalId` is undefined and causes `DeploymentOutputEvaluationFailed`. Every App Service has a managed identity (mandatory â€” the compute floor is B1, which supports MI; F1/D1/Free are never selected).

