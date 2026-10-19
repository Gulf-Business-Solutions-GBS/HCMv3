---
helpId: pa.employee-asset-invoice.edit
lang: en
title: Employee Asset Invoices — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeAssetInvoice
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/asset-invoices/create-or-edit-employee-asset-invoice-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAssetInvoices.Create]
---

## Overview

A monthly bill for an asset the employee holds -- a mobile phone bill, a fuel card statement -- and how much of it the employee pays. Opened from the Asset Invoices section of the employee record.

An employee can have only one invoice per asset per month (by invoice date). Every amount is entered by hand: nothing is worked out from the employee's asset allowance, and payroll does not deduct anything from these records. The Employee Asset Invoice report lists them, and the printed Employee Deduction report shows the deduction amount of each ticked invoice in its month.

## AssetId {#EmployeeAssetInvoice.AssetId}
<!-- alias: loc:AssetId, dom:EmployeeAssetInvoice_AssetId -->

**What it is.** The asset the invoice is for.

**Example.** Mobile phone — 9999 1234

**Rules.** Required. Chosen from Asset master data.

:::admin
Maintained under General Master Info → Assets.
:::

## InvoiceDate {#EmployeeAssetInvoice.InvoiceDate}
<!-- alias: loc:InvoiceDate, dom:EmployeeAssetInvoice_InvoiceDate -->

**What it is.** The date of the invoice.

**Example.** 30 Sep 2026

**Why it matters.** Its month and year decide which month the invoice belongs to, both for the one-invoice-per-month check and on the Employee Deduction report.

**Rules.** Required. Date value.

## InvoiceAmount {#EmployeeAssetInvoice.InvoiceAmount}
<!-- alias: loc:InvoiceAmount, dom:EmployeeAssetInvoice_InvoiceAmount -->

**What it is.** The total of the invoice.

**Example.** 32.500

**Rules.** Optional. Number.

## EmployeePlanAmount {#EmployeeAssetInvoice.EmployeePlanAmount}
<!-- alias: loc:EmployeePlanAmount, dom:EmployeeAssetInvoice_EmployeePlanAmount -->

**What it is.** The part of the invoice the company covers under the employee's plan or allowance.

**Example.** 20.000

**Why it matters.** Shown on the Employee Asset Invoice report. It is not calculated; enter it yourself.

**Rules.** Optional. Number.

## DeductionAmount {#EmployeeAssetInvoice.DeductionAmount}
<!-- alias: loc:DeductionAmount, dom:EmployeeAssetInvoice_DeductionAmount -->

**What it is.** The part of the invoice the employee pays.

**Example.** 12.500

**Why it matters.** Printed on the Employee Deduction report when Deduct From Employee is ticked. Payroll does not deduct it; add a payment on the employee record if it is to come off the salary.

**Rules.** Optional. Number.

## DeductFromEmployee {#EmployeeAssetInvoice.DeductFromEmployee}
<!-- alias: loc:DeductFromEmployee, dom:EmployeeAssetInvoice_DeductFromEmployee -->

**What it is.** Whether the employee is to pay the deduction amount.

**Example.** Yes

**Why it matters.** Only ticked invoices with a deduction amount appear on the Employee Deduction report.

**Rules.** Yes or no.
