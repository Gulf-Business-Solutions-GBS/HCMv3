# Deploying the AI assistant to the Webtropia server

For whoever prepares the release package. Everything the AI assistant needs on top of a normal HCMv3
release. Written 18 Sep 2026, for the first release that includes it. Build from `feat/ess-workflow-foundation`
at the commit that last changed this file, or later.

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

Add this section. **It is required** — without `Model` and `ContextTokens` the assistant assumes the plain
model's 4,096-token limit, and an administrator's request (the assistant's rules plus 18 tools, about 5,900
tokens) will not fit: the assistant refuses rather than send it without its rules.

```json
"AiAgent": {
  "BaseUrl": "http://localhost:11434/v1",
  "Model": "hcmv3-qwen3-4b",
  "ContextTokens": 12288,
  "ReservedOutputTokens": 1024,
  "TimeoutSeconds": 900,
  "StartupWaitSeconds": 300,
  "AllowExternalModelHost": false
}
```

- **`AllowExternalModelHost` must stay `false`.** It is the switch that would let the API send employee data
  to an AI service outside the server. Do not change it to "make it work".
- **`Model` and `ContextTokens` go together.** `hcmv3-qwen3-4b` is the variant the install script creates with
  a 12,288-token limit built in. The API trims every request to `ContextTokens` (oldest conversation first,
  never the assistant's rules) — so it must never be set higher than the model really holds, or the model
  would silently drop the start of the request, where the rules are. If `Model` is ever set back to plain
  `qwen3:4b`, set `ContextTokens` to 4096 (or remove it: 4096 is the default).
- No `ApiKey` — Ollama does not use one.
- `TimeoutSeconds` 900: on a server without a graphics card an answer can take several minutes.
- `StartupWaitSeconds` 300: how long a question keeps waiting while the model program is not answering at
  all — it has just started, or is loading the model back into memory. In that window the API waits and asks
  again instead of telling the person the assistant is unavailable.

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
use, downloads the model and creates the `hcmv3-qwen3-4b` variant with a 12,288-token limit built in, blocks Ollama from the internet, and runs
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

**Start here: `why-no-answer.ps1`** (this folder). Run it on the server and it says in plain words what is
wrong and what to do — whether the model program is running, whether it answers, how long it takes, and what
the API recorded. Add `-Restart` to start the model program if it has stopped, which is the usual cause.

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\why-no-answer.ps1 -Restart
```

A model host that is starting up no longer produces an error: since the change of 27 Sep 2026 the API waits
and asks again for up to `AiAgent:StartupWaitSeconds` (5 minutes by default) while the host refuses
connections or says it is busy or loading. So "not available" now means it was still not answering after
five minutes of trying — the program is stopped or the server has no room for it, not merely slow.

| Symptom | Check |
|---|---|
| "The AI assistant is not available right now" | Run `why-no-answer.ps1`. Is Ollama running? `Get-ScheduledTask 'HCMv3 AI - Ollama'`, then `C:\Ollama\ollama-serve.log`. The API's log says exactly why the call failed. |
| API log: "outside this server and its private network" | `AiAgent:BaseUrl` was changed to another address. Put it back to `http://localhost:11434/v1`. |
| "took too long to answer" | The server is too slow for the model or busy. Raise `TimeoutSeconds`, or consider a bigger server / GPU. |
| "…set up with too little room for everything it needs" | `AiAgent:Model` / `AiAgent:ContextTokens` are missing or still the plain 4,096 model. Set them as in section 3, restart the site. |
| "This message is too long for the assistant" | The question plus the assistant's rules and tools do not fit `ContextTokens`. Start a new chat. Check `Model` is `hcmv3-qwen3-4b` and `ContextTokens` is 12288. |
| API log: "request trimmed to fit the model" | Normal in long conversations: the oldest messages were left out of what was sent. The rules never are. |
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

  a. Add the "AiAgent" section to api\appsettings.Production.json - REQUIRED:
         "AiAgent": {
           "BaseUrl": "http://localhost:11434/v1",
           "Model": "hcmv3-qwen3-4b",
           "ContextTokens": 12288,
           "ReservedOutputTokens": 1024,
           "TimeoutSeconds": 900,
           "StartupWaitSeconds": 300,
           "AllowExternalModelHost": false
         }
     Leave AllowExternalModelHost = false: it is what stops employee data from ever
     being sent outside this server. Model and ContextTokens must match the model the
     install script creates, or administrators are told the assistant is not set up.
  b. Open PowerShell AS ADMINISTRATOR in the ai\ folder and run:
         Set-ExecutionPolicy -Scope Process Bypass
         .\install-ollama.ps1
     It needs about 20 minutes and internet access once, to download the model; after
     that it blocks the AI program from the internet. It must end with "PASS".
  c. Sign in as admin: "AI Assistant" is in the menu under Home. Give it to other users
     under Roles -> Employee Self Service -> AI Assistant.

Answers take one to several minutes on this server: it has no graphics card.
```
