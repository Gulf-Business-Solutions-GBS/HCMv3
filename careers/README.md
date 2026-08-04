# HCMv3 Careers Portal (wave R3b)

The **candidate-facing** careers portal: a standalone Angular 21 application, deliberately separate from
both the recruiter-facing admin SPA in `../angular` and from `HCMv3.Web.Public`.

- It is **not** part of `../angular`: that app is an authenticated admin SPA whose CSS/JS bundle
  (Metronic + Bootstrap + PrimeNG + Quill + FullCalendar) is several hundred kilobytes a job seeker never
  needs, and whose service proxies expose the entire internal HR API.
- It is **not** hosted in `HCMv3.Web.Public`: wave R1 rejected that host because its `AccountController`
  only performs admin SSO token handoff, which is the wrong auth shape for candidates. The Metronic
  `careers` demo assets there were used only as a **styling reference**.

The recruiter-facing screens under `../angular/src/app/admin/recruitment/` are untouched by this wave.

---

## Tenant resolution — read this first

The portal is **subdomain-per-tenant**. The backend resolves the tenant from the **Host header of the API
request** via `App:CareersDomainFormat` (see `HCMv3.Careers.CareersTenantResolver`) and **fails closed**: if
it cannot resolve a tenant, every careers endpoint returns a "This careers portal address is not recognized"
`UserFriendlyException`. There is deliberately **no dev header or query-string bypass** — on a public
surface that would be a leak vector.

Two consequences the portal is built around:

1. **The API must be called on the tenant subdomain.** `AppConsts.remoteServiceBaseUrl` is therefore built at
   boot from `window.location.hostname` (see `src/environments/environment.ts`). Calling a fixed
   `https://localhost:44301` would send `Host: localhost`, match no `{TENANCY_NAME}.localhost` pattern, and
   fail closed on every request.
2. **Login works on the stock endpoint.** Candidates are ordinary ABP users in the `Candidate` role, so
   sign-in posts to `/api/TokenAuth/Authenticate`. `CareersTenantResolveContributor` makes ABP's ambient
   tenant resolution understand careers subdomains, so that endpoint lands on the right tenant. There is no
   careers login endpoint and there must not be one.

### Local development setup

Configuration already added to `aspnet-core/src/HCMv3.Web.Host/appsettings.json`:

```jsonc
"CareersDomainFormat": "{TENANCY_NAME}.localhost:4300",
"CareersUrlScheme": "http",
"CorsOrigins": "...,http://*.localhost:4300,https://*.localhost:4300"
```

The port in `CareersDomainFormat` is **ignored when matching** a request host (the resolver strips it) but is
**kept when building emailed links**, so the password-reset mail points at `http://{tenancy}.localhost:4300/`
— the portal's dev server — instead of a portless `https://` URL that would 404.

**You must add a hosts-file entry per tenant** (requires administrator rights):

```
127.0.0.1 default.localhost
```

Then:

```bash
# 1. Run HCMv3.Web.Host  (https://localhost:44301)
# 2. Generate proxies (Web.Host must be running):
npm run nswag
# 3. Serve the portal:
npm start                      # http://0.0.0.0:4300
# 4. Browse http://default.localhost:4300
```

> The ASP.NET Core dev certificate is issued for `localhost` only, so `https://default.localhost:44301` will
> show a certificate-name warning in the browser that must be accepted once (or replace the dev cert with one
> carrying a `*.localhost` SAN).

---

## Service proxies

The portal has its **own** NSwag pipeline in `nswag/` and does not read, write or share anything with
`../angular/nswag`. `refresh.bat` is a two-step process:

1. `filter-swagger.mjs` downloads the full OpenAPI document (~2,250 paths / ~2,450 schemas — the entire
   admin API) and reduces it to the three careers services (16 paths / 31 schemas). Shipping clients for the
   whole API in a public, anonymous-facing bundle would hand any reader the shape of the internal HR API.
2. NSwag generates `src/app/service-proxies/service-proxies.ts` from that reduced document.

The output is 100% generated — never hand-edit it, and never hand-write a proxy or DTO.

Two generator behaviours worth knowing:

- `EntityDto` is **flattened** for GET/DELETE: call `getPublishedVacancy(id)`, not `getPublishedVacancy({ id })`.
- Enums use **real member names** (`AppStatus.Applied`, `VacancyEmploymentType.FullTime`), not `._0`.
- Acronym-initial properties are camelCased as a unit: `CVName` becomes **`cvName`** (not `cVName`), while
  `CivilID` stays `civilID`. Check the generated file rather than guessing.

Proxies are generated with `useSingletonProvider: true`, so they are `providedIn: 'root'` and need **no**
manual provider registration (unlike the admin SPA's `service-proxy.providers.ts`).

---

## Conventions

- Standalone components, `inject()`, signals throughout. The app is **zoneless**
  (`provideZonelessChangeDetection()`), matching the admin SPA — so all component state must be a signal.
- All user-facing text is localized through the server's `HCMv3` localization source, fetched at boot from
  the anonymous `/AbpUserConfiguration/GetAll`. Add new keys to
  `aspnet-core/src/HCMv3.Core/Localization/HCMv3/HCMv3.xml` — and search every file in that folder first,
  because a duplicate key makes ABP throw `AbpException` at startup.
- A missing entry in a standalone component's `imports` array is invisible to `tsc --noEmit`. Run a real
  `ng build`.

## Commands

| Command | Purpose |
|---|---|
| `npm start` | Dev server on port 4300 |
| `npm run build` | Development build |
| `npm run build:prod` | Production build |
| `npm run nswag` | Regenerate service proxies (Web.Host must be running) |
