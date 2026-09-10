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

Number series for generating codes. Not used at present: employee codes, for example, are assigned as the highest existing number plus one, not from a number series.

## Name {#NumberSeries.Name}
<!-- alias: loc:Name, dom:NumberSeries_Name -->

**What it is.** A name to recognise the series by.

**Rules.** Optional.

## Start {#NumberSeries.Start}
<!-- alias: loc:Start, dom:NumberSeries_Start -->

**What it is.** The first number in the series.

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
