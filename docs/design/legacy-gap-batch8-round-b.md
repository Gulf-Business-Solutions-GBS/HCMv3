# Legacy gap review, Batch 8 round B: design note for four features

Date: 2026-10-19. Status: **for owner decision. No code written yet.**

The four features: the appraisal review workflow, rejoining with a new employee code, the notification rule engine, and licensing.

Every design here follows two rules you set:

- **Zero template screens and code stay untouched.** This covers Users, Roles, Tenants, Editions, the account pages, the settings UI, and Zero's authorization and multi-tenancy code. New work goes in new files. At most, a template file gets a one-line hook.
- **If ASP.NET Zero or ABP already does it, we use that.** Each section names the Zero or ABP feature it builds on and says what is genuinely new.

Payroll and HR results must match legacy as it is. Where legacy does something odd, this note says so and asks you whether to keep it. The default answer is "keep it and pin it with a test".

---

## 1. Appraisal review workflow

### (a) What legacy did

Legacy had **two separate appraisal flows**. Both are in scope.

**The 360 review (PAEmployee).** An appraisal is opened for an employee and then goes through the workflow. Each workflow step has its own row in a setup table called "Appraisal WF Setup" (`PARequest`, one row per request type and step). That row decides what the people on that step may do:

- **Can Add KPI.** On this step the approver can use **Copy KPI**. It fills the appraisal's KPI list from a job title, an org unit, or another employee's appraisal for the same period. Copy KPI also checks the form type's maximum number of reviewers.
- **Can Add Peer / Allowed Peer Count.** The employee may nominate peers as reviewers, up to the allowed number. Peers added by someone else do not count towards that limit.
- **Can Employee Self Rate.** When approvers are assigned to this step, the employee is automatically added as a reviewer of their own appraisal.
- **Can Add Ratings.** The approver on this step is automatically added as a reviewer and can enter ratings.
- **Auto Add Manager.** The employee's current manager is automatically added as a reviewer.
- **Auto Add Relationship** (with a list of relationship types). Everyone linked to the employee through one of those relationship types today, for example "dotted-line manager", is automatically added as a reviewer.
- **Can View Ratings / Can View Peer Ratings / Can View Overall Average / Can Peer Add Ratings.** These decide which rating sections and average scores are visible to whom.
- **Auto Submit On Last Fill.** When the last reviewer presses **Submit Review**, the appraisal is approved on to the next step automatically.

On the appraisal screen, a reviewer who has not yet submitted sees **Save Review** and **Submit Review**. Submitting locks that reviewer's part. Whether marks or a rating scale is used comes from Appraisal Settings ("Enable Marks In PA Review").

Important: the reviewers are added at the moment **approvers are assigned to a step**, not when the step is approved.

**The goal form (PAForm).** This is the KPI or goal plan, with 12 tick boxes in pairs: Plan, Quarter 1, Mid-Year, Quarter 3, End-of-Year (each with an Appraisee and an Appraiser box), then Appraisee Signed Off and Appraiser Signed Off. The rules were:

- **Strict order.** A phase's boxes only open after both boxes of the previous phase are ticked: Plan, then Q1, then Mid-Year, then Q3, then End-of-Year, then sign-off.
- **Each side ticks only its own box.** The employee ticks the Appraisee box and the manager ticks the Appraiser box.
- **Only inside the phase's time window.** The window is the PA Timeline dates for that phase, or a personal extension in PA Timeline Exception.
- **Phases not in the period's timeline are hidden.** The list view also hides the quarter boxes by Appraisal Cycle: Annual hides Q1, Mid-Year and Q3, and Half-Yearly hides Q1 and Q3.
- **Peer reviewers** see only a "Submitted" box during the End-of-Year window.
- **Legacy oddity.** Both sign-off boxes can be ticked by the **manager** only. The code comment says the employee should tick "Appraisee Signed Off", but the code does the opposite.

**The ESS appraisal screen.** Employees and managers did all of the above from ESS: open an appraisal, launch appraisals in bulk for an org unit, copy KPIs, enter a rating, marks and a comment per KPI (with a rating balance limit), add a peer, and submit a review.

Legacy files read:
- `HCM10.Module\BusinessObjects\Workflow\WorkflowSetup\PARequest.cs` and `PARequestRelationship.cs`
- `HCM10.Module\Controllers\PerformanceManagement\PAEmployeeController.cs`
- `HCM10.Module\Controllers\PerformanceManagement\PAFormController.cs`
- `HCM10.Module\BusinessObjects\PerformanceManagement\PAEmployeeReviewer.cs` (peer limit)
- `HCM10.Module\BusinessObjects\Workflow\WorkflowSetup\WorkFlowServices.cs` (around lines 4740–4830: auto-adding reviewers)
- `HCM10.ESS\Controllers\PerformanceManagement\PerformanceManagementController.cs`
- `HCM10.ESS\Views\PerformanceManagement\RequestForm.cshtml`

### (b) What HCMv3 has today

- **Data and admin screens are all ported.** That includes appraisals, reviewers (with "is peer", "added by" and "review submitted"), ratings, the goal form with its Plan, Annual Review and Goals tabs, timelines and timeline exceptions, form types (with reviewer limits), and Appraisal Settings.
- **The 360 appraisal goes through the HCMv3 workflow engine** (`AppraisalWorkflowTransactionHandler`), routed on form type. The handler says plainly that none of the per-step flags were ported, so no reviewer is ever added automatically.
- **The goal-form edit dialog shows all 12 boxes, always editable,** with no order, window or role rules.
- **There is no ESS appraisal screen.**
- **The workflow engine already has two useful pieces:** a hook that fires on every step action, and a per-step field access table ("Workflow Group Field Access": visible, editable and required per field per step).

### (c) Proposed design

**Step setup: an extension table, not new columns on the workflow tables.** We add a new "Appraisal Step Settings" table, with one row per request master and step, holding the 12 legacy flags and the allowed peer count. Relationship types sit in a child list. This is a straight port of `PARequest`.

It is a separate table, not new columns on `RequestDetail`, so the general workflow setup stays the same for the other 50 request types. It has its own small setup screen under Performance Management, plus a tab on the request master detail page if you prefer.

**Auto-adding reviewers.** The appraisal workflow handler gets the "approvers assigned to step N" moment. Most of this is already there. Where the engine has no hook for that exact moment, we add one more overridable method, as the base class already does for submit, approve and reject.

At that moment the handler adds reviewers using the legacy rules: self, approver, manager, and relationships. It never adds the same person twice, and it marks manager and relationship reviewers as peers, exactly as legacy did.

**Review actions.** We add three new app service methods:

- **Copy KPI**, from a job title, an org unit or an employee, with the reviewer limit check.
- **Save Review.**
- **Submit Review.** It marks the reviewer's review as submitted. If the step is set to "auto submit on last fill" and nobody else is still pending, it calls the normal workflow **Approve** for the first approver, as legacy did.

The peer limit check moves into the reviewer save.

**Who sees what.** The step flags and the user's role decide visibility: employee, reviewer or peer, or approver on this step. The server works out a small "what may I see and do" object for the signed-in user, and the screen hides and disables accordingly. The server also enforces the same rules on save, so hiding a field is not the only protection.

We do **not** reuse Workflow Group Field Access for this. Its rules are per field and per step, while legacy's rules are per role within a step, so forcing them into it would distort it.

**Goal form order and windows.** We add a server-side "may tick" check for each of the 12 boxes, using order, side, window and exception exactly as legacy did. It is enforced on save, and the dialog greys boxes out from the same answer. Boxes for phases outside the period's timeline, or outside the Appraisal Cycle, are hidden.

**ESS appraisal screen.** We add one new ESS screen in the Exit Permit style, with a list and a details dialog. It covers My appraisals, Appraisals I must review, and Team appraisals for managers. The details dialog offers KPI rows with rating or marks and a comment, Add peer, Save, Submit review, and Copy KPI where allowed. Bulk launch for an org unit stays on the admin screen unless you want it in ESS (see question 4).

**Builds on.** The existing HCMv3 workflow engine, ABP authorization, and ABP entity history (which records who ticked a box, for audit).

**Genuinely new.** One table and its screen, about four app service methods, the "may tick" and visibility rules, and one ESS screen.

### (d) Decisions for you

1. **Should the step settings live in their own table, or as extra fields on the request master step?** I recommend their own table. It leaves the workflow setup for every other request type unchanged.
2. **Legacy lets only the manager tick both sign-off boxes. Keep that?** I recommend keeping it to match legacy, pinned with a test. If you say this was a legacy bug, the employee will tick "Appraisee Signed Off" instead.
3. **Should reviewers still be added when approvers are assigned, as in legacy, even if the step is later sent back?** I recommend yes, as legacy: a reviewer once added stays.
4. **Should bulk launch for an org unit also be in ESS for managers, or stay admin-only?** I recommend admin-only for the first release.
5. **When should we agree the screen layout?** Your batch note says "spec with user first". I recommend we agree the ESS screen layout (one mock-up) before building.

### (e) Size and risks

**Size: L.** That is roughly backend M, ESS screen M, and goal-form rules S.

Risks:
- A wrong default on a flag silently adds or drops a reviewer, which changes the score. Every flag defaults to off, and a migration seeds nothing. Each tenant must set its steps (a `Setup:` line in the commit).
- Auto-approve on last fill runs the workflow as the first approver. It needs careful tests so it cannot approve twice.
- Existing appraisals already in progress have no reviewers. We will not back-fill them.

### (f) What will NOT be built

- No change to how other request types are routed or shown.
- No new rating scales or scoring formulas: scores are calculated as today.
- No back-fill of reviewers on appraisals already open.
- No appraisal screen in the mobile app.

---

## 2. Rejoin with a new employee code

### (a) What legacy did

A rejoin record names the former employee, the rejoin date, and five ticks: **Create New Employee Code**, and carry forward previous leave, ticket, indemnity and loan balances.

**If "Create New Employee Code" is NOT ticked,** the old employee is made Active again. For each balance that is not carried forward, legacy writes a zeroing record dated the day before rejoin:

- leave: an absence adjustment
- indemnity: a gratuity record with "reset"
- ticket: a ticket adjustment
- loans: an external loan recovery

**If it IS ticked,** legacy creates a **brand-new employee**:

- **What is copied:** names, title, birth date, nationality, religion, gender, marital status, country, phones, addresses, qualification, blood group, Civil ID, email and birth place.
- **What changes:** the new employee is Active, with joining date = rejoin date and probation end recalculated.
- **The link:** "Previous Employee Code" holds the old code, and the record is marked system-generated.
- **The code:** it is generated by the normal number series.
- **Not copied:** photo, passport, org assignment, salary, bank and login. HR enters those as for any new joiner.
- **The old employee is left as it was** (separated).

For each balance that IS carried forward, legacy opens the balance on the new employee:

- **Leave:** a Time Transfer of the old balance on the rejoin date.
- **Indemnity:** a new indemnity quota equal to the old balance, with deduction starting 9 months later (Kuwait rule).
- **Ticket:** a new ticket quota the same way.
- **Loans:** one new loan per loan type, combining the old balances and future instalments. It runs for 2 years from rejoining.

Unticked balances are simply not brought over.

Guards: once a new employee exists for a rejoin, that rejoin cannot be edited or deleted. Full & Final settlement only counts rejoins that **kept** the old code when it works out where service restarts. Duplicate Civil ID and Access ID checks ignore the employee's own previous code.

**Legacy oddities to note** (parity says keep, but you should know):
- The new employee's Arabic full name is copied from the **English** full name.
- The "zero ticket balance" adjustment uses the **indemnity** balance figure, not the ticket balance.
- Employment Type is not copied to the new employee, so the code comes from the default number series.

Legacy files read:
- `HCM10.Module\BusinessObjects\PersonnelAdministration\EmployeeRejoin.cs` (SaveEmployeeInformation, ValidatePreviousEntry, UpdatePreviousValues)
- `HCM10.Module\BusinessObjects\PersonnelAdministration\EmployeeFnFPayment.cs` (around lines 714 and 1077)
- `HCM10.Module\BusinessObjects\PersonnelAdministration\Employee.cs` (code generation, duplicate Civil ID rule)

### (b) What HCMv3 has today

- **The rejoin screen is record-keeping only.** The four carry-forward ticks are stored but do nothing. There is no "Create New Employee Code" field, and the employee is not even made Active again.
- **Full & Final settlement assumes every rejoin kept the same employee.** It dropped the legacy filter for that reason.
- **Several pieces we need already exist:** employee codes now come from the number series per employment type (`EmployeeCodeGenerator`), the "Previous Employee Code" field exists, and all the balance records legacy wrote exist in HCMv3. The recruitment "Joined" hand-off already creates an employee from another record, which is the same pattern we need.

### (c) Proposed design

**The tick box.** We add "Create New Employee Code" to the rejoin form, with a migration that defaults it to off for existing rows.

**A new Rejoin Service** (domain service, new file) runs when a rejoin is saved:
- **Keep-code branch:** make the employee Active and write the zeroing records for unticked balances.
- **New-code branch:** create the new employee with the legacy field list and `EmployeeCodeGenerator`, set Previous Employee Code, and open the carried-forward balances exactly as listed above.

Both branches run in one transaction, so it all happens or none of it does.

**On edit or delete,** the service undoes its own zeroing records, as legacy did (`UpdatePreviousValues`). Once a new employee exists, it refuses to edit or delete, with legacy's message.

**Full & Final.** We restore the legacy filter: only keep-code rejoins move the service start date. Without it, the old employee's later settlement would be calculated from the wrong date. This is a small, test-pinned change in `FullAndFinalSettlementService`.

**Duplicate checks.** The Civil ID and Access ID checks for employees ignore the previous code, as in legacy, so the new record can be saved.

**The link.** The employee screen shows "Previous Employee Code" as a link to the old record. Nothing is copied back.

**Builds on.** The existing number series and code generator, ABP unit of work (all-or-nothing), and ABP entity history (the trail of what was changed).

**Genuinely new.** One field, one domain service, and two small rule changes.

### (d) Decisions for you

1. **Should the new employee's Arabic full name copy the English name, as legacy did?** I recommend **fixing** it to copy the Arabic name. It is a data copy, not a payroll figure, and a wrong Arabic name prints on letters. Say "keep" if you want strict parity.
2. **Should the "zero ticket balance" use the indemnity balance, as legacy did?** I recommend **fixing** it to use the ticket balance. Legacy's version zeroes the wrong amount. This one does touch money, so it needs your explicit yes.
3. **Should the new employee take the old one's Employment Type** (so the code comes from the right series), or stay blank as legacy? I recommend copying it. HCMv3 now numbers per employment type, and a blank type gives the fallback number.
4. **Should the keep-code branch (reactivate and zero balances) be built at the same time?** It is the same flags and the same service. I recommend yes. Otherwise half the rejoin screen still does nothing.
5. **Should the new employee also copy the current org assignment, salary profile and bank?** Legacy did not. I recommend no, to match legacy: HR enters them as for a new joiner, and the screen will say so.

### (e) Size and risks

**Size: M.**

Risks:
- Money moves through the indemnity, ticket and loan carry-forward. Each branch is pinned with a test against the legacy rules, including the 9-month deduction start and the merging of loans by type.
- Existing rejoin rows were saved with no side effects. The new service runs on new saves only. We will not run it backwards over old rows.

### (f) What will NOT be built

- No automatic copy of salary, org assignment, bank, documents, dependants or photo.
- No new login user for the new employee. The admin links one as today.
- No re-running over rejoins already saved.

---

## 3. Notification rule engine

### (a) What legacy did

Legacy had one daily background job ("SendRealTimeNotification", Hangfire, once a day). It ran a long list of fixed notifications:

- employee and company document expiry, warning a number of days before expiry set per document type
- pending-request reminders
- leave return date, birthday, anniversary, advance leave pay, separation, leave resumption, punch during leave, probation
- news, annual leave balance over the limit
- leave-not-returned and absence violations

Then it ran the **generic rules** from the **Notification Master** screen. Each rule has:

- **Notification Type:** any table that has an Employee field, for example Employee Document, Leave, or Disciplinary.
- **Notification Sub Type:** a pick list that changes with the type, such as Absence Types for leave, Document Types for documents, or Disciplinary Types.
- **Criterion:** a filter built in the XAF filter editor.
- **Notification Config:** who receives it and whether it goes by in-app notice and/or email.
- **Enabled.**

For each enabled rule, the job finds records of that type where "Notification Sent" is false and the criterion matches. It sends the notification and/or email, then sets "Notification Sent" on the record.

**Legacy oddities:**
- The **Sub Type is never used** when the rule runs. It only shows on the screen, and the real filtering is in the criterion.
- "Notification Sent" is **one flag on the record**, shared by every rule and by the fixed notifications. If two rules target the same table, the first one to fire stops the second from ever firing.
- Errors are swallowed silently.

Legacy files read:
- `HCM10.Module\BusinessObjects\Services\NotificationService.cs` (around lines 39–565 for the daily job, and 1947 for `SendNotification_Generic`)
- `HCM10.Module\BusinessObjects\SysSecurity\NotificationMaster.cs`
- `HCM10.Web\Startup.cs` (around line 65, the Hangfire schedule)

### (b) What HCMv3 has today

- **Notification Master is table and screen only.** The type is free text, there is no sub type, the criterion is free text, and nothing ever reads it.
- **Delivery is built:**
  - Notification Config and Notification Group, with `NotificationRecipientResolver` working out who receives it.
  - `IEmailService.SendEmailForNotificationConfigAsync` sends by email template.
  - Zero's `IAppNotifier` (the bell) is already used by workflow notifications.
- **Scheduled work runs in ABP periodic workers.** Hangfire is referenced but switched off. The workers are:
  - `DailyNotificationWorker` (every 4 hours): birthday, anniversary, advance leave pay, leave resumption, punch during leave, leave-not-returned, absence violation, document overdue
  - `WorkflowEscalationWorker`: escalation and pending reminders
  - `ReportScheduleDispatcherWorker`, `PayrollEngineAutorunBackgroundWorker`, `TimeEngineAutorunBackgroundWorker`, and the sync workers
  - all built on `RecordedPeriodicBackgroundWorkerBase`, so the Background Jobs screen shows each run
- **Still not running from the legacy fixed list:** employee document expiry, company document expiry, probation, separation, annual leave balance over the limit, and news.
- **HCMv3 already has a business rule engine** with a formula editor, which safely evaluates conditions over a whitelisted set of fields.

### (c) Proposed design

**Rule sources: a fixed, whitelisted list instead of "any table".** "Notification Type" becomes a choice from a list registered in code: Employee Document, Company Document, Leave, Disciplinary, Employee, Loan, and so on. Each source declares:
- which records it reads
- the date it is about
- its sub type list (Document Type, Absence Type, Disciplinary Type and so on), which **now actually filters**
- its fields for the criterion
- how the employee is found, so recipients resolve as they do today

This is the same whitelist idea the business rule engine uses. We cannot safely allow "any table", and legacy's version depended on XAF.

**Criterion: the existing business rule formula editor.** For example: `DaysUntil(ExpiryDate) <= 30 AND IsActive`. We add a "Notification" rule purpose, with no new formula language and no second editor. Rules with no criterion match every record of the type and sub type.

**Run once per record per rule.** Instead of the shared "Notification Sent" flag, we add a small new log table: rule, record and date sent. It stops repeats without touching the source tables. It also lets two rules on the same table both work, and gives the admin a "sent" history. (See question 2. This is a deliberate change from legacy.)

**Schedule.** One more line in the existing `DailyNotificationWorker`: "run notification rules" for each tenant, in its own unit of work, and recorded on the Background Jobs screen. We do not use Hangfire.

**Delivery: Zero first.**
- **In-app:** Zero `IAppNotifier` and the notification bell. Each rule source is registered as a Zero notification definition, through a **new** notification provider class plus a one-line registration. Users can then switch a type on or off in Zero's own "Notification settings".
- **Email:** the existing `IEmailService` and email templates, through the rule's Notification Config.
- **Recipients:** the existing `NotificationRecipientResolver`.

**The six fixed legacy notifications still missing** (document expiry ×2, probation, separation, annual leave limit, news) are ported **as-is** into the same daily worker, with their legacy rules. We do not turn them into generic rules: their behaviour is known and customers rely on it.

**Builds on.** ABP periodic background workers (and the recorded base), Zero `IAppNotifier`, notification definitions and user subscriptions, Zero email templates and `IEmailService`, the HCMv3 business rule engine, and Notification Config and Group.

**Genuinely new.** The source registry (about 6 sources to start), the log table, the rule runner, and the reworked Notification Master screen with a type picker, a sub type picker and the formula field.

### (d) Decisions for you

1. **Which sources should the first release cover?** I recommend six: Employee Document, Company Document, Leave, Disciplinary, Employee (dates such as probation and contract end), and Loan. More can be added later, one small class each.
2. **Should a rule fire once per record per rule (new log table), or keep legacy's single shared "Notification Sent" flag?** I recommend once per rule. Legacy's flag silently blocks a second rule, and notifications are not payroll figures.
3. **Should we also port the six missing fixed notifications as-is in this build?** I recommend yes. Document expiry is the one customers will notice first.
4. **The criterion moves from XAF's filter text to the business rule formula, so legacy criteria cannot be imported as they are. Who rewrites them?** I recommend that each tenant re-enters them during go-live (a `Setup:` line), helped by examples in F1 help.
5. **Should a rule repeat** (for example "remind every 7 days until renewed")? Legacy could not. I recommend not now, but the log table allows it later.

### (e) Size and risks

**Size: L.** Roughly: source registry and runner M, screen S, the six fixed ports M.

Risks:
- **A broad rule could email hundreds of people on its first run.** The screen will show a "would match N records today" preview before enabling.
- **Old records will fire on day one.** The first run after enabling sends for every matching record that is not yet logged. We offer "mark existing as sent" on enable (question for go-live).
- **Time zone.** "Today" is the tenant's date, as in the other workers.

### (f) What will NOT be built

- No "any table" type picker and no XAF-style criteria text.
- No SMS or WhatsApp channel.
- No mobile push. Mobile would read the same Zero notifications.
- No Hangfire.
- No change to Zero's own notification screens.

---

## 4. Licensing

### (a) What legacy did

A security setting, **Enable License Check**, turns licensing on. A **License Configuration** record holds:

- **Contract dates:** contract from and to, stored encrypted so the customer cannot edit them.
- **Licence type:** Perpetual or Subscription.
- **Grace days.**
- **Reminder days** and "send email reminder".
- **Licensed to / licensee email.**
- **A dashboard message.**

Its status works out to Pending, Active, In Grace or Expired.

It has one line per **module**: HRMS (admin app), ESS, Mobile App, Active Employees, Performance and Recruitment. Each line has Active or Inactive, Limited or Unlimited, and a total number of licences.

**ApplicationUserLicense** assigns a module to a user. The **License Assignment** screen (`uscLicenseAssignment`) lets an admin tick users per module, filter assigned and unassigned, and see used versus available.

Enforcement:
- **At login (admin app and ESS):**
  - The contract must be Active or In Grace, unless perpetual. Otherwise login is refused: "Contract Not Active" or "Subscription Contract has Expired… contact your GBS Account Manager".
  - The user must have that module assigned.
  - The module must be Active and not over its count.
  - System and super-admin users are not counted.
- **When a user is created for an employee:** ESS and Mobile licences are assigned automatically if seats are free.
- **"Active Employees":** this counts active employees, not users, and is checked when an employee is activated.
- **A banner** in the admin app shows the contract status and the days left.

Legacy files read:
- `HCM10.Module\BusinessObjects\SysSecurity\LicenseConfiguration.cs`, `LicenseConfigurationDetail.cs`, `ApplicationUserLicense.cs` and `LicenseModule.cs`
- `HCM10.Module\BusinessObjects\Services\LicenseManagerService.cs`
- `HCM10.Module\BusinessObjects\SysSettings\ManageSecuritySettings.cs`
- `HCM10.Web\UserControls\Administration\uscLicenseAssignment.ascx.cs`
- `HCM10.Web\MyDefaultVerticalTemplateContent1.ascx.cs` (the banner)
- `HCM10.ESS\Controllers\AccountController.cs` (around line 710)
- `HCM10.Module\Module.cs` (around line 174)
- `HCM10.Module\BusinessObjects\PersonnelAdministration\Employee.cs` (around lines 2158 and 2381)

### (b) What HCMv3 has today

**Nothing HCM-specific.** Zero itself provides:

- **Editions** with **Features**. Today the features are only Zero's own: Max User Count and Chat.
- **Max User Count** is enforced when a user is created (`UserPolicy`).
- **Subscriptions:**
  - an end date per tenant
  - trial days
  - a grace period after expiry ("waiting days after expire") per edition
  - an optional "drop to this edition when it expires"
  - a daily worker that deactivates the tenant or moves its edition when the subscription ends
  - reminder emails a set number of days before expiry
  - a subscription warning bar in the layout

Only the host (GBS) can change a tenant's edition, features or subscription. A tenant admin cannot.

### (c) Proposed design

**Option A (recommended): Zero Editions + Features + Subscription, plus one small HCM check.**

| Legacy need | How Zero meets it |
|---|---|
| Contract to-date, Subscription vs Perpetual | Tenant subscription end date. Perpetual means no end date. |
| Grace days | Edition "waiting days after expire" |
| Reminder days, reminder email | Zero setting "subscription expire notify day count" and Zero's expiry emails |
| Expired: block login | Zero deactivates the tenant at the end of grace, and inactive tenants cannot log in |
| Dashboard banner | Zero subscription warning bar |
| Module Active/Inactive (HRMS, ESS, Mobile, Performance, Recruitment) | New **on/off features**: `HCM.Module.Ess`, `HCM.Module.Mobile`, `HCM.Module.Performance`, `HCM.Module.Recruitment`, and so on. Menus, permissions and app services already understand features: a permission or menu item can depend on a feature, and an app service can require one. |
| Seats for the admin app (users) | Zero **Max User Count**, as it is |
| "Active Employees" limit | New numeric feature `HCM.MaxActiveEmployees`, checked by HCM's own employee save, activation and rejoin code |
| Encrypted dates the customer cannot edit | The dates live in the host database and only the host can edit them |

All new features are defined in a **new** feature provider file, registered with a one-line hook in the module start-up. Zero's Editions screen then shows them with no change to it.

**What Zero cannot cover:**
1. **Per-user module assignment.** Zero cannot say "these 40 users may use ESS, these 15 the admin app", or count seats per module. Its user limit is one number for all users, checked only when a user is created.
2. **A contract start date ("Pending").** Zero has an end date only. In practice the host simply does not activate the tenant until the start date, so I suggest we do not build this.

**Option B (optional add-on): per-user module seats.** This is the version you approved earlier. It covers gap 1 with:

- A numeric feature per module for the seat count, for example `HCM.Seats.Ess` = 500 and `HCM.Seats.Admin` = 25. The counts still live in Zero editions, set by the host.
- A new **User Module Licence** table: user and module. It is like legacy's ApplicationUserLicense.
- A new **Licence Assignment** screen in HCM's own menu, not inside Zero's Users screen. It lists users with a tick per module, shows "used / available" per module, and refuses a tick when no seat is left. Admin and host users are not counted, as in legacy.
- **When a user is linked to an employee,** ESS and Mobile seats are assigned automatically if free, as legacy did. This hooks into HCM's own employee-user link code, not Zero's.
- **At login,** one hook line in Zero's token login calls the HCM seat check: "user must hold a seat for the app they are signing into (admin app, ESS or Mobile)". This is the only touch on a template file.

**My recommendation:** build **Option A now**, which meets the business need with Zero and is small. Add **Option B** only if a customer contract really sells seats per module. If GBS sells "N ESS users + M admin users", B is needed. If it sells "N employees" plus an edition, A is enough.

### (d) Decisions for you

1. **Do GBS contracts sell seats per module (ESS users, admin users, mobile users), or employees plus an edition?** This decides whether Option B is needed. I recommend Option A first, and B only if per-module seats are in real contracts.
2. **Should the "Active Employees" limit be a hard block** (refuse to activate) or a warning? I recommend a hard block, as legacy.
3. **For on-premise customers, where the customer runs the server and holds the host login, is "only the host can edit" enough?** Legacy's encryption was the only protection there. I recommend yes for now: GBS keeps the host password on on-premise installs. If not enough, we add a signed licence file later as a separate decision.
4. **When the subscription ends after grace, should the tenant be deactivated (Zero default) or moved to a smaller "read-only" edition?** I recommend the Zero default, deactivate. It matches legacy's "Expired: login refused".
5. **Should we skip the "Pending" contract start date** and handle it by not activating the tenant until it starts? I recommend yes, skip it.

### (e) Size and risks

**Size:** Option A is **S** (about 6 features, one numeric check, feature dependencies on menus and permissions). Option B is **M**.

Risks:
- **Existing tenants lose access to modules unless their edition switches them on.** Every new module feature defaults to **on**, so nothing is switched off by the upgrade. The host then turns off what was not sold (a `Setup:` line).
- **Option B's login hook** touches a Zero template file (one line). It must be re-applied on a Zero upgrade.
- **Zero's Max User Count** counts all users, including deactivated ones. Legacy counted only active users. If that matters, Option B's seat counting handles it. Otherwise it is accepted as Zero behaviour.

### (f) What will NOT be built

- No copy of legacy's License Configuration screen. Zero's Editions and Tenants screens replace it.
- No encrypted licence dates or licence file.
- No change to Zero's Users, Roles, Tenants or Editions screens.
- No payment or online purchase flow. Zero's payment pages are left as they are and unused.

---

## Summary

| # | Feature | Recommendation | Size | Decisions needed |
|---|---|---|---|---|
| 1 | Appraisal review workflow | Port the per-step flags as an extension table. Auto-add reviewers when approvers are assigned. Add Copy KPI, Save and Submit Review, and goal-form order and window rules on the server. Add a new ESS appraisal screen. Built on the HCMv3 workflow engine and ABP entity history. | L | Own table? Keep manager-only sign-off? Reviewers stay after send-back? Bulk launch in ESS? Agree ESS mock-up first. |
| 2 | Rejoin with new employee code | New Rejoin Service with both branches (new code / keep code) and legacy carry-forward. Restore the Full & Final filter. Uses the number series code generator. | M | Fix Arabic name copy? Fix ticket-zeroing amount? Copy Employment Type? Build keep-code branch too? Copy no org or salary data? |
| 3 | Notification rule engine | Whitelisted rule sources with a working sub type, criterion in the business rule formula editor, a per-rule sent log, and a run in the existing daily ABP worker. Delivery through Zero IAppNotifier, notification subscriptions and email templates. Port the 6 missing fixed notifications as-is. | L | Which first sources? Per-rule log vs shared flag? Port the 6 fixed ones now? Who rewrites criteria? Repeats later? |
| 4 | Licensing | **A:** Zero Editions + new module on/off features + Max User Count + Zero subscription, grace and reminders, plus an HCM Max Active Employees feature. **B (optional):** per-user module seats with an HCM assignment screen and a one-line login hook. | A: S, B: M | Are per-module seats sold? Hard block on employee limit? Is host-only enough on-premise? Deactivate at expiry? Skip "Pending"? |
