# Claude Code Instructions (ASP.NET Zero / ABP)

This repository is an **ASP.NET Zero** solution built on **ASP.NET Boilerplate (ABP)**.

Claude Code automatically loads this file into context when you start a session in this repo.
Keep instructions **concise, actionable, and layer-correct**.

## Non‑Negotiables (Architecture)
- Follow **strict layer separation** under `aspnet-core/src`. Never bypass layers.
- Never put business logic in controllers.
- Never access EF Core `DbContext` outside the `EntityFrameworkCore` project.
- Never mix **Entities** and **DTOs**.
- Avoid "plain ASP.NET Core" patterns if they conflict with ABP/ASP.NET Zero conventions.
- Default entities to inherit from `FullAuditedEntity` unless explicitly instructed otherwise.

### Server-side layering (quick map)
- **Core.Shared**: constants/enums/value objects/helpers only. No EF Core, no app logic.
- **Core**: domain entities/aggregates/domain services only. No DTOs/app services, no EF/Web deps.
- **Application.Shared**: DTOs + app service interfaces. DTOs must be serializable + validation-ready. No business logic.
- **Application**: application services + orchestration. Uses repositories (interfaces), DTOs, authorization/validation. No direct DbContext.
- **EntityFrameworkCore**: DbContext, repository implementations, migrations/config. No business logic.
- **Web.Host**: remote API host only (no MVC/views/assets), token auth.
- **Web.Mvc**: presentation/controllers thin; delegate to Application layer.

## Client-side expectations
- Angular talks only to **Web.Host**, uses token auth, and uses **NSwag-generated service proxies**.
- React talks only to **Web.Host**, uses token auth, and uses **NSwag-generated service proxies** via `useServiceProxy()`.
- MAUI talks only to **Web.Host**, token auth, no shared server-side code.

### Angular component/table rules
- Don't call `getRecords` in `ngOnInit` when PrimeNG table `(onLazyLoad)` is set.
- Don't use `[responsive]` attribute for Angular tables.

### React component rules
- Use `useServiceProxy(ServiceProxy, [])` for all API calls — NEVER raw `fetch` or manually edited `service-proxies.ts`.
- Use `useDataTable<Dto>(fetchFn)` for table paging, sorting, and loading state.
- Use `usePermissions()` + `isGranted()` on all action buttons and create buttons.
- Use `L("Key")` for all user-facing text — no hardcoded strings.
- NSwag flattens `EntityDto` for GET/DELETE: pass `id` directly (e.g. `deleteProduct(id)`, not `deleteProduct({ id })`).
- Regenerate proxies: run Web.Host, then `react/nswag/refresh.bat`.

## Application layer rules (DTOs + mapping)
- Don't use `double.Max` on DTO properties as a max attribute.
- Always map Entity -> DTO when returning data from an Application Service.
- Always map DTO -> Entity when creating/updating entities in Application Services.
- After mapping DTO -> Entity, set `TenantId` from `AbpSession.TenantId` manually.
- In GetForEdit methods, require an identifier; do not return an empty DTO if identifier is not sent from the client.
- Use non-null Id input (e.g., `EntityDto`) for GetForEdit methods.
- For maintainability, in `CreateAsync` and `UpdateAsync`, map DTO -> Entity first.
- **Add `[HttpPost]` to `Get{Feature}s` list methods that take a custom input DTO** (e.g. `GetRoles(GetRolesInput)`). ABP maps `Get*` to HTTP GET by default; NSwag then emits the input as individual query-string params and never generates the DTO class in `service-proxies.ts`, breaking React/Angular clients. Do NOT add `[HttpPost]` to `GetForEdit` or other `Get*` methods that take standard ABP types (`EntityDto`, `NullableIdDto`).

### Localization (XML)
- When you introduce or use a new localization key in generated/modified code, add it to the Core localization XML files under `aspnet-core/src/HCMv3.Core/Localization/HCMv3/`.
- `HCMv3.xml` is the English (default) file; other languages are in `HCMv3-{CODE}.xml` (e.g., `HCMv3-tr.xml`).

**When adding localization entries to XML files:**

1. **ALWAYS search for existing keys first** before adding new entries
   - Check all XML files in the localization directory to avoid duplicates
   - Use one of the following commands based on your environment:

2. **NEVER add duplicate keys** - ABP framework throws `AbpException` if same key appears twice

3. **Reuse existing localization keys** when possible instead of creating new ones

4. **Before adding localization entries, check if key exists:**
   
   **PowerShell (Windows):**
   ```powershell
   Select-String -Pattern 'name="YourKeyName"' -Path aspnet-core/src/HCMv3.Core/Localization/HCMv3/*.xml
   ```
   
   **Git Bash / WSL / Linux / Mac:**
   ```bash
   grep -n 'name="YourKeyName"' aspnet-core/src/HCMv3.Core/Localization/HCMv3/*.xml
   ```
   
   **CMD (Windows):**
   ```cmd
   findstr /n "name=\"YourKeyName\"" aspnet-core\src\HCMv3.Core\Localization\HCMv3\*.xml
   ```

### Angular Service Proxies (IMPORTANT)
- **NEVER manually create service proxy classes** (e.g., `*ServiceProxy`, DTOs) in the Angular project.
- All service proxies are **auto-generated by NSwag** from the backend Swagger/OpenAPI specification.
- To generate/update proxies:
  1. Run the `Web.Host` project (backend must be running)
  2. Execute `angular/nswag/refresh.bat` (or `refresh.sh` on Linux/Mac)
- Generated proxies are output to `angular/src/shared/service-proxies/service-proxies.ts`.
- When adding new backend services, just implement the backend; proxies will be generated automatically.
- Frontend components should import from `@shared/service-proxies/service-proxies`.

### React Service Proxies (IMPORTANT)
- **NEVER manually edit `react/src/api/generated/service-proxies.ts`** — it is auto-generated by NSwag.
- Use `useServiceProxy(ServiceProxy, [])` inside components; `createServiceProxy(ServiceProxy)` outside components.
- To generate/update proxies:
  1. Run the `Web.Host` project (backend must be running)
  2. Execute `react/nswag/refresh.bat`
- NSwag flattens `EntityDto` for GET/DELETE methods: pass the raw `id` number directly, never `{ id }`.
- Frontend components import from `@api/generated/service-proxies`.

## How to work (Claude Code workflow)
- **Explore → Plan → Implement → Verify**.
  - First, read relevant files and describe what you found.
  - Then propose a short plan and wait for confirmation **before** large edits.
  - Implement in small, reviewable steps.
  - Verify with the most specific build/test commands available.
- Prefer **single tests / targeted commands** over running everything.
- If requirements are ambiguous: ask 1–3 clarifying questions.

## Tooling & safety
- Prefer using safe, reversible commands.
- Avoid destructive operations (e.g., deleting folders, resetting git history) unless explicitly requested.
- If using auto-accept / dangerous permission modes, do so only in an isolated environment.

## Unit Testing (MANDATORY)
- **ALWAYS write unit tests for ALL generated/modified code**
- Test location: `aspnet-core/test/HCMv3.Tests/`
- Framework: xUnit with Shouldly assertions and NSubstitute for mocking
- Base class: Inherit from `AppTestBase` (the non-generic wrapper in the Tests project, which extends `AppTestBase<HCMv3TestModule>`)
- Naming: `{ClassName}_Tests.cs` or `{ClassName}_{MethodName}_Tests.cs` (method-scoped) for files, `Should_{ExpectedBehavior}` for methods
- **Test requirements:**
  - Application Services: Test all public methods
  - Domain Services: Test business logic and validation
  - Complex logic: Test edge cases and error scenarios
  - Use Arrange-Act-Assert pattern
  - One behavior per test method
- **DO NOT skip tests** - they are mandatory for code quality

## Repository commands (typical)
Use the smallest scope that proves correctness.

### .NET
- Build: `dotnet build aspnet-core/HCMv3.All.sln`
- Test: `dotnet test aspnet-core/test/*.sln` (or a specific test project when possible)
- **Always run tests after generating code** to ensure they pass

### Angular (if working under `angular/`)
- Install: `npm ci`
- Build: `npm run build`
- Test: `npm test`

## Output expectations
- Code must be **compilable**, **layer-correct**, and ABP/ASP.NET Zero compliant.
- **Code must be tested** - unit tests are mandatory for all generated/modified code.
- Always say which **project/layer** a change belongs to.
- Keep changes minimal; don't "improve" unrelated code.
- **Always verify tests pass** before completing the task.

## Common Commands

This project includes pre-defined commands for common development tasks. Commands are located in `.claude/commands/`.

### Available Commands

1. **create-bundles** - Creates dev bundles for Angular or MVC
   - See: `.claude/commands/create-bundles.md`
   
2. **generate-service-proxies** - Generates Angular service proxies from backend
   - See: `.claude/commands/generate-service-proxies.md`

When a user asks about these tasks, refer to the command files for detailed, platform-specific instructions.
