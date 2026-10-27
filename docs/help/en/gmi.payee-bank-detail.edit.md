---
helpId: gmi.payee-bank-detail.edit
lang: en
title: Payee Bank Details — Create / Edit
entity: HCMv3.GeneralMasterInfo.PayeeBankDetail
surfaces:
  - angular/src/app/admin/general-master-info/payee-bank-details/create-or-edit-payee-bank-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.PayeeBankDetails.Create]
---

## Overview

For a paying bank, the code it uses for each other bank in its salary transfer file. When the transfer file is produced for a paying bank, each employee's bank is looked up here to find the code to write.

## Bank {#PayeeBankDetail.BankId}
<!-- alias: loc:Bank, dom:PayeeBankDetail_BankId -->

**What it is.** The paying bank.

**Rules.** Optional. Chosen from Bank master data.

## PayeeBank {#PayeeBankDetail.PayeeBankId}
<!-- alias: loc:PayeeBank, dom:PayeeBankDetail_PayeeBankId -->

**What it is.** The employee's bank -- the bank being paid into.

**Rules.** Optional. Chosen from Bank master data.

## BankCode {#PayeeBankDetail.BankCode}
<!-- alias: loc:BankCode, dom:PayeeBankDetail_BankCode -->

**What it is.** The code the paying bank uses for the employee's bank.

**Why it matters.** Written into the paying bank's salary transfer file for every employee paid into that bank.

**Rules.** Optional. Maximum 256 characters.

## BankFile {#PayeeBankDetail.BankFileId}
<!-- alias: loc:BankFile, dom:PayeeBankDetail_BankFileId -->

**What it is.** A bank file format for this pairing.

**Why it matters.** Not read at present -- the salary transfer file does not use it.

**Rules.** Optional. Chosen from Bank File master data.
