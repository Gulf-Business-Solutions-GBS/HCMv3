---
helpId: tm.time-transfer.edit
lang: ar
title: عمليات نقل الوقت — إنشاء / تحرير
entity: HCMv3.TimeManagement.TimeTransfer
surfaces:
  - angular/src/app/admin/time-management/time-transfers/create-or-edit-time-transfer-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimeTransfers.Create]
---

## Overview

تعديل يدوي لأحد أرصدة الوقت للموظف في تاريخ معيّن. وما يعدّله يتوقف على نوع التحويل، الذي يتعرّف عليه نظام الرواتب ومحرك الوقت برمزه:

- `1` يُضاف إلى الإجازة السنوية المستحقة في ذلك التاريخ.
- `2` يُعدّ إجازة مرضية مأخوذة مسبقاً في الدورة.
- `5` يُعدّ أيام غياب تُخصم من مدة الخدمة التي تُحتسب عليها مكافأة نهاية الخدمة.
- `6` يُرحّل مبلغاً افتتاحياً إلى صندوق الموظف في فترة الدفع تلك.

## Employee {#TimeTransfer.EmployeeId}
<!-- alias: loc:Employee, dom:TimeTransfer_EmployeeId -->

**ما هو.** الموظف الذي يُعدَّل رصيده.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## TransferDate {#TimeTransfer.TransferDate}
<!-- alias: loc:TransferDate, dom:TimeTransfer_TransferDate -->

**ما هو.** تاريخ سريان التعديل.

**مثال.** `2026-01-01`

**القواعد.** إلزامي. تاريخ.

## TransferType {#TimeTransfer.TransferTypeId}
<!-- alias: loc:TransferType, dom:TimeTransfer_TransferTypeId -->

**ما هو.** الرصيد الذي يُعدَّل.

**مثال.** الرصيد الافتتاحي للإجازة السنوية

**القواعد.** إلزامي. يُختار من قائمة أنواع نقل الوقت.

## TransferCount {#TimeTransfer.TransferCount}
<!-- alias: loc:TransferCount, dom:TimeTransfer_TransferCount -->

**ما هو.** أيام التعديل -- أو مبلغه في تحويل الصندوق.

**مثال.** `12.5`

**لماذا يهم.** يُقرَّب إلى ثلاث منازل عشرية عند الحفظ.

**القواعد.** اختياري. رقم.
