---
helpId: pay.social-security-marital-allowance.edit
lang: ar
title: علاوات الحالة الاجتماعية في التأمينات — إنشاء / تحرير
entity: HCMv3.PayrollManagement.SocialSecurityMaritalAllowance
surfaces:
  - angular/src/app/admin/payroll-management/social-security-marital-allowances/create-or-edit-social-security-marital-allowance-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SocialSecurityMaritalAllowances.Create]
---

## Overview

مبلغ يُضاف إلى الأجر للتأمينات الاجتماعية بحسب الحالة الاجتماعية، للموظفين الذين يقع مؤهلهم في تخصص معيّن.

ولا تُحتسب إلا الأسطر الخاصة بتخصص أحد مؤهلات الموظف. فالموظف الذكر يحصل على السطر المطابق لحالته الاجتماعية، أما الموظفة غير العزباء فتُجمع لها كل الأسطر غير الخاصة بالعزوبية في تخصصاتها -- وهذا سلوك النظام السابق وقد أُبقي عليه عمداً.

## SocialSecurityId {#SocialSecurityMaritalAllowance.SocialSecurityId}
<!-- alias: loc:SocialSecurityId, dom:SocialSecurityMaritalAllowance_SocialSecurityId -->

**ما هو.** إعداد التأمينات الاجتماعية الذي تتبعه هذه العلاوة.

**القواعد.** إلزامي. يُختار من قائمة التأمينات الاجتماعية.

## EducationBranchId {#SocialSecurityMaritalAllowance.EducationBranchId}
<!-- alias: loc:EducationBranchId, dom:SocialSecurityMaritalAllowance_EducationBranchId -->

**ما هو.** التخصص الدراسي الذي تنطبق عليه العلاوة.

**مثال.** الهندسة

**القواعد.** إلزامي. يُختار من قائمة EducationBranch.

## MaritalStatusId {#SocialSecurityMaritalAllowance.MaritalStatusId}
<!-- alias: loc:MaritalStatusId, dom:SocialSecurityMaritalAllowance_MaritalStatusId -->

**ما هو.** الحالة الاجتماعية التي يخصها المبلغ.

**مثال.** متزوج

**القواعد.** إلزامي. يُختار من قائمة الحالات الاجتماعية.

## Amount {#SocialSecurityMaritalAllowance.Amount}
<!-- alias: loc:Amount, dom:SocialSecurityMaritalAllowance_Amount -->

**ما هو.** المبلغ المضاف.

**مثال.** `50`

**القواعد.** اختياري. رقم.
