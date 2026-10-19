---
helpId: pa.employee-asset-invoice.edit
lang: ar
title: فواتير عهد الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeAssetInvoice
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/asset-invoices/create-or-edit-employee-asset-invoice-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAssetInvoices.Create]
---

## Overview

فاتورة شهرية لعهدة يحملها الموظف -- كفاتورة الهاتف النقال أو كشف بطاقة الوقود -- والجزء الذي يتحمله الموظف منها. وتُفتح من قسم فواتير العهد في سجل الموظف.

لا يجوز أن يكون للموظف أكثر من فاتورة واحدة للعهدة نفسها في الشهر الواحد (حسب تاريخ الفاتورة). وتُدخل المبالغ كلها يدوياً: فلا يُحتسب شيء منها من بدل العهدة للموظف، ولا تخصم الرواتب شيئاً بناءً على هذه السجلات. ويعرضها تقرير فواتير عهد الموظفين، ويُظهر تقرير خصومات الموظفين المطبوع مبلغ الخصم لكل فاتورة مؤشَّر عليها في شهرها.

## AssetId {#EmployeeAssetInvoice.AssetId}
<!-- alias: loc:AssetId, dom:EmployeeAssetInvoice_AssetId -->

**ما هو.** العهدة التي تخصها الفاتورة.

**مثال.** هاتف نقال — 9999 1234

**القواعد.** إلزامي. يُختار من قائمة العهد.

:::admin
تُدار من البيانات الأساسية العامة ← العهد.
:::

## InvoiceDate {#EmployeeAssetInvoice.InvoiceDate}
<!-- alias: loc:InvoiceDate, dom:EmployeeAssetInvoice_InvoiceDate -->

**ما هو.** تاريخ الفاتورة.

**مثال.** 30 سبتمبر 2026

**لماذا يهم.** شهره وسنته يحددان الشهر الذي تنتمي إليه الفاتورة، في التحقق من فاتورة واحدة في الشهر وفي تقرير خصومات الموظفين.

**القواعد.** إلزامي. تاريخ.

## InvoiceAmount {#EmployeeAssetInvoice.InvoiceAmount}
<!-- alias: loc:InvoiceAmount, dom:EmployeeAssetInvoice_InvoiceAmount -->

**ما هو.** إجمالي الفاتورة.

**مثال.** 32.500

**القواعد.** اختياري. رقم.

## EmployeePlanAmount {#EmployeeAssetInvoice.EmployeePlanAmount}
<!-- alias: loc:EmployeePlanAmount, dom:EmployeeAssetInvoice_EmployeePlanAmount -->

**ما هو.** الجزء من الفاتورة الذي تتحمله الشركة ضمن خطة الموظف أو بدله.

**مثال.** 20.000

**لماذا يهم.** يظهر في تقرير فواتير عهد الموظفين. ولا يُحتسب تلقائياً، بل يُدخل يدوياً.

**القواعد.** اختياري. رقم.

## DeductionAmount {#EmployeeAssetInvoice.DeductionAmount}
<!-- alias: loc:DeductionAmount, dom:EmployeeAssetInvoice_DeductionAmount -->

**ما هو.** الجزء من الفاتورة الذي يتحمله الموظف.

**مثال.** 12.500

**لماذا يهم.** يُطبع في تقرير خصومات الموظفين إذا أُشِّر على الخصم من الموظف. ولا تخصمه الرواتب؛ فإن كان سيُقتطع من الراتب فأضِف دفعة في سجل الموظف.

**القواعد.** اختياري. رقم.

## DeductFromEmployee {#EmployeeAssetInvoice.DeductFromEmployee}
<!-- alias: loc:DeductFromEmployee, dom:EmployeeAssetInvoice_DeductFromEmployee -->

**ما هو.** هل يتحمل الموظف مبلغ الخصم.

**مثال.** نعم

**لماذا يهم.** لا تظهر في تقرير خصومات الموظفين إلا الفواتير المؤشَّر عليها والتي لها مبلغ خصم.

**القواعد.** نعم أو لا.
