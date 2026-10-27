# HCMv3 - Server Deployment Package

Built from `8702a800` on branch `feat/ess-workflow-foundation`, 26 Sep 2026.

New since the 25 Sep package:

- **Workflow setup without SQL.** A workflow chain is configured on screen, and
  built by dragging and copying steps instead of typing step numbers.
- **Every tenant starts ready**: the catalog, the self-service role and a
  notification address are set up for them.
- **Approver's morning.** Four questions and a way to clear the day's queue, and
  every self-service request can be opened, not only seen as a badge.
- **Dates** are typed as dd/MM/yyyy (and months as mm/yyyy), with the field you
  are in clearly outlined.
- **Mobile app** groundwork: the template is now Pinnacle Lite, an employee
  self-service app, and it no longer hangs on a stuck connection.
- **The migration fix** for the failure you hit on 25 Sep (`PeriodState`).

Earlier in this release: requests carry a plain number, the approver sees the
real form at full width with the conversation beside it, an approver can ask a
colleague without handing over the decision, people are named consistently as
name and code, payroll lock, delegation of approvals, and Go-Live Setup V2 only.

**141 migrations in total.** The database step must run; the `PeriodState`
migration now survives the half-finished state left by the failed attempt.

**Target server:** `vps2656276.fastwebserver.de` (81.30.156.82)
Windows Server 2025, IIS + .NET 10 Hosting Bundle + URL Rewrite 2.1 +
SQL Server Express. Single C: volume.

> **All configuration is pre-filled.** Hostname, database password and the
> ASP.NET Zero licence code are already set, for HTTPS. Nothing needs editing
> before you deploy.

---

## Contents

| Item | What it is |
|---|---|
| `api\` | Backend, 703 files, framework-dependent |
| `web\` | Angular website, ~9,120 files |
| `sql\01-create-database.sql` | Creates `HCMv3Db` and the `hcm_app` login |
| `sql\02-schema-all-migrations.sql` | All 141 migrations, idempotent |
| `sql\03-demo-data.sql` | Optional: two demo employees |
| `Deploy-OnServer.ps1` | Does the whole deployment |
| `Enable-Https.ps1` | Certificate setup - not needed on a redeploy |
| `ai\` | AI Assistant scripts - run `install-ollama.ps1` FIRST, see "AI Assistant" below |

All 24 DevExpress assemblies are inside `api\`, so **DevExpress must not be
installed on the server**.

---

## Deploy

Copy this folder to the server, then in an **elevated** PowerShell prompt
inside it:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\Deploy-OnServer.ps1
```

Then press **Ctrl+F5** in the browser. Without a hard refresh the old cached
website keeps showing and it looks like nothing changed.

The script stops the sites, applies any new migrations, copies both folders,
configures IIS, opens the firewall and starts everything again. It is safe to
re-run. Add `-SkipDatabase` if the SQL has already been applied.

**Do not run `Enable-Https.ps1`** on this server. HTTPS is already set up and
the configuration in this package already points at it. It is only needed on a
brand-new server, or to obtain a trusted certificate.

---

## AI Assistant - run ai\install-ollama.ps1 BEFORE Deploy-OnServer.ps1

**Order matters in this release.** The assistant now needs the model to hold
**12,288** tokens (it was 8,192). Re-running the install script upgrades the
model already on the server in place. If the model holds fewer tokens than the
app is told to send, the model silently drops the start of each request, which
is where the assistant's safety rules are. `Deploy-OnServer.ps1` therefore
**refuses to run** until the model has been upgraded.

1. In an **elevated** PowerShell prompt, in the `ai\` folder:

   ```powershell
   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
   .\install-ollama.ps1
   ```

   Ollama is already installed, so it skips the installer, rewrites its
   settings, re-creates `hcmv3-qwen3-4b` with a 12,288-token limit, and must
   end with **PASS**. The model is already downloaded, so this is quick.

2. Then go back up one folder and run `.\Deploy-OnServer.ps1` as usual.

The Ollama installer itself (`OllamaSetup.exe`, 1.5 GB) is **not** in this
package, because Ollama is already installed on this server. It is in the
previous package if it is ever needed again.

The API settings for the assistant are already in
`api\appsettings.Production.json` (`AiAgent` section). Leave
`AllowExternalModelHost` as `false`: it is what stops employee data ever
leaving this server.

Afterwards, sign in as the tenant admin: **AI Assistant** is in the menu. Give
it to other users under Roles -> Employee Self Service -> AI Assistant.

This server has no graphics card, so answers take one to several minutes.
`check-server.ps1` in the same folder is a read-only report on the server's
CPU, memory, disk and graphics card.

---

## Addressing

One hostname, two ports.

| Site | Purpose | HTTP | HTTPS |
|---|---|---|---|
| `hcm-web` | The application | 80 | **443** |
| `hcm-api` | The API | 9901 | **9443** |

Users go to **https://vps2656276.fastwebserver.de**

The certificate is self-signed, so the browser shows a warning once. Click
Advanced, then Proceed. The connection is still encrypted, which is what the
application needs.

---

## Verifying

| Check | Expected |
|---|---|
| `https://vps2656276.fastwebserver.de/` | Login page |
| `https://vps2656276.fastwebserver.de:9443/swagger` | Swagger UI |
| Log in as `admin` | Dashboard loads, no console errors |

```sql
USE HCMv3Db;
SELECT COUNT(*) AS Migrations FROM __EFMigrationsHistory;   -- expect 141
```

---

## If something fails

| Symptom | Cause |
|---|---|
| Site refuses to connect after `iisreset` | Default Web Site took port 80. The script now disables its auto-start; if it recurs, stop it and set `serverAutoStart` to false |
| `500.19` or `502.5` | Hosting Bundle installed before IIS. Re-run its installer, choose **Repair** |
| Deep links 404, home page fine | URL Rewrite module missing |
| Notifications never arrive | WebSocket Protocol feature not enabled in IIS |
| Mixed Content errors in the browser | A config file reverted to `http`. Re-run `Enable-Https.ps1` |
| Blank page, `NullInjectorError` in console | An Angular service proxy was not regenerated before the build |
| App starts then stops | See `C:\inetpub\hcm-api\App_Data\Logs\` |

---

## Notes

- Logs: `C:\inetpub\hcm-api\App_Data\Logs\`
- First request after a restart is slow: ASP.NET Zero builds its Entity
  Framework model across ~491 entities at startup. This is normal.
- Database lives on `C:\SQLData` and `C:\SQLLogs`. Data cannot exceed 10 GB
  (SQL Express limit) and the log is capped at 8 GB.
- Default login is `admin` / `123qwe` on a fresh database. Change it
  immediately - this server is reachable from the internet.
