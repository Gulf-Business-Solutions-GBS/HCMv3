# ASP.NET Zero feature usage — HCMv3 (checked against template and legacy HCM10)

Date: 2026-09-25. Template baseline: commit `32582fc3`. Legacy: `C:\GBS\HCM\HRMS_Production`.

**Column meanings**
- **Std as-is**: the ASP.NET Zero feature, used unchanged.
- **Std + custom**: the ASP.NET Zero feature, used with our changes or additions.
- **Fully custom**: ASP.NET Zero has this, but we built our own instead (reinvented).
- **Std not used**: ASP.NET Zero has this, but we don't use it.
- **Not in Std**: ASP.NET Zero has nothing like it, so we built it from scratch. Rows marked "not built" are legacy features that are still missing.

**Where to enable / disable:**
- Menu paths read as *Menu → Screen → Tab*. "Settings" means *Administration → Settings*: a tenant admin sees the tenant version, and the host admin sees the host version with the same tabs plus *Tenant Management*.
- "Roles" means the permission is granted or removed in *Administration → Roles*.
- "Code" or "Config" means there is no screen; a developer changes the named file and redeploys. `appsettings.json` is `aspnet-core/src/HCMv3.Web.Host/appsettings.json`.
- Menu paths were read from `app-navigation.service.ts`. They were not clicked through in a browser.

| # | Feature | What it does / how we use it | Std as-is | Std + custom | Fully custom | Std not used | Not in Std | Where to enable / disable in HCMv3 | Corrective action |
|---|---|---|:-:|:-:|:-:|:-:|:-:|---|---|
| | **SIGN-IN & SECURITY** | | | | | | | | |
| 1 | Login / logout | Sign-in page restyled; each tenant can choose its start page | | ✅ | | | | Start page: System Settings → General Settings | None |
| 2 | Password reset / email confirmation | Standard forgot-password flow; the careers portal also uses it | ✅ | | | | | Settings → User Management tab → "Email confirmation required for login" | None |
| 3 | Password policy | Complexity, expiry, change at first login | ✅ | | | | | Settings → Security tab → Password Complexity / Password Expiration | None |
| 4 | Account lockout + login-attempts log | Locks after failed logins; log screen | ✅ | | | | | Settings → Security tab → User Lock Out. Log: user menu (top right) → Login Attempts | None |
| 5 | Two-factor login | Switched off | | | | ✅ | | Settings → Security tab → Two Factor Login | Enable per customer only if asked |
| 6 | Password-less / QR login | Switched off | | | | ✅ | | Settings → User Management tab → Passwordless Login / QR Login | None |
| 7 | External logins (Google, Microsoft, etc.) | Switched off; Azure SSO goes through the Integration Center | | | | ✅ | | Config: `appsettings.json` → `Authentication` → each provider's `IsEnabled`. Keys: Settings → Other Settings → External Login Settings | None |
| 8 | Active Directory (LDAP) login | Commented out. **Legacy had it** | | | | ✅ | | Code: `HCMv3.Core/HCMv3CoreModule.cs:117` (un-comment). Then Settings → User Management tab → LDAP Settings appears | Ask customers whether they used AD login; if yes, enable it |
| 9 | reCAPTCHA | Switched off; template demo keys still in config | | | | ✅ | | Settings → User Management tab → "Use captcha on login / registration". Keys: `appsettings.json` → `Recaptcha` | Remove the demo keys |
| 10 | User self-registration | Link hidden, but the setting is still ON | | | | ✅ | | Settings → User Management tab → "Allow users to register" | Untick it (default is in `AppSettingProvider.cs:167`) |
| 11 | Tenant self-registration | Link hidden | | | | ✅ | | Host: Settings → Tenant Management tab → "Allow self registration" | Confirm it is off |
| 12 | Idle session timeout | Standard, plus a keep-alive during AI-assistant calls | | ✅ | | | | Settings → User Management tab → Session Time Out | None |
| 13 | Active sessions | Lists and ends user sessions | ✅ | | | | | Administration → Active Sessions | None |
| 14 | Impersonation (user / tenant) | Admin "log in as" | ✅ | | | | | Roles → "Users → Impersonation" / "Tenants → Impersonation" permissions | None |
| 15 | Linked accounts | Switch between linked logins | ✅ | | | | | User menu → Manage Linked Accounts. No on/off switch | Consider hiding (no HR need) |
| 16 | User delegation (login level) | Lets one user act as another | ✅ | | | | | User menu → Manage User Delegations. Off switch in code: `Authorization/Delegation/UserDelegationConfiguration.cs:9` | Rename or hide: it gets confused with workflow delegation (ESS → My Delegations) |
| 17 | Users management | Create, edit, unlock and set permissions for users | ✅ | | | | | Administration → Users | None |
| 18 | Roles & permissions | Standard engine; 2,211 HR permissions, Employee and Candidate roles | | ✅ | | | | Administration → Roles | None |
| 19 | Profile | Change password, picture, language | ✅ | | | | | User menu → My Settings / Change Picture. Gravatar: Settings → User Management tab | None |
| 20 | JWT token login | API authentication | ✅ | | | | | Config: `appsettings.json` → `Authentication:JwtBearer` | **Replace the template SecurityKey before production** |
| 21 | OpenIddict (OAuth server) | Switched off; template secret still in config | | | | ✅ | | Config: `appsettings.json` → `OpenIddict:IsEnabled` | Remove the secret |
| 22 | Employee ↔ user link | Links a login to an employee record | | | | | ✅ | **No screen found**: backend only (`EmployeeUserLinkAppService`) | Add a screen; block linking the admin user; assign the Employee role automatically |
| 23 | Row-level security | Limits which employees a role can see, by org unit | | | | | ✅ | On/off: System Settings → Security Settings → Enable Row Level Security. Rules: System Security → Sys User Org Unit Data | **Extend it to admin screens (e.g. the Employee list) as in legacy** |
| 24 | Wage-type security | Hide secured wage types by role; nothing enforces it | | | | | ✅ | Payroll Management → Wage Types → open a wage type → security roles | Implement enforcement (legacy parity) |
| | **ORGANIZATION & TENANTS** | | | | | | | | |
| 25 | Organization units | HR structure built on standard org units, plus our profile table | | ✅ | | | | Administration → Organization Units; HR view under Organization Management | Check the depth limit of 10 (legacy used 7 levels) |
| 26 | Org chart | Visual org chart | | | | | ✅ | Personnel Administration → Employee Org Chart; Manpower Planning → Position Org Chart. Roles controls access | None |
| 27 | Multi-tenancy | Standard tenants; reference-data setup added on tenant creation | | ✅ | | | | Host: Saas → Tenants. Global switch in code: `HCMv3Consts.cs:14` | **Make reports and Go-Live payslips use the tenant's own database** |
| 28 | Editions & features (module licensing) | Not used; modules are controlled by permissions | | | | ✅ | | Host: Saas → Editions → edit → Features | Use this if modules are ever sold separately |
| 29 | Subscriptions / payments / invoices | Not used | | | | ✅ | | Administration → Subscription. Gateways: `appsettings.json` → `Payment` → `Stripe`/`PayPal` `IsActive`. Invoice data: Settings → Invoice tab | Set the gateways to inactive |
| 30 | Host / tenant settings screens | Email, security and appearance settings | ✅ | | | | | Administration → Settings | None |
| 31 | Settings storage (`Manage*Settings`, 14 tables) | Legacy settings kept as tables instead of standard settings | | | ✅ | | | System Settings → (General, Security, Document, Payroll, Time, Workflow, Notification, … Settings) | Add caching, remove dead switches, put new settings in standard settings |
| 32 | Date-effective config (`SysTenantConfig`) | Settings with start and end dates (legacy) | | | | | ✅ | System Security → Sys Tenant Configs | None |
| 33 | Languages / translations | English and Arabic, standard XML plus text editor | ✅ | | | | | Administration → Languages (enable/disable a language, edit texts) | None |
| | **SCREENS & NAVIGATION** | | | | | | | | |
| 34 | Themes / UI customization | Standard themes plus our Theme14 | | ✅ | | | | Administration → Visual Settings | None |
| 35 | Menu + menu search | Search extended with people search, recent items, screen codes | | ✅ | | | | Menu items appear or disappear by permission (Roles). No separate switch | None |
| 36 | Screen-code catalog | A second list of all screens, next to the menu | | | ✅ | | | Administration → Manage Screen Codes. Show codes per user: Quick Links panel on the dashboard | Generate it from the menu, or add a test that keeps them in sync |
| 37 | Host dashboard | Standard widgets | ✅ | | | | | Host: Dashboard (edit mode on the page) | None |
| 38 | Tenant dashboard | Standard widgets replaced by a fixed My Summary / ESS home | | ✅ | | | | Dashboard; quick links in Administration → Manage Quick Links | Build future HR tiles as standard widgets |
| 39 | F1 help | Help pages in English and Arabic | | | | | ✅ | Always on (press F1). Content: `docs/help` | None |
| | **NOTIFICATIONS & EMAIL** | | | | | | | | |
| 40 | In-app notifications (bell) | Used by workflow and absence alerts | | ✅ | | | | Each user: bell → Settings. Which events notify: System Settings → Notification Settings | Give each notification type its own name so users can switch types off |
| 41 | Legacy notification tables (`TaskEvent`, `NotificationMaster`, `EmployeesNotification`) | Nothing writes or sends them | | | ✅ | | | System Security → Notification Masters. `TaskEvent` and `EmployeesNotification` have **no screen** (backend only) | Remove them, or connect them to real notifications |
| 42 | Notification rules + email templates | Legacy rules and templates; sending uses standard email | | ✅ | | | | General Master Info → Notification Configs; System Security → Email Templates / Email Tokens / System Notifications | None |
| 43 | Mass notifications | Standard broadcast screen | ✅ | | | | | Administration → Notifications → Mass Notifications | None |
| 44 | News → send notification / email | Switches are stored but nothing sends (legacy sent) | | | | | ✅ | Personnel Administration → Announcements ("Send notification" / "Send email" boxes) | Implement sending |
| 45 | Email sending | Standard sender plus a global on/off switch | | ✅ | | | | SMTP: Settings → Email (SMTP) tab (host only; hidden for tenants) **and** Administration → Integration Center → SMTP card. The global on/off switch is on the SMTP card | Pick one place to own SMTP settings |
| 46 | SMS (Twilio) | Not used | | | | ✅ | | Config: `appsettings.json` → `Twilio` | None |
| 47 | Chat & friends | Switched on, but no HR need | ✅ | | | | | Host: Saas → Editions → edit → Features → Chat | Switch it off in the edition if not wanted |
| 48 | Webhooks | Only a test webhook; menu visible | | | | ✅ | | Administration → Webhook Subscriptions. Events are defined in code: `Core/Webhooks/AppWebhookDefinitionProvider.cs` | Define HR events or hide the menu (Roles) |
| | **DATA, FILES & AUDIT** | | | | | | | | |
| 49 | File storage / attachments | Moved from database to disk per tenant (your rule) | | ✅ | | | | Folder: `appsettings.json` → file store section. Limit: System Settings → Document Settings → Max Storage Size | **Fix the storage limit (it always reports 0 used)** |
| 50 | Dynamic properties (custom fields) | Only set up for User; menu visible | | | | ✅ | | Administration → Dynamic Properties. Which entities: code `DynamicEntityProperties/AppDynamicEntityPropertyDefinitionProvider.cs` | Hide the menu, or enable it for Employee |
| 51 | Excel export | Standard exporter, also used by grid reports | ✅ | | | | | Export button on each list/report. No switch | None |
| 52 | Bulk Excel import/export (any entity) | Legacy tool; standard import covers users only | | | | | ✅ | Payroll Management → Bulk Import Export (access by Roles) | Standardize on one Excel library |
| 53 | Audit logs | Records who called what | ✅ | | | | | Administration → Audit Logs | None |
| 54 | Change history (field-level) | **No HR data tracked**; legacy tracked everything | | | | ✅ | | Code only: `Core/EntityHistory/EntityHistoryHelper.cs:15,20` (list of tracked entities). Viewed in Administration → Audit Logs → Change Logs tab | **Top priority: add HR entities to that list** |
| 55 | Run log tables (PayrollLog, ExecutionLog, …) | Legacy run logs | | | | | ✅ | System Security → Execution Logs / Bulk Upload Logs / Email Notification Logs / Punch Notification Logs / Retro Reason Logs | None |
| 56 | GDPR data download | Contains no HR data | ✅ | | | | | User menu → Download Collected Data | Add HR data only if GDPR applies |
| 57 | Object mapping (Mapperly) | Hand-written mapping in almost all modules | | | ✅ | | | Code only | Use Mapperly for new code |
| 58 | Clock / time zone | Standard clock mostly bypassed | | | ✅ | | | Code only (`Clock.Provider` is not set anywhere) | Switch to `Clock.Now` |
| 59 | Raw SQL executor | Legacy helper; no callers; not parameterized | | | ✅ | | | Code only: `Application/Common/RawSqlExecutor.cs` | Delete it |
| | **BACKGROUND PROCESSING** | | | | | | | | |
| 60 | Background jobs & workers | Standard; we added a monitoring page and failure log | | ✅ | | | | Monitor: Administration → Background Jobs. Workers registered in code: `Web.Host/Startup/HCMv3WebHostModule.cs:110-160` | None |
| 61 | Hangfire | Switched off | | | | ✅ | | Code: `Web.Core/Common/WebConsts.cs:11`. A "Start Hangfire" box exists in System Settings → General Settings but **does nothing** | Remove that dead box |
| 62 | Engine run progress | Live progress for payroll and time runs | | | | | ✅ | Shown on the payroll / time engine run screens. No switch | None |
| 63 | Workflow auto-close job | Legacy daily job; **not built** | | | | | ✅ | The "Auto Close" field exists in Workflow → Request Masters, but nothing acts on it | Implement it |
| 64 | Missed punch-in/out emails | Legacy job; **not built** | | | | | ✅ | The fields exist in Time Management → Time Policies, but nothing sends | Implement it |
| 65 | Sponsor quota calculation job | Service exists; nothing calls it | | | | | ✅ | No screen or schedule | Schedule it |
| | **BUSINESS FEATURES** | | | | | | | | |
| 66 | Workflow / approvals | Approval routing, delegation, escalation | | | | | ✅ | Workflow → Workflow Roles / Groups / Request Masters; System Settings → Workflow Settings | Add auto-close (row 63) |
| 67 | Employee self-service (ESS) | Employee portal | | | | | ✅ | Employee Self Service menu. Each item controlled by `Ess.*` permissions (Roles); System Settings → Self Service Settings | None |
| 68 | Business rules engine | Configurable rules | | | | | ✅ | Configure Business Rules (menu). Each rule can be switched on/off | None |
| 69 | Payroll lock / retro | Blocks edits to locked payroll periods | | | | | ✅ | Payroll Management (payroll period lock). Rules in code: `PayrollLockGuard` | None |
| 70 | Grid reports | 111 analysis reports | | | | | ✅ | Analysis Reports menu. Access by Roles | None |
| 71 | Print reports + designer | DevExpress reports | | | | | ✅ | Print Reports menu. Catalog and access: Report Management → Print Reports / Print Report Permissions | Use the tenant database (row 27) |
| 72 | Report distribution | Scheduled report emails (new; not in legacy) | | | | | ✅ | Report Distribution menu | None |
| 73 | Go-Live setup | Guided tenant setup | | | | | ✅ | Go-Live Setup menu | None |
| 74 | AI assistant | Per-tenant AI chat | | | | | ✅ | AI Assistant menu; access by the `Ess.AiAgent` permission (Roles) | None |
| 75 | Careers portal | Candidate site; candidates are standard users | | | | | ✅ | Separate site (`careers/`). Candidate self-registration setting: `AppSettings` `Careers.AllowCandidateSelfRegistration` (no screen found) | None |
| 76 | Integration Center | Connector screen; SMTP/SSO saved into standard settings | | | | | ✅ | Administration → Integration Center (one card per connector) | None |
| 77 | API dictionary | Entity and field catalog, rebuilt by reflection | | | ✅ | | | Administration → API Dictionary | Generate it from Swagger instead |
| 78 | HR modules (Payroll, Time, Personnel, and 17 more) | All HR business functions | | | | | ✅ | Each module's menu; access by Roles | None |
| 79 | Dashboard designer / KPI / pivot analysis | Legacy XAF feature; **not built** | | | | | ✅ | — | Decide whether customers need it |
| 80 | Saved grid layouts per user | Legacy XAF feature; **not built** | | | | | ✅ | — | Decide whether customers need it |
| | **PLATFORM & DEPLOYMENT** | | | | | | | | |
| 81 | Swagger (API docs) | API documentation | ✅ | | | | | Code: `Web.Core/Common/WebConsts.cs:10` (`SwaggerUiEnabled`) | None |
| 82 | Health checks | Health endpoint and screen | ✅ | | | | | Administration → Health Check. Config: `appsettings.json` → `HealthChecks` | None |
| 83 | Maintenance screen | Clear caches, download logs | ✅ | | | | | Administration → Maintenance | None |
| 84 | Caching (in-memory) | Standard cache | ✅ | | | | | Clear: Administration → Maintenance | None |
| 85 | Repositories / unit of work / events | Data access layer | ✅ | | | | | Code only | None |
| 86 | GraphQL | Not used, but **switched on with a public playground** | | | | ✅ | | Code: `Web.Core/Common/WebConsts.cs:18-24` (`Enabled`, `PlaygroundEnabled`) | **Switch it off** |
| 87 | Redis | Not used | | | | ✅ | | Code: `Web.Core/HCMv3WebCoreModule.cs:83-88` + `appsettings.json` → `Abp:RedisCache` | Enable only for multi-server deployment |
| 88 | Mobile app (MAUI) | Not used. Legacy had a mobile app | | | | ✅ | | Project `aspnet-core/src/HCMv3.Maui` (not deployed) | Decide: build it out or remove it |
| 89 | Public website (Web.Public) | Not used; careers portal replaces it | | | | ✅ | | Project `aspnet-core/src/HCMv3.Web.Public` (not deployed) | Remove it |
| 90 | Docker / k8s / Key Vault | Not used; config points at template hosts | | | | ✅ | | `angular/src/assets/appconfig.k8s.json`, Docker files; Key Vault: `appsettings.json` → `Configuration:AzureKeyVault` | Remove or fix |
| 91 | Power Tools (code generator) | Not used; replaced by our Claude generators | | | | ✅ | | Folder `aspnet-core/AspNetZeroRadTool` | Delete the binaries |
| 92 | Demo UI components | Removed | | | | ✅ | | — | None |
