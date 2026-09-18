# Deploying the AI assistant to the Webtropia server

For whoever prepares the release package. Everything the AI assistant needs on top of a normal HCMv3
release. Written 18 Sep 2026, for the first release that includes it (commit `2f63a385` or later on
`feat/ess-workflow-foundation`).

The assistant has three parts on the server:

| Part | What it is | Where |
|---|---|---|
| The feature | Code in the normal API and web builds, plus 3 new database tables | `api\`, `web\`, `migrator\` as usual |
| Ollama | Free program that runs the AI model | `C:\Ollama`, started at boot by a scheduled task |
| Qwen3-4B | The model file, about 2.5 GB | `C:\Ollama\models` |

Nothing is sent outside the server. The API refuses to call a model that is not on the server itself or its
private network, and step 5 of the install blocks Ollama from the internet.

---

## 1. What to add to the release package

Build `api\`, `web\` and `migrator\` exactly as for any release. Then add a folder **`ai\`** containing:

| File | From |
|---|---|
| `install-ollama.ps1` | this folder |
| `test-ollama.ps1` | this folder |
| `OllamaSetup.exe` | https://github.com/ollama/ollama/releases/download/v0.34.1/OllamaSetup.exe (about 1 GB). Version 0.34.1 is the one the assistant was tested with. |

The model itself is **not** in the package — the install script downloads it on the server (step 4 of the
script), then blocks Ollama from the internet.

Add the section at the end of this file to the package's `READ-ME-FIRST.txt`.

## 2. Database

The release adds one migration, **`Added_AiAgent_Conversations`**: three new tables (`AiConversations`,
`AiMessages`, `AiPendingActions`). Nothing existing is changed. The migrator applies it with the others.
Take the usual backup first.

## 3. API settings — `api\appsettings.Production.json`

Add this section. Every value shown is also the built-in default, so it works without it — it is written
out so anyone reading the server's config can see where the AI runs:

```json
"AiAgent": {
  "BaseUrl": "http://localhost:11434/v1",
  "Model": "qwen3:4b",
  "TimeoutSeconds": 900,
  "AllowExternalModelHost": false
}
```

- **`AllowExternalModelHost` must stay `false`.** It is the switch that would let the API send employee data
  to an AI service outside the server. Do not change it to "make it work".
- No `ApiKey` — Ollama does not use one.
- `TimeoutSeconds` 900: on a server without a graphics card an answer can take several minutes.

## 4. Web server / network

- The API runs **in-process** under IIS (see `api\web.config`), which has no request time limit, so long AI
  answers are fine as the site is set up today.
- **If the site is ever put behind Cloudflare or another proxy/CDN**, it will cut requests off after about
  100 seconds and most AI answers will fail. Check this before blaming the assistant.
- Port 11434 must **not** be opened in any firewall. Ollama listens on 127.0.0.1 only; the API reaches it on
  the same machine.

## 5. On the server, once

In an **administrator** PowerShell, in the `ai\` folder:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\install-ollama.ps1
```

It stops early and says why if the server has less than 8 GB RAM or 4 GB free — that is a server-size
decision to take before going further, not something to force through.

What it does, and the settings it uses, are commented at the top of the script. In short: installs to
`C:\Ollama`, starts at boot as SYSTEM, listens on 127.0.0.1 only, holds the model in memory for 24 hours after
use, allows 8,192 tokens per request, downloads the model, blocks Ollama from the internet, and runs
`test-ollama.ps1`, which should end with **PASS**.

## 6. After deploying

1. Sign in as the tenant **admin**. The Admin role receives new permissions automatically, so **AI Assistant**
   appears in the menu under Home straight away.
2. Other users: *Administration → Roles* (or the user) → *Employee Self Service* → tick **AI Assistant**. The
   Employee role does **not** get it by default.
3. An employee asking about their own leave needs their login linked to their employee record.
4. Try: *"add 50 kd car allowance to employee [a real code] starting 01/10/2026"* — a card shows every pay line
   before and after; nothing is saved until **Confirm**.

## 7. If something goes wrong

| Symptom | Check |
|---|---|
| "The AI assistant is not available right now" | Is Ollama running? `Get-ScheduledTask 'HCMv3 AI - Ollama'`, then `C:\Ollama\ollama-serve.log`. The API's log says exactly why the call failed. |
| API log: "outside this server and its private network" | `AiAgent:BaseUrl` was changed to another address. Put it back to `http://localhost:11434/v1`. |
| "took too long to answer" | The server is too slow for the model or busy. Raise `TimeoutSeconds`, or consider a bigger server / GPU. |
| The assistant ignores its rules in long conversations | `OLLAMA_CONTEXT_LENGTH` is not being applied — check `C:\Ollama\start-ollama.cmd` and that the scheduled task, not a tray app, is what is running. |
| Menu item missing | The user lacks the AI Assistant permission. Sign out and in after granting it. |

**Switch it off without a new release:** remove the AI Assistant permission from every role and user.
**Remove it completely:** see `aspnet-core/src/HCMv3.Application/AiAgent/ROLLBACK.md`, then
`Unregister-ScheduledTask 'HCMv3 AI - Ollama'`, remove the two `HCMv3 AI - block internet*` firewall rules,
and delete `C:\Ollama`.

---

## Section to add to READ-ME-FIRST.txt

```
AI ASSISTANT (new in this release)
----------------------------------
The ai\ folder installs the AI model the assistant uses. Do this once per server, after
the migrator and before telling users about the new menu item.

  a. Add the "AiAgent" section to api\appsettings.Production.json (see
     docs\deploy\ai-assistant\README.md). Leave AllowExternalModelHost = false: it is what
     stops employee data from ever being sent outside this server.
  b. Open PowerShell AS ADMINISTRATOR in the ai\ folder and run:
         Set-ExecutionPolicy -Scope Process Bypass
         .\install-ollama.ps1
     It needs about 20 minutes and internet access once, to download the model; after
     that it blocks the AI program from the internet. It must end with "PASS".
  c. Sign in as admin: "AI Assistant" is in the menu under Home. Give it to other users
     under Roles -> Employee Self Service -> AI Assistant.

Answers take one to several minutes on this server: it has no graphics card.
```
