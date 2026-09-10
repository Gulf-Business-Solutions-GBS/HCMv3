---
helpId: gmi.overtime-type-detail.edit
lang: ar
title: تفاصيل أنواع العمل الإضافي — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.OvertimeTypeDetail
surfaces:
  - angular/src/app/admin/general-master-info/overtime-type-details/create-or-edit-overtime-type-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.OvertimeTypeDetails.Create]
---

## Overview

إعدادات أسعار مؤرخة لنوع عمل إضافي. عند صرف العمل الإضافي، يبحث احتساب الرواتب عن تفصيل نوع العمل الإضافي الذي تشمل تواريخه تاريخ الصرف.

## OvertimeType {#OvertimeTypeDetail.OvertimeTypeId}
<!-- alias: loc:OvertimeType, dom:OvertimeTypeDetail_OvertimeTypeId -->

**ما هو.** نوع العمل الإضافي الذي تنتمي إليه هذه الإعدادات.

**القواعد.** اختياري. يُختار من قائمة أنواع العمل الإضافي.

## StartDate {#OvertimeTypeDetail.StartDate}
<!-- alias: loc:StartDate, dom:OvertimeTypeDetail_StartDate -->

**ما هو.** أول تاريخ صرف تنطبق عليه هذه الإعدادات.

**القواعد.** اختياري. تاريخ.

## EndDate {#OvertimeTypeDetail.EndDate}
<!-- alias: loc:EndDate, dom:OvertimeTypeDetail_EndDate -->

**ما هو.** آخر تاريخ صرف تنطبق عليه هذه الإعدادات.

**القواعد.** اختياري. تاريخ.

## OvertimeRateType {#OvertimeTypeDetail.OvertimeRateType}
<!-- alias: loc:OvertimeRateType, dom:OvertimeTypeDetail_OvertimeRateType -->

**ما هو.** طريقة تحديد سعر الساعة للعمل الإضافي.

**لماذا يهم.** «ثابت»: السعر أدناه هو سعر الساعة. «فعلي»: يُحتسب سعر الساعة من عناصر الأجر المحددة للاحتساب في العمل الإضافي. وإذا لم يشمل أي تفصيل تاريخ الصرف، يُستخدم «فعلي».

**القواعد.** إلزامي. إحدى القيم: فعلي، ثابت.

## Rate {#OvertimeTypeDetail.Rate}
<!-- alias: loc:Rate, dom:OvertimeTypeDetail_Rate -->

**ما هو.** سعر الساعة المستخدم عندما يكون نوع السعر «ثابت».

**القواعد.** إلزامي. رقم.
