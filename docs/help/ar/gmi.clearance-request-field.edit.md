---
helpId: gmi.clearance-request-field.edit
lang: ar
title: بنود براءة الذمة — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.ClearanceRequestField
surfaces:
  - angular/src/app/admin/general-master-info/clearance-request-fields/create-or-edit-clearance-request-field-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.ClearanceRequestFields.Create]
---

## Overview

بنود قائمة إخلاء الطرف -- «تسليم الحاسوب المحمول»، «تسليم بطاقة الدخول» -- التي تؤكدها الإدارات قبل مغادرة الموظف.

يفتح خيار التفاصيل على أي بند في القائمة صفحة البند نفسه: بياناته في الأعلى مع زر التحرير، وتحتها الإدارات (أدوار سير العمل) المسند إليها، ويمكن إضافتها وتعديلها وحذفها من هناك.

## ClearanceRequestType {#ClearanceRequestField.ClearanceRequestTypeId}
<!-- alias: loc:ClearanceRequestType, dom:ClearanceRequestField_ClearanceRequestTypeId -->

**ما هو.** نوع إخلاء الطرف الذي ينتمي إليه هذا البند.

**لماذا يهم.** البند الذي له نوع لا يظهر إلا في طلبات إخلاء الطرف من ذلك النوع. أما البند الذي لا نوع له فيظهر في كل طلب.

**القواعد.** اختياري. يُختار من قائمة أنواع طلبات براءة الذمة.

## IsActive {#ClearanceRequestField.IsActive}
<!-- alias: loc:IsActive, dom:ClearanceRequestField_IsActive -->

**ما هو.** هل البند قيد الاستخدام.

**لماذا يهم.** لا تُدرج في قوائم إخلاء الطرف إلا البنود المفعّلة.

**القواعد.** نعم أو لا.
