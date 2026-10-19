---
helpId: org.org-unit.edit
lang: ar
title: وحدات الشركة — إنشاء / تحرير
entity: HCMv3.OrganizationManagement.OrgUnitProfile
surfaces:
  - angular/src/app/admin/organization-management/org-units/create-or-edit-org-unit-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.OrgUnits]
---

## Overview

وحدة في شجرة المنظمة -- الشركة أو القطاع أو القسم وما إلى ذلك -- مع اسميها بالإنجليزية والعربية والوحدة الأم. ولا يجوز أن تتجاوز الشجرة 10 مستويات. اترك الرمز فارغاً ليُعطى الرقم التالي. ويوضع الموظفون في وحدة في تعيينهم التنظيمي، وتجد التقارير وحدة الموظف في كل مستوى بالصعود من تلك الوحدة.

## OrganizationUnitType {#OrgUnitProfile.OrganizationUnitTypeId}
<!-- alias: loc:OrganizationUnitType, dom:OrgUnit_OrganizationUnitTypeId -->

**ما هو.** مستوى الوحدة في الشجرة.

**مثال.** قسم

**القواعد.** إلزامي. يُختار من قائمة أنواع وحدات الشركة.

## Address {#OrgUnitProfile.Address}
<!-- alias: loc:Address, dom:OrgUnit_Address -->

**ما هو.** عنوان الوحدة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 4000 حرف.

## OrgGeoLocation {#OrgUnitProfile.OrgGeoLocation}
<!-- alias: loc:OrgGeoLocation, dom:OrgUnit_OrgGeoLocation -->

**ما هو.** الموقع الجغرافي للوحدة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## OrgUnitLogo {#OrgUnitProfile.LogoPath}
<!-- alias: loc:OrgUnitLogo, dom:OrgUnit_Logo -->

**ما هو.** شعار الوحدة، لطباعته على الخطابات وكشوف الرواتب والتقارير المصرفية. بصيغة PNG أو JPEG أو GIF، بحد أقصى 1 ميجابايت، ويُزال بزر الإزالة.

**لماذا يهم.** يُحفظ ملفاً في مخزن ملفات الجهة، لا في قاعدة البيانات. ولا يعرضه أي تقرير مطبوع حتى الآن، وستعرضه التقارير تباعاً كلما حُدِّث كل منها لقراءته.

**القواعد.** اختياري.

## PayrollAreaId {#OrgUnitProfile.PayrollAreaId}
<!-- alias: loc:PayrollAreaId, dom:OrgUnit_PayrollAreaId -->

**ما هو.** منطقة الرواتب لموظفي هذه الوحدة.

**مثال.** شهري - الكويت

**لماذا يهم.** يُستخدم فقط عندما لا تكون لتعيين الموظف التنظيمي منطقة رواتب خاصة به، ولا ينتقل إلى الوحدات الأدنى. وبشكل منفصل، تُقرَّب بعض مبالغ الرواتب، مثل استردادات التأمين، إلى عدد المنازل العشرية لعملة منطقة الرواتب هذه لكل موظف في الوحدة، حتى لو حدد تعيينه منطقة رواتب أخرى؛ وإن لم تُحدَّد منطقة رواتب هنا فلا تُقرَّب.

**القواعد.** اختياري. يُختار من قائمة مناطق الرواتب.
