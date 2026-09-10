# HCMv3 Help System — Design

Status: **approved; Phases 0–2 implemented for the pilot screen**
Owner: Documentation / Platform
Last updated: 2026-09-10

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

**Extended 2026-09-10:** the edit form is now a help page in its own right, `pa.employee.edit` — 58 fields, of which 36 are shared with Personal Information and 22 are form-only. Its content is written; showing it in-app needs the drawer to follow the modal (§6.1), which is pending a browser-testable environment.

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
| Read card | `<dt data-help-loc="Nationality">` | `loc:Nationality` |

Aliases are generated in Phase 0, not hand-written, and are compiled into the page JSON as an
`aliasIndex` the runtime resolver looks up directly.

**The read view needs one marker per field; the edit form needs none.** A rendered template
contains the *translated* label, not the localization key, so a read-only `<dt>` has nothing a
resolver can key on — hence `data-help-loc="<localization key>"`, which simply repeats the key the
template already names one line later. Edit forms are free: `id="Entity_Property"` is already there
by ASP.NET Zero convention.

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

**One file per screen, one home per topic.** A screen's file holds its Overview and the topics for the fields it *introduces*. A field that appears on several screens is written once, in its **home** file — the first screen in `pages.json` that shows it — and every other screen borrows it: the tooling pools all topics of a language by canonical id. So `pa.employee.edit.md` contains only the 22 form-only fields; the other 36 it shows come from `pa.employee.personal-info.md`. The same id defined in two files fails the coverage gate (§12), which is what enforces principle 1.

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

**Read-only screens have no focused input.** On a `<dl>` card the target is resolved from the hovered `<dt>`/`<dd>` pair — the pointer is usually over the value, so the resolver walks from a `<dd>` back to its preceding `<dt>`.

Making each `<dt>` focusable with `tabindex="0"` was considered and **rejected**: it would insert 39 extra tab stops into one screen, which costs every keyboard user on every visit to buy a feature few of them would find. Keyboard users press **Alt+H** instead and get the whole screen's fields as one readable list, which is the better experience anyway.

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

**"Required" means the reader cannot save without it** — `[Required]` on the entity, a form label marked required, or a date or choice that always holds a value. It does *not* follow from a non-nullable number: `long Mobile` left blank saves as 0, so calling it required told readers about a rule the system does not enforce. The first version made exactly that mistake for Mobile, Telephone, Permanent Telephone, Civil ID and Probation Days; it was corrected on 2026-09-10.

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
| Compiled help JSON | `angular/src/assets/help/` | see below |

No new entities in Phase 1–3. The dynamic-property `HelpText` field (§8.3) is the only schema change, and it lands in Phase 4.

**The compiled JSON ships as an Angular asset, not from Web.Host.** `src/assets` is already served
wholesale, so this needs no configuration, no API round trip and no running backend to work in
development; the content is versioned with the SPA that renders it. Content is fetched per page and
cached in memory, so a screen costs one small request the first time it is opened and nothing
afterwards. Tenant-specific help (§8.3) still arrives from the API and is merged client-side, so
this choice does not foreclose it.

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
| Compiled JSON location | Angular asset, not Web.Host static | 2026-09-10 |
| Read-view field marker | `data-help-loc="<loc key>"`; edit forms unchanged | 2026-09-10 |
| `tabindex` on `<dt>` | Rejected — Alt+H is the keyboard path instead | 2026-09-10 |
| Admin notes in-app | Shown to all, clearly labelled; the gated surface is the technical tab | 2026-09-10 |
| Motion | Never load-bearing: entry animation offsets by 1.5rem, not 100%; F1 scroll is instant, not smooth | 2026-09-10 |
| Shared fields | One home file per topic; other screens borrow it; duplicates fail coverage | 2026-09-10 |
| Property-name matching | Case-insensitive — `Employee_PaciNumber` is the `PACINumber` property | 2026-09-10 |

**On that last one.** Both the entry animation and the F1 scroll were first written the obvious way
— `translateX(100%)` and `scrollIntoView({behavior: 'smooth'})` — and both failed the same way when
animation did not run: the drawer sat entirely off-screen, and the topic highlighted somewhere the
reader could not see. A help system whose whole purpose is answering "what is this field" must not
be able to leave the reader looking at nothing because a compositor was busy. Decoration degrades;
it does not take the feature with it.

## 16. Survey findings behind this design

Measured on the repository, 2026-09-09:

- 1,143 Angular component templates; 1,027 under `admin/` across ~50 functional modules. Any design requiring per-component edits is not viable.
- 392 templates use the `<label for="Entity_Property">` convention — useful, but **absent on the pilot screen**, which is why §4.1 does not key on it.
- 5,894 English localization keys; Arabic file fully populated; 13 further languages present.
- `AppFeatures.cs` contains stock ASP.NET Zero entries only — see §8.4.
- No field-visibility or screen-configuration table exists in `aspnet-core/src` or `angular/src`.
- Employee entity carries `[Required]` / `[StringLength]` annotations densely enough for §10 to be worthwhile.
