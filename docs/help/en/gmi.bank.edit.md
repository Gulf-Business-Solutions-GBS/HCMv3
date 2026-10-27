---
helpId: gmi.bank.edit
lang: en
title: Banks — Create / Edit
entity: HCMv3.GeneralMasterInfo.Bank
surfaces:
  - angular/src/app/admin/general-master-info/banks/create-or-edit-bank-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.Banks.Create]
---

## Overview

The banks employees are paid into and the banks the organisation pays from. Each employee bank account names one of these banks, and the salary transfer file is produced for one paying bank at a time.

## ExternalCode {#Bank.ExternalCode}
<!-- alias: loc:ExternalCode, dom:Bank_ExternalCode -->

**What it is.** An identifier for this bank used outside HCMv3.

**Why it matters.** When this bank pays salaries, its external code is written into the salary transfer file.

**Rules.** Optional. Maximum 256 characters.

## SWIFTCode {#Bank.SWIFTCode}
<!-- alias: loc:SWIFTCode, dom:Bank_SWIFTCode -->

**What it is.** The bank's SWIFT (BIC) code.

**Example.** `NBOKKWKW`

**Why it matters.** Written into the salary transfer file for employees who are paid into this bank.

**Rules.** Optional. Maximum 256 characters.

## CompanyCode {#Bank.CompanyCode}
<!-- alias: loc:CompanyCode, dom:Bank_CompanyCode -->

**What it is.** A company code for this bank, as its transfer file format requires.

**Why it matters.** When this bank pays salaries, this code is written into the salary transfer file.

**Rules.** Optional. Maximum 256 characters.

## BankFile {#Bank.BankFileId}
<!-- alias: loc:BankFile, dom:Bank_BankFileId -->

**What it is.** A bank file format linked to this bank.

**Why it matters.** Not read at present -- the salary transfer file does not use this setting.

**Rules.** Optional. Chosen from Bank File master data.

:::admin
The codes a paying bank uses for other banks in its transfer file are set in Payee Bank Details.
:::
