# Field-Level Security — Design

Status: proposal, 2026-10-18. Nothing built yet.

## 1. What we are solving

Today a role either sees a whole screen or nothing. We want: one role sees all 10 fields of the
Employee screen, another sees only 5, and every screen (masters, transactions, ESS requests,
workflow approvals) respects that automatically. No runtime creation of new fields.

## 2. The idea in one paragraph

The code stays the single source of truth for *what fields exist* (the DTO classes already carry
name, data type, length and mandatory). At start-up the application reads those classes once and
writes them into a **Field Catalog** table, exactly the way `ScreenCatalog` is written into the
`Screens` table today. A tenant administrator then sets **Field Access Rules** (role × field →
Hidden / Read-only / Editable) on one new screen. The server strips hidden fields out of every
response and refuses writes to them; Angular hides or disables the inputs. Access lists are
cached, so a screen that has no restrictions costs nothing extra.

## 3. New tables

| Table | Level | One row per | Columns |
|---|---|---|---|
| `AppScreenFields` (Field Catalog) | Host (shared by all tenants) | screen × field | ScreenKey (links to `Screens.ScreenKey`), FieldName, DisplayNameKey, DataType, MaxLength, IsSystemMandatory, IsKey, IsRetired, SortOrder |
| `AppFieldAccessRules` | Tenant | role × field | TenantId, ScreenFieldId, RoleId, Access (0 Hidden / 1 ReadOnly / 2 Editable) |

Rules: default is Editable when no row exists. If a user has several roles, the most permissive wins
(same as ABP permissions). Rules are per role only in this version; a per-user override column can be
added later. `IsKey` and
`IsSystemMandatory` fields cannot be set to Hidden on an edit form — the catalog screen greys the option
out, and the seeder marks them.

No new field can be created from the UI. Rows come only from the seeder.

## 4. How the catalog is filled and kept up to date

1. A DTO is tagged once with which screen it belongs to:
   `[ScreenFields("app.admin.employees")] public class CreateOrEditEmployeeDto`.
   (The key is the same `ScreenCatalog` key, so a screen rename does not break the link.)
2. On start-up a `FieldCatalogSeeder` (same place `ScreenSeeder` runs) reflects over every tagged DTO
   and compares with `AppScreenFields`:
   * new property → new row;
   * changed type/length/mandatory → row updated;
   * property removed from the class → row marked `IsRetired` (its access rules are kept but ignored,
     so a field that comes back keeps its rules; nothing is deleted).
3. A unit test lists every DTO that has a `Pages.*` permission and fails if it has no `[ScreenFields]`
   tag — so a developer cannot forget to tag a new screen.
4. The Tenant Version Check screen already lists new screens; it will list new catalog fields the same
   way ("3 new fields on Employees, access rules default to Editable").

So when a developer adds a field to a class: tag is already there, build, start → the row appears.
Nothing to do by hand.

## 5. Can `ScreenCatalog` and the rule engine read from this table?

**ScreenCatalog — yes, it becomes the parent.** `ScreenCatalog` (548 screens) stays as it is; the Field
Catalog hangs below it by `ScreenKey`. One screen → many fields. No change to the Screens screen.

**Rule engine — partly, and deliberately not fully.** The rule engine lists fields through a compiled
whitelist (`RuleBaseObjectBuilder`: each field is declared with a typed getter/setter so a rename breaks
the build rather than a live rule). That safety is worth keeping, so the rule engine will **not** load
its field list from the table. What it *will* share:

* display name and data type: the rule builder reads them from the Field Catalog row instead of its
  own copy, so a label is maintained once;
* the rule builder hides fields the current user is not allowed to see (uses the same access list).

Later, if wanted, the rule providers can be generated from the Field Catalog tags; that is a separate
step and not needed for security.

## 6. Where enforcement happens (server first)

1. **Access resolver** (Core): `IFieldAccessResolver.GetForScreen(screenKey)` returns the restricted
   fields for the current user. Result cached per (tenant, user, screen) in the ABP cache, cleared when
   a rule or role assignment is saved. Same pattern as `BusinessRuleCache` / `FeatureValueStore`.
2. **Output filter** (Application): one ABP interceptor on app services whose DTOs are tagged. After the
   method returns, hidden properties are set to null (lists included). Read-only fields are left in.
   It runs only when the resolver says this user has at least one restriction on that screen —
   otherwise it returns immediately.
3. **Input guard** (Application): before Create/Update, a hidden or read-only field that differs from
   the stored value is rejected with a friendly message. This stops anyone using Swagger to bypass the UI.
4. **Angular**: the access list for all screens the user can open is sent once at login inside the
   existing user-configuration payload (next to permissions). A directive `[hcmField]="'BasicSalary'"`
   on an input or grid column hides / disables it. No extra HTTP call per screen.

## 7. Screens impacted and created

**Created (2 screens, both under Administration, with permissions and EN + AR help pages):**

| Screen | Purpose |
|---|---|
| Field Catalog | Read-only list of screens → fields with type, length, mandatory; retired fields shown struck through. |
| Field Access Rules | Pick a role (or user), pick a screen, set Hidden / Read-only / Editable per field in a grid. Copy rules from another role. |

**Impacted (existing screens that get the directive):**

| Phase | Screens | Why first |
|---|---|---|
| 1 — pilot | Employees (`SPA007`, 60 fields on the edit form) | Largest master, the example in the request. |
| 2 | Workflow Request Details dialog + Approvals list | An approver must not see a field hidden for their role, including the "changed fields" view (PreviousValue / IsChange). |
| 2 | All 23 ESS screens | They share one Details-dialog pattern, so the directive goes in once. |
| 3 | Remaining masters and transactions from the catalog, module by module | Mechanical: tag the DTO, add the directive to the form. |

No screen changes layout; a hidden field simply does not render, a read-only field renders disabled.

## 8. Performance — load time must not get slower

| Point | Cost | Why it is safe |
|---|---|---|
| Start-up seeding | One reflection pass over tagged DTOs (≈ 550 screens × ~20 fields ≈ 11k rows), diffed against the table. | Runs once per start, like `ScreenSeeder`; measured in milliseconds to low seconds. Not on any user path. |
| Login | Access list added to the existing configuration call. | Only *restricted* rows are sent (default is Editable and not sent). A tenant that restricts 200 fields sends ≈ 10 KB once. Cached in the browser session. |
| Opening a screen | Zero extra HTTP calls. | Directive reads from memory. |
| Each API call | Interceptor asks the cached resolver "any restriction for this user on this screen?" | Cache hit is a dictionary lookup. No restriction → interceptor exits. With restriction → property nulling via compiled setters (cached per type), microseconds per row. |
| Database | No change to queries. | Hidden columns are removed from the *response*, not from SQL, so no new joins and no index work. |
| Cache invalidation | Only when a Field Access Rule or a user's roles change. | Rare administrative events. |

Grid lists keep their paging exactly as now.

## 9. Build steps (in order)

1. **Core** — `ScreenField` entity, `FieldAccessRule` entity, `[ScreenFields]` attribute, `FieldCatalogSeeder`,
   `IFieldAccessResolver` + cache. EF migration. Tests for the seeder diff (add / change / retire).
2. **Application** — `FieldCatalogAppService` (list), `FieldAccessRulesAppService` (get for role/screen,
   save, copy), permissions `Pages.Administration.FieldCatalog` and `Pages.Administration.FieldAccessRules`,
   localization keys. Tests.
3. **Application** — output interceptor + input guard, registered for tagged DTOs only. Tests: hidden
   field nulled on Get and GetAll; write to hidden field rejected; untagged service untouched.
4. **Angular** — two new screens, menu entries, `hcmField` directive, access list in login payload,
   proxies regenerated, help pages EN + AR, screen codes added to `ScreenCatalog`.
5. **Pilot** — tag `CreateOrEditEmployeeDto` and the Employee list DTO, add the directive to the Employee
   form and grid. Browser test with two roles (one sees all 60 fields, one sees a reduced set).
6. **Roll-out** — Workflow Request Details dialog and changed-fields view first, then ESS screens, then
   remaining modules. One commit per module.
7. Rule engine reads display name / type from the catalog (optional, last).

Commit `Setup:` line for step 5: "Setup: Field access defaults to Editable; set rules under
Administration → Field Access Rules only where a role must see less."

## 10. Decisions taken (2026-10-18)

1. **Rules per role only.** `AppFieldAccessRules.UserId` is dropped from the first version; the column
   can be added later without redesign. Most permissive role wins.
2. **Security only.** No tenant-level "mandatory" switch. Mandatory stays as the code defines it.
3. **Exports yes, reports later.** Excel exports go through the same app services, so the interceptor
   covers them. Printed reports keep their columns until a later step.
4. **Pilot = Employees only.** Step 5 above shrinks to the Employee form and grid with two roles. The
   Workflow Request Details dialog and changed-fields view move to the start of step 6, ahead of ESS.
