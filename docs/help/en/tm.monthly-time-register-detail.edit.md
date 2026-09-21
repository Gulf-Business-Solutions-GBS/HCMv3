---
helpId: tm.monthly-time-register-detail.edit
lang: en
title: Monthly Time Register Details — Create / Edit
entity: HCMv3.TimeManagement.MonthlyTimeRegisterDetail
surfaces:
  - angular/src/app/admin/time-management/monthly-time-register-details/create-or-edit-monthly-time-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.MonthlyTimeRegisterDetails.Create]
---

## Overview

One time type's value in a monthly register.

Not written at present: no process fills these registers and nothing reads them. Payroll and the reports work from the daily time register.

## MonthlyTimeRegister {#MonthlyTimeRegisterDetail.MonthlyTimeRegisterId}
<!-- alias: loc:MonthlyTimeRegister, dom:MonthlyTimeRegisterDetail_MonthlyTimeRegisterId -->

**What it is.** The monthly register this line belongs to.

**Rules.** Required. Chosen from Monthly Time Register master data.

## TimeType {#MonthlyTimeRegisterDetail.TimeTypeId}
<!-- alias: loc:TimeType, dom:MonthlyTimeRegisterDetail_TimeTypeId -->

**What it is.** The time type the value is for.

**Rules.** Required. Chosen from Time Type master data.

## TimeValue {#MonthlyTimeRegisterDetail.TimeValue}
<!-- alias: loc:TimeValue, dom:MonthlyTimeRegisterDetail_TimeValue -->

**What it is.** The value.

**Example.** `2.5`

**Rules.** Optional.
