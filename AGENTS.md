# ASP.NET Zero / ABP Repository Instructions

These instructions apply to the entire repository.

## Architecture Boundaries

- Keep strict layer separation under `aspnet-core/src`.
- Do not put business logic in controllers.
- Do not access EF Core `DbContext` outside the `EntityFrameworkCore` project.
- Do not mix entities and DTOs.

## Application Service Rules

- Return DTOs, not entities.
- Map DTO -> Entity for create/update; map Entity -> DTO for responses.
- After DTO -> Entity mapping, set `TenantId` from `AbpSession.TenantId`.
- For `Get*` list methods with custom input DTOs, add `[HttpPost]` so NSwag generates typed request DTOs.

## Frontend Rules

- Angular and React must consume backend APIs via NSwag-generated service proxies.
- Do not manually create or edit generated proxy files.
- Regenerate proxies after backend API contract changes.

## Testing and Validation

- Unit tests are mandatory for generated/modified code in `aspnet-core/test/HCMv3.Tests/`.
- Prefer targeted build/test commands first.

## Localization

- Add new localization keys to Core localization XML files.
- Search for existing keys first and avoid duplicates.
