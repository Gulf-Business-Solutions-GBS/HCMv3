---
helpId: tm.yearly-time-register-detail.edit
lang: en
title: Yearly Time Register Details — Create / Edit
entity: HCMv3.TimeManagement.YearlyTimeRegisterDetail
surfaces:
  - angular/src/app/admin/time-management/yearly-time-register-details/create-or-edit-yearly-time-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.YearlyTimeRegisterDetails.Create]
---

## Overview

One time type's value in a yearly register.

Not written at present: no process fills these registers and nothing reads them. Payroll and the reports work from the daily time register.

## YearlyTimeRegister {#YearlyTimeRegisterDetail.YearlyTimeRegisterId}
<!-- alias: loc:YearlyTimeRegister, dom:YearlyTimeRegisterDetail_YearlyTimeRegisterId -->

**What it is.** The yearly register this line belongs to.

**Rules.** Required. Chosen from Yearly Time Register master data.

## TimeType {#YearlyTimeRegisterDetail.TimeTypeId}
<!-- alias: loc:TimeType, dom:YearlyTimeRegisterDetail_TimeTypeId -->

**What it is.** The time type the value is for.

**Rules.** Required. Chosen from Time Type master data.

## TimeValue {#YearlyTimeRegisterDetail.TimeValue}
<!-- alias: loc:TimeValue, dom:YearlyTimeRegisterDetail_TimeValue -->

**What it is.** The value.

**Example.** `2.5`

**Rules.** Optional.
