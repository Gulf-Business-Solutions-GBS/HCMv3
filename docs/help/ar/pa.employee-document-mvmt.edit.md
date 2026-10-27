---
helpId: pa.employee-document-mvmt.edit
lang: ar
title: حركة الوثائق — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeDocumentMvmt
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/documents/employee-document-movements-modal.component.html
audience: [admin]
permissions: []
---

## Overview

سحب إحدى وثائق الموظف وإرجاعها -- كتسليم جواز السفر للموظف من أجل السفر.

ما دامت الحركة بلا تاريخ إرجاع فعلي، تظهر الوثيقة خارجة. ويمكن للموظفين طلب سحب جواز السفر عبر الخدمة الذاتية.

## HandedOverOn {#EmployeeDocumentMvmt.ActualWithdrawDate}
<!-- alias: loc:HandedOverOn, dom:Mvmt_HandOverDate -->

**ما هو.** اليوم الذي سُلِّم فيه المستند فعلياً إلى الموظف.

**لماذا يهم.** تسجيله يجعل المستند في عهدة الموظف. ولا يجوز أن يكون بعد اليوم.

**القواعد.** إلزامي. تاريخ.

## ActualReturnDate {#EmployeeDocumentMvmt.ActualReturnDate}
<!-- alias: loc:ActualReturnDate, dom:Mvmt_ActualReturnDate -->

**ما هو.** التاريخ الذي أُرجعت فيه الوثيقة فعلياً.

**مثال.** 18 يوليو 2026

**لماذا يهم.** اتركه فارغاً ما دامت الوثيقة خارجة.

**القواعد.** اختياري. تاريخ.

## ReturnRemarks {#EmployeeDocumentMvmt.ReturnRemarks}
<!-- alias: loc:ReturnRemarks, dom:Mvmt_ReturnRemarks -->

**ما هو.** ملاحظة تُدوَّن عند إعادة المستند.

**مثال.** أُعيد جواز السفر بحالة جيدة؛ ختم تأشيرة جديد في الصفحة 12.

**لماذا يهم.** للعلم فقط. وعندما تأتي الإعادة عن طريق طلب خدمة ذاتية، يراها المعتمِدون على الطلب.

**القواعد.** اختياري.

## WithdrawDate {#EmployeeDocumentMvmt.WithdrawDate}
<!-- alias: loc:WithdrawDate, dom:Mvmt_WithdrawDate -->

**ما هو.** تاريخ سحب الوثيقة.

**مثال.** 1 يوليو 2026

**القواعد.** إلزامي. تاريخ.

## ReturnDate {#EmployeeDocumentMvmt.ReturnDate}
<!-- alias: loc:ReturnDate, dom:Mvmt_ReturnDate -->

**ما هو.** التاريخ المتوقع لإرجاع الوثيقة.

**مثال.** 20 يوليو 2026

**القواعد.** إلزامي. تاريخ.
