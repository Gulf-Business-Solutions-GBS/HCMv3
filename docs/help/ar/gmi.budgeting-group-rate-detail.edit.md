---
helpId: gmi.budgeting-group-rate-detail.edit
lang: ar
title: تفاصيل نسب مجموعة الموازنة — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.BudgetingGroupRateDetail
surfaces:
  - angular/src/app/admin/general-master-info/budgeting-group-rate-details/create-or-edit-budgeting-group-rate-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.BudgetingGroupRateDetails.Create]
---

## Overview

نسب المكافأة لمجموعة موازنة خلال فترة زمنية. وهي مسجلة للرجوع إليها فقط: لا يقرأ أي احتساب هذه النسب حالياً.

## BudgetingGroup {#BudgetingGroupRateDetail.BudgetingGroupId}
<!-- alias: loc:BudgetingGroup, dom:BudgetingGroupRateDetail_BudgetingGroupId -->

**ما هو.** مجموعة الموازنة التي تنتمي إليها هذه النسبة.

**القواعد.** اختياري. يُختار من قائمة مجموعات الموازنة.

## StartDate {#BudgetingGroupRateDetail.StartDate}
<!-- alias: loc:StartDate, dom:BudgetingGroupRateDetail_StartDate -->

**ما هو.** أول تاريخ تنطبق عليه النسبة.

**القواعد.** اختياري. تاريخ.

## EndDate {#BudgetingGroupRateDetail.EndDate}
<!-- alias: loc:EndDate, dom:BudgetingGroupRateDetail_EndDate -->

**ما هو.** آخر تاريخ تنطبق عليه النسبة.

**القواعد.** اختياري. تاريخ.

## BonusRate {#BudgetingGroupRateDetail.BonusRate}
<!-- alias: loc:BonusRate, dom:BudgetingGroupRateDetail_BonusRate -->

**ما هو.** نسبة المكافأة للمجموعة خلال الفترة.

**القواعد.** إلزامي. رقم.
