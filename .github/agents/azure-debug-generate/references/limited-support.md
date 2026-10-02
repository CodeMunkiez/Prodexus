<!-- azure-cor-disclaimer -->
> **Important:** This skill provides guidance and recommended instructions to assist the AI system. Outputs are not guaranteed to be complete, correct, secure, or applicable to every scenario. Results should be reviewed and validated by a human before being applied. The AI model may choose not to follow all instructions exactly, and additional verification may be required.

# Limited Support Warnings

> Emit standard warning for features not fully supported. Consistent format across categories sets user expectations.

---

## â›” Detection Algorithm â€” MANDATORY

For every planned project type, runtime, and emulator, follow **exactly**:

1. **Check for a matching reference file** â€” List category files/subdirectories, ignoring `_template.md`. Loose filename/subdirectory match to declared valueâ€”ignoring case, spaces, dashes, underscoresâ€”means reference exists.
2. **Check the status inside the reference file** â€” Open match; find status. `ðŸ”² Planned` in frontmatter note, status field, or top-level callout means **limited support** despite reference. Emit warning.
3. **Match exists AND not Planned** â†’ fully supported; proceed.
4. **No match** â†’ limited support; emit warning. Never substitute another supported feature.

| Category | Category Folder | Path Notes |
|----------|-----------------|------------|
| Project type | `references/project-types/` | Some types use subdirectories (e.g., `frontend-spa/frontend-spa.md`). Match against both filenames and subdirectory names. |
| Runtime | `references/runtimes/` | |
| Emulator | `references/emulators/` | |

> âš ï¸ **Do NOT skip.** Verify every declared feature against category folder before artifact generation.

---

## Warning Format

```
âš ï¸ LIMITED SUPPORT: {Category} "{value}" is not yet fully supported.
```

Where:
- `{Category}` â€” short feature-area label (e.g., `Project type`, `Runtime`, `Emulator`)
- `{value}` â€” planned feature (e.g., `python`, `Container App`, `Cosmos DB`)

---

## â›” Emission Protocol â€” MANDATORY

On limited-support detection, follow exactly:

### Step 1: Emit in assistant message

Write canonical warning in **regular assistant message text**, visible in chatâ€”not hidden in tool call.

```
âš ï¸ LIMITED SUPPORT: Emulator "Durable Task Scheduler" is not yet fully supported.
```

### Step 2: Confirm with user

For session's **first** limited-support feature, call `ask_user` to confirm:

```
ask_user(
  question: "âš ï¸ LIMITED SUPPORT: {Category} \"{value}\" is not yet fully supported. Would you still like me to put forth a best-effort attempt?",
  choices: [
    "Yes, proceed with best effort",
    "No, stop here"
  ]
)
```

Agreement gives session-wide consent. Still warn for later limited-support features, but do not call `ask_user` again.

> Emit `âš ï¸ LIMITED SUPPORT:` exactly once per `(Category, value)` pair in assistant messages.
---

## No Silent Substitution

**Never silently substitute supported alternatives** for limited support (e.g. Container App â†’ Azure Functions, Python â†’ Node.js). Always generate declared project type, runtime, and emulators.

