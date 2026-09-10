---
helpId: gmi.overtime-type-detail.edit
lang: en
title: Overtime Type Details — Create / Edit
entity: HCMv3.GeneralMasterInfo.OvertimeTypeDetail
surfaces:
  - angular/src/app/admin/general-master-info/overtime-type-details/create-or-edit-overtime-type-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.OvertimeTypeDetails.Create]
---

## Overview

Dated rate settings for an overtime type. When overtime is paid, payroll looks for the detail of the overtime type whose dates cover the pay date.

## OvertimeType {#OvertimeTypeDetail.OvertimeTypeId}
<!-- alias: loc:OvertimeType, dom:OvertimeTypeDetail_OvertimeTypeId -->

**What it is.** The overtime type these settings belong to.

**Rules.** Optional. Chosen from Overtime Type master data.

## StartDate {#OvertimeTypeDetail.StartDate}
<!-- alias: loc:StartDate, dom:OvertimeTypeDetail_StartDate -->

**What it is.** The first pay date these settings apply to.

**Rules.** Optional. Date value.

## EndDate {#OvertimeTypeDetail.EndDate}
<!-- alias: loc:EndDate, dom:OvertimeTypeDetail_EndDate -->

**What it is.** The last pay date these settings apply to.

**Rules.** Optional. Date value.

## OvertimeRateType {#OvertimeTypeDetail.OvertimeRateType}
<!-- alias: loc:OvertimeRateType, dom:OvertimeTypeDetail_OvertimeRateType -->

**What it is.** How the hourly rate for overtime is found.

**Why it matters.** Fixed: the rate below is the hourly rate. Actual: the hourly rate is worked out from the pay elements set to count towards overtime. When no detail covers the pay date, Actual is used.

**Rules.** Required. One of: Actual, Fixed.

## Rate {#OvertimeTypeDetail.Rate}
<!-- alias: loc:Rate, dom:OvertimeTypeDetail_Rate -->

**What it is.** The hourly rate used when the rate type is Fixed.

**Rules.** Required. Number.
