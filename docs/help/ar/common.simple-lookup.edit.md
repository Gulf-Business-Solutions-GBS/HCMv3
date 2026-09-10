---
helpId: common.simple-lookup.edit
lang: ar
title: القوائم البسيطة — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.SimpleLookupEntity
surfaces:
  - angular/src/app/admin/general-master-info/shared/simple-lookup-modal.component.html
audience: [admin]
permissions: []
---

## Overview

النموذج المشترك للقوائم البسيطة في HCMv3 -- وهي القوائم التي لا يحمل كل بند فيها إلا رمزاً واسماً بالإنجليزية واسماً بالعربية، مثل الألقاب والدول والديانات والحالات الاجتماعية والمسميات الوظيفية. وتستخدمه أكثر من تسعين قائمة في جميع الوحدات، لذا فالحقول والقواعد في هذا النموذج واحدة أياً كانت القائمة التي تعدّلها.

إذا تُرك الرمز فارغاً، يُمنح الرقم التالي عند حفظ البند. ويجب ألا يتكرر الرمز ولا الاسم بالإنجليزية ولا الاسم بالعربية داخل القائمة.
