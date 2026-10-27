---
helpId: gr.sponsor-bank.edit
lang: en
title: Sponsor Banks — Create / Edit
entity: HCMv3.GovtRelationsManagement.SponsorBank
surfaces:
  - angular/src/app/admin/govt-relations-management/sponsor-detail/banks/create-or-edit-sponsor-bank-modal.component.html
audience: [admin]
permissions: [Pages.GovtRelationsManagement.SponsorBanks.Create]
---

## Overview

A bank account a sponsor pays salaries from. The end date cannot be before the start date. Each account with a payment bank can be chosen when generating a bank file.

## EmployeeBankId {#SponsorBank.EmployeeBankId}
<!-- alias: loc:EmployeeBankId, dom:SponsorBank_EmployeeBankId -->

**What it is.** The bank the employees bank with.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from Bank master data.

## PaymentBankId {#SponsorBank.PaymentBankId}
<!-- alias: loc:PaymentBankId, dom:SponsorBank_PaymentBankId -->

**What it is.** The bank the account is with.

**Example.** NBK

**Rules.** Required. Chosen from Bank master data.

## AccountNumber {#SponsorBank.AccountNumber}
<!-- alias: loc:AccountNumber, dom:SponsorBank_AccountNumber -->

**What it is.** The account number.

**Why it matters.** Written into the bank file header for formats that carry the paying account.

**Rules.** Optional. Maximum 256 characters.

## Iban {#SponsorBank.IBAN}
<!-- alias: loc:Iban, dom:SponsorBank_Iban -->

**What it is.** The account's IBAN.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 64 characters.
