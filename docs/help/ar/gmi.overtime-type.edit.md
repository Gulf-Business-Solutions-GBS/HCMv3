---
helpId: gmi.overtime-type.edit
lang: ar
title: أنواع العمل الإضافي — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.OvertimeType
surfaces:
  - angular/src/app/admin/general-master-info/overtime-types/create-or-edit-overtime-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.OvertimeTypes.Create]
---

## Overview

أنواع العمل الإضافي -- في يوم عمل، أو يوم راحة، أو عطلة رسمية، ونحو ذلك. يسمّي كل طلب عمل إضافي نوعاً منها، ويمكن للدوامات والعطل أن تشير إلى النوع الذي ينطبق عليها.

## OvertimeRate {#OvertimeType.OvertimeRate}
<!-- alias: loc:OvertimeRate, dom:OvertimeType_OvertimeRate -->

**ما هو.** المُضاعِف الذي يُطبَّق على ساعات العمل الإضافي المعتمدة.

**مثال.** `1.25`

**لماذا يهم.** يضرب احتساب الرواتب ساعات العمل الإضافي المعتمدة في هذا المعدل قبل تسعيرها، وتستخدم تكاليف المشاريع المُضاعِف نفسه.

**القواعد.** إلزامي. رقم.

## WageType {#OvertimeType.WageTypeId}
<!-- alias: loc:WageType, dom:OvertimeType_WageTypeId -->

**ما هو.** نوع الأجر الذي يُصرف تحته العمل الإضافي من هذا النوع.

**مثال.** `OT125 - عمل إضافي 125%`

**لماذا يهم.** لا يصرف احتساب الرواتب العمل الإضافي المعتمد إلا لأنواع العمل الإضافي التي لها نوع أجر: فيُسعِّر الساعات عند وصوله إلى نوع الأجر هذا ويُدرج المبلغ تحته في كشف الراتب. وإذا تُرك فارغاً يُسجَّل العمل الإضافي من هذا النوع ولكنه لا يُصرف أبداً. ويستخدمه أيضاً تقرير ملخص المشاريع الشهري لتمييز تكلفة العمل الإضافي عن غيرها من الأجور.

**القواعد.** اختياري. يُختار من قائمة أنواع الأجور.
