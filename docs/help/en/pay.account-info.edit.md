---
helpId: pay.account-info.edit
lang: en
title: Account Infos — Create / Edit
entity: HCMv3.PayrollManagement.AccountInfo
surfaces:
  - angular/src/app/admin/payroll-management/account-infos/create-or-edit-account-info-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AccountInfos.Create]
---

## Overview

A ledger account in the finance system that payroll posts to -- salary expense, accruals, the salary bank account and so on. Wage accounts, payroll areas and the journal voucher refer to accounts from this list.

## AccountType {#AccountInfo.AccountTypeId}
<!-- alias: loc:AccountType, dom:AccountInfo_AccountTypeId -->

**What it is.** The kind of account in the finance system.

**Example.** Ledger

**Why it matters.** Printed as the account type beside the account on each journal voucher line, for the main account and the offset account alike.

**Rules.** Required. Chosen from Account Type master data.
