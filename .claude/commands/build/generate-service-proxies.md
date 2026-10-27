---
description: "Generate/update Angular and React service proxies from backend Swagger/OpenAPI using NSwag"
allowed-tools: Bash, Read
argument-hint: "Optional: 'angular' or 'react' (generates both if not specified)"
---

# generate-service-proxies

Generate/update Angular and/or React service proxies after adding new backend code.

## Prerequisites

- Backend has no build errors
- Host project must be running

## Steps

1. **Start the Host project**:

   **PowerShell (Windows):**
   ```powershell
   $workspaceRoot = if (Test-Path "aspnet-core") { Get-Location } else { Get-Location | Split-Path -Parent }
   cd "$workspaceRoot\aspnet-core\src\HCMv3.Web.Host"
   dotnet run
   ```

   **Git Bash / WSL / Linux / Mac:**
   ```bash
   WORKSPACE_ROOT=$(if [ -d "aspnet-core" ]; then pwd; else cd .. && pwd; fi)
   cd "$WORKSPACE_ROOT/aspnet-core/src/HCMv3.Web.Host"
   dotnet run
   ```

2. **Generate service proxies**:

   ### For Angular:

   **PowerShell (Windows):**
   ```powershell
   $workspaceRoot = if (Test-Path "aspnet-core") { Get-Location } else { Get-Location | Split-Path -Parent }
   cd "$workspaceRoot\angular"
   npm run nswag
   ```

   **Git Bash / WSL / Linux / Mac:**
   ```bash
   WORKSPACE_ROOT=$(if [ -d "aspnet-core" ]; then pwd; else cd .. && pwd; fi)
   cd "$WORKSPACE_ROOT/angular"
   npm run nswag
   ```

   ### For React:

   **PowerShell (Windows):**
   ```powershell
   $workspaceRoot = if (Test-Path "aspnet-core") { Get-Location } else { Get-Location | Split-Path -Parent }
   cd "$workspaceRoot\react\nswag"
   .\refresh.bat
   ```

   **Git Bash / WSL / Linux / Mac:**
   ```bash
   WORKSPACE_ROOT=$(if [ -d "aspnet-core" ]; then pwd; else cd .. && pwd; fi)
   cd "$WORKSPACE_ROOT/react/nswag"
   sh refresh.sh
   ```

3. **Stop the Host project** after proxies are generated

## Output

- **Angular**: Updates `angular/src/shared/service-proxies/service-proxies.ts`
- **React**: Updates `react/src/api/generated/service-proxies.ts`

## Notes

- Always build backend successfully before generating proxies
- Host must be running for NSwag to access the Swagger endpoint
- **Angular**: The `npm run nswag` script runs `cd nswag/ && refresh.bat`
- **React**: Run `refresh.bat` (Windows) or `refresh.sh` (Linux/Mac) directly from `react/nswag/` directory
- Generated proxy files are auto-generated - **NEVER manually edit them**
- Regenerate proxies whenever backend API contracts change (new DTOs, app services, or method signatures)
