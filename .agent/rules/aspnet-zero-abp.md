# ASP.NET Zero Architecture & Coding Rules

This is an **ASP.NET Zero** solution built on **ASP.NET Boilerplate (ABP)**.
Generate production-ready, layer-correct code. Never use generic ASP.NET Core patterns if they conflict with ABP conventions.

## Solution Architecture (`aspnet-core/src`)

Follow **strict layer separation**. Never bypass layers.

| Layer | Purpose | Restrictions |
|-------|---------|-------------|
| **Core.Shared** | Constants, enums, value objects, helpers | No EF Core, no app logic |
| **Core** | Entities, aggregates, domain services | No DTOs, no app services, no EF/Web deps |
| **Application.Shared** | DTOs + app service interfaces | Serializable, validation-ready. No business logic |
| **Application** | App services + orchestration | Uses repositories (interfaces), DTOs, auth/validation. No direct DbContext |
| **EntityFrameworkCore** | DbContext, repositories, migrations | No business logic |
| **Web.Host** | Remote API host (no MVC/views) | Token auth only |
| **Web.Mvc** | MVC presentation | Thin controllers; delegate to Application layer |

## Client Side

### Angular
- Communicates only with **Web.Host** via token auth
- **NSwag-generated service proxies** — NEVER manually create. Run Web.Host, then `npm run nswag` in `angular/`
- Standalone components with signals, `inject()`, PrimeNG tables, Bootstrap modals
- Don't call `getRecords` in `ngOnInit` when PrimeNG `(onLazyLoad)` is set
- Don't use `[responsive]` attribute on tables

### React
- Communicates only with **Web.Host** via token auth
- **NSwag-generated service proxies** via `useServiceProxy()` — NEVER manually edit `service-proxies.ts`
- Ant Design v5 for UI, Metronic 8 for layout, Redux Toolkit, Vite 7
- Regenerate proxies: run Web.Host, then `react/nswag/refresh.bat`

## Mandatory Coding Rules

### Entities
- Default to `FullAuditedEntity` + `IMustHaveTenant`
- Keep persistence-ignorant, no DTOs in Core

### Application Services
- Inherit `HCMv3AppServiceBase`
- `[AbpAuthorize]` on **all** public methods
- Return DTOs, never entities
- Entity → DTO when returning; DTO → Entity when creating/updating
- Set `TenantId = AbpSession.GetTenantId()` after DTO → Entity mapping
- `GetForEdit` must require an identifier (no empty DTO return)
- Use `IRepository<TEntity>`, never inject DbContext
- **`[HttpPost]` on `Get{Feature}s` list methods** — ABP routes `Get*` as HTTP GET by default, which causes NSwag to emit custom paging/filter inputs as query-string args instead of typed DTO classes. Add `[HttpPost]` only to list methods that take a **custom input DTO** (e.g. `GetRoles(GetRolesInput)`). Do NOT add it to `GetForEdit`, `GetById`, or `Get*` methods that take standard ABP types (`EntityDto`, `NullableIdDto`).

### Localization
- **ALWAYS search for existing keys first** — ABP throws `AbpException` on duplicates
- Search command:

```powershell
Select-String -Pattern 'name="YourKey"' -Path aspnet-core/src/HCMv3.Core/Localization/HCMv3/*.xml
```

## Hard Restrictions

- Never bypass Application layer
- Never put business logic in Controllers
- Never access DbContext outside EntityFrameworkCore project
- Never mix DTOs and Entities
- Never manually create service proxy classes
- Never inject one app service into another

## Unit Testing (Mandatory)

- **Always write tests** for generated/modified code
- Location: `aspnet-core/test/HCMv3.Tests/`
- Framework: xUnit + Shouldly + NSubstitute, base class: `AppTestBase`
- Naming: `{ClassName}_Tests.cs` or `{ClassName}_{MethodName}_Tests.cs` (method-scoped), methods: `Should_{ExpectedBehavior}`

## Build & Test

```bash
dotnet build aspnet-core/HCMv3.All.sln

dotnet test aspnet-core/test/HCMv3.Tests/

cd angular && npm run build

cd react && npm run build
```

## Commands in `.agent/commands/`

- `create-bundles`
- `generate-service-proxies`

Use these command files for execution details.

## Related Guidance

- Cursor rule source: `.cursor/rules/aspnetzero.mdc`
- Windsurf rules: `.windsurf/rules/aspnet-zero-abp.md`
- GitHub Copilot instructions: `.github/copilot-instructions.md`
- Claude Code instructions: `.claude/rules/claude-instructions.md`
