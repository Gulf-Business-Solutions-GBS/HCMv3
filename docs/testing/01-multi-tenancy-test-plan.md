# Test Plan 01 — Multi-Tenancy Isolation (Webtropia)

**Target build:** HCMv3 (`feat/ess-workflow-foundation`)
**Environment:** Webtropia hosted deployment
**Scope:** Tenant provisioning, tenant admin + user creation, and proof that one tenant cannot read, write, or infer another tenant's data.
**Tenants under test:** `S180` and `SCC`

---

## Step 0 — Naming rules you must stay inside

No code change is needed for this round. `S180` and `SCC` are both valid, but the rule that makes them valid also constrains three other fields, so read this before filling in any form.

`aspnet-core/src/HCMv3.Core.Shared/MultiTenancy/TenantConsts.cs`:

```csharp
public const string TenancyNameRegex = "^[a-zA-Z][a-zA-Z0-9_-]{1,}$";
```

`CreateTenantInput` applies that regex to **four** fields: `TenancyName`, `Name`, `AdminName` and `AdminSurname`. It means all four must:

- **start with a letter** — a leading digit is rejected, which is why the codes are `S180` / `SCC` and not `060S180` / `061SCC`;
- be **at least 2 characters** long;
- contain only letters, digits, `_` and `-` — **no spaces**, apostrophes or accented characters.

Two consequences that catch people out:

1. The tenant **display name** (`Name`) is validated too, so you cannot set a friendly name like `Company S180 Ltd.` Keep it identical to the tenancy name for this round.
2. **Admin name and surname** cannot contain spaces or apostrophes. Use single-word values.

The same pattern is mirrored client-side in the Angular templates (`create-tenant-modal`, `edit-tenant-modal`, `register-tenant`), so an invalid value is blocked in the form before it ever reaches the API. If you later want tenancy names that begin with a digit, both the C# constant **and** those 9 `pattern=` attributes have to change together, followed by a backend and Angular redeploy.

---

## Step 1 — Pre-flight checks on the server

Run these once. Every later step assumes they passed.

| # | Check | How | Pass criteria |
|---|---|---|---|
| 1.1 | API is up | Browse `{API_URL}/swagger` | Swagger UI renders |
| 1.2 | Angular is up | Browse `{APP_URL}` | Login page renders, no console errors |
| 1.3 | CORS is correct | `App:CorsOrigins` in `appsettings.Production.json` contains the exact `{APP_URL}` origin | Login POST succeeds, no CORS error in the browser console |
| 1.4 | Root addresses | `App:ServerRootAddress` = `{API_URL}`, `App:ClientRootAddress` = `{APP_URL}` | Not the shipped `localhost:9901` / `localhost:4200` defaults |
| 1.5 | DB migrated and host seeded | Log in with no tenant selected: `admin` / `123qwe` | Host dashboard loads |
| 1.6 | Default edition exists | Administration → Editions | A `Standard` edition is listed |
| 1.7 | SMTP status | Administration → Settings → Email (host) | Note whether SMTP is configured — this drives a setting in Step 2 |

> **Change the host `admin` password immediately** if this server is reachable from the internet. The shipped seed password is `123qwe`.

Fill in once and reuse:

- `{API_URL}` = ______________________
- `{APP_URL}` = ______________________

---

## Step 2 — Create the two tenants

Do this **twice**, once per tenant, logged in as the **host** `admin` — the tenant selector must read *"Current tenant: Not selected"*.

1. Log in to `{APP_URL}` as host `admin`.
2. Left menu → **Administration → Tenants**.
3. Click **Create new tenant**.
4. Fill the modal:

| Field | Tenant A | Tenant B | Notes |
|---|---|---|---|
| Tenancy name | `S180` | `SCC` | Must start with a letter — see Step 0 |
| Name | `S180` | `SCC` | Same regex applies — no spaces |
| Use host database | **ON** | **ON** | Keep ON. Shared DB is the harder isolation case and the one you actually ship |
| Edition | `Standard` | `Standard` | |
| Subscription end date | *Unlimited* | *Unlimited* | Tick **Unlimited time subscription** |
| Admin name | `Admin` | `Admin` | Single word — regex |
| Admin surname | `Alpha` | `Bravo` | Distinct per tenant, makes leakage obvious |
| Admin email address | `admin.s180@test.local` | `admin.scc@test.local` | Must be unique across the whole system |
| Set random password | **OFF** | **OFF** | So you know the password |
| Admin password | `Test@12345` | `Test@12345` | Must satisfy digit + lowercase + uppercase |
| Should change password on next login | **OFF** | **OFF** | Otherwise the first login forces a reset and blocks the rest of the run |
| Send activation email | **OFF** | **OFF** | **Critical** — if SMTP is not configured (check 1.7), leaving this ON makes tenant creation throw |

5. **Save.**
6. Repeat for the second tenant.

**Expected:** both tenants appear in the Tenants grid, `Standard` edition, Active.

**Record:**

| Tenant | Tenancy name | Tenant Id | Admin login | Password |
|---|---|---|---|---|
| A | | | `admin` | `Test@12345` |
| B | | | `admin` | `Test@12345` |

> To find the Tenant Id: open the tenant row for edit, or call `{API_URL}/api/services/app/Account/IsTenantAvailable`.

---

## Step 3 — Log in as each tenant admin

For each tenant:

1. Go to `{APP_URL}`, log out if needed.
2. On the login box, find the line **"Current tenant: Not selected ( Change )"** and click **Change**.
3. Type the tenancy name, e.g. `S180` → **Save**.
4. The line now reads **"Current tenant: S180"**.
5. Log in as `admin` / `Test@12345`.

**Expected:** the tenant dashboard loads, and the **Tenants** and **Editions** menu items are **absent** — those are host-only.

> The tenant `admin` user is deliberately not linked to an employee record. Dashboard widgets that need an employee show an empty state, not an error. That is expected behaviour, not a defect.

---

## Step 4 — Create dummy users in each tenant

Do this **twice** — once inside `S180`, once inside `SCC`. You must be logged in **as that tenant's admin**, not as the host admin. Check the top-right user menu shows `Admin Alpha` (S180) or `Admin Bravo` (SCC) before you start.

### 4a — Create a role first

Roles are tenant-scoped, and you need a distinct one per tenant for TC-05.

1. **Administration → Roles → Create new role.**
2. **Role name:** `RoleAlpha` in S180, `RoleBravo` in SCC.
3. **Permissions** tab: tick a few obvious ones (e.g. Employees → View). Exact set does not matter for this round.
4. **Save.**

### 4b — Create the users

**Administration → Users → Create new user.** Create both users below in **each** tenant.

Deliberately use the **same two usernames in both tenants**. That is the test: usernames in ASP.NET Zero are unique *per tenant*, not globally.

| Field | `hrmanager` in S180 | `hrmanager` in SCC | `employee1` in S180 | `employee1` in SCC |
|---|---|---|---|---|
| Name | `Hana` | `Hani` | `Emp` | `Emp` |
| Surname | `Alpha` | `Bravo` | `Alpha` | `Bravo` |
| User name | `hrmanager` | `hrmanager` | `employee1` | `employee1` |
| Email address | `hr@s180.test.local` | `hr@scc.test.local` | `e1@s180.test.local` | `e1@scc.test.local` |
| Phone number | *(leave blank)* | *(leave blank)* | *(leave blank)* | *(leave blank)* |
| Set random password | **OFF** | **OFF** | **OFF** | **OFF** |
| Password / repeat | `Test@12345` | `Test@12345` | `Test@12345` | `Test@12345` |
| Should change password on next login | **OFF** | **OFF** | **OFF** | **OFF** |
| Send activation email | **OFF** | **OFF** | **OFF** | **OFF** |
| Active | **ON** | **ON** | **ON** | **ON** |
| Lockout enabled | ON (default) | ON (default) | ON (default) | ON (default) |
| **Roles** tab | tick `RoleAlpha` | tick `RoleBravo` | tick `RoleAlpha` | tick `RoleBravo` |

Then **Save**.

> **Roles is a separate tab** in the user modal, next to *User informations*. It is easy to save without assigning one — go back and check.

### Field rules that differ from tenant creation

Unlike the tenant form, user **Name** and **Surname** have **no** character restriction — only a length limit. Spaces, apostrophes and Arabic characters are all fine here. Only the tenant-creation form applies `TenancyNameRegex` to its admin name fields.

**Email must be unique across the entire installation**, not just within the tenant — that is why each user above has a different address. If you reuse one, you get a validation error that looks like a tenancy leak but is not one.

### What to expect

| Check | Expected |
|---|---|
| Creating `hrmanager` in SCC after it already exists in S180 | **Succeeds.** Usernames are scoped per tenant |
| Creating `employee1` in SCC | **Succeeds** |
| S180 → Users list | 3 rows: `admin`, `hrmanager` (Hana Alpha), `employee1` (Emp Alpha) |
| SCC → Users list | 3 rows: `admin`, `hrmanager` (Hani Bravo), `employee1` (Emp Bravo) |
| Either tenant's Users list | The **host** `admin` is **not** listed |

**Failure signal:** if the second tenant rejects `hrmanager` with *"username already taken"*, user uniqueness is being enforced globally instead of per tenant. That is a **critical isolation defect** — log it and stop the round.

### Verify the logins work

Before moving on, log out and log in as `hrmanager` / `Test@12345` with the tenant selector set to `S180`. Then repeat with the selector set to `SCC`. Both must succeed and land on their own tenant's dashboard.

**Record:**

| Tenant | User name | User Id | Role | Login verified |
|---|---|---|---|---|
| S180 | `hrmanager` | | RoleAlpha | |
| S180 | `employee1` | | RoleAlpha | |
| SCC | `hrmanager` | | RoleBravo | |
| SCC | `employee1` | | RoleBravo | |

---

## Step 5 — Create tenant-scoped business data

You need real, tenant-owned rows to probe. `Employee` is `IMustHaveTenant`, so it is the right target.

In **each** tenant, as `admin`, create 2 employees with unmistakably tenant-specific names:

| Tenant A | Tenant B |
|---|---|
| `Alpha One` (+ Arabic equivalent) | `Bravo One` (+ Arabic equivalent) |
| `Alpha Two` | `Bravo Two` |

Required fields: first and last name in **both English and Arabic**. Leave `Code` blank — it auto-generates as max+1 *within the tenant*.

**Record the generated Ids and Codes — you need the raw Ids for TC-20:**

| Tenant | Employee name | Id | Code |
|---|---|---|---|
| A | Alpha One | | |
| A | Alpha Two | | |
| B | Bravo One | | |
| B | Bravo Two | | |

### The employee code sequence is per tenant — verify it

`AssignCodeIfEmptyAsync` and the duplicate-code check in `EmployeesAppService` both query through `_employeeRepository.GetAll()`. Because `Employee` implements `IMustHaveTenant`, ABP appends `WHERE TenantId = @currentTenant` to both. So each tenant gets its **own** sequence, and the same code may legitimately exist in both tenants.

| | Tenant A (`S180`) | Tenant B (`SCC`) |
|---|---|---|
| 1st employee | Code `1` | Code `1` |
| 2nd employee | Code `2` | Code `2` |

**Expected:** identical codes across the two tenants, with no "duplicate code" error when you create `Bravo One` in SCC after `Alpha One` already holds code `1` in S180.

**Failure signals — both are isolation defects, log and stop:**

- SCC's first employee is assigned code `3` → the max+1 lookup is seeing tenant A's rows.
- Creating SCC's first employee throws `DuplicateCodeFound` → the uniqueness check is running globally instead of per tenant.

> Side note, out of scope for this round: there is no unique index on `Employee.Code` in the database — uniqueness rests entirely on that application-layer check, so two concurrent creates could still collide within a single tenant. Worth a separate ticket, not a multi-tenancy bug.

---

## Step 6 — Isolation test cases

This is the verdict section. Run every case and record Pass/Fail.

### UI-level tests

| ID | Test | Steps | Expected result | P/F |
|---|---|---|---|---|
| TC-01 | Cross-tenant login rejected | Set current tenant to `SCC`, log in with tenant A's `hr@s180.test.local` / `Test@12345` | Login fails: "Invalid user name or password" | |
| TC-02 | User list isolation | Tenant A admin → Administration → Users | Only Alpha users plus tenant A `admin`. No `Hani Bravo`, no host `admin` | |
| TC-03 | Employee list isolation | Tenant A admin → Employees; then tenant B admin → Employees | A sees only `Alpha *`, B sees only `Bravo *`; counts match what each created | |
| TC-04 | Search cannot cross tenants | In tenant A, search employees for `Bravo` | Zero results — the row must be invisible, not "access denied" | |
| TC-05 | Role isolation | Tenant A → Roles | `RoleAlpha` present, `RoleBravo` absent | |
| TC-06 | Host-only menus hidden | As tenant admin, inspect the left menu | No **Tenants**, no **Editions**, no host **Maintenance** | |
| TC-07 | Direct route guard | As tenant A admin, paste `{APP_URL}/app/admin/tenants` into the address bar | Redirected or "not authorized" — never the tenant list | |
| TC-08 | Settings isolation | In tenant A change the app display name / logo, then log in as tenant B | Tenant B still shows its own values | |
| TC-09 | Audit log isolation | Tenant A → Administration → Audit Logs | Only tenant A user actions; nothing from `Hani Bravo` or the host admin | |
| TC-10 | Host sees both | Host `admin` → Tenants grid | Both tenants listed with correct edition and status | |
| TC-11 | Impersonation | Host admin → Tenants → **Login as this tenant** on tenant B | Lands inside tenant B, sees only `Bravo *`; **Back to my account** returns to host | |
| TC-12 | Deactivate blast radius | Host admin → edit tenant B → untick Active → Save. Try tenant B admin login, then tenant A admin login | B is blocked ("tenant is not active"); **A logs in normally**. Re-activate B afterwards | |

### API-level tests — the ones that actually catch leaks

The UI can hide data the API still returns. Run these with curl, Postman, or the Swagger UI at `{API_URL}/swagger`.

Get a token for tenant A:

```bash
curl -s -X POST "{API_URL}/api/TokenAuth/Authenticate" -H "Content-Type: application/json" -H "Abp.TenantId: {TENANT_A_ID}" -d '{"userNameOrEmailAddress":"admin","password":"Test@12345"}'
```

Save the `accessToken` from the response as `$TOKEN_A`. Repeat for tenant B to get `$TOKEN_B`.

| ID | Test | Request | Expected result | P/F |
|---|---|---|---|---|
| TC-20 | Direct Id probing | `GET .../Employees/GetEmployeeForEdit?Id={TENANT_B_EMPLOYEE_ID}` with `Authorization: Bearer $TOKEN_A` | **404 / EntityNotFoundException.** Any 200 returning Bravo data is a **critical** leak | |
| TC-21 | Cross-tenant update | Post to the Employees `CreateOrEdit` endpoint with `$TOKEN_A` and tenant B's employee Id in the body | Rejected — then verify in tenant B's UI that `Bravo One` is **unchanged** | |
| TC-22 | Cross-tenant delete | `DELETE .../Employees/Delete?Id={TENANT_B_EMPLOYEE_ID}` with `$TOKEN_A` | Rejected; the row is still present in tenant B | |
| TC-23 | Tenant header spoofing | Any list endpoint with `Authorization: Bearer $TOKEN_A` **and** `Abp.TenantId: {TENANT_B_ID}` | Still returns **tenant A** data (the token wins) or 401. Returning tenant B data is a **critical** leak | |
| TC-24 | Token replay after deactivation | Deactivate tenant B, then reuse `$TOKEN_B` on a list endpoint | 401 / rejected | |
| TC-25 | Anonymous access | Any list endpoint with **no** Authorization header | 401, never data | |
| TC-26 | Host endpoint from a tenant token | `GET .../Tenant/GetTenants` with `$TOKEN_A` | 403 authorization failure | |
| TC-27 | Paging leak | List employees with `MaxResultCount=1000, SkipCount=0` using `$TOKEN_A` | `totalCount` equals tenant A's employee count only | |

> **How to read TC-20 and TC-23.** ABP enforces tenancy through an EF Core global query filter driven by the ambient `AbpSession.TenantId`. Any code path that calls `IgnoreQueryFilters()`, uses raw SQL, or opens its own `DbContext` bypasses that filter. Those are exactly the paths TC-20 through TC-27 are designed to smoke out — a UI-only round will not find them.

---

## Step 7 — Evidence and sign-off

For every failed case capture: the request (URL, headers minus the token secret, body), the full response, a screenshot, and the matching server log excerpt from the Webtropia box.

**Round summary**

| Metric | Value |
|---|---|
| Date executed | |
| Build / commit | |
| Cases run | 20 |
| Passed | |
| Failed | |
| Critical leaks (TC-20, TC-21, TC-22, TC-23) | |
| Verdict | Ship / Block |

**Exit criteria:** TC-20, TC-21, TC-22 and TC-23 all pass. A failure in any one of those blocks the release regardless of the rest.

---

## Step 8 — Cleanup

The test tenants share the host database. Once the round is signed off:

1. Re-activate any tenant you deactivated in TC-12.
2. Host admin → Tenants → delete both test tenants (soft delete — rows remain with `IsDeleted = 1`).
3. Confirm the host `admin` password is no longer `123qwe`.

---

## Next rounds (out of scope here, worth queueing)

- **02 — ESS workflow across tenants:** approval chains must not resolve approvers from another tenant.
- **03 — Reports and file drops:** `ReportDistribution:FileDropRootPath` is a single shared folder path. Verify generated report files are not readable across tenants.
- **04 — Background jobs:** verify scheduled jobs set the correct `TenantId` and do not run once globally across all tenants' rows.
