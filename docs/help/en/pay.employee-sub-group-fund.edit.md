---
helpId: pay.employee-sub-group-fund.edit
lang: en
title: Employee Sub Group Funds — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroupFund
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/funds/create-or-edit-employee-sub-group-fund-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupFunds.Create]
---

## Overview

One pay band of the grade's fund deduction -- for example a savings or provident fund. The employee's pay for the fund is the monthly total of wagetypes marked *Include In Fund*; the band whose From–To range holds that total sets the deduction.

The deduction is made only for employees enrolled in a fund, and not while they have a fund exemption or after their last day in service.

## FromPay {#EmployeeSubGroupFund.FromPay}
<!-- alias: loc:FromPay, dom:EmployeeSubGroupFund_FromPay -->

**What it is.** The lowest pay in this band.

**Example.** `0`

**Why it matters.** From cannot be greater than To.

**Rules.** Optional. Number.

## ToPay {#EmployeeSubGroupFund.ToPay}
<!-- alias: loc:ToPay, dom:EmployeeSubGroupFund_ToPay -->

**What it is.** The highest pay in this band.

**Example.** `999`

**Rules.** Optional. Number.

## FundDedType {#EmployeeSubGroupFund.FundDedType}
<!-- alias: loc:FundDedType, dom:EmployeeSubGroupFund_FundDedType -->

**What it is.** How the deduction is worked out for this band.

**Example.** Percentage

**Why it matters.** *Fix Amount* deducts the fixed amount. *Percentage* deducts the percentage of the pay. *PercentageWithinLimit* deducts the percentage, but no more than the maximum and no less than the minimum. The result is rounded to 2 decimals.

**Rules.** Required. One of: Percentage, PercentageWithinLimit, Fix Amount.

## Percentage {#EmployeeSubGroupFund.Percentage}
<!-- alias: loc:Percentage, dom:EmployeeSubGroupFund_Percentage -->

**What it is.** The percentage of pay deducted, for the two percentage types.

**Example.** `5`

**Rules.** Optional. Number.

## MinAmount {#EmployeeSubGroupFund.MinAmount}
<!-- alias: loc:MinAmount, dom:EmployeeSubGroupFund_MinAmount -->

**What it is.** The least deducted, for *PercentageWithinLimit*.

**Example.** `10`

**Rules.** Optional. Number.

## MaxAmount {#EmployeeSubGroupFund.MaxAmount}
<!-- alias: loc:MaxAmount, dom:EmployeeSubGroupFund_MaxAmount -->

**What it is.** The most deducted, for *PercentageWithinLimit*.

**Example.** `100`

**Rules.** Optional. Number.

## FixAmount {#EmployeeSubGroupFund.FixAmount}
<!-- alias: loc:FixAmount, dom:EmployeeSubGroupFund_FixAmount -->

**What it is.** The amount deducted, for *Fix Amount*.

**Example.** `25`

**Rules.** Optional. Number.
