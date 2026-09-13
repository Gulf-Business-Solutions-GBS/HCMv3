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

## ActualReturnDate {#EmployeeDocumentMvmt.ActualReturnDate}
<!-- alias: loc:ActualReturnDate, dom:Mvmt_ActualReturnDate -->

**ما هو.** التاريخ الذي أُرجعت فيه الوثيقة فعلياً.

**مثال.** 18 يوليو 2026

**لماذا يهم.** اتركه فارغاً ما دامت الوثيقة خارجة.

**القواعد.** اختياري. تاريخ.
