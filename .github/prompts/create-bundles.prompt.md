# create-bundles

Use this command to create dev bundles when there are changes in bundles.

## When to use

Run this command after making changes to JavaScript or CSS files that require bundle creation. This command checks the `package.json` files in the project and runs the appropriate bundle creation commands based on where changes were made.

## Instructions

### When changes are in Angular

If there are changes in the Angular project, run bundles for both Angular and Host:

**PowerShell (Windows):**
```powershell
$workspaceRoot = if (Test-Path "aspnet-core") { Get-Location } else { Get-Location | Split-Path -Parent }
cd "$workspaceRoot\angular"; npm run create-dynamic-bundles
cd "$workspaceRoot\aspnet-core\src\HCMv3.Web.Host"; npm run create-bundles
```

**Or if using yarn (PowerShell):**
```powershell
$workspaceRoot = if (Test-Path "aspnet-core") { Get-Location } else { Get-Location | Split-Path -Parent }
cd "$workspaceRoot\angular"; yarn create-dynamic-bundles
cd "$workspaceRoot\aspnet-core\src\HCMv3.Web.Host"; yarn create-bundles
```

**Git Bash / WSL / Linux / Mac:**
```bash
WORKSPACE_ROOT=$(if [ -d "aspnet-core" ]; then pwd; else cd .. && pwd; fi)
cd "$WORKSPACE_ROOT/angular" && npm run create-dynamic-bundles
cd "$WORKSPACE_ROOT/aspnet-core/src/HCMv3.Web.Host" && npm run create-bundles
```

**Or if using yarn (Bash):**
```bash
WORKSPACE_ROOT=$(if [ -d "aspnet-core" ]; then pwd; else cd .. && pwd; fi)
cd "$WORKSPACE_ROOT/angular" && yarn create-dynamic-bundles
cd "$WORKSPACE_ROOT/aspnet-core/src/HCMv3.Web.Host" && yarn create-bundles
```

### When changes are in MVC

If there are changes in the MVC project, run bundles only for MVC:

**PowerShell (Windows):**
```powershell
$workspaceRoot = if (Test-Path "aspnet-core") { Get-Location } else { Get-Location | Split-Path -Parent }
cd "$workspaceRoot\aspnet-core\src\HCMv3.Web.Mvc"; npm run create-bundles
```

**Or if using yarn (PowerShell):**
```powershell
$workspaceRoot = if (Test-Path "aspnet-core") { Get-Location } else { Get-Location | Split-Path -Parent }
cd "$workspaceRoot\aspnet-core\src\HCMv3.Web.Mvc"; yarn create-bundles
```

**Git Bash / WSL / Linux / Mac:**
```bash
WORKSPACE_ROOT=$(if [ -d "aspnet-core" ]; then pwd; else cd .. && pwd; fi)
cd "$WORKSPACE_ROOT/aspnet-core/src/HCMv3.Web.Mvc" && npm run create-bundles
```

**Or if using yarn (Bash):**
```bash
WORKSPACE_ROOT=$(if [ -d "aspnet-core" ]; then pwd; else cd .. && pwd; fi)
cd "$WORKSPACE_ROOT/aspnet-core/src/HCMv3.Web.Mvc" && yarn create-bundles
```

## Notes

- **Angular changes**: Runs `create-dynamic-bundles` in Angular (which executes `yarn && gulp buildDev`) and `create-bundles` in Host (which also executes `yarn && gulp buildDev`)
- **MVC changes**: Runs `create-bundles` in MVC only (which executes `yarn && gulp buildDev`)
- Run bundle creation commands only if JavaScript or CSS files are modified
- These commands must be executed last, after all related changes are finalized
- Check the `package.json` file in the relevant project to see the exact bundle creation command
