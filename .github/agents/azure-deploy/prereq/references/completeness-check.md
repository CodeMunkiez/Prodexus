<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Completeness Check

> â›” **No build/install/test commands during this check.** Use static analysis only.

Verify repository has required components for a deployable app.

### 1. Entry Point

Verify main/index file exists for each component.

| Stack | Expected Entry Point |
|-------|---------------------|
| Node.js | `main` or `start` script in `package.json` |
| Python | `app.py`, `main.py`, `manage.py`, or entry in `pyproject.toml` |
| .NET | `Program.cs` or `Startup.cs` with `<OutputType>Exe</OutputType>` |
| Java | Class with `public static void main` or `@SpringBootApplication` |
| Go | `main.go` in `package main` |
| Static | `index.html` at root or in output folder |

| Outcome | Verdict |
|---------|---------|
| Entry point found and file exists on disk | âœ… PASS |
| Ambiguous (multiple candidates) | âš ï¸ WARN |
| No entry point | âŒ FAIL |
| Entry point declared but file missing | âŒ FAIL â€” `MODULE_NOT_FOUND` |

### 2. Dependency Manifest

| Outcome | Verdict |
|---------|---------|
| Manifest found with dependencies | âœ… PASS |
| Manifest exists but empty deps | âš ï¸ WARN |
| No manifest found | âŒ FAIL (unless static site) |

> â›” **Oryx reads manifests ONLY at repo root.** Subdirectory manifests â†’ âš ï¸ WARN (ðŸ”§ Fix): create root wrapper (Python: `-r {subdir}/requirements.txt`, Node: workspaces). Add to batch-then-approve.

### 3. Configuration

| Outcome | Verdict |
|---------|---------|
| Config properly externalized | âœ… PASS |
| Hardcoded values but no secrets | âš ï¸ WARN |
| Hardcoded secrets in source (no env var fallback) | âŒ FAIL |
| Env var + hardcoded fallback default | âš ï¸ WARN |
| `.env` with placeholder values + runtime validation | âœ… PASS |

> **Compose file credentials:** Literal `*PASSWORD=<value>` with NO `${VAR}` â†’ âŒ FAIL. `${VAR:-default}` â†’ âš ï¸ WARN. `${VAR}` only â†’ âœ… PASS. â›” Do NOT downgrade based on filename ("dev-only") â€” treat every compose file as potentially production-bound.

### 4. Documentation

README with build/run instructions â†’ âœ… PASS. Sparse/no README â†’ âš ï¸ WARN.

### 5. Listening Port

Web apps must bind a port. Detect via `app.listen`, `PORT` env var, framework port config, `WebApplication.CreateBuilder()` (implicit 5000/5001 .NET 6+).

| Outcome | Verdict |
|---------|---------|
| Port binding detected | âœ… PASS |
| Non-web (CLI, worker, function) | âœ… PASS â€” N/A |
| Web app with no port binding | âŒ FAIL |

### 6. Static Asset Integrity

Parse `href`/`src` from HTML tags. Check relative to HTML file directory. Ignore external URLs.

| Outcome | Verdict |
|---------|---------|
| All referenced assets found | âœ… PASS |
| Broken favicon only | âš ï¸ WARN |
| Broken `<link>`, `<script>`, `<img>` reference | ðŸ”§ Recommended Fix â€” set `fixPhase: "prereq"` |

### 7. Container Readiness

| Outcome | Verdict |
|---------|---------|
| Dockerfile + .dockerignore present | âœ… PASS |
| Dockerfile, no .dockerignore | âš ï¸ WARN |
| Multi-process container detected | âš ï¸ WARN |
| `CMD.*uv run` or `CMD.*poetry run` | âš ï¸ WARN â€” dep sync at startup fails as non-root. **fix:** "Replace with direct command" **fixPhase:** `prereq` |
| `EXPOSE` port mismatch with app | ðŸ”§ Fix â€” mismatch causes 502. Extract to `buildRequirements.exposedPort` |

### Stack-Specific Checks

Verify these patterns. Assess severity with tier definitions from [readiness-gate.md](readiness-gate.md) â€” only âŒ FAIL if causes deploy failure or startup crash.

- **Node.js:** `engines` field, session store type (MemoryStore = ephemeral), health endpoint
- **Express:** trust proxy when secure cookies are used behind reverse proxy
- **Any web app:** health endpoint (`/health`, `/healthz`), README documentation
- **Static sites:** health endpoint is N/A (responds 200 on `/`)

> â›” Default these to **âš ï¸ WARN / `fixPhase: "postdeploy"`** â€” an app that deploys and runs (missing trust proxy, README, in-memory sessions) is **not** `blocked`. Escalate to âŒ FAIL / `prereq` only when the case actually breaks THIS deploy: `engines` when the app needs a runtime the platform default won't provide, or a health endpoint when a probe is wired to a route the app lacks.

> **Do not short-circuit.** Iterate ALL sub-checks (1â€“7 + stack-specific) per component.

Severity tiers are defined in [readiness-gate.md](readiness-gate.md). Use the verdict tables in checks 1â€“7 above for deterministic outcomes. For judgment calls, assess based on deployment impact to this specific app.

