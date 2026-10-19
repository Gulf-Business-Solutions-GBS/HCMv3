---
helpId: pa.overtime-batch.edit
lang: en
title: Overtime Batches — Create / Edit
entity: HCMv3.PersonnelAdministration.OvertimeBatch
surfaces:
  - angular/src/app/admin/personnel-administration/overtime-batches/create-or-edit-overtime-batch-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.OvertimeBatches.Create]
---

## Overview

Overtime for a whole department over a date range, put forward as one request for approval instead of one overtime record at a time.

Choose the department and the dates, tick **Overtime days only** to see just the days the time register calculated overtime on, and press **Get Data**. The grid then lists, from the time register, one line per day for every active employee assigned to the department or to any unit below it: shift start and end, sign in and out, planned hours, overtime hours, late in and early out. On each line you decide the **Approved hours**, the **Overtime Type** and a comment. Approved hours start at 0, and only lines with approved hours above 0 or a comment are kept; **Submit** stays disabled until there is at least one.

A day that is already on another batch still standing (not sent, waiting for approval, approved or recorded) is left out of the grid. A day that already has an overtime record is shown highlighted and cannot be filled in. Submitting refuses the same employee and day twice, a day outside the dates, a day already on another live batch, a day already recorded, and an employee who is not in the department.

The batch is routed by the person who submits it, so with approval switched on your user must be linked to an employee. Nothing is written to the employees' overtime until the batch is approved; then one overtime record is written per line, once, dated the line's day, with the line's overtime hours and approved hours. Every record is written under the overtime type with code 4, whatever type the line shows, as in the old system — so the code-4 type's rates apply. Without a code-4 overtime type the approval is refused. A day recorded by someone else while the batch was waiting also refuses the approval, and nothing from the batch is written.

When overtime batches are not set up for approval, submitting records the overtime at once and the list shows the batch as **Recorded**. A batch can be edited or deleted only while it is with nobody — sent back, or never routed and not yet recorded; a sent-back batch is sent again with **Submit** in its Actions menu.

:::admin
Overtime batches are the "Overtime Batch" type under Workflow Transaction Types; whether they go to approval is set there. Create the overtime type with code 4 under Overtime Types before the first approval.
:::

## Department {#OvertimeBatch.OrganizationUnitId}
<!-- alias: loc:Department, dom:OvertimeBatch_OrganizationUnitId -->

**What it is.** The organization unit whose people are loaded. Units below it are included.

**Example.** Operations — which also brings in Operations / Warehouse and Operations / Fleet.

**Why it matters.** Only active employees with an org assignment to this unit or one below it can be on the batch. The list shows every unit in tree order.

**Rules.** Required. Chosen from Organization Unit master data.

## StartDate {#OvertimeBatch.StartDate}
<!-- alias: loc:StartDate, dom:OvertimeBatch_StartDate -->

**What it is.** The first day of the period to load.

**Example.** 1 Oct 2026

**Why it matters.** Every line must fall between the start and end dates. Changing the dates after **Get Data** does not reload the grid; press it again. What you already entered on a day that is still in the new range is kept.

**Rules.** Required. Date value.

## EndDate {#OvertimeBatch.EndDate}
<!-- alias: loc:EndDate, dom:OvertimeBatch_EndDate -->

**What it is.** The last day of the period to load.

**Example.** 15 Oct 2026

**Why it matters.** It cannot be before the start date.

**Rules.** Required. Date value.

## RequestComments {#OvertimeBatch.Comments}
<!-- alias: loc:RequestComments, dom:OvertimeBatch_Comments -->

**What it is.** A note for the approvers about the whole batch. Each line has its own comment as well.

**Example.** Stock count weekend, overtime agreed with the warehouse manager.

**Rules.** Optional. Maximum 4000 characters.
