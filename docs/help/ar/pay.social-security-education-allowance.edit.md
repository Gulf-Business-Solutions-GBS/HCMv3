---
helpId: pay.social-security-education-allowance.edit
lang: ar
title: علاوات المؤهل الدراسي في التأمينات — إنشاء / تحرير
entity: HCMv3.PayrollManagement.SocialSecurityEducationAllowance
surfaces:
  - angular/src/app/admin/payroll-management/social-security-education-allowances/create-or-edit-social-security-education-allowance-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SocialSecurityEducationAllowances.Create]
---

## Overview

مبلغ يُضاف إلى الأجر للتأمينات الاجتماعية عن مؤهل دراسي يحمله الموظف.

كل مؤهل دراسي للموظف سارٍ في الشهر يضيف مبلغ السطر الذي له المؤهل والتخصص نفسيهما.

## SocialSecurityId {#SocialSecurityEducationAllowance.SocialSecurityId}
<!-- alias: loc:SocialSecurityId, dom:SocialSecurityEducationAllowance_SocialSecurityId -->

**ما هو.** إعداد التأمينات الاجتماعية الذي تتبعه هذه العلاوة.

**القواعد.** إلزامي. يُختار من قائمة التأمينات الاجتماعية.

## EducationBranchId {#SocialSecurityEducationAllowance.EducationBranchId}
<!-- alias: loc:EducationBranchId, dom:SocialSecurityEducationAllowance_EducationBranchId -->

**ما هو.** التخصص الدراسي.

**مثال.** الهندسة

**القواعد.** إلزامي. يُختار من قائمة EducationBranch.

## EducationId {#SocialSecurityEducationAllowance.EducationId}
<!-- alias: loc:EducationId, dom:SocialSecurityEducationAllowance_EducationId -->

**ما هو.** المؤهل الدراسي.

**مثال.** بكالوريوس

**القواعد.** إلزامي. يُختار من قائمة المؤهلات الدراسية.

## Amount {#SocialSecurityEducationAllowance.Amount}
<!-- alias: loc:Amount, dom:SocialSecurityEducationAllowance_Amount -->

**ما هو.** المبلغ المضاف.

**مثال.** `100`

**القواعد.** اختياري. رقم.
