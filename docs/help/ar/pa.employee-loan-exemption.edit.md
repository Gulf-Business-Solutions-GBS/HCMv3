---
helpId: pa.employee-loan-exemption.edit
lang: ar
title: إعفاءات قروض الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeLoanExemption
surfaces:
  - angular/src/app/admin/personnel-administration/employee-loan-exemptions/create-or-edit-employee-loan-exemption-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeLoanExemptions.Create]
---

## Overview

يُعفي من قسط شهر واحد على أحد قروض الموظف. ولا تسترد الرواتب القرض في شهر فيه إعفاء.

يجب أن يقع تاريخ الإعفاء ضمن فترة استرداد القرض، ولا يجوز أن يكون للقرض أكثر من إعفاء واحد في الشهر. ويمكن طلب الإعفاء عبر الخدمة الذاتية وتوجيهه للاعتماد.

وما إن يسري الإعفاء -- بتسجيله دون حاجة إلى اعتماد، أو باعتماده -- حتى يُطبَّق فوراً: إذا كان خيار **التحديث التلقائي لتاريخ نهاية استرداد القرض** مفعّلاً في إعدادات الرواتب يتأخر تاريخ نهاية استرداد القرض، ويعيد تشغيل الرواتب التالي احتساب رواتب الموظف اعتباراً من اليوم الأول من شهر الإعفاء. ونقل الإعفاء إلى شهر آخر يعيد الاحتساب اعتباراً من أسبق الشهرين، وحذفه يعيد تاريخ النهاية إلى ما كان عليه ويعيد الاحتساب اعتباراً من شهره. أما الطلب الذي لا يزال بانتظار الاعتماد فلا يغيّر شيئاً من ذلك.

وعند فتحه من نافذة الأقساط والاستردادات والإعفاءات لقرض ما، يكون القرض معبأً مسبقاً ولا يمكن تغييره.

## EmployeeLoanId {#EmployeeLoanExemption.EmployeeLoanId}
<!-- alias: loc:EmployeeLoanId, dom:EmployeeLoanExemption_EmployeeLoanId -->

**ما هو.** القرض الذي يُعفى من قسطه.

**القواعد.** إلزامي. يُختار من قائمة.

## ExemptionDate {#EmployeeLoanExemption.ExemptionDate}
<!-- alias: loc:ExemptionDate, dom:EmployeeLoanExemption_ExemptionDate -->

**ما هو.** أي تاريخ في الشهر الذي يُعفى من قسطه.

**مثال.** 15 ديسمبر 2026

**لماذا يهم.** يجب أن يقع بين تاريخ بدء استرداد القرض وتاريخ انتهائه.

**القواعد.** إلزامي. تاريخ.
