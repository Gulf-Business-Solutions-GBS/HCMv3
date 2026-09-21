---
helpId: pay.cost-register.edit
lang: ar
title: سجلات التكلفة — إنشاء / تحرير
entity: HCMv3.PayrollManagement.CostRegister
surfaces:
  - angular/src/app/admin/payroll-management/cost-registers/create-or-edit-cost-register-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.CostRegisters.Create]
---

## Overview

تكلفة رواتب الموظف لشهر واحد وتشغيل واحد، موزعة على الحسابات وعناصر التكلفة. يكتبها محرك التكلفة من كشف الراتب وحسابات الأجور للموظف، ويُبنى منها قيد اليومية.

## EmployeeId {#CostRegister.EmployeeId}
<!-- alias: loc:EmployeeId, dom:CostRegister_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Period {#CostRegister.Period}
<!-- alias: loc:Period, dom:CostRegister_Period -->

**ما هو.** الشهر الذي تخصه التكلفة.

**مثال.** `2026-04-30`

**القواعد.** إلزامي. تاريخ.

## PayrollRunType {#CostRegister.PayrollRunType}
<!-- alias: loc:PayrollRunType, dom:CostRegister_PayrollRunType -->

**ما هو.** ما إذا كانت التكلفة ناتجة عن تشغيل عادي أو خارج الدورة.

**مثال.** عادي

**القواعد.** إلزامي. إحدى القيم: اعتيادي، خارج الدورة.
