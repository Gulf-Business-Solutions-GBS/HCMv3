---
helpId: gmi.number-series.edit
lang: en
title: Number Series — Create / Edit
entity: HCMv3.GeneralMasterInfo.NumberSeries
surfaces:
  - angular/src/app/admin/general-master-info/number-series/create-or-edit-number-series-modal.component.html
audience: [admin]
permissions: []
---

## Overview

Number series for employee codes. A new employee saved without a code is numbered from the series for their employment type: the highest number already used with the series prefix, plus one, written in the series format and prefixed. With no series at all, employee codes are the highest existing number plus one.

## Name {#NumberSeries.Name}
<!-- alias: loc:Name, dom:NumberSeries_Name -->

**What it is.** A name to recognise the series by.

**Rules.** Optional.

## Start {#NumberSeries.Start}
<!-- alias: loc:Start, dom:NumberSeries_Start -->

**What it is.** The first number the series was meant to start from.

**Why it matters.** Not read at present: a new series always starts at 1.

**Rules.** Optional. Whole number.

## Prefix {#NumberSeries.Prefix}
<!-- alias: loc:Prefix, dom:NumberSeries_Prefix -->

**What it is.** Text put in front of each number.

**Example.** `EMP-`

**Rules.** Optional. Maximum 64 characters.

## Formatting {#NumberSeries.Formatting}
<!-- alias: loc:Formatting, dom:NumberSeries_Formatting -->

**What it is.** The pattern numbers are written in.

**Rules.** Required. Maximum 64 characters.

## EmploymentType {#NumberSeries.EmploymentType}
<!-- alias: loc:EmploymentType, dom:NumberSeries_EmploymentType -->

**What it is.** Which employees the series numbers: one employment type, or All for every employee.

**Example.** Permanent -- permanent staff are numbered `EMP-0001`, `EMP-0002`…, while a Temporary series numbers temporary staff separately.

**Why it matters.** A new employee saved without a code gets the next number of the series for their employment type, counted over the codes of employees of that type. An All series wins over the others and is counted over every employee. Changing an employee to a type that has its own series gives them a new code from it. With no matching series, the code is the highest existing number plus one. There can be one series per employment type, and an All series must be the only one.

**Rules.** Required. One of: Permanent, Temporary, All.
