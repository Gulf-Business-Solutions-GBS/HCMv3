# Help content tooling

Phase 0 of the help system. Design: [`docs/help/DESIGN.md`](../../docs/help/DESIGN.md).

Dependency-free Node — no `package.json`, no `npm install`. Run from the repository root.

## Commands

```bash
node tools/help/help-tool.mjs extract
```
Parses the screens listed in `docs/help/pages.json` plus the backing entity, and writes a field
inventory to `artifacts/help/inventory.json` (gitignored). Prints how each label resolved.

```bash
node tools/help/help-tool.mjs stubs [--lang en,ar]
```
Creates or **merges** skeleton Markdown under `docs/help/{en,ar}/`. Safe to re-run:

- hand-written prose is never touched
- the `**Rules.**` line is regenerated every run from the entity's data annotations
- new fields are inserted in screen order
- sections for fields no longer on screen are **kept**, and reported by `coverage`

```bash
node tools/help/help-tool.mjs build [--lang en,ar]
```
Compiles the Markdown into the runtime payload the Angular drawer loads,
`angular/src/assets/help/{lang}/{helpId}.json` — one file per screen, fetched on first open and
cached. Rules text is regenerated here too, so prose can never carry a stale validation rule.
Build only the languages that actually have prose: a missing language falls back to English at
runtime, which is better than shipping a file full of `_TODO_`.

```bash
node tools/help/help-tool.mjs coverage [--check]
```
Three-bucket report (DESIGN.md §12). `--check` exits non-zero on errors, for CI.

| Bucket | Meaning |
|---|---|
| `warn   undocumented field` | On screen, no section. Authoring gap. |
| `ERROR  stale section` | Section exists, field does not. Fix or annotate. |
| `ok     conditional field` | Absent field carrying `<!-- when: … -->`. Expected. |

## Adding a screen

Append an entry to `docs/help/pages.json`, then run `extract` and read the output before
generating stubs. `unresolved labels: 0` means every label on the screen mapped to a real entity
property; anything else needs an alias or a registry fix first.

## Screens that share fields

A field shown on several screens is written **once**, in its home file — the first screen in
`pages.json` that shows it. Every other screen borrows it: `stubs`, `build` and `coverage` pool
all topics of a language by canonical id. So a new screen's file only ever gets stubs for the
fields no earlier screen documents. Defining the same field in two files is a coverage error.

A screen with no read view (a form only) is registered with just an `edit` surface; give it a
`sectionTitleClass` so its fields are grouped under the form's own section headings.

## How labels are resolved

A read-only screen has only a localization key in the DOM. The **edit surface** for the same
entity carries both the key and the property (`<label for="Employee_TitleId">` +
`{{ 'Title' | localize }}`), so it is used as the authoritative mapping. Fallbacks, in order:
exact entity property, then `<key>Id` for lookups. Resolution source is recorded per field as
`resolvedVia` in the inventory.
