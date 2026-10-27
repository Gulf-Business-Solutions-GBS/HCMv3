---
helpId: gmi.clearance-request-field-detail.edit
lang: ar
title: تفاصيل بنود براءة الذمة — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.ClearanceRequestFieldDetail
surfaces:
  - angular/src/app/admin/general-master-info/clearance-request-field-details/create-or-edit-clearance-request-field-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.ClearanceRequestFieldDetails.Create]
---

## Overview

يُسند بنداً واحداً من قائمة إخلاء الطرف عند المغادرة إلى إدارة معيّنة. ففي خطوة الاعتماد المتوازي ينظر معتمِد كل إدارة إلى طلب إخلاء الطرف نفسه في الوقت نفسه، ولا يُعرض على كلٍّ منهم إلا البنود المسندة إلى دوره -- فتجيب تقنية المعلومات عن بنودها، والمالية عن بنودها. ويمكن إسناد البند الواحد إلى أكثر من إدارة.

## ClearanceRequestField {#ClearanceRequestFieldDetail.ClearanceRequestFieldId}
<!-- alias: loc:ClearanceRequestField, dom:ClearanceRequestFieldDetail_ClearanceRequestFieldId -->

**ما هو.** بند القائمة الذي يجري إسناده. ولا يظهر عند فتح النافذة من صفحة البند نفسه، إذ يُملأ تلقائياً.

**القواعد.** اختياري. يُختار من قائمة بنود براءة الذمة.

## WorkflowRole {#ClearanceRequestFieldDetail.WorkflowRoleId}
<!-- alias: loc:WorkflowRole, dom:ClearanceRequestFieldDetail_WorkflowRoleId -->

**ما هو.** دور سير العمل الذي يجيب عن هذا البند من قائمة التحقق أثناء اعتماد إخلاء الطرف.

**مثال.** قسم تقنية المعلومات، للبند «تسليم الحاسوب المحمول»

**لماذا يهم.** عندما يصل طلب إخلاء الطرف إلى معتمِد، لا يرى إلا بنود قائمة التحقق التي يشغل دورها في ذلك اليوم، ويُسجَّل البند باسمه. أما المعتمِد الذي لا يشغل الدور فلا يرى البند، لذا فإن البند المسند إلى دور لا يشغله أحد لا يُجاب عنه أبداً.

**القواعد.** إلزامي. يُختار من قائمة أدوار سير العمل.
