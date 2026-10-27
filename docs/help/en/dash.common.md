---
helpId: dash.common
lang: en
title: Dashboards — shared topics
audience: [end-user]
permissions: []
---

## Overview

Not a screen. This file holds the topics every dashboard shows — whose data you are looking at, the filter window and the download menu — written once here and borrowed by each dashboard, so the same thing is never explained two different ways. See DESIGN.md §5.1 and §5.4.

## Showing {#dash.common.showing}

**What it is.** The line in the toolbar that says whose data the dashboard is built from, and how many employees that is. You never choose it: it follows from who you are.

**How it is worked out.** The employees you see are:

- everyone, if you hold the **Admin** role;
- otherwise the employees in the organization units given to your roles in row-level security, including every unit below them, **plus** your own team — you and everybody below you in the manager hierarchy, at every level;
- nobody, if neither gives anyone. The dashboard then shows nothing, never everything.

**Example.** "Showing: Finance + Payroll + your team (123 employees)" — you are an HR officer for Finance and Payroll, and also manage a team of your own.

**Why it matters.** Two people can open the same dashboard and see different numbers, and both are right. Every figure, chart, list and download on the page is limited to these employees — changing a filter can narrow them further but never widen them.

:::admin
Resolved by `IDashboardScopeService` for every dashboard. The row-level security on/off switch in Security Settings is deliberately not consulted: with nothing assigned the user sees nothing, whatever the switch says. The Admin rule matches the static tenant role name `Admin`.
:::

## Filter {#dash.common.filter}

**What it is.** The window behind the **Filter** button: the period and the organization unit. Nothing changes until you press **Apply**; **Reset** goes back to the defaults.

**How the period works.**

- **This year to date** (the default): 1 January of this year up to today.
- **This year** and **Last year**: 1 January to 31 December.
- **This month**: the first to the last day of the current month.
- **Custom**: any From and To dates you choose. A period longer than five years is shortened to the last five.

**Example.** Pick **Last year** to see 2025 while this year's data is still thin.

**Why it matters.** Figures "as of today" (who is on leave today, headcount) do not depend on the period; figures "in the period" (leave days, joiners, leavers, turnover) do. Choosing an organization unit includes every unit below it. Clicking a bar or a slice on a chart adds another filter, shown as a chip next to **Showing** — remove it with its **×**, or all of them with **Clear filters**.

## Download {#dash.common.download}

**What it is.** The download icon on the toolbar and on every tile. Clicking it offers three formats.

**What each format contains.**

- **PDF** and **JPEG**: a picture of the tile exactly as it looks on screen, charts included. From the toolbar, a picture of the whole dashboard.
- **XLSX**: the data behind the tile, with the same filters. From the toolbar, a workbook with a **Summary** sheet (whose data, the period, the headline figures) and one sheet per tile.

**Example.** Send the **Leave by type** chart as a JPEG in an email; give payroll the **Not returned from leave** list as XLSX.

**Why it matters.** Whatever you download contains only what you are allowed to see, and exactly what the screen shows for the filters you have applied.
