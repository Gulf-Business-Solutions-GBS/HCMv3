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

**ما هو.** بند القائمة الذي يجري إسناده.

**القواعد.** اختياري. يُختار من قائمة بنود براءة الذمة.
