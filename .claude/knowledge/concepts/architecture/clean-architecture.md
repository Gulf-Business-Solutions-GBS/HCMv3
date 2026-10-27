---
name: Clean Architecture
category: concepts/architecture
---

# Clean Architecture in ASP.NET Zero

Clean Architecture enforces the **Dependency Rule**: source code dependencies must point inward.

## Concentric Layers (Inside Out)

| Layer | Clean Architecture | ASP.NET Zero Mapping |
|-------|-------------------|---------------------|
| 1 | **Entities** | Core -- domain entities, aggregates, domain services |
| 2 | **Use Cases** | Application -- application services, orchestration |
| 3 | **Interface Adapters** | Web.Host / Web.Mvc -- controllers, presenters |
| 4 | **Frameworks** | EntityFrameworkCore -- ORM, DB, external I/O |

## The Dependency Rule in Practice

- Core has zero references to EF Core, Web, or Application projects.
- Application depends on Core but never on EntityFrameworkCore directly.
- EntityFrameworkCore implements repository interfaces -- depends inward on Core.
- Web.Host / Web.Mvc depend on Application but never contain business logic.

## How ABP Enforces This

- **IRepository<TEntity, TPrimaryKey>**: abstracts data access.
- **Application Services**: use-case handlers coordinating entities and repositories.
- **Domain Services**: cross-entity business rules inside Core.
- **DTOs**: live in Application.Shared as data contracts.

## Key Takeaways

1. Business rules live in Core -- never in controllers or EF layer.
2. Application services orchestrate but do not own business rules.
3. The database is a detail -- swap EF Core without touching business logic.
4. DTOs cross boundaries; entities do not leave the domain.
5. DI wires outer layers to inner interfaces at runtime.

## Anti-Patterns to Avoid

- Injecting DbContext into an Application Service.
- Putting validation/business logic in a controller action.
- Returning domain entities directly from an API endpoint.
- Referencing Microsoft.AspNetCore namespaces inside Core.
- Using one application service in another application service
