---
helpId: ben.employee-insurance-detail.edit
lang: ar
title: تفاصيل تأمينات الموظفين — إنشاء / تحرير
entity: HCMv3.BenefitsManagement.EmployeeInsuranceDetail
surfaces:
  - angular/src/app/admin/benefits-management/employee-insurance-details/create-or-edit-employee-insurance-detail-modal.component.html
audience: [admin]
permissions: [Pages.BenefitsManagement.EmployeeInsuranceDetails.Create]
---

## Overview

دفعة واحدة من حصة الموظف في تأمين الموظف. والرصيد المتبقي للاسترداد هو مساهمة الموظف مطروحاً منها كل هذه الأسطر أياً كان نوعها. وفي كل شهر تستبدل الرواتب سطر "عن طريق الرواتب" لذلك الشهر بسطر جديد بمبلغ القسط المحدد في التأمين، مقلَّصاً إلى ما يسمح به صافي الراتب والرصيد؛ وأضف يدوياً أسطراً من نوع "خارجي" أو "إعفاء" للمبالغ المسوّاة بطريقة أخرى. ولا يجوز أن يتجاوز المبلغ الرصيد المتبقي، ولا أن يكون التاريخ قبل تاريخ بدء أقساط التأمين.

## EmployeeInsuranceId {#EmployeeInsuranceDetail.EmployeeInsuranceId}
<!-- alias: loc:EmployeeInsuranceId, dom:EmployeeInsuranceDetail_EmployeeInsuranceId -->

**ما هو.** تأمين الموظف.

**القواعد.** إلزامي. يُختار من قائمة.

## InstallmentDate {#EmployeeInsuranceDetail.InstallmentDate}
<!-- alias: loc:InstallmentDate, dom:EmployeeInsuranceDetail_InstallmentDate -->

**ما هو.** تاريخ الدفعة.

**القواعد.** إلزامي. تاريخ.

## InstallmentAmount {#EmployeeInsuranceDetail.InstallmentAmount}
<!-- alias: loc:InstallmentAmount, dom:EmployeeInsuranceDetail_InstallmentAmount -->

**ما هو.** المبلغ المدفوع.

**مثال.** 15.000

**القواعد.** اختياري. رقم.

## InstallmentType {#EmployeeInsuranceDetail.InstallmentType}
<!-- alias: loc:InstallmentType, dom:EmployeeInsuranceDetail_InstallmentType -->

**ما هو.** طريقة تسوية المبلغ.

**مثال.** خارجي

**لماذا يهم.** أسطر "عن طريق الرواتب" تكتبها الرواتب. وأسطر "نهاية الخدمة" المؤرخة في الشهر تُخصم في تشغيل خارج الدورة لنهاية الخدمة. أما "خارجي" و"إعفاء" فيقللان الرصيد فقط.

**القواعد.** إلزامي. إحدى القيم: عن طريق الرواتب، خارجي، نهاية الخدمة، إعفاء.
