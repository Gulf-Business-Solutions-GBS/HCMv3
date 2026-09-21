---
helpId: gr.sponsor.edit
lang: en
title: Sponsors — Create / Edit
entity: HCMv3.GovtRelationsManagement.Sponsor
surfaces:
  - angular/src/app/admin/govt-relations-management/sponsors/create-or-edit-sponsor-modal.component.html
audience: [admin]
permissions: [Pages.GovtRelationsManagement.Sponsors.Create]
---

## Overview

A sponsor: a licensed company or owner that employees are sponsored under for residence and work permits. Employees are linked to a sponsor on their Employee Sponsors records, and the sponsor's name then appears on employee and payroll reports. Its licences, signatories, representatives, bank accounts and job-title quotas are kept on the related screens.

## Email {#Sponsor.Email}
<!-- alias: loc:Email, dom:Sponsor_Email -->

**What it is.** The sponsor's email address.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Optional. Maximum 256 characters.

## Gender {#Sponsor.Gender}
<!-- alias: loc:Gender, dom:Sponsor_Gender -->

**What it is.** The owner's gender, where the sponsor is a person.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Required. One of: Male, Female.

## Profession {#Sponsor.Profession}
<!-- alias: loc:Profession, dom:Sponsor_Profession -->

**What it is.** The owner's profession.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Optional. Maximum 256 characters.

## Building {#Sponsor.Building}
<!-- alias: loc:Building, dom:Sponsor_Building -->

**What it is.** The building of the sponsor's address.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Optional. Maximum 256 characters.

## Street {#Sponsor.Street}
<!-- alias: loc:Street, dom:Sponsor_Street -->

**What it is.** The street.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Optional. Maximum 256 characters.

## Area {#Sponsor.Area}
<!-- alias: loc:Area, dom:Sponsor_Area -->

**What it is.** The area.

**Example.** Shuwaikh

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Optional. Maximum 256 characters.

## Block {#Sponsor.Block}
<!-- alias: loc:Block, dom:Sponsor_Block -->

**What it is.** The block.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Optional. Maximum 256 characters.

## Telephone {#Sponsor.Telephone}
<!-- alias: loc:Telephone, dom:Sponsor_Telephone -->

**What it is.** The telephone number.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Required. Whole number.

## Fax {#Sponsor.Fax}
<!-- alias: loc:Fax, dom:Sponsor_Fax -->

**What it is.** The fax number.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Optional. Maximum 50 characters.

## POBox {#Sponsor.POBox}
<!-- alias: loc:POBox, dom:Sponsor_POBox -->

**What it is.** The post office box.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Optional. Maximum 50 characters.

## Nationality {#Sponsor.NationalityId}
<!-- alias: loc:Nationality, dom:Sponsor_NationalityId -->

**What it is.** The owner's nationality.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Required. Chosen from Nationality master data.

## BusinessType {#Sponsor.BusinessTypeId}
<!-- alias: loc:BusinessType, dom:Sponsor_BusinessTypeId -->

**What it is.** The kind of business.

**Example.** Trading

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Required. Chosen from Business Type master data.

## OrganizationUnit {#Sponsor.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:Sponsor_OrganizationUnitId -->

**What it is.** The part of the organisation the sponsor belongs to.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Required. Chosen from Organization Unit master data.

## Governorate {#Sponsor.GovernorateId}
<!-- alias: loc:Governorate, dom:Sponsor_GovernorateId -->

**What it is.** The governorate of the address.

**Example.** Capital

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Required. Chosen from Governorate master data.

## UnifiedNum {#Sponsor.UnifiedNum}
<!-- alias: loc:UnifiedNum, dom:Sponsor_UnifiedNum -->

**What it is.** The sponsor's unified number with the authorities.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Required. Whole number.

## SocialSecurityNum {#Sponsor.SocialSecurityNum}
<!-- alias: loc:SocialSecurityNum, dom:Sponsor_SocialSecurityNum -->

**What it is.** The sponsor's social security registration number.

**Why it matters.** Shown on the Sponsor Report.

**Rules.** Required. Whole number.

## MOSALID {#Sponsor.MOSALID}
<!-- alias: loc:MOSALID, dom:Sponsor_MOSALID -->

**What it is.** The sponsor's file number with the Ministry of Social Affairs and Labour.

**Why it matters.** Shown on the Sponsor Report and on the payroll sheet reports.

**Rules.** Required. Whole number.

## BusinessOwnerNameEn {#Sponsor.BusinessOwnerNameEn}
<!-- alias: loc:BusinessOwnerNameEn, dom:Sponsor_BusinessOwnerNameEn -->

**What it is.** The business owner's name in English.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 256 characters.

## BusinessOwnerNameAr {#Sponsor.BusinessOwnerNameAr}
<!-- alias: loc:BusinessOwnerNameAr, dom:Sponsor_BusinessOwnerNameAr -->

**What it is.** The business owner's name in Arabic.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 256 characters.
