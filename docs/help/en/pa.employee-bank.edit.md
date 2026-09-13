---
helpId: pa.employee-bank.edit
lang: en
title: Employee Banks — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeBank
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/banks/create-or-edit-employee-bank-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeBanks.Create]
---

## Overview

How and where the employee's salary is paid, from a given date. Two records for the same employee cannot start on the same date.

Bank transfer files use the record in force on the file's end date. Saving a new record does not close the previous one, so set the previous record's end date yourself.

## BankId {#EmployeeBank.BankId}
<!-- alias: loc:BankId, dom:EmployeeBank_BankId -->

**What it is.** The employee's own bank, which receives the salary.

**Example.** National Bank of Kuwait

**Why it matters.** Required when the payment type is Bank. Bank files can be filtered by it.

**Rules.** Optional. Chosen from Bank master data.

:::admin
Maintained under General Master Info → Banks.
:::

## PayerBankId {#EmployeeBank.PayerBankId}
<!-- alias: loc:PayerBankId, dom:EmployeeBank_PayerBankId -->

**What it is.** The company bank that pays this employee, when employees are split across paying banks.

**Example.** Gulf Bank

**Rules.** Optional. Chosen from Bank master data.

:::admin
When any employee's record names a paying bank, that bank's transfer file includes only the employees who name it. Otherwise the file includes only employees with no paying bank set.
:::

## BankCountryId {#EmployeeBank.BankCountryId}
<!-- alias: loc:BankCountryId, dom:EmployeeBank_BankCountryId -->

**What it is.** The country of the employee's bank.

**Example.** Kuwait

**Why it matters.** Recorded only; bank files do not read it at present.

**Rules.** Optional. Chosen from Country master data.

## PaymentType {#EmployeeBank.PaymentType}
<!-- alias: loc:PaymentType, dom:EmployeeBank_PaymentType -->

**What it is.** How the salary is paid.

**Example.** Bank

**Why it matters.** Bank files can be restricted to one payment type, and a bank must be chosen when the type is Bank.

**Rules.** Required. One of: Bank, Cash, N/A.

## PaymentCurrencyId {#EmployeeBank.PaymentCurrencyId}
<!-- alias: loc:PaymentCurrencyId, dom:EmployeeBank_PaymentCurrencyId -->

**What it is.** The currency the salary is paid in.

**Example.** KWD

**Why it matters.** Recorded only; bank files do not read it at present.

**Rules.** Optional. Chosen from Currency master data.

## AccountNum {#EmployeeBank.AccountNum}
<!-- alias: loc:AccountNum, dom:EmployeeBank_AccountNum -->

**What it is.** The employee's bank account number.

**Example.** 2010123456

**Why it matters.** Written into the bank transfer file.

**Rules.** Optional. Maximum 256 characters.

## Iban {#EmployeeBank.IBAN}
<!-- alias: loc:Iban, dom:EmployeeBank_Iban -->

**What it is.** The employee's international bank account number.

**Example.** KW81CBKU0000000000001234560101

**Why it matters.** Written into the bank transfer file. A change requested through self-service needs an account number or an IBAN.

**Rules.** Optional. Maximum 256 characters.

## BankBranch {#EmployeeBank.BankBranch}
<!-- alias: loc:BankBranch, dom:EmployeeBank_BankBranch -->

**What it is.** The branch of the employee's bank.

**Example.** Salmiya

**Why it matters.** Included in bank files that carry a branch, and on employee reports.

**Rules.** Optional. Maximum 256 characters.
