---
helpId: pa.employee-visa-info.edit
lang: en
title: Visa Info
entity: HCMv3.PersonnelAdministration.EmployeeVisaInfo
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/visa-info/employee-visa-info.component.html
audience: [admin]
permissions: []
---

## Overview

The employee's visa, residency and passport particulars, including the details carried over from a previous sponsor.

A few government forms print the visa number, residency number and current file number from here. The other details are recorded for reference.

## CurrentFileNo {#EmployeeVisaInfo.CurrentFileNo}
<!-- alias: loc:CurrentFileNo, dom:VisaInfo_CurrentFileNo -->

**What it is.** The current file number with the authorities.

**Example.** 2026/45871

**Why it matters.** Printed on government forms.

**Rules.** Optional. Maximum 64 characters.

## CurrentContractNo {#EmployeeVisaInfo.CurrentContractNo}
<!-- alias: loc:CurrentContractNo, dom:VisaInfo_CurrentContractNo -->

**What it is.** The current work contract number.

**Example.** C-889214

**Rules.** Optional. Maximum 64 characters.

## CurrentLicenseNo {#EmployeeVisaInfo.CurrentLicenseNo}
<!-- alias: loc:CurrentLicenseNo, dom:VisaInfo_CurrentLicenseNo -->

**What it is.** The current work licence number.

**Example.** L-55120

**Rules.** Optional. Maximum 64 characters.

## OldFileNo {#EmployeeVisaInfo.OldFileNo}
<!-- alias: loc:OldFileNo, dom:VisaInfo_OldFileNo -->

**What it is.** The file number under the previous sponsor.

**Example.** 2019/11234

**Rules.** Optional. Maximum 64 characters.

## OldContractNo {#EmployeeVisaInfo.OldContractNo}
<!-- alias: loc:OldContractNo, dom:VisaInfo_OldContractNo -->

**What it is.** The contract number under the previous sponsor.

**Example.** C-440182

**Rules.** Optional. Maximum 64 characters.

## OldLicenseNo {#EmployeeVisaInfo.OldLicenseNo}
<!-- alias: loc:OldLicenseNo, dom:VisaInfo_OldLicenseNo -->

**What it is.** The work licence number under the previous sponsor.

**Example.** L-30211

**Rules.** Optional. Maximum 64 characters.

## MoiFileNumber {#EmployeeVisaInfo.MOIFileNumber}
<!-- alias: loc:MoiFileNumber, dom:VisaInfo_MoiFileNumber -->

**What it is.** The Ministry of Interior file number.

**Example.** 7712045

**Rules.** Optional. Maximum 64 characters.

## VisaNum {#EmployeeVisaInfo.VisaNum}
<!-- alias: loc:VisaNum, dom:VisaInfo_VisaNum -->

**What it is.** The visa number.

**Example.** 18001234567

**Why it matters.** Printed on government forms.

**Rules.** Optional. Whole number.

## ResidencyNum {#EmployeeVisaInfo.ResidencyNum}
<!-- alias: loc:ResidencyNum, dom:VisaInfo_ResidencyNum -->

**What it is.** The residency number.

**Example.** 28803140012

**Why it matters.** Printed on government forms.

**Rules.** Optional. Whole number.

## AuthorizedSign {#EmployeeVisaInfo.AuthorizedSign}
<!-- alias: loc:AuthorizedSign, dom:VisaInfo_AuthorizedSign -->

**What it is.** The authorised signatory, as free text.

**Example.** HR Manager

**Rules.** Optional. Maximum 128 characters.

## PreviousResidencyPeriod {#EmployeeVisaInfo.PreviousResidencyPeriod}
<!-- alias: loc:PreviousResidencyPeriod, dom:VisaInfo_PreviousResidencyPeriod -->

**What it is.** The residency period under the previous sponsor.

**Example.** 3 years

**Rules.** Optional. Maximum 64 characters.

## CurrentResidencyPeriod {#EmployeeVisaInfo.CurrentResidencyPeriod}
<!-- alias: loc:CurrentResidencyPeriod, dom:VisaInfo_CurrentResidencyPeriod -->

**What it is.** The current residency period.

**Example.** 2 years

**Rules.** Optional. Maximum 64 characters.

## OldContractSalary {#EmployeeVisaInfo.OldContractSalary}
<!-- alias: loc:OldContractSalary, dom:VisaInfo_OldContractSalary -->

**What it is.** The contract salary under the previous sponsor.

**Example.** 450.000

**Rules.** Optional. Number.

## CurrentContractSalary {#EmployeeVisaInfo.CurrentContractSalary}
<!-- alias: loc:CurrentContractSalary, dom:VisaInfo_CurrentContractSalary -->

**What it is.** The current contract salary.

**Example.** 600.000

**Rules.** Optional. Number.

## NewProfession {#EmployeeVisaInfo.NewProfesion}
<!-- alias: loc:NewProfession, dom:VisaInfo_NewProfesion -->

**What it is.** The new profession on the visa, in English.

**Example.** Accountant

**Rules.** Optional. Maximum 256 characters.

## NewProfessionAr {#EmployeeVisaInfo.NewProfessionAr}
<!-- alias: loc:NewProfessionAr, dom:VisaInfo_NewProfessionAr -->

**What it is.** The new profession on the visa, in Arabic.

**Example.** محاسب

**Rules.** Optional. Maximum 256 characters.

## OldProfession {#EmployeeVisaInfo.OldProfession}
<!-- alias: loc:OldProfession, dom:VisaInfo_OldProfession -->

**What it is.** The profession under the previous sponsor.

**Example.** Clerk

**Rules.** Optional. Maximum 256 characters.

## OldCompany {#EmployeeVisaInfo.OldCompany}
<!-- alias: loc:OldCompany, dom:VisaInfo_OldCompany -->

**What it is.** The previous employer.

**Example.** Gulf Trading Co.

**Rules.** Optional. Maximum 256 characters.

## OldSponsor {#EmployeeVisaInfo.OldSponser}
<!-- alias: loc:OldSponsor, dom:VisaInfo_OldSponser -->

**What it is.** The previous sponsor.

**Example.** Gulf Trading Co.

**Rules.** Optional. Maximum 256 characters.

## PassportType {#EmployeeVisaInfo.PassportTypeId}
<!-- alias: loc:PassportType, dom:VisaInfo_PassportTypeId -->

**What it is.** The kind of passport, from the list.

**Example.** Ordinary

**Rules.** Optional. Chosen from Passport Type master data.

## TypeOfPassport {#EmployeeVisaInfo.TypeOfPassport}
<!-- alias: loc:TypeOfPassport, dom:VisaInfo_TypeOfPassport -->

**What it is.** The kind of passport, as free text.

**Example.** Ordinary

**Rules.** Optional. Maximum 64 characters.

## IssuePlacePassport {#EmployeeVisaInfo.IssuePlacePassport}
<!-- alias: loc:IssuePlacePassport, dom:VisaInfo_IssuePlacePassport -->

**What it is.** Where the passport was issued.

**Example.** Cairo

**Rules.** Optional. Maximum 128 characters.

## PassportIssueDate {#EmployeeVisaInfo.PassportIssueDate}
<!-- alias: loc:PassportIssueDate, dom:VisaInfo_PassportIssueDate -->

**What it is.** The date the passport was issued.

**Example.** 3 Mar 2022

**Rules.** Optional. Date value.

## PassportExpiryDate {#EmployeeVisaInfo.PassportExpiryDate}
<!-- alias: loc:PassportExpiryDate, dom:VisaInfo_PassportExpiryDate -->

**What it is.** The date the passport expires.

**Example.** 2 Mar 2032

**Rules.** Optional. Date value.

## EntryDate {#EmployeeVisaInfo.EntryDate}
<!-- alias: loc:EntryDate, dom:VisaInfo_EntryDate -->

**What it is.** The date the employee entered the country.

**Example.** 10 Aug 2019

**Rules.** Optional. Date value.

## TerminationDate {#EmployeeVisaInfo.TerminationDate}
<!-- alias: loc:TerminationDate, dom:VisaInfo_TerminationDate -->

**What it is.** The date the visa or residency ended.

**Example.** 31 Aug 2026

**Rules.** Optional. Date value.
