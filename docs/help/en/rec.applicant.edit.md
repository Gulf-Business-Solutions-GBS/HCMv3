---
helpId: rec.applicant.edit
lang: en
title: Applicants — Create / Edit
entity: HCMv3.Recruitment.Applicant
surfaces:
  - angular/src/app/admin/recruitment/applicants/create-or-edit-applicant-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.Applicants.Create]
---

## Overview

A job candidate. Candidates usually create themselves by registering on the careers site and filling in their profile; HR can also enter them here.

When an application of theirs is set to *Joined*, the applicant is copied into a new employee record -- names, birth date, gender, nationality, religion, marital status, qualification, contact details, Civil ID, joining and probation dates -- unless an employee with the same English names and birth date already exists.

The birth date must be in the past, the joining date after the birth date and probation end after joining. An email can belong to one applicant only.

## Title {#Applicant.TitleId}
<!-- alias: loc:Title, dom:Applicant_TitleId -->

**What it is.** The applicant's title.

**Example.** Mr

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Optional. Chosen from Title master data.

## FirstNameEn {#Applicant.FirstNameEn}
<!-- alias: loc:FirstNameEn, dom:Applicant_FirstNameEn -->

**What it is.** The first part in English of the applicant's name.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Required.

## SecondNameEn {#Applicant.SecondNameEn}
<!-- alias: loc:SecondNameEn, dom:Applicant_SecondNameEn -->

**What it is.** The second part in English of the applicant's name.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Optional.

## ThirdNameEn {#Applicant.ThirdNameEn}
<!-- alias: loc:ThirdNameEn, dom:Applicant_ThirdNameEn -->

**What it is.** The third part in English of the applicant's name.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Optional.

## LastNameEn {#Applicant.LastNameEn}
<!-- alias: loc:LastNameEn, dom:Applicant_LastNameEn -->

**What it is.** The family name in English of the applicant's name.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Required.

## FirstNameAr {#Applicant.FirstNameAr}
<!-- alias: loc:FirstNameAr, dom:Applicant_FirstNameAr -->

**What it is.** The first part in Arabic of the applicant's name.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Required.

## SecondNameAr {#Applicant.SecondNameAr}
<!-- alias: loc:SecondNameAr, dom:Applicant_SecondNameAr -->

**What it is.** The second part in Arabic of the applicant's name.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Optional.

## ThirdNameAr {#Applicant.ThirdNameAr}
<!-- alias: loc:ThirdNameAr, dom:Applicant_ThirdNameAr -->

**What it is.** The third part in Arabic of the applicant's name.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Optional.

## LastNameAr {#Applicant.LastNameAr}
<!-- alias: loc:LastNameAr, dom:Applicant_LastNameAr -->

**What it is.** The family name in Arabic of the applicant's name.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Required.

## BirthDate {#Applicant.BirthDate}
<!-- alias: loc:BirthDate, dom:Applicant_BirthDate -->

**What it is.** The date of birth.

**Why it matters.** Copied to the new employee when the applicant joins. Counts towards the profile strength shown to the candidate on the careers site. The applicant search report shows the age from it.

**Rules.** Required. Date value.

## Gender {#Applicant.Gender}
<!-- alias: loc:Gender, dom:Applicant_Gender -->

**What it is.** Male or female.

**Why it matters.** Copied to the new employee when the applicant joins. The applicant search report can filter by it.

**Rules.** Required. One of: Male, Female.

## MaritalStatus {#Applicant.MaritalStatusId}
<!-- alias: loc:MaritalStatus, dom:Applicant_MaritalStatusId -->

**What it is.** The marital status.

**Why it matters.** Copied to the new employee when the applicant joins. The applicant search report can filter by it.

**Rules.** Optional. Chosen from Marital Status master data.

## Nationality {#Applicant.NationalityId}
<!-- alias: loc:Nationality, dom:Applicant_NationalityId -->

**What it is.** The nationality.

**Why it matters.** Copied to the new employee when the applicant joins. Counts towards the profile strength shown to the candidate on the careers site.

**Rules.** Required. Chosen from Nationality master data.

## Religion {#Applicant.ReligionId}
<!-- alias: loc:Religion, dom:Applicant_ReligionId -->

**What it is.** The religion.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Optional. Chosen from Religion master data.

## Qualification {#Applicant.QualificationId}
<!-- alias: loc:Qualification, dom:Applicant_QualificationId -->

**What it is.** The highest qualification.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Optional. Chosen from Qualification master data.

## JobTitle {#Applicant.JobTitleId}
<!-- alias: loc:JobTitle, dom:Applicant_JobTitleId -->

**What it is.** The job title the applicant is looking for.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional. Chosen from Job Title master data.

## BloodGroup {#Applicant.BloodGroup}
<!-- alias: loc:BloodGroup, dom:Applicant_BloodGroup -->

**What it is.** The blood group.

**Example.** `O+`

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Optional. Maximum 50 characters.

## CivilID {#Applicant.CivilID}
<!-- alias: loc:CivilID, dom:Applicant_CivilID -->

**What it is.** The Kuwait Civil ID number.

**Why it matters.** Copied to the new employee when the applicant joins. Counts towards the profile strength shown to the candidate on the careers site.

**Rules.** Required. Whole number.

## Country {#Applicant.CountryId}
<!-- alias: loc:Country, dom:Applicant_CountryId -->

**What it is.** The country of residence.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Optional. Chosen from Country master data.

## PassportNum {#Applicant.PassportNum}
<!-- alias: loc:PassportNum, dom:Applicant_PassportNum -->

**What it is.** The passport number.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional. Maximum 100 characters.

## PlaceOfBirth {#Applicant.PlaceOfBirth}
<!-- alias: loc:PlaceOfBirth, dom:Applicant_PlaceOfBirth -->

**What it is.** The place of birth.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional. Maximum 256 characters.

## DrivingLicense {#Applicant.DrivingLicense}
<!-- alias: loc:DrivingLicense, dom:Applicant_DrivingLicense -->

**What it is.** The driving licence number.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional. Whole number.

## Photo {#Applicant.PhotoPath}
<!-- alias: loc:Photo, dom:Applicant_Photo -->

**What it is.** A photo of the applicant.

**Why it matters.** Counts towards the profile strength shown to the candidate on the careers site.

**Rules.** Optional.

## CurrentAddress {#Applicant.CurrentAddress}
<!-- alias: loc:CurrentAddress, dom:Applicant_CurrentAddress -->

**What it is.** The current address.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Optional.

## PermanentAddress {#Applicant.PermanentAddress}
<!-- alias: loc:PermanentAddress, dom:Applicant_PermanentAddress -->

**What it is.** The permanent address.

**Why it matters.** Copied to the new employee when the applicant joins. Counts towards the profile strength shown to the candidate on the careers site.

**Rules.** Optional.

## Address1 {#Applicant.Address1}
<!-- alias: loc:Address1, dom:Applicant_Address1 -->

**What it is.** An address line.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional.

## Address2 {#Applicant.Address2}
<!-- alias: loc:Address2, dom:Applicant_Address2 -->

**What it is.** An address line.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional.

## Telephone {#Applicant.Telephone}
<!-- alias: loc:Telephone, dom:Applicant_Telephone -->

**What it is.** A landline number.

**Why it matters.** Copied to the new employee when the applicant joins.

**Rules.** Optional. Whole number.

## Mobile {#Applicant.Mobile}
<!-- alias: loc:Mobile, dom:Applicant_Mobile -->

**What it is.** The mobile number.

**Example.** `96590001234`

**Why it matters.** Copied to the new employee when the applicant joins. Counts towards the profile strength shown to the candidate on the careers site.

**Rules.** Optional. Whole number.

## Email {#Applicant.Email}
<!-- alias: loc:Email, dom:Applicant_Email -->

**What it is.** The email address.

**Why it matters.** Copied to the new employee when the applicant joins. Counts towards the profile strength shown to the candidate on the careers site. Each email can belong to one applicant.

**Rules.** Optional. Maximum 256 characters.

## JoiningDate {#Applicant.JoiningDate}
<!-- alias: loc:JoiningDate, dom:Applicant_JoiningDate -->

**What it is.** The date the applicant joins, once hired.

**Why it matters.** Must be filled in before an application can be set to *Joined*; it becomes the employee's joining date.

**Rules.** Required. Date value.

## ProbationEndDate {#Applicant.ProbationEndDate}
<!-- alias: loc:ProbationEndDate, dom:Applicant_ProbationEndDate -->

**What it is.** The end of probation, once hired.

**Why it matters.** Must be filled in before an application can be set to *Joined*.

**Rules.** Optional. Date value.

## IsAgency {#Applicant.IsAgency}
<!-- alias: loc:IsAgency, dom:Applicant_IsAgency -->

**What it is.** Whether the applicant came through an agency.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Yes or no.
