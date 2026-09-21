---
helpId: trv.travel.edit
lang: ar
title: السفريات — إنشاء / تحرير
entity: HCMv3.TravelManagement.Travel
surfaces:
  - angular/src/app/admin/travel-management/travels/create-or-edit-travel-modal.component.html
audience: [admin]
permissions: [Pages.TravelManagement.Travels.Create]
---

## Overview

رحلة عمل لموظف، مع مراحلها وتكاليفها. تذهب الرحلة المُدخلة هنا إلى الموافقة تماماً كالرحلة التي يطلبها الموظف في الخدمة الذاتية، وتخضع للقواعد نفسها: يلزم مرحلة واحدة على الأقل، ولا يمكن أن يكون تاريخ الانتهاء قبل تاريخ البدء، ولا يجوز أن تتداخل التواريخ مع رحلة أخرى للموظف موافق عليها أو بانتظار الموافقة. ويرى المعتمد الوجهة والتواريخ والتكلفة الإجمالية. اترك الرمز فارغاً ليُعطى الرقم التالي. ولا تُدفع الرحلات عبر الرواتب.

## Employee {#Travel.EmployeeId}
<!-- alias: loc:Employee, dom:Travel_EmployeeId -->

**ما هو.** الموظف المسافر.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## TravelType {#Travel.TravelTypeId}
<!-- alias: loc:TravelType, dom:Travel_TravelTypeId -->

**ما هو.** نوع الرحلة.

**مثال.** اجتماع عمل

**القواعد.** إلزامي. يُختار من قائمة أنواع السفر.
