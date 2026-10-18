---
helpId: pa.employee-insurance.edit
lang: ar
title: تأمينات الموظفين — إنشاء / تحرير
entity: HCMv3.BenefitsManagement.EmployeeInsurance
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/insurances/create-or-edit-employee-insurance-modal.component.html
audience: [admin]
permissions: [Pages.BenefitsManagement.EmployeeInsurances.Create]
---

## Overview

اشتراك تأمين للموظف أو أحد المعالين، وكيفية تقاسم تكلفته واستردادها.

تصرف الرواتب اشتراكي الموظف والشركة في شهر الرواتب الذي يبدأ فيه التأمين، ثم تستردهما على أقساط اعتباراً من تاريخ بدء الأقساط حتى يُسدَّد كل رصيد. ويجب أن يساوي مبلغ التأمين اشتراك الموظف مضافاً إليه اشتراك الشركة.

## InsurancePlanId {#EmployeeInsurance.InsurancePlanId}
<!-- alias: loc:InsurancePlanId, dom:EmployeeInsurance_InsurancePlanId -->

**ما هو.** خطة التأمين.

**مثال.** التأمين الطبي الذهبي

**لماذا يهم.** تُسجَّل فقط، ولا تقرؤها الرواتب.

**القواعد.** إلزامي. يُختار من قائمة خطط التأمين.

## InsuranceProviderId {#EmployeeInsurance.InsuranceProviderId}
<!-- alias: loc:InsuranceProviderId, dom:EmployeeInsurance_InsuranceProviderId -->

**ما هو.** شركة التأمين.

**مثال.** الخليج للتأمين

**لماذا يهم.** تُسجَّل فقط، ولا تقرؤها الرواتب.

**القواعد.** إلزامي. يُختار من قائمة شركات التأمين.

## DependantId {#EmployeeInsurance.DependantId}
<!-- alias: loc:DependantId, dom:EmployeeInsurance_DependantId -->

**ما هو.** المعال المشمول، عندما لا يكون التأمين للموظف.

**مثال.** سارة المطيري (ابنة)

**لماذا يهم.** يُصرف تأمين المعال ويُسترد بالطريقة نفسها التي يُصرف بها تأمين الموظف.

**القواعد.** اختياري. يُختار من قائمة.

## InstallmentStartDate {#EmployeeInsurance.InstallmentStartDate}
<!-- alias: loc:InstallmentStartDate, dom:EmployeeInsurance_InstallmentStartDate -->

**ما هو.** التاريخ الذي تبدأ فيه الرواتب استرداد الاشتراكات.

**مثال.** 1 أكتوبر 2026

**لماذا يهم.** لا يجوز أن يسبق تاريخ البداية.

**القواعد.** إلزامي. تاريخ.

## InsuranceAmount {#EmployeeInsurance.InsuranceAmount}
<!-- alias: loc:InsuranceAmount, dom:EmployeeInsurance_InsuranceAmount -->

**ما هو.** إجمالي تكلفة التأمين.

**مثال.** 600.000

**لماذا يهم.** لا يجوز أن يكون صفراً، ويجب أن يساوي اشتراك الموظف مضافاً إليه اشتراك الشركة.

**القواعد.** اختياري. رقم.

## CompanyInterval {#EmployeeInsurance.CompanyInterval}
<!-- alias: loc:CompanyInterval, dom:EmployeeInsurance_CompanyInterval -->

**ما هو.** الدورية التي يُعبَّر بها عن اشتراك الشركة.

**مثال.** سنوي

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب.

**القواعد.** إلزامي. إحدى القيم: شهري، سنوي.

## CompanyContribution {#EmployeeInsurance.CompanyContribution}
<!-- alias: loc:CompanyContribution, dom:EmployeeInsurance_CompanyContribution -->

**ما هو.** حصة الشركة من التكلفة.

**مثال.** 400.000

**لماذا يهم.** تُصرف في شهر بدء التأمين، ثم تُسترد على أقساط.

**القواعد.** اختياري. رقم.

## InstallmentCompanyDedAmount {#EmployeeInsurance.InstallmentCompanyDedAmount}
<!-- alias: loc:InstallmentCompanyDedAmount, dom:EmployeeInsurance_InstallmentCompanyDedAmount -->

**ما هو.** القسط الشهري لاسترداد اشتراك الشركة.

**مثال.** 33.333

**لماذا يهم.** لا تسترد الرواتب أكثر من الرصيد المتبقي، ولا أكثر مما يسمح به صافي راتب الموظف.

**القواعد.** اختياري. رقم.

## BalanceCompanyContribution {#EmployeeInsurance.BalanceCompanyContribution}
<!-- alias: loc:BalanceCompanyContribution, dom:EmployeeInsurance_BalanceCompanyContribution -->

**ما هو.** المتبقي من اشتراك الشركة الذي لم يُسترد بعد. يُحتسب ولا يُدخل يدوياً.

**مثال.** 360.000 من اشتراك قدره 600.000، بعد أربعة أقساط قيمة كل منها 60.000

**لماذا يهم.** يبدأ بكامل الاشتراك وينقص كلما سُجِّل قسط أو خُصم في الرواتب. ولا تخصم الرواتب التأمين إلا ما دام هناك رصيد، ولا يتجاوزه آخر خصم أبداً.

**القواعد.** اختياري. رقم.

## EmployeeInterval {#EmployeeInsurance.EmployeeInterval}
<!-- alias: loc:EmployeeInterval, dom:EmployeeInsurance_EmployeeInterval -->

**ما هو.** الدورية التي يُعبَّر بها عن اشتراك الموظف.

**مثال.** سنوي

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب.

**القواعد.** إلزامي. إحدى القيم: شهري، سنوي.

## EmployeeContribution {#EmployeeInsurance.EmployeeContribution}
<!-- alias: loc:EmployeeContribution, dom:EmployeeInsurance_EmployeeContribution -->

**ما هو.** حصة الموظف من التكلفة.

**مثال.** 200.000

**لماذا يهم.** تُصرف في شهر بدء التأمين، ثم تُسترد على أقساط.

**القواعد.** اختياري. رقم.

## InstallmentEmployeeDedAmount {#EmployeeInsurance.InstallmentEmployeeDedAmount}
<!-- alias: loc:InstallmentEmployeeDedAmount, dom:EmployeeInsurance_InstallmentEmployeeDedAmount -->

**ما هو.** القسط الشهري لاسترداد اشتراك الموظف.

**مثال.** 16.667

**لماذا يهم.** لا تسترد الرواتب أكثر من الرصيد المتبقي، ولا أكثر مما يسمح به صافي راتب الموظف.

**القواعد.** اختياري. رقم.

## BalanceEmployeeContribution {#EmployeeInsurance.BalanceEmployeeContribution}
<!-- alias: loc:BalanceEmployeeContribution, dom:EmployeeInsurance_BalanceEmployeeContribution -->

**ما هو.** المتبقي من اشتراك الموظف الذي لم يُسترد بعد. يُحتسب ولا يُدخل يدوياً.

**مثال.** 120.000

**لماذا يهم.** يعمل بالطريقة نفسها التي يعمل بها رصيد اشتراك الشركة، لحصة الموظف.

**القواعد.** اختياري. رقم.
