---
description: "Create dev bundles for Angular or MVC when bundle files are changed"
allowed-tools: Bash
argument-hint: "$TARGET - angular or mvc (optional, will detect automatically)"
---

# create-bundles

Use this command to create dev bundles when there are changes in bundles.

## Usage

This command checks the `package.json` files in the project and runs the appropriate bundle creation commands based on where changes were made.

## Commands

### When changes are in Angular

If there are changes in the Angular project, run bundles for both Angular and Host:

**PowerShell (Windows):**
```powershell
$workspaceRoot = if (Test-Path "aspnet-core") { Get-Location } else { Get-Location | Split-Path -Parent }
cd "$workspaceRoot\angular"; npm run create-dynamic-bundles
cd "$workspaceRoot\aspnet-core\src\HCMv3.Web.Host"; npm run create-bundles
```

**Git Bash / WSL / Linux / Mac:**
```bash
WORKSPACE_ROOT=$(if [ -d "aspnet-core" ]; then pwd; else cd .. && pwd; fi)
cd "$WORKSPACE_ROOT/angular" && npm run create-dynamic-bundles
cd "$WORKSPACE_ROOT/aspnet-core/src/HCMv3.Web.Host" && npm run create-bundles
```

### When changes are in MVC

If there are changes in the MVC project, run bundles only for MVC:

**PowerShell (Windows):**
```powershell
$workspaceRoot = if (Test-Path "aspnet-core") { Get-Location } else { Get-Location | Split-Path -Parent }
cd "$workspaceRoot\aspnet-core\src\HCMv3.Web.Mvc"; npm run create-bundles
```

**Git Bash / WSL / Linux / Mac:**
```bash
WORKSPACE_ROOT=$(if [ -d "aspnet-core" ]; then pwd; else cd .. && pwd; fi)
cd "$WORKSPACE_ROOT/aspnet-core/src/HCMv3.Web.Mvc" && npm run create-bundles
```

## Description

- **Angular changes**: Runs `create-dynamic-bundles` in Angular and `create-bundles` in Host
- **MVC changes**: Runs `create-bundles` in MVC only

## Notes

- Run this command after making any changes to bundles
- Run bundle creation commands only if JavaScript or CSS files are modified
- Execute last, after all related changes are finalized
