---
helpId: org.organization-bank-info.edit
lang: en
title: Organization Bank Info — Create / Edit
entity: HCMv3.OrganizationManagement.OrganizationBankInfo
surfaces:
  - angular/src/app/admin/organization-management/organization-bank-infos/create-or-edit-organization-bank-info-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.OrganizationBankInfos.Create]
---

## Overview

A bank account held by part of the organisation. Bank files that carry the company's account take it from here: the account of the top-level unit is used, or failing that any account that is filled in. The organisation unit, bank and account number cannot be blank.

## OrgUnit {#OrganizationBankInfo.OrganizationUnitId}
<!-- alias: loc:OrgUnit, dom:OrganizationBankInfo_OrganizationUnitId -->

**What it is.** The part of the organisation that holds the account.

**Rules.** Optional. Chosen from Organization Unit master data.

## Bank {#OrganizationBankInfo.BankId}
<!-- alias: loc:Bank, dom:OrganizationBankInfo_BankId -->

**What it is.** The bank.

**Example.** NBK

**Rules.** Optional. Chosen from Bank master data.

## AccountNo {#OrganizationBankInfo.AccountNo}
<!-- alias: loc:AccountNo, dom:OrganizationBankInfo_AccountNo -->

**What it is.** The account number.

**Rules.** Required. Maximum 256 characters.

## SC_Code {#OrganizationBankInfo.SC_Code}
<!-- alias: loc:SC_Code, dom:OrganizationBankInfo_SC_Code -->

**What it is.** A sort or branch code for the account.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 256 characters.
