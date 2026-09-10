# HCMv3 Help System — Design

Status: **approved, Phase 0 in progress**
Owner: Documentation / Platform
Last updated: 2026-09-09

---

## 1. Purpose

One body of content that serves four channels without being written four times:

1. **Field help** — F1 on a field opens an explanation, with examples and rules.
2. **Page help** — a help icon on every screen explains every field on that screen; F1 deep-links into a section of that same page.
3. **Web help** — an end-user guide and an admin guide, published as a static site.
4. **Offline manual** — PDF/DOCX for customers who ask for a book.

English and Arabic throughout.

## 2. Principles

These are the constraints that decide every later question. Violating one is what turns a help system into a second, rotting copy of the product.

1. **One topic, many surfaces.** A field is described exactly once. The read screen, the edit modal, the web guide and the PDF all render the same topic.
2. **Help mirrors visibility; it never decides it.** The help layer must never hold its own copy of who-can-see-what. See §8.
3. **Anything a machine can know, a machine writes.** Table names, column types, max lengths, required-ness, enum members. Humans write only meaning and examples. See §9, §10.
4. **The identifier is the contract.** One string ties DOM to content to URL anchor to database column. See §4.
5. **Absent help is a warning; wrong help is an error.** The build gate distinguishes them. See §12.

## 3. Scope of the pilot

`employee-detail/personal-info` — the Employee **Personal Information** tab only.

Explicitly out of scope: the ~40 sibling sections under `employee-detail/` (contracts, banks, absences, loans, dependants, gratuities, …), and every other module.

In scope by necessity: `employees/create-or-edit-employee-modal`, because it is the *edit surface for the same fields* and proves the "one topic, two screens" claim.

## 4. The identifier contract

### 4.1 Canonical id

```
Employee.NationalityId          entity property path: <Entity>.<Property>
```

The canonical id is the **entity property**, not the DTO property and not the DOM id.

Why not the DOM id, as first proposed: the Personal Information screen is a read view built from `<dt>`/`<dd>` pairs and carries no input ids at all. Only the edit modal has them.

Why not the DTO property: `EmployeeProfileDto` is read-shaped and exposes `NationalityName` (a resolved string), while `CreateOrEditEmployeeDto` exposes `NationalityId` (an int). Two DTOs, one field. The entity is the only stable common ancestor — and it is also what the EF model can be queried with for the technical tab.

### 4.2 Aliases

Each topic may declare aliases. The runtime resolver maps whatever it finds in the DOM onto the canonical id:

| Surface | What is in the DOM | Alias form |
|---|---|---|
| Edit modal | `<input id="Employee_NationalityId">` | `dom:Employee_NationalityId` |
| Edit modal | `name="NationalityId"` | resolved via page `entity:` |
| Read card | `<dt>` with localize key `Nationality` | `loc:Nationality` |

Aliases are declared in frontmatter and are generated automatically in Phase 0, not hand-written.

### 4.3 Page id

```
pa.employee.personal-info
```

Dotted, stable, independent of the Angular route. Routes change; help URLs published in a PDF must not.

### 4.4 The one string, four jobs

```
docs/help/en/pa.employee.personal-info.md   ->  ## Nationality {#Employee.NationalityId}
in-app drawer                               ->  scrolls to Employee.NationalityId
web help                                    ->  /en/guide/pa.employee.personal-info#Employee.NationalityId
technical tab                               ->  Model.FindEntityType("...Employee").FindProperty("NationalityId")
```

## 5. Content model

### 5.1 Layout

```
docs/help/
  DESIGN.md                    this file
  en/
    pa.employee.personal-info.md
    pa.employee.edit.md
  ar/
    pa.employee.personal-info.md      identical ids and anchors, Arabic prose
```

Parallel trees. Arabic prose does **not** go into `HCMv3-ar.xml` — those files are for UI strings, and paragraph content would make them unmanageable.

### 5.2 File format

Frontmatter, then one `##` section per field:

```
---
helpId: pa.employee.personal-info
title: Employee — Personal Information
entity: HCMv3.PersonnelAdministration.Employee
surfaces:
  - employee-detail/personal-info                  # read view
  - employees/create-or-edit-employee-modal        # edit surface
audience: [end-user, admin]
permissions: [Pages.PersonnelAdministration.Employees]
---

## Overview

What the screen is for, who uses it, the typical flow.

## Nationality {#Employee.NationalityId}
<!-- alias: loc:Nationality, dom:Employee_NationalityId -->

**What it is.** The employee's nationality as recorded on the civil ID.

**Example.** Kuwaiti

**Why it matters.** Drives PIFSS eligibility, the indemnity formula and the
expatriate allowance rules.

:::admin
Maintained under General Master Info -> Nationalities. Deactivating a nationality
hides it from new records but leaves existing employees untouched.
:::
```

### 5.3 Block conventions

| Block | Meaning |
|---|---|
| `**What it is.**` | Required. One or two sentences, plain language. |
| `**Example.**` | Required where a value is not self-evident. |
| `**Why it matters.**` | Optional. Downstream effects — payroll, statutory, reporting. |
| `**Rules.**` | **Generated.** Never hand-written. See §10. |
| `:::admin … :::` | Admin-guide only. Stripped from the end-user build. |
| `<!-- when: … -->` | Conditional field. See §8.2. |

The `:::admin` fence is what makes one file produce two manuals.

## 6. Channels

### 6.1 In-app drawer (primary)

A right-hand drawer (RTL: left-hand), not a modal — the user must be able to read the help *and* see the field.

- **Page help icon** → opens at the Overview, all visible fields listed in screen order.
- **F1 on a field** → opens the same drawer, scrolled to that field's section.
- **Technical icon** per section → §9.
- **"Open full guide ↗"** → the web site at the identical anchor.

One component, one content payload, two entry points. There is no separate "field popup" implementation.

### 6.2 Web help

Docusaurus v3: built-in i18n with `direction: rtl`, versioning, local search. Two builds from one tree (end-user / admin). Deployed under `/help` on the same IIS site so the in-app link is same-origin.

### 6.3 Offline manual

pandoc over the same tree. No separate authoring.

## 7. Keyboard and affordances

| Input | Action |
|---|---|
| **F1** | Field help for the focused or hovered field. `preventDefault` suppresses browser help in Chrome/Edge/Firefox. |
| **Shift+F1** / **Alt+H** | Same. Alias for browsers or kiosks that swallow F1. |
| **Ctrl+F1** | Straight to technical info (permission-gated). |
| **Esc** | **Closes the drawer. Never opens it.** |

Esc is rejected as an open-help key: Bootstrap modals, PrimeNG overlays and `appBsModal` all consume it, and rebinding it would fight the entire UI.

**Read-only screens have no focused input.** On a `<dl>` card the target is resolved from the hovered `<dt>`/`<dd>` pair; each `<dt>` also becomes focusable (`tabindex="0"`) so keyboard users can reach it. A `?` glyph appears on hover/focus for mouse users.

## 8. Visibility and tenancy

### 8.1 Runtime filtering (in-app) — the primary mechanism

When the drawer opens it enumerates the field ids **actually present in the live DOM** of the host screen and renders only those sections.

A field suppressed by `@if`, by `| permission`, by a feature check, or by any gating mechanism invented later is simply not in the DOM, so its section never renders. No configuration, no annotation, no drift — and it tracks future mechanisms for free.

This also closes a disclosure hole: help text for a module the customer never bought is never rendered to them.

### 8.2 Declarative conditions (web/PDF) — no DOM available

```
## Religion {#Employee.ReligionId}
<!-- when: feature=App.HCM.KuwaitLocalization -->
```

- **Public site**: render with a badge — *"Available with: Kuwait Localization"*.
- **Authenticated guide**: filter client-side against the reader's own feature/permission set.

Also enables per-edition PDF manuals later without forking content.

### 8.3 Tenant custom fields (dynamic properties)

Fields a tenant added through ABP `DynamicEntityProperties` have no repo Markdown by definition. Add an optional `HelpText` to the dynamic property definition; the drawer **merges** repo topics with dynamic-property topics in screen order.

This does not reopen the repo-vs-database decision — it is help for fields that only ever existed in the database.

### 8.4 Known gap

`AppFeatures.cs` currently holds only stock ASP.NET Zero entries (`MaxUserCount`, `ChatFeature`, `TestCheckFeature`). There is **no HCM feature taxonomy and no field-visibility table** anywhere in the solution.

Consequence: today, "hidden from this tenant" can only mean *not permitted* or *hardcoded `@if`*. §8.1 handles all of those correctly regardless. Building a real per-customer field-toggle mechanism is a separate application-architecture task and is **not** a dependency of this project; `when:` annotations are added opportunistically as features get defined.

## 9. Technical info — generated, never authored

A `HelpMetadataAppService` (Application layer) reads the EF model:

```csharp
DbContext.Model.FindEntityType("HCMv3.PersonnelAdministration.Employee")
```

and returns, per property: table, schema, column, CLR type, SQL type, nullability, max length, and FK target entity/table.

Verified feasible on the pilot: `DbSet<Employee> Employees` with fluent configuration in `HCMv3DbContext`, so table and column names come from the model rather than from a guess.

Gated behind a new permission — `Pages.Administration.HelpTechnicalInfo`. End users should not see schema; SAP restricts technical info for the same reason.

## 10. The Rules block — generated from annotations

`Employee.cs` already carries what a validation description needs:

```csharp
[Required]
[StringLength(128)]
public virtual string FirstNameEn { get; set; }
```

The build emits **Rules: Required. Maximum 128 characters.** Enum-typed properties emit their members. Nullability comes from the EF model.

This is the single largest reduction in authoring effort: a writer facing a field already has its name, its label, its type and its rules on the page, and only has to supply meaning and an example.

## 11. Build pipeline

`tools/help/` — dependency-free Node (`.mjs`), no new npm install.

| Command | Input | Output |
|---|---|---|
| `extract` | templates + DTOs + entity | field inventory JSON |
| `stubs` | inventory + existing content | skeleton `.md` (en + ar) |
| `build` | markdown tree | `help/{lang}/{helpId}.json`, chunked; Docusaurus source |
| `coverage` | inventory vs content | three-bucket report, CI gate |

Chunked per page — a single index over thousands of fields would be a multi-megabyte download.

## 12. Coverage gate

| Situation | Outcome |
|---|---|
| Field in template, no help section | **warn** — authoring gap |
| Help section, no matching field anywhere | **error** — stale doc |
| Help section, no match, carries `when:` | **pass** — conditionally shipped field |
| EN section with no AR counterpart | **error** once Arabic lands (Phase 4) |

Without the third bucket every conditional field would fail the gate.

## 13. Backend surface

| Piece | Layer | Note |
|---|---|---|
| `HelpMetadataAppService` | Application | EF model reflection, §9 |
| `IHelpMetadataAppService` + DTOs | Application.Shared | |
| `Pages.Administration.HelpTechnicalInfo` | Core (AppPermissions + provider) | |
| Compiled help JSON | Web.Host static assets | versioned, cacheable |

No new entities in Phase 1–3. The dynamic-property `HelpText` field (§8.3) is the only schema change, and it lands in Phase 4.

## 14. Phasing

| Phase | Deliverable |
|---|---|
| **0** | Identifier scheme frozen; field-inventory extractor; stub generator; coverage report. **No prose.** |
| 1 | Drawer + F1 resolver + JSON pipeline. Pilot screen, EN only. |
| 2 | Technical tab from EF model, permission-gated. |
| 3 | Docusaurus site from the same tree, deployed at `/help`. |
| 4 | Arabic + RTL + parity gate; dynamic-property help. |
| 5 | Module-by-module rollout; PDF manual. |

## 15. Decisions log

| Decision | Choice | Date |
|---|---|---|
| Authoring surface | Markdown in repo, PR-reviewed | 2026-09-09 |
| Pilot scope | Personal Information tab only | 2026-09-09 |
| Canonical id | Entity property path, not DOM id | 2026-09-09 |
| Help-open key | F1 / Shift+F1 / Alt+H. **Not Esc.** | 2026-09-09 |
| Visibility | Runtime DOM filtering in-app; `when:` for static | 2026-09-09 |
| Arabic prose location | Parallel Markdown tree, not localization XML | 2026-09-09 |

## 16. Survey findings behind this design

Measured on the repository, 2026-09-09:

- 1,143 Angular component templates; 1,027 under `admin/` across ~50 functional modules. Any design requiring per-component edits is not viable.
- 392 templates use the `<label for="Entity_Property">` convention — useful, but **absent on the pilot screen**, which is why §4.1 does not key on it.
- 5,894 English localization keys; Arabic file fully populated; 13 further languages present.
- `AppFeatures.cs` contains stock ASP.NET Zero entries only — see §8.4.
- No field-visibility or screen-configuration table exists in `aspnet-core/src` or `angular/src`.
- Employee entity carries `[Required]` / `[StringLength]` annotations densely enough for §10 to be worthwhile.
