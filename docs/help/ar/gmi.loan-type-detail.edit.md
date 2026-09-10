---
helpId: gmi.loan-type-detail.edit
lang: ar
title: تفاصيل أنواع القروض — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.LoanTypeDetail
surfaces:
  - angular/src/app/admin/general-master-info/loan-type-details/create-or-edit-loan-type-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.LoanTypeDetails.Create]
---

## Overview

نسب مرتبطة بنوع قرض. لا تُستخدم حالياً: فهي تخص الاسترداد المتغير للقرض، الذي لا يدعمه احتساب الرواتب بعدُ.

## LoanType {#LoanTypeDetail.LoanTypeId}
<!-- alias: loc:LoanType, dom:LoanTypeDetail_LoanTypeId -->

**ما هو.** نوع القرض الذي تنتمي إليه النسبة.

**القواعد.** اختياري. يُختار من قائمة أنواع القروض.

## Percent {#LoanTypeDetail.Percent}
<!-- alias: loc:Percent, dom:LoanTypeDetail_Percent -->

**ما هو.** النسبة.

**القواعد.** إلزامي. رقم.
