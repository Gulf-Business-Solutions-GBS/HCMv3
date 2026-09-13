---
helpId: pa.employee.personal-info
lang: en
title: Employee — Personal Information
entity: HCMv3.PersonnelAdministration.Employee
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/personal-info/employee-personal-info.component.html
  - angular/src/app/admin/personnel-administration/employees/create-or-edit-employee-modal.component.html
audience: [end-user, admin]
permissions: [Pages.PersonnelAdministration.Employees]
---

## Overview

The employee's own particulars, shown as a read-only record with **Edit** in reach.

Every other tab on an employee record lists things that happen *to* them -- absences, loans, contracts, gratuities. This tab is the person themselves: who they are, how to reach them, and who to call in an emergency.

The fields are shown as text rather than inputs because the common case is looking something up, not changing it. Choosing **Edit** opens the same form used to create an employee, so the same checks apply wherever the record is changed.

## Code {#Employee.Code}
<!-- alias: loc:Code, dom:Employee_Code -->

**What it is.** The employee's business identifier, unique across the organisation. It is how the employee is identified on lists, reports and printed forms.

**Example.** `001245`

**Why it matters.** The next number assigned to a blank code is the highest existing numeric code plus one.

**Rules.** Optional. Maximum 64 characters. Left blank, the next number is assigned when the record is saved. Must be unique in this list.

## FullNameEn {#Employee.FullNameEn}
<!-- alias: loc:FullNameEn -->

**What it is.** The complete English name, assembled from the four English name parts on the edit form.

**Example.** `Ahmed Khaled Saleh Al-Mutairi`

**Why it matters.** Derived rather than typed. To correct it, edit the individual name parts; this field then rebuilds itself when the record is saved.

**Rules.** Set by the system when the record is saved; not entered on the form.

## FullNameAr {#Employee.FullNameAr}
<!-- alias: loc:FullNameAr -->

**What it is.** The complete Arabic name, assembled from the four Arabic name parts.

**Example.** `أحمد خالد صالح المطيري`

**Why it matters.** Printed on Arabic government forms, such as the absence report cancellation and waiver forms. To correct it, edit the Arabic name parts; it rebuilds itself when the record is saved.

**Rules.** Set by the system when the record is saved; not entered on the form.

## Title {#Employee.TitleId}
<!-- alias: loc:Title, dom:Employee_TitleId -->

**What it is.** The courtesy title used before the employee's name.

**Example.** `Mr`

**Why it matters.** Printed before the name on the employment notice. Employees can ask for a change through self-service.

**Rules.** Optional. Chosen from Title master data.

:::admin
Maintained under General Master Info → Titles.
:::

## Status {#Employee.Status}
<!-- alias: loc:Status, dom:Employee_Status -->

**What it is.** Whether this record is currently in service. Inactive employees keep their full record and history, but are left out of payroll runs, the bulk password and attendance tools, self-service lookups and many reports.

**Example.** `Active`

**Why it matters.** Set a leaver to Inactive rather than deleting them: deleting removes the employee from the system, while Inactive keeps the record and its history available. An inactive employee's Civil ID and Access ID can be given to a new record.

**Rules.** Required. One of: Active, Inactive.

## EmploymentType {#Employee.EmploymentType}
<!-- alias: loc:EmploymentType, dom:Employee_EmploymentType -->

**What it is.** Whether the employee is on permanent or temporary terms.

**Example.** `Permanent`

**Why it matters.** No built-in calculation uses it. Business rules can test it -- for example, to apply a rule only to temporary staff.

**Rules.** Required. One of: Permanent, Temporary.

## Gender {#Employee.Gender}
<!-- alias: loc:Gender, dom:Employee_Gender -->

**What it is.** The gender recorded on the Civil ID.

**Example.** `Female`

**Why it matters.** Used by the social security (PIFSS) calculations in payroll. Business rules can also test it; the sample maternity-leave rule uses it to refuse maternity leave for male employees, where that rule is switched on. Employees can ask for a change through self-service.

**Rules.** Required. One of: Male, Female, Not specified.

:::admin
Absence types have a gender setting, but the system does not currently check it against this field. To restrict a leave type by gender, use a business rule.
:::

## BirthDate {#Employee.BirthDate}
<!-- alias: loc:BirthDate, dom:Employee_BirthDate -->

**What it is.** Date of birth exactly as it appears on the Civil ID.

**Example.** `14 March 1988`

**Why it matters.** It must be before today, and the joining date must come after it. Active employees receive a birthday notification on the day, and business rules can test it. Employees can ask for a change through self-service.

**Rules.** Required. Date value.

## BirthPlace {#Employee.BirthPlaceId}
<!-- alias: loc:BirthPlace, dom:Employee_BirthPlaceId -->

**What it is.** The city recorded as place of birth.

**Example.** `Kuwait City`

**Why it matters.** Printed on the new work-visa application and the end-of-service / visa cancellation forms. Employees can ask for a change through self-service.

**Rules.** Optional. Chosen from City master data.

:::admin
The list is the shared City master data, maintained under General Master Info → Cities.
:::

## Nationality {#Employee.NationalityId}
<!-- alias: loc:Nationality, dom:Employee_NationalityId -->

**What it is.** The employee's nationality as recorded on the Civil ID.

**Example.** `Kuwaiti`

**Why it matters.** One of the most consequential fields on the record. It decides which social security (PIFSS) rules apply in payroll, and whether the employee counts as a citizen or a non-citizen in the workforce quota under Government Relations. It is also printed on the work-visa request form.

**Rules.** Required. Chosen from Nationality master data.

:::admin
Maintained under General Master Info → Nationalities. For the workforce quota, a nationality counts as citizen when its nationality group is named *Citizen*.
:::

## Religion {#Employee.ReligionId}
<!-- alias: loc:Religion, dom:Employee_ReligionId -->

**What it is.** Religion, where the organisation records it.

**Example.** `Muslim`

**Why it matters.** Recorded for information and printed on the work-visa request form; no calculation uses it. Leave it blank if your organisation does not track it.

**Rules.** Optional. Chosen from Religion master data.

:::admin
Maintained under General Master Info → Religions. Absence types have a religion setting (for leave such as Hajj), but the system does not currently check it against this field.
:::

## MaritalStatus {#Employee.MaritalStatusId}
<!-- alias: loc:MaritalStatus, dom:Employee_MaritalStatusId -->

**What it is.** Current marital status.

**Example.** `Married`

**Why it matters.** Used by the social security (PIFSS) calculations in payroll, including the marital-status allowance, so keep it current. Employees can ask for a change through self-service.

**Rules.** Optional. Chosen from Marital Status master data.

:::admin
Maintained under General Master Info → Marital Statuses.
:::

## Qualification {#Employee.QualificationId}
<!-- alias: loc:Qualification, dom:Employee_QualificationId -->

**What it is.** Highest completed academic or professional qualification.

**Example.** `Bachelor of Engineering`

**Why it matters.** The social security allowance in payroll can depend on it, alongside nationality, gender and marital status. It is also printed on the work-visa request form.

**Rules.** Optional. Chosen from Qualification master data.

:::admin
Maintained under General Master Info → Qualifications.
:::

## BloodGroup {#Employee.BloodGroup}
<!-- alias: loc:BloodGroup, dom:Employee_BloodGroup -->

**What it is.** Blood group, held for medical and emergency use.

**Example.** `O+`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 32 characters.

## CivilID {#Employee.CivilID}
<!-- alias: loc:CivilID, dom:Employee_CivilID -->

**What it is.** The 12-digit Kuwait Civil ID number. It must match the card exactly.

**Example.** `288031400123`

**Why it matters.** Printed on government forms. Two active employees cannot share a Civil ID: the form refuses a number that another active employee already has.

**Rules.** Optional. Whole number.

## PACINumber {#Employee.PACINumber}
<!-- alias: loc:PACINumber, dom:Employee_PaciNumber, loc:PaciNumber -->

**What it is.** The PACI address reference for the employee, issued by the Public Authority for Civil Information.

**Example.** `12345678`

**Why it matters.** Kept for reference; no form or calculation uses it yet. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 32 characters.

## JoiningDate {#Employee.JoiningDate}
<!-- alias: loc:JoiningDate, dom:Employee_JoiningDate -->

**What it is.** The date employment actually began. It must come after the birth date.

**Example.** `1 September 2024`

**Why it matters.** The anchor date for the record: leave accruals, leave provisions, service bonuses and the probation period are all measured from it, and active employees receive a work-anniversary notification. Nothing is recalculated the moment you save -- payroll, accruals and provisions use the new date the next time they run.

**Rules.** Required. Date value.

## ProbationDays {#Employee.ProbationDays}
<!-- alias: loc:ProbationDays, dom:Employee_ProbationDays -->

**What it is.** Length of the probation period, counted in calendar days from the joining date.

**Example.** `90`

**Why it matters.** Sets the probation end date, which is the joining date plus these days. It cannot be negative.

**Rules.** Optional. Whole number.

## ProbationEndDate {#Employee.ProbationEndDate}
<!-- alias: loc:ProbationEndDate -->

**What it is.** The date probation ends.

**Example.** `30 November 2024`

**Why it matters.** Calculated as the joining date plus the probation days, every time the record is saved. To change it, change either of those.

**Rules.** Set by the system when the record is saved; not entered on the form.

## RecruitmentType {#Employee.RecruitmentTypeId}
<!-- alias: loc:RecruitmentType, dom:Employee_RecruitmentTypeId -->

**What it is.** How this employee came into the organisation -- direct hire, agency, internal transfer, and so on.

**Example.** `Direct`

**Why it matters.** Recorded for information; no built-in calculation or report uses it. Business rules can test it.

**Rules.** Optional. Chosen from Recruitment Type master data.

:::admin
Maintained under General Master Info → Recruitment Types.
:::

## PreviousEmpCode {#Employee.PreviousEmpCode}
<!-- alias: loc:PreviousEmpCode, dom:Employee_PreviousEmpCode -->

**What it is.** The code this employee held before -- in an earlier period of employment, or in a system HCMv3 replaced.

**Example.** `EMP-8841`

**Why it matters.** Shown on the employee rejoin report, so a returning employee can be matched to their earlier record.

**Rules.** Optional. Maximum 64 characters.

## ReferenceCode {#Employee.ReferenceCode}
<!-- alias: loc:ReferenceCode, dom:Employee_ReferenceCode -->

**What it is.** A reference of your own choosing, typically the key used for this employee in another system.

**Example.** `SAP-00123`

**Why it matters.** No calculation uses it. It appears on the employee information report, and some customised payroll journal exports use it to identify the employee in the accounting system.

**Rules.** Optional. Maximum 64 characters.

## Email {#Employee.Email}
<!-- alias: loc:Email, dom:Employee_Email -->

**What it is.** The work email address.

**Example.** `ahmed.mutairi@company.com`

**Why it matters.** Where HCMv3 emails the employee: new sign-in details from the bulk password tool, scheduled report deliveries, and notifications such as birthday and work-anniversary greetings. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 256 characters.

## AlternateEmail {#Employee.AlternateEmail}
<!-- alias: loc:AlternateEmail, dom:Employee_AlternateEmail -->

**What it is.** A secondary or personal email address.

**Example.** `a.mutairi@gmail.com`

**Why it matters.** Kept for reference; HCMv3 does not send anything to it. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 256 characters.

## Mobile {#Employee.Mobile}
<!-- alias: loc:Mobile, dom:Employee_Mobile -->

**What it is.** Mobile telephone number, entered as digits without spaces or symbols.

**Example.** `96550123456`

**Why it matters.** The number HR and managers use to reach the employee, and it is printed on the work-visa request form. Include the country code. Employees can ask for a change through self-service.

:::admin
This is not the number sign-in codes are sent to. Text messages from HCMv3 -- verification and sign-in codes -- go to the phone number on the employee's *user account*, which is maintained separately under Users.
:::

**Rules.** Optional. Whole number.

## Telephone {#Employee.Telephone}
<!-- alias: loc:Telephone, dom:Employee_Telephone -->

**What it is.** Home or personal landline number, entered as digits.

**Example.** `22551234`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Whole number.

## BusinessLandline {#Employee.BusinessLandline}
<!-- alias: loc:BusinessLandline, dom:Employee_BusinessLandline -->

**What it is.** The direct office line for this employee.

**Example.** `+965 2222 3344`

**Rules.** Optional. Maximum 64 characters.

## BusinessExtension {#Employee.BusinessExtension}
<!-- alias: loc:BusinessExtension, dom:Employee_BusinessExtension -->

**What it is.** Internal switchboard extension.

**Example.** `4417`

**Rules.** Optional. Maximum 32 characters.

## CurrentAddress {#Employee.CurrentAddress}
<!-- alias: loc:CurrentAddress, dom:Employee_CurrentAddress -->

**What it is.** Where the employee lives now, in full.

**Example.** `Block 4, Street 12, Building 7, Flat 3, Salmiya`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 1000 characters.

## PermanentAddress {#Employee.PermanentAddress}
<!-- alias: loc:PermanentAddress, dom:Employee_PermanentAddress -->

**What it is.** The permanent or home-country address, where it differs from the current one.

**Example.** `House 22, Sector G-9, Islamabad, Pakistan`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 1000 characters.

## City {#Employee.City}
<!-- alias: loc:City, dom:Employee_City -->

**What it is.** City of the current address, typed in.

**Example.** `Salmiya`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 128 characters.

## Governorate {#Employee.GovernorateId}
<!-- alias: loc:Governorate, dom:Employee_GovernorateId -->

**What it is.** Governorate of the current address.

**Example.** `Hawalli`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Chosen from Governorate master data.

:::admin
Maintained under General Master Info → Governorates.
:::

## Country {#Employee.CountryId}
<!-- alias: loc:Country, dom:Employee_CountryId -->

**What it is.** Country of the current address.

**Example.** `Kuwait`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Chosen from Country master data.

:::admin
Maintained under General Master Info → Countries.
:::

## ZipCode {#Employee.ZipCode}
<!-- alias: loc:ZipCode, dom:Employee_ZipCode -->

**What it is.** Postal code for the current address, where one applies.

**Example.** `22001`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 32 characters.

## EmergencyContact {#Employee.EmergencyContact}
<!-- alias: loc:EmergencyContact, dom:Employee_EmergencyContact -->

**What it is.** The name of the person to contact if something happens to this employee at work.

**Example.** `Fatima Al-Mutairi`

**Why it matters.** Worth confirming once a year -- it is the one field nobody checks until it is needed. Employees can update it through self-service.

**Rules.** Optional. Maximum 256 characters.

## EmergencyRelation {#Employee.EmergencyRelation}
<!-- alias: loc:EmergencyRelation, dom:Employee_EmergencyRelation -->

**What it is.** How the emergency contact is related to the employee.

**Example.** `Spouse`

**Why it matters.** Employees can update it through self-service.

**Rules.** Optional. Maximum 128 characters.

## EmergencyNumber {#Employee.EmergencyNumber}
<!-- alias: loc:EmergencyNumber, dom:Employee_EmergencyNumber -->

**What it is.** Telephone number for the emergency contact.

**Example.** `96555512345`

**Why it matters.** Employees can update it through self-service.

**Rules.** Optional. Maximum 64 characters.

## EmergencyContactAddress {#Employee.EmergencyContactAddress}
<!-- alias: loc:EmergencyContactAddress, dom:Employee_EmergencyContactAddress -->

**What it is.** Address of the emergency contact, where it differs from the employee's own.

**Example.** `Block 4, Street 12, Building 7, Flat 3, Salmiya`

**Why it matters.** Unlike the other emergency details, it is not on the self-service change request, so only HR can update it.

**Rules.** Optional. Maximum 1000 characters.

## CriticalIllness {#Employee.CriticalIllness}
<!-- alias: loc:CriticalIllness, dom:Employee_CriticalIllness -->

**What it is.** Medical conditions, allergies or medication that first responders or the company doctor should know about.

**Example.** `Type 1 diabetes; allergic to penicillin`

**Why it matters.** Sensitive personal data. Record only what genuinely serves the employee in an emergency, and restrict who can view this screen accordingly. Employees can update it through self-service.

**Rules.** Optional. Maximum 1000 characters.
