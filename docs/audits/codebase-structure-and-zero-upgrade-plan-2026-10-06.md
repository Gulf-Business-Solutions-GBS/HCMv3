# HCMv3 code structure review and ASP.NET Zero upgrade-safety plan

Date: 6 October 2026. Status: **plan only, awaiting approval. Nothing in this plan has been carried out.**

The single change made during the review was switching off the unused GraphQL endpoint
(`HCMv3.Web.Core/Common/WebConsts.cs`), at the owner's request. It was not built, run or committed.

## How this was produced

- File and class counts: counted by script over the whole repository.
- Template drift: `git diff` between the first commit (`32582fc3`, the untouched ASP.NET Zero 15.2.0 template) and the current code.
- Judgement: six `code-reviewer` runs (domain layer, application services, DTOs, database/reports/tests, legacy comparison, Angular), then one `backend-architect` run and one `code-reviewer` run against the owner's constraint below.
- The reviewers were read-only. Where a point was inferred from file names and not from reading the code, it is marked "not verified".

## The owner's constraint

> I don't want to break any standard ASP.NET Zero pattern and conventions. Tomorrow if ASP.NET Zero
> gives any new updates it should be possible to update our code base.

Everything below is judged against that.

## 1. Where the code stands today

### Size

| Project | Files | Classes | Interfaces | Enums | Top-level folders |
|---|---|---|---|---|---|
| Core (entities, domain services) | 1,136 | 1,213 | 175 | 8 | 65 |
| Core.Shared (enums, constants) | 199 | 56 | 2 | 159 | 40 |
| Application (app services) | 980 | 1,204 | 26 | 6 | 74 |
| Application.Shared (DTOs, interfaces) | 2,586 | 2,649 | 758 | 50 | 68 |
| Reports (98 reports, 27 subreports) | 285 | 300 | 1 | 0 | 8 |
| EntityFrameworkCore | 20 (+126 migrations) | 23 | 0 | 1 | |
| Web.Core / Web.Host / Web.Public | 97 / 36 / 13 | | | | |
| Maui (mobile ESS app) | 100 | 103 | 9 | 1 | |
| Tests | 930 | 988 | 0 | 0 | 61 |
| Angular | 1,410 TypeScript, 1,220 HTML | | | | |

Back end total, migrations excluded: 6,467 C# files. Per-module counts are in Appendix A.

### Drift from the ASP.NET Zero template

- About 1,800 template source files at the baseline; **140 modified, 44 deleted**.
- Package versions are in step with 15.2.0 (Abp 11.1.0). No template folder was renamed or moved.
- Product code follows the ASP.NET Zero shape: class names, base classes, folder layout, permission pattern, `CreateOrEdit` / `Delete` / `Get...ForEdit`.
- Of the modified files the convention audit classed 33 as additive, 62 as invasive (template logic edited) and 27 as cosmetic or configuration. Of the 62 invasive, 49 are client-side (Angular and the mobile app).

Product code sitting inside template files is the main obstacle to an update:

| Template file | Product lines added inside it |
|---|---|
| `Localization/HCMv3/HCMv3.xml` | 6,179 |
| `Localization/HCMv3/HCMv3-ar.xml` | 6,297 |
| `angular/src/app/app.routes.ts` | 4,613 |
| `angular/src/app/shared/layout/nav/app-navigation.service.ts` | 3,549 |
| `HCMv3.Core.Shared/Authorization/AppPermissions.cs` | 3,098 |
| `HCMv3.Core/Authorization/AppAuthorizationProvider.cs` | 2,731 |
| `HCMv3.EntityFrameworkCore/EntityFrameworkCore/HCMv3DbContext.cs` | 2,559 |
| `angular/src/shared/service-proxies/service-proxy.providers.ts` | 700 |

Template files whose own logic was edited, hardest to re-apply after an update first:

1. `default-layout.component.html` / `.ts` (mobile drawer replaced)
2. `menu-search-bar.component.*` (rewritten as global search)
3. `permission-tree.component.ts` (filter rewritten)
4. `side-bar-menu.component.*`, `header-notifications.component.*`, `UserNotificationHelper.ts`
5. `PrimengTableHelper.ts`, `app-bs-modal.directive.ts` (used by every list and dialog)
6. `TenantManager.cs` (constructor and tenant creation), `EmailSettingsChecker.cs` (rewritten)
7. `HCMv3CoreModule.cs`, `HCMv3WebHostModule.cs`, `Startup.cs` (additive, +117 / +76 / +60)
8. Six template background workers (base class swapped)
9. The mobile app: about 20 template files changed and 21 deleted; it is effectively a fork

## 2. The rule going forward

1. Product code goes in new files.
2. A template file may gain only the word `partial` or a single hook line, marked `// HCM-HOOK`.
3. No moving, renaming, reformatting, re-ordering or deleting of template files or projects.
4. Unused template features are switched off by configuration, not removed.
5. Order of preference when extending the template: a separate provider or service replacement; then a subclass; then a partial class; then one hook line; a recorded patch only as a last resort.
6. Keep a list of every template file that differs from the baseline and why (`docs/upgrade/stock-patches.md`), and a script that fails when an unlisted template file changes.
7. Never rename an entity class or its namespace. Entity full names are stored in tenant data (payroll lock list, audit history), in 758 help files, in the database model snapshot and in bulk import.
8. New product code keeps the template shape: one class per file, namespace matches folder, `Dto` folder, one explicit app service per entity.

Never modify: template app services and DTOs (`Authorization`, `MultiTenancy`, `Editions`, `Configuration`, `Sessions`, `Organizations`), `TokenAuthController` and other Web.Core controllers, template mappers, template tests, template migrations, solution project lists, Angular `account/`, `shared/helpers/`, `shared/layout/themes/`, Metronic assets, `AspNetZeroRadTool/`.

## 3. Verdict on each proposal from the structure review

| Proposal | Verdict | Reason |
|---|---|---|
| Move product code out of the big template files into separate files | Keep, first | The only change that makes updates easier |
| Merge about 1,200 small DTO files into about 300 | Drop | One class per file is the template shape; no update benefit |
| Merge 91 empty lookup entity files | Drop | Same |
| Shared base class retrofitted onto about 400 CRUD services | Drop | Template services are explicit; large regression risk; no update benefit. Keep the existing `SimpleLookupAppServiceBase` |
| Subfolders inside modules with the namespace unchanged | Change | Folder must match namespace. Entities stay put; subfolders only for services and tests, namespace following |
| Move the template's Angular screens to `app/platform` | Drop | Every template path would change |
| Move HR screens from `app/admin` to `app/hcm/<module>` | Keep, later | Afterwards everything left in `app/admin` is template |
| Shared Angular list component and dialog base | New screens only | No retrofit of the 250 existing lists |
| Split the template's large files (tenant settings, setting provider) | Drop | They are near-original today |
| Delete unused template projects (GraphQL, Web.Public, chat, payments) | Change | Switch off; never delete |
| One shared test fake repository in place of 21 copies | Keep | Product code only |
| Split large product logic files | Keep, low priority | Prefer extraction to domain services; pin payroll and time behaviour with tests first |
| Regroup the Reports "Others" folder; repository clean-up | Keep, last | Product code only |

## 4. Target shape of the hot files

| File | Stays in the template file | Moves out to | Hook left behind |
|---|---|---|---|
| `AppPermissions.cs` | Template constants, original order | `AppPermissions.{Module}.cs` in the same folder | `partial` on the class |
| `AppAuthorizationProvider.cs` | Template definitions | `AppAuthorizationProvider.Hcm.cs` calling one method per module | `partial`, plus one call at the end of `SetPermissions` |
| `HCMv3DbContext.cs` | Template tables, constructor, template model setup | `HCMv3DbContext.{Module}.cs` (tables), `HCMv3DbContext.Hcm.cs` (product overrides), `Configurations/{Module}/` | `partial`, plus one call at the end of `OnModelCreating` |
| `app.routes.ts` | Template routes | `app/hcm/routes/{module}.routes.ts` | One import and two spread lines |
| `app-navigation.service.ts` | Unchanged template | A product menu service that extends it | One provider line |
| `service-proxy.providers.ts` | Template list | `hcm-service-proxy.providers.ts` | One spread line |
| `HCMv3*.xml` | Template text | A second, product-owned dictionary | One line in the localization setup |

Checks before each of these is accepted:

- the set of permission names is identical before and after
- a throwaway database migration comes out empty
- the list of route paths and the rendered menu are identical
- `tools/release-log/build-release-log.ps1` is updated first; it reads `AppPermissions.cs` by exact file name, and a split would silently stop new permissions appearing on the Tenant Version Check screen

Known side effect: the permission tree's display order may change, because product permissions would be registered after the template's.

## 5. Order of work

1. **Baseline.** Commit pending work. Tag `32582fc3` as the template baseline, keep a branch that only ever holds untouched template drops, add the patch list and the drift-check script.
2. **Empty the hot files** (section 4): permissions, permission provider, database context, routes, menu, proxy list. One file per commit, each behind its check.
3. **Undo edits inside template C# files.** Move product code out of `HCMv3CoreModule`, `HCMv3WebHostModule`, `Startup`, `TenantManager`, `EmailSettingsChecker`, `AppSettingProvider`, `OrganizationUnitAppService` into product classes, leaving one or two hook lines.
4. **Undo edits inside Angular layout files.** Rebuild the global search, drawer and layout styles as product components and restore the template files. Revert the whitespace-only reformatting of `angular.json`.
5. **Localization.** Move product text to a product-owned dictionary. Needs a small trial first, and the localization rule in `.claude/rules/claude-instructions.md` and the tools that read the XML must be updated with it.
6. **Rehearse an update** on a throwaway branch against the next ASP.NET Zero release and record the conflicts. This is the real test of steps 2 to 5.
7. **Product-only clean-up**, in this order: shared test fake; large logic files; HR screens to `app/hcm`; restricted subfolders; Reports folders; repository hygiene.

Not doing: DTO file merge, lookup entity merge, CRUD base-class retrofit, `app/platform` move, splitting template files, deleting template projects.

## 6. How an ASP.NET Zero update is done

From the architect's recollection of the vendor's guidance; not checked against the current documentation.

- The template is source code downloaded per version under the project name. Only the framework arrives as packages.
- Method: keep a branch holding the untouched template; download the new version; overwrite that branch; merge it into development. Git then merges three ways with the old template as the base.
- Migrations: the template's model snapshot will always conflict. Keep ours, discard their migration files, and generate one migration of our own for their entity changes.

What would make it hard today: the hot files; the localization files (a duplicate key stops the application starting); the edited layout files; the 44 deleted files; DevExpress pinned at 23.2.13 and held by a patch script, which a newer Angular version is likely to break first.

## 7. Comparison with the legacy HRMS project (HCM10)

- All 24 legacy business modules exist in HCMv3 under the same name. Renamed: SysSecurity to SystemSecurity, SysSettings to SystemSettings.
- Legacy `BusinessObjects`: 629 files. The same modules now span about 4,700 files across four layers.

| Legacy file | Lines | HCMv3 |
|---|---|---|
| `Religion.cs` (simple lookup) | 120 | 3 files, 31 lines |
| `AbsenceType.cs` | 622 | 23 files (8 back end, 15 Angular), about 9 boilerplate |
| `EmployeeAbsence.cs` | 4,414 | about 33 files, about 10 boilerplate |

**Where HCMv3 is better:** no giant classes (legacy had files of 16,533, 10,358 and 8,041 lines; the largest HCMv3 logic file is 1,778); one table per transaction instead of about 50 duplicate request classes; engines are testable services, not console programs; 90 lookups share one base; tenant isolation and audit come from the framework.

**Where legacy is better organised:** workflow request types grouped by area (Action, General, GovtRelations, Leave, Payroll, Time, Travel), where HCMv3 has 21 handlers flat in the Personnel folder; ESS had one folder per feature, where HCMv3 has 28 flat files; the payroll engine was one 10-file project, where HCMv3 has 87 flat files in one folder.

**Legacy habit carried over:** client-specific report copies. Five "Late/Early Log" report services of 1,000 to 1,300 lines each are near-clones.

**Possibly not ported** (searched by name only; may exist under another name): Phone Bill request, Training request, Health Check request, Leave Planner, the Copy features (wage type, absence type, loan type, workflow group, role clone), recruitment onboarding screens, the Appraisal / Template / Violation report folders. The Excel import is far smaller than the legacy 16,533 lines; not determined whether that is a rewrite or a gap.

## 8. Other findings

Confirmed by direct check:

- GraphQL endpoint and playground were switched on in the template defaults. Now switched off in code; not yet built, committed or deployed.
- Two services share the class name `BankFilesAppService` (General Master Info and Payroll), so they are served as one API route. It works only because no method names clash.

Reported by the reviewers, not independently confirmed:

- At least one of the 21 test fake-repository copies ignores deletes, so tests using it can pass while a delete is broken.
- Thin test coverage: General Master Info has 2 test files for 61 services; Organization Management 2 for 11.
- `AbpSession.TenantId ?? 0` appears 429 times; a call made as host would write tenant 0 instead of failing.
- `Get...ForEdit` methods carry only the view permission, not the edit permission.
- One list method lacks `[HttpPost]`: `GetPositionsForOrgAssignment` in `EmployeeOrgAssignmentsAppService`.
- `Application/Common/RawSqlExecutor.cs` reaches the database context from the Application layer, against the project's own layering rule.
- PayPal and Stripe are set active in `appsettings.json`.
- The mobile client's default server address is hard-coded to the Webtropia test server (`ApiUrlConfig.cs`).
- The ASP.NET Zero licence code is committed in `appsettings.json` and the Power Tools configuration.
- The template's database file storage was replaced by a file-system one; an update would restore the template file and could silently switch storage back. Needs a guard test.
- The Power Tools configuration points at four files that do not exist in this project, so its output cannot be relied on.
- `app.routes.ts` line 321 redirects to a `dashboard` route that no longer exists (it became `home`).

## 9. Open decisions

1. Approve the revised direction, with steps 1 and 2 as the first piece of work?
2. Commit the pending leave-screen changes and the GraphQL switch before step 1.
3. Check the "possibly not ported" list properly against the legacy project?
4. Review the payment flags and the committed licence code.

## Appendix A. Files and types per module folder

Files / types (classes, interfaces, enums and records together) in each top-level folder, per project. Blank means the folder does not exist in that project. Folders with fewer than 4 files in total are left out.

| Folder | Core | Core.Shared | Application | Application.Shared | Tests |
|---|---|---|---|---|---|
| PayrollManagement | 184 / 265 | 30 / 30 | 84 / 85 | 342 / 358 | 115 / 143 |
| GridReports |  | 2 / 2 | 167 / 279 | 277 / 483 | 138 / 139 |
| PersonnelAdministration | 145 / 159 | 27 / 27 | 73 / 78 | 153 / 378 | 83 / 85 |
| TimeManagement | 80 / 110 | 7 / 7 | 46 / 46 | 177 / 185 | 55 / 58 |
| GeneralMasterInfo | 63 / 63 | 14 / 14 | 61 / 61 | 177 / 177 | 2 / 2 |
| Workflow | 47 / 55 | 13 / 13 | 20 / 21 | 74 / 106 | 60 / 61 |
| PerformanceManagement | 26 / 26 | 1 / 1 | 26 / 26 | 128 / 130 | 30 / 30 |
| Authorization | 63 / 63 | 5 / 7 | 21 / 21 | 75 / 75 | 28 / 29 |
| SystemSecurity | 30 / 31 | 7 / 7 | 16 / 16 | 80 / 80 | 25 / 25 |
| Recruitment | 17 / 17 | 4 / 4 | 17 / 17 | 85 / 85 | 20 / 20 |
| HealthAndSafetyManagement | 24 / 24 |  | 24 / 24 | 64 / 64 | 27 / 27 |
| ReportManagement | 31 / 37 | 6 / 13 | 16 / 18 | 60 / 72 | 24 / 25 |
| MultiTenancy | 36 / 37 | 11 / 11 | 13 / 13 | 51 / 52 | 4 / 4 |
| SystemSettings | 18 / 19 | 11 / 11 | 16 / 16 | 32 / 49 | 21 / 21 |
| JobProfileManagement | 15 / 15 |  | 15 / 15 | 47 / 47 | 18 / 18 |
| Ess |  |  | 28 / 32 | 47 / 152 | 20 / 22 |
| GovtRelationsManagement | 15 / 15 | 1 / 1 | 12 / 12 | 52 / 52 | 14 / 14 |
| AccommodationManagement | 11 / 11 |  | 11 / 11 | 55 / 55 | 14 / 14 |
| GatePassManagement | 14 / 14 |  | 14 / 14 | 42 / 42 | 17 / 17 |
| CompetencyDevelopment | 10 / 10 |  | 10 / 10 | 50 / 50 | 13 / 13 |
| ProjectManagement | 13 / 21 | 1 / 1 | 11 / 12 | 40 / 42 | 14 / 15 |
| TravelManagement | 13 / 13 |  | 11 / 11 | 37 / 37 | 14 / 14 |
| AiAgent | 9 / 17 | 3 / 3 | 39 / 66 | 2 / 10 | 14 / 19 |
| SuccessionManagement | 14 / 14 | 1 / 1 | 14 / 14 | 20 / 38 | 17 / 17 |
| OrganizationManagement | 11 / 11 |  | 11 / 12 | 41 / 41 | 2 / 3 |
| ManpowerPlanningManagement | 8 / 8 | 2 / 2 | 6 / 9 | 30 / 30 | 9 / 11 |
| GoLiveSetup | 4 / 11 |  | 20 / 35 | 12 / 47 | 18 / 18 |
| Dashboards | 2 / 3 | 1 / 1 | 21 / 59 | 14 / 109 | 14 / 14 |
| Configuration | 10 / 34 |  | 5 / 5 | 35 / 35 | 2 / 2 |
| BusinessRules | 31 / 84 | 5 / 7 | 1 / 1 | 3 / 20 | 9 / 10 |
| Integrations | 25 / 25 | 7 / 7 | 1 / 1 | 8 / 8 | 4 / 9 |
| SurveyManagement | 4 / 4 | 2 / 2 | 5 / 5 | 22 / 27 | 8 / 8 |
| TrainingManagement | 4 / 4 | 3 / 3 | 4 / 4 | 20 / 20 | 7 / 7 |
| BenefitsManagement | 5 / 5 | 2 / 2 | 5 / 5 | 17 / 17 | 8 / 8 |
| Storage | 23 / 30 | 2 / 2 | 2 / 7 | 3 / 7 | 5 / 5 |
| Careers |  |  | 7 / 7 | 21 / 30 | 4 / 5 |
| DashboardCustomization | 16 / 16 |  | 13 / 13 |  |  |
| Notifications | 5 / 5 | 1 / 1 | 4 / 4 | 16 / 16 | 1 / 1 |
| Editions | 3 / 3 | 1 / 1 | 3 / 3 | 16 / 16 | 1 / 1 |
| Friendships | 11 / 11 | 1 / 1 | 1 / 1 | 9 / 9 |  |
| Mappers | 1 / 1 |  | 19 / 83 |  |  |
| Chat | 7 / 7 | 2 / 2 | 3 / 3 | 8 / 8 |  |
| Organizations | 1 / 1 |  | 1 / 1 | 16 / 16 | 1 / 1 |
| Navigation | 5 / 7 | 1 / 1 | 3 / 6 | 6 / 20 | 4 / 4 |
| Localization | 5 / 5 |  | 2 / 2 | 10 / 10 | 2 / 2 |
| (root) | 6 / 6 | 4 / 10 | 2 / 2 | 2 / 2 | 5 / 5 |
| Tenants |  |  | 1 / 1 | 17 / 17 |  |
| Common | 4 / 6 |  | 3 / 3 | 5 / 5 | 6 / 6 |
| EngineRuns | 6 / 8 | 3 / 3 | 1 / 1 | 3 / 3 | 4 / 4 |
| DynamicEntityProperties | 1 / 1 |  | 5 / 5 | 11 / 11 |  |
| Auditing |  |  | 9 / 9 | 6 / 7 | 2 / 2 |
| Administration |  |  | 4 / 4 | 10 / 15 | 2 / 2 |
| Net | 13 / 13 |  |  |  | 1 / 1 |
| ImportExport | 6 / 12 | 1 / 1 | 3 / 6 | 2 / 14 | 2 / 2 |
| WebHooks |  |  | 3 / 3 | 10 / 10 |  |
| SetupChecks | 3 / 7 | 1 / 2 | 2 / 2 | 2 / 7 | 4 / 5 |
| BackgroundJobs | 5 / 8 | 1 / 1 | 1 / 1 | 2 / 6 | 3 / 4 |
| Authentication | 4 / 4 | 8 / 8 |  |  |  |
| Sessions |  |  | 1 / 1 | 9 / 9 | 1 / 1 |
| EntityChanges |  |  | 2 / 2 | 5 / 5 |  |
| ApiDictionary |  |  | 4 / 5 | 2 / 5 | 1 / 1 |
| Timing | 2 / 2 |  | 1 / 1 | 3 / 3 |  |
| Help | 2 / 3 |  | 1 / 1 | 2 / 3 | 1 / 1 |
| HealthChecks |  |  | 4 / 4 | 2 / 2 |  |
| Security | 2 / 2 | 1 / 1 | 2 / 2 |  |  |
| MobileClient |  |  |  |  | 5 / 7 |
| Install |  |  | 5 / 5 |  |  |
| Identity | 5 / 5 |  |  |  |  |
| Gdpr |  |  | 5 / 5 |  |  |
| Features | 4 / 4 |  |  |  | 1 / 1 |
| EffectiveDated |  |  | 2 / 3 | 2 / 4 | 1 / 1 |
| DynamicEntityPropertyValues |  |  |  | 5 / 7 |  |
| DataImporting |  |  | 5 / 5 |  |  |
| DataExporting |  |  | 4 / 4 | 1 / 1 |  |
| Webhooks | 3 / 3 | 1 / 1 |  |  |  |
| ExtraProperties |  | 4 / 4 |  |  |  |
| Dto |  |  |  | 4 / 4 |  |
