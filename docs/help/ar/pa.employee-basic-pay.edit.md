---
helpId: pa.employee-basic-pay.edit
lang: ar
title: الرواتب الأساسية للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeBasicPay
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/basic-pays/create-or-edit-employee-basic-pay-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeBasicPays.Create]
---

## Overview

رأس سجل أجر الموظف لفترة معينة. تحمل بنوده التفصيلية أنواع الأجور والمبالغ، ويحدد هذا السجل الدورية التي يُعبَّر بها عن الأجر.

تستخدم الرواتب سجل الأجر الأساسي الذي تغطي فترته تاريخ الدفع. ولا يؤدي حفظ سجل جديد إلى إغلاق السابق، ولا يُرفض إلا تكرار تاريخ البداية، لذا حدد تاريخ انتهاء السجل السابق بنفسك.

## PayType {#EmployeeBasicPay.PayType}
<!-- alias: loc:PayType, dom:EmployeeBasicPay_PayType -->

**ما هو.** نوع الأجر.

**مثال.** عادي

**لماذا يهم.** يُحفظ للرجوع إليه، ولا يقرؤه أي احتساب حالياً.

**القواعد.** إلزامي. إحدى القيم: لا شيء، اعتيادي.

## PayInterval {#EmployeeBasicPay.PayInterval}
<!-- alias: loc:PayInterval, dom:EmployeeBasicPay_PayInterval -->

**ما هو.** الفترة التي يُعبَّر بها عن مبالغ البنود التفصيلية.

**مثال.** شهري

**لماذا يهم.** تحتسب الرواتب كل بند تفصيلي بناءً عليها: فالمبالغ الشهرية تُوزَّع نسبياً حسب طريقة احتساب نوع الأجر، والمبالغ اليومية وبالساعة تُضرب في الأيام أو الساعات المسجلة للموظف في مكافآت الموظف خلال ذلك الشهر.

**القواعد.** إلزامي. إحدى القيم: شهري، يومي، بالساعة، نهاية الخدمة، سنوي.

:::admin
عند اختيار نهاية الخدمة أو سنوي، لا ينتج عن احتساب الأجر الأساسي أي مبلغ للبنود التفصيلية.
:::

## EmployeeActionId {#EmployeeBasicPay.EmployeeActionId}
<!-- alias: loc:EmployeeActionId, dom:EmployeeBasicPay_EmployeeActionId -->

**ما هو.** الإجراء الشخصي الذي ينتمي إليه هذا التغيير في الأجر، إن وُجد.

**مثال.** ترقية — 1 يناير 2026

**لماذا يهم.** يُسجَّل كرابط فقط، ولا تقرؤه الرواتب.

**القواعد.** اختياري. يُختار من قائمة.
