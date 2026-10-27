---
helpId: pa.employee-social-security.edit
lang: ar
title: التأمينات الاجتماعية للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeSocialSecurity
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/social-securities/create-or-edit-employee-social-security-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeSocialSecurities.Create]
---

## Overview

تسجيل الموظف في التأمينات الاجتماعية خلال فترة.

لا تحتسب الرواتب بدلات التأمينات الاجتماعية واقتطاعاتها لشهر ما إلا إذا غطى نهايةَ ذلك الشهر تسجيلٌ محدد فيه خيار المساهمة.

## SocialSecurityNum {#EmployeeSocialSecurity.SocialSecurityNum}
<!-- alias: loc:SocialSecurityNum, dom:EmployeeSocialSecurity_SocialSecurityNum -->

**ما هو.** رقم تسجيل الموظف في التأمينات الاجتماعية.

**مثال.** 1234567

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** إلزامي. رقم صحيح.

## SocialSecurityContrib {#EmployeeSocialSecurity.SocialSecurityContrib}
<!-- alias: loc:SocialSecurityContrib, dom:EmployeeSocialSecurity_SocialSecurityContrib -->

**ما هو.** هل يساهم الموظف في التأمينات الاجتماعية خلال هذه الفترة.

**لماذا يهم.** من دونه لا تحتسب الرواتب أي تأمينات اجتماعية للأشهر التي يغطيها هذا السجل.

**القواعد.** نعم أو لا.
