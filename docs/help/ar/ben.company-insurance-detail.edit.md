---
helpId: ben.company-insurance-detail.edit
lang: ar
title: تفاصيل تأمين الشركة — إنشاء / تحرير
entity: HCMv3.BenefitsManagement.CompanyInsuranceDetail
surfaces:
  - angular/src/app/admin/benefits-management/company-insurance-details/create-or-edit-company-insurance-detail-modal.component.html
audience: [admin]
permissions: [Pages.BenefitsManagement.CompanyInsuranceDetails.Create]
---

## Overview

دفعة واحدة من حصة الشركة في تأمين الموظف. والرصيد المتبقي للاسترداد هو مساهمة الشركة مطروحاً منها كل هذه الأسطر أياً كان نوعها. وفي كل شهر تستبدل الرواتب سطر "عن طريق الرواتب" لذلك الشهر بسطر جديد بمبلغ القسط المحدد في التأمين، مقلَّصاً إلى ما يسمح به صافي الراتب والرصيد؛ وأضف يدوياً أسطراً من نوع "خارجي" أو "إعفاء" للمبالغ المسوّاة بطريقة أخرى. ولا يجوز أن يتجاوز المبلغ الرصيد المتبقي، ولا أن يكون التاريخ قبل تاريخ بدء أقساط التأمين.

## EmployeeInsuranceId {#CompanyInsuranceDetail.EmployeeInsuranceId}
<!-- alias: loc:EmployeeInsuranceId, dom:CompanyInsuranceDetail_EmployeeInsuranceId -->

**ما هو.** تأمين الموظف.

**القواعد.** إلزامي. يُختار من قائمة.

## InstallmentDate {#CompanyInsuranceDetail.InstallmentDate}
<!-- alias: loc:InstallmentDate, dom:CompanyInsuranceDetail_InstallmentDate -->

**ما هو.** تاريخ الدفعة.

**القواعد.** إلزامي. تاريخ.

## InstallmentAmount {#CompanyInsuranceDetail.InstallmentAmount}
<!-- alias: loc:InstallmentAmount, dom:CompanyInsuranceDetail_InstallmentAmount -->

**ما هو.** المبلغ المدفوع.

**مثال.** 15.000

**القواعد.** اختياري. رقم.

## InstallmentType {#CompanyInsuranceDetail.InstallmentType}
<!-- alias: loc:InstallmentType, dom:CompanyInsuranceDetail_InstallmentType -->

**ما هو.** طريقة تسوية المبلغ.

**مثال.** خارجي

**لماذا يهم.** أسطر "عن طريق الرواتب" تكتبها الرواتب. وأسطر "نهاية الخدمة" المؤرخة في الشهر تُخصم في تشغيل خارج الدورة لنهاية الخدمة. أما "خارجي" و"إعفاء" فيقللان الرصيد فقط.

**القواعد.** إلزامي. إحدى القيم: عن طريق الرواتب، خارجي، نهاية الخدمة، إعفاء.
