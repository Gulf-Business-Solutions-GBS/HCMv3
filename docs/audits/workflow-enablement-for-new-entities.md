# Putting a new entity through workflow: what is configuration and what is code

Written 2026-09-27, from a walkthrough of eight questions about enabling workflow on a new entity
(`EmployeeCovidVaccination` was the worked example).

Everything below describes HCMv3 **as it is today**, not as it is documented to be. Where behaviour is
inconsistent the table says so rather than describing the intent.

## The short version

Three of the eight points are settled and working. Five are gaps, and two of those are decisions only you
can make — whether the employee profile should route like self-service does, and how an authorised person
records something without approval.

The single most important finding: **whether a transaction goes for approval currently depends on which
screen it was typed into, not on what you configured.** Thirteen admin screens ask the catalog; the rest,
including Absences and Assets, do not.

---

## The eight points

| # | Question | How it works today | Pros | Cons | Action to automate | Recommendation |
|---|---|---|---|---|---|---|
| 1 | Where do I enable workflow on a new entity? | A catalog row must exist with `Code` = the **exact entity class name** (a code change, seeded off). Then **Workflow → Workflow Transaction Types** → tick Workflow Enabled + pick a chain. | One screen, per tenant, no SQL. Seeded off, so deploying never starts routing anything. Server refuses a half-configuration. | The catalog row itself is a code change, so a brand-new entity is not purely an admin job. A `Code` typo produces a row the resolver silently never finds. | Validate `Code` against the registered transaction handlers when the row is saved, so a typo fails at save rather than at submit. | **Keep registration in code** — it pairs a row with a real handler. Add the validation. |
| 2 | Must I create the chain first? | Yes. The server rejects enabling a type with no default chain. Chains live in **Request Masters**; steps are dragged into order. | No such thing as a type that routes nowhere. Chains are reusable across types — most tenants need a handful. | Two screens to visit in the right order for a first-time setup. | A "create chain" shortcut inside the transaction-type dialog, and "copy an existing chain". | **Keep the refusal.** Add the shortcut — it is the one place the order of work bites. |
| 3 | How do the ESS menu and page appear? | The **ESS home tile** is catalog-driven (Self-Service Enabled + Active + permission + icon + route). The **left-hand ESS menu** is hardcoded. The **page** is an Angular component. | The tile is free — tick a box, it appears, permission-filtered on the server. | Two of the three places a screen shows up are configuration and one is not. Ticking the box gives a tile but no menu entry. | Make the ESS left menu read the catalog exactly as the tiles do. | **Do it.** Small, contained, removes one of three code touchpoints. The page cannot be eliminated the same way. |
| 4 | Does it appear in the employee profile too? | Both. Every such entity has an ESS screen *and* a profile section over the same table. Profile sections are a hardcoded catalog of 67 entries. | The two audiences genuinely differ — ESS is always "me", the profile is "whoever is open", and HR sees fields the employee should not. | A fully-built entity costs: catalog row + ESS page + ESS menu entry + profile section + section entry. Only the tile is configuration. | Nothing realistic. A generic metadata-driven form is a much larger project. | **Leave as code.** Two real screens for two real audiences is the right shape. |
| 5 | Does creating from the profile trigger workflow? | **Now consistent.** 48 services across 31 workflow-enabled entities call the submitter, Absences and Assets among them. Routing follows the catalog, not the screen. (This row described the state on the morning of 2026-09-27, before the plumbing wave.) | Where it is wired, it is right: the row is held as Draft and the request is filed against the employee it is about, not the HR user who typed it. | With workflow on for leave: ESS routes, the profile does not. Nothing on screen says which you are getting. "HR entry is immediate" is hardcoded, not configured. | Route every screen writing a workflow-enabled entity through the same submitter. | **Do it.** Behaviour should follow the catalog, not the accident of which service was written when. |
| 6 | How do I show Submit vs Save? | `IsWorkflowEnabled` already reaches the browser. Only the ESS home uses it (a "No approval needed" note). Buttons are static: Exit Permit has Save + Submit, everything else has Submit. | The outcome is already correct — the server records rather than routes, and the toast says "recorded" not "submitted". | The button says "Submit" when nothing is submitted to anyone. Exit Permit has two buttons and twenty screens have one, for no reason a user could name. | Label the primary button from the catalog: **Submit** when routed, **Save** when not. Show a draft button only where a draft endpoint exists. | **Do it.** The data is already there; this is a label and a condition. |
| 7 | How do I stop workflow when an admin records something? | There is no such feature. Some screens bypass because they never ask the catalog — any authorised user gets the bypass there, nobody gets it elsewhere, and nothing is recorded. | — (the current behaviour is an accident, not a design) | Invisible and unprincipled. A leave row that never reached a manager is indistinguishable from an approved one. | Per-type **"HR entry may skip approval"** on the catalog + a `RecordWithoutApproval` permission + an explicit unticked choice on the form + an audit line on every bypass. | **No workbench.** It duplicates twenty forms and answers "where it was typed" when the real question is "who, and may this type skip". |
| 8 | How do I stop workflow on bulk import? | Importers write straight to the repository and never call the submitter, so rows land immediately effective. Correct — but by accident, not by rule. | Go-live history loads without raising thousands of requests. | The moment the universal check from #5 arrives, imports would get caught and a history load would flood inboxes. Null status means both "not routed" and "imported". | State the rule: routing lives in the submitter and non-interactive paths never call it. Mark imported rows. Give import its own permission. Say so on the upload screen. | **Keep import out of workflow, structurally** — not via a flag somebody can forget. |

---

## Built since this was written (point 7)

Point 7's recommendation is now in the product, in the shape the table proposed rather than a workbench.

- **`AllowDirectEntry` on the catalog row**, off for every type, shown on the Transaction Types form only
  while routing is on -- with routing off a transaction is already immediate, so a "skip approval" switch
  there would offer a choice that does not exist.
- **`Pages.Workflow.RecordWithoutApproval`**, held by nobody until granted.
- **An unticked choice on the form**, one shared component rather than the same block pasted into every
  dialog. It shows itself only when the user holds the permission *and* the catalog allows it for that type
  -- the same two conditions the server enforces, so a form never offers a choice the save would refuse.
- **A log line on every bypass**, `RecordedDirectly`, its own action type rather than Approved, because
  nobody approved it. The row is Closed, and carries **no request number**: the counter numbers requests,
  and a record nobody has to answer is not one.

Both gates live in `AdminTransactionSubmitter`, not in a screen, so they hold for every caller including
ones written later. Asking for a bypass that is not allowed is an error rather than a quiet fall back to
routing -- a caller that asked and silently did not would leave HR believing a record was effective when it
was sitting in an inbox.

**Reachable from ten of the thirteen wired screens.** Exit permits, HR services and letter requests have
no admin form at all -- they exist only in self-service -- so their server side accepts the flag and
nothing sends it. That is a screen to build, not a gap in the mechanism.

---

## Built since this was written (point 8)

Point 8 said imports avoid workflow by accident rather than by rule, and that the accident would end the
moment routing became universal. Routing did become universal, and the accident had already ended -- in the
wrong direction.

**The go-live employee load writes bank details through `EmployeeBanksAppService`, which routes.** So
ticking "Workflow Enabled" on Employee Bank would have turned a five-hundred-employee opening load into
five hundred approval requests for data that was migrated, not requested. Two settings, in different
screens, with nothing connecting them -- and the symptom appears at go-live.

The rule is now `NonInteractiveScope`. Work that declares itself non-interactive never routes, whatever the
catalog says:

- **The bulk import, the profile-photo import and the go-live employee load** open the scope around their
  whole run.
- **The admin submitter** treats a scope as "not workflow-enabled" rather than refusing, because that is
  the behaviour these paths want and already had: the row is written immediately and stays visible, exactly
  as an imported row always has been.
- **The approval service** refuses outright, as a backstop for any future path that builds a request some
  other way and tries to send it.
- Nesting keeps the outermost mark, so an importer calling an importer cannot unmark itself halfway and
  leave the remaining rows routing.

Deliberately not a stored flag on the row: a flag records what happened afterwards and can be forgotten at
the one call site that matters. **Marking imported rows is still worth doing** and is not what this is --
it remains open, along with giving import its own permission.

---

## What has to be decided

Everything else follows from these two.

**Decision A — should the employee profile route like self-service does?** (point 5)

Yes means: one rule, visible on screen, honouring what you configured. It also means leave typed by HR stops
counting immediately unless point 7's bypass is granted — which is a real change to how HR works and needs
saying out loud before it ships.

**Decision B — per-type bypass plus a permission, or a separate workbench?** (point 7)

The recommendation is the former. A workbench is a second copy of every form, and it still needs
permissioning, so it buys a screen and solves nothing the permission does not.

## Ordering, once those are settled

1. Universal routing check (#5) **together with** the import exclusion (#8) — they are one change, and doing
   #5 without #8 floods inboxes on the next import.
2. The bypass setting and permission (#7) — needed before #5 can ship, or HR loses direct entry with no
   replacement.
3. Button labels from the catalog (#6) — cheap, and makes #5 legible to users.
4. Catalog-driven ESS menu (#3) — independent, do whenever.
5. `Code` validation (#1) and the chain shortcut (#2) — small quality-of-life items.

Steps 1–3 are one piece of work and should not be split across releases: each on its own leaves the product
in a state that is worse than today.
