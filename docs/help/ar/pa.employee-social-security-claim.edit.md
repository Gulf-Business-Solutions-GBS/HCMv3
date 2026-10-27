---
helpId: pa.employee-social-security-claim.edit
lang: ar
title: مطالبات التأمينات الاجتماعية للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeSocialSecurityClaim
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/social-security-claims/create-or-edit-employee-social-security-claim-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeSocialSecurityClaims.Create]
---

## Overview

مطالبة أو استرداد تأمينات اجتماعية مسجل للموظف. ويجب إدخال أحد مبلغي الاشتراك على الأقل.

يُسجَّل للرجوع إليه، ولا تقرؤه الرواتب حالياً.

## ClaimDate {#EmployeeSocialSecurityClaim.ClaimDate}
<!-- alias: loc:ClaimDate, dom:EmployeeSocialSecurityClaim_ClaimDate -->

**ما هو.** تاريخ المطالبة.

**مثال.** 20 أغسطس 2026

**القواعد.** إلزامي. تاريخ.

## EmployeeContributionAmount {#EmployeeSocialSecurityClaim.EmployeeContributionAmount}
<!-- alias: loc:EmployeeContributionAmount, dom:EmployeeSocialSecurityClaim_EmployeeContributionAmount -->

**ما هو.** مبلغ اشتراك الموظف المطالب به.

**مثال.** 52.500

**القواعد.** اختياري. رقم.

## EmployerContributionAmount {#EmployeeSocialSecurityClaim.EmployerContributionAmount}
<!-- alias: loc:EmployerContributionAmount, dom:EmployeeSocialSecurityClaim_EmployerContributionAmount -->

**ما هو.** مبلغ اشتراك صاحب العمل المطالب به.

**مثال.** 112.500

**القواعد.** اختياري. رقم.
