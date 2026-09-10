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

The fields are shown as text rather than inputs because the common case is looking something up, not changing it. Choosing **Edit** opens the same form used to create an employee, so validation and business rules are identical wherever the record is changed.

## Code {#Employee.Code}
<!-- alias: loc:Code, dom:Employee_Code -->

**What it is.** The employee's business identifier, unique within the company. It appears on payslips, reports and import files, and is the number colleagues will quote when they refer to this person.

**Example.** `001245`

**Why it matters.** Left blank on a new record, the system assigns the next number automatically (highest existing number plus one).

**Rules.** Optional. Maximum 64 characters.

## FullNameEn {#Employee.FullNameEn}
<!-- alias: loc:FullNameEn -->

**What it is.** The complete English name, assembled from the four English name parts on the edit form.

**Example.** `Ahmed Khaled Saleh Al-Mutairi`

**Why it matters.** Derived rather than typed. To correct it, edit the individual name parts; this field then rebuilds itself.

**Rules.** Optional. Maximum 512 characters.

## FullNameAr {#Employee.FullNameAr}
<!-- alias: loc:FullNameAr -->

**What it is.** The complete Arabic name, assembled from the four Arabic name parts.

**Example.** `أحمد خالد صالح المطيري`

**Why it matters.** Used on Arabic statutory output such as PIFSS submissions and Ministry of Labour paperwork, where the name must match the civil ID exactly.

**Rules.** Optional. Maximum 512 characters.

## Title {#Employee.TitleId}
<!-- alias: loc:Title, dom:Employee_TitleId -->

**What it is.** The courtesy title used when addressing the employee in letters and generated correspondence.

**Example.** `Mr`

**Rules.** Optional. Chosen from Title master data.

:::admin
Maintained under General Master Info → Titles.
:::

## Status {#Employee.Status}
<!-- alias: loc:Status, dom:Employee_Status -->

**What it is.** Whether this record is currently in service. Inactive employees stay on file with their full history but drop out of payroll runs, org charts and most selection lists.

**Example.** `Active`

**Why it matters.** Set this to Inactive rather than deleting a leaver -- deleting would take their payroll and leave history with them.

**Rules.** Required. One of: Inactive, Active.

## EmploymentType {#Employee.EmploymentType}
<!-- alias: loc:EmploymentType, dom:Employee_EmploymentType -->

**What it is.** Whether the employee is on permanent or temporary terms.

**Example.** `Permanent`

**Why it matters.** Determines eligibility for benefits, leave accrual and end-of-service entitlements that are restricted to permanent staff.

**Rules.** Required. One of: Permanent, Temporary, ALL.

## Gender {#Employee.Gender}
<!-- alias: loc:Gender, dom:Employee_Gender -->

**What it is.** The gender recorded on the civil ID.

**Example.** `Female`

**Why it matters.** Drives statutory reporting and gender-specific leave entitlements such as maternity leave.

**Rules.** Required. One of: Male, Female.

## BirthDate {#Employee.BirthDate}
<!-- alias: loc:BirthDate, dom:Employee_BirthDate -->

**What it is.** Date of birth exactly as it appears on the civil ID.

**Example.** `14 March 1988`

**Why it matters.** Drives retirement age, age-based benefit rules and several statutory returns. A mismatch against the civil ID will cause government file submissions to be rejected.

**Rules.** Required. Date value.

## BirthPlace {#Employee.BirthPlaceId}
<!-- alias: loc:BirthPlace, dom:Employee_BirthPlaceId -->

**What it is.** The city recorded as place of birth.

**Example.** `Kuwait City`

**Why it matters.** Appears on residency and visa paperwork for expatriate employees.

**Rules.** Optional. Chosen from City master data.

:::admin
The list is the shared City master data, maintained under General Master Info → Cities.
:::

## Nationality {#Employee.NationalityId}
<!-- alias: loc:Nationality, dom:Employee_NationalityId -->

**What it is.** The employee's nationality as recorded on the civil ID.

**Example.** `Kuwaiti`

**Why it matters.** One of the most consequential fields on the record. It drives PIFSS social-security eligibility, the indemnity calculation, expatriate allowances and work-permit handling.

**Rules.** Required. Chosen from Nationality master data.

:::admin
Maintained under General Master Info → Nationalities. Deactivating a nationality hides it from new records but leaves existing employees untouched.
:::

## Religion {#Employee.ReligionId}
<!-- alias: loc:Religion, dom:Employee_ReligionId -->

**What it is.** Religion, where the organisation records it.

**Example.** `Muslim`

**Why it matters.** Used only where statutory or policy entitlements differ, such as Hajj leave. Leave it blank if your organisation does not track it.

**Rules.** Optional. Chosen from Religion master data.

## MaritalStatus {#Employee.MaritalStatusId}
<!-- alias: loc:MaritalStatus, dom:Employee_MaritalStatusId -->

**What it is.** Current marital status.

**Example.** `Married`

**Why it matters.** Feeds dependant-related allowances and benefit eligibility. Keep it current -- a change of status can alter entitlement from the effective month.

**Rules.** Optional. Chosen from Marital Status master data.

## Qualification {#Employee.QualificationId}
<!-- alias: loc:Qualification, dom:Employee_QualificationId -->

**What it is.** Highest completed academic or professional qualification.

**Example.** `Bachelor of Engineering`

**Why it matters.** Used in job-profile matching, succession planning and manpower reporting.

**Rules.** Optional. Chosen from Qualification master data.

## BloodGroup {#Employee.BloodGroup}
<!-- alias: loc:BloodGroup, dom:Employee_BloodGroup -->

**What it is.** Blood group, held for medical and emergency use.

**Example.** `O+`

**Rules.** Optional. Maximum 32 characters.

## CivilID {#Employee.CivilID}
<!-- alias: loc:CivilID, dom:Employee_CivilID -->

**What it is.** The 12-digit Kuwait Civil ID number. This is the single strongest identifier on the record and must match the card exactly.

**Example.** `288031400123`

**Why it matters.** Used to identify the employee in PIFSS and PACI submissions. An incorrect digit will cause those files to be rejected for the whole batch, not just this employee.

**Rules.** Required. Whole number.

## PACINumber {#Employee.PACINumber}
<!-- alias: loc:PACINumber -->

**What it is.** The PACI address reference for the employee, issued by the Public Authority for Civil Information.

**Example.** `12345678`

**Why it matters.** Identifies the registered civil address, which some government submissions require alongside the civil ID.

**Rules.** Optional. Maximum 32 characters.

## JoiningDate {#Employee.JoiningDate}
<!-- alias: loc:JoiningDate, dom:Employee_JoiningDate -->

**What it is.** The date employment actually began.

**Example.** `1 September 2024`

**Why it matters.** The anchor date for the entire record: seniority, indemnity and end-of-service calculations, leave accrual and the probation period are all measured from it. Changing it recalculates a great deal downstream.

**Rules.** Required. Date value.

## ProbationDays {#Employee.ProbationDays}
<!-- alias: loc:ProbationDays, dom:Employee_ProbationDays -->

**What it is.** Length of the probation period, counted in calendar days from the joining date.

**Example.** `90`

**Rules.** Required. Whole number.

## ProbationEndDate {#Employee.ProbationEndDate}
<!-- alias: loc:ProbationEndDate -->

**What it is.** The date probation ends.

**Example.** `30 November 2024`

**Why it matters.** Calculated as joining date plus probation days and recalculated automatically whenever either changes. It is stored rather than derived on the fly so that reports and reminders can query it directly.

**Rules.** Required. Date value.

## RecruitmentType {#Employee.RecruitmentTypeId}
<!-- alias: loc:RecruitmentType, dom:Employee_RecruitmentTypeId -->

**What it is.** How this employee came into the organisation -- direct hire, agency, internal transfer, and so on.

**Example.** `Direct`

**Why it matters.** Used for recruitment cost analysis and source-of-hire reporting.

**Rules.** Optional. Chosen from Recruitment Type master data.

## PreviousEmpCode {#Employee.PreviousEmpCode}
<!-- alias: loc:PreviousEmpCode, dom:Employee_PreviousEmpCode -->

**What it is.** The identifier this employee held previously -- either in a system replaced by HCM, or during an earlier spell of employment.

**Example.** `EMP-8841`

**Why it matters.** Kept so that historical records and archived reports can still be traced back to the person.

**Rules.** Optional. Maximum 64 characters.

## ReferenceCode {#Employee.ReferenceCode}
<!-- alias: loc:ReferenceCode, dom:Employee_ReferenceCode -->

**What it is.** A free reference of your own choosing, typically the key used for this employee in an external system.

**Example.** `SAP-00123`

**Why it matters.** Not used by any calculation. It exists so integrations have somewhere to keep their own identifier.

**Rules.** Optional. Maximum 64 characters.

## Email {#Employee.Email}
<!-- alias: loc:Email, dom:Employee_Email -->

**What it is.** The work email address.

**Example.** `ahmed.mutairi@company.com`

**Why it matters.** Where the system sends notifications, workflow approvals and self-service account details. If this is wrong the employee will not receive approval requests.

**Rules.** Optional. Maximum 256 characters.

## AlternateEmail {#Employee.AlternateEmail}
<!-- alias: loc:AlternateEmail, dom:Employee_AlternateEmail -->

**What it is.** A secondary or personal email address.

**Example.** `a.mutairi@gmail.com`

**Why it matters.** Used as a fallback, and for correspondence that must continue after the work account is closed at the end of service.

**Rules.** Optional. Maximum 256 characters.

## Mobile {#Employee.Mobile}
<!-- alias: loc:Mobile, dom:Employee_Mobile -->

**What it is.** Mobile telephone number, entered as digits without spaces or symbols.

**Example.** `96550123456`

**Why it matters.** Used for SMS notifications and one-time passcodes. Include the country code so international sending works.

**Rules.** Required. Whole number.

## Telephone {#Employee.Telephone}
<!-- alias: loc:Telephone, dom:Employee_Telephone -->

**What it is.** Home or personal landline number.

**Example.** `22551234`

**Rules.** Required. Whole number.

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

**Why it matters.** Used for correspondence and for any document that must show a residential address.

**Rules.** Optional. Maximum 1000 characters.

## PermanentAddress {#Employee.PermanentAddress}
<!-- alias: loc:PermanentAddress, dom:Employee_PermanentAddress -->

**What it is.** The permanent or home-country address, where it differs from the current one.

**Example.** `House 22, Sector G-9, Islamabad, Pakistan`

**Why it matters.** Used at end of service for final correspondence and repatriation ticketing.

**Rules.** Optional. Maximum 1000 characters.

## City {#Employee.City}
<!-- alias: loc:City, dom:Employee_City -->

**What it is.** City of the current address.

**Example.** `Salmiya`

**Rules.** Optional. Maximum 128 characters.

## Governorate {#Employee.GovernorateId}
<!-- alias: loc:Governorate, dom:Employee_GovernorateId -->

**What it is.** Governorate of the current address.

**Example.** `Hawalli`

**Rules.** Optional. Chosen from Governorate master data.

:::admin
Maintained under General Master Info → Governorates.
:::

## Country {#Employee.CountryId}
<!-- alias: loc:Country, dom:Employee_CountryId -->

**What it is.** Country of the current address.

**Example.** `Kuwait`

**Rules.** Optional. Chosen from Country master data.

## ZipCode {#Employee.ZipCode}
<!-- alias: loc:ZipCode, dom:Employee_ZipCode -->

**What it is.** Postal code for the current address, where one applies.

**Example.** `22001`

**Rules.** Optional. Maximum 32 characters.

## EmergencyContact {#Employee.EmergencyContact}
<!-- alias: loc:EmergencyContact, dom:Employee_EmergencyContact -->

**What it is.** The name of the person to contact if something happens to this employee at work.

**Example.** `Fatima Al-Mutairi`

**Why it matters.** Reviewed during onboarding and worth confirming annually -- it is the one field nobody checks until it is needed.

**Rules.** Optional. Maximum 256 characters.

## EmergencyRelation {#Employee.EmergencyRelation}
<!-- alias: loc:EmergencyRelation, dom:Employee_EmergencyRelation -->

**What it is.** How the emergency contact is related to the employee.

**Example.** `Spouse`

**Rules.** Optional. Maximum 128 characters.

## EmergencyNumber {#Employee.EmergencyNumber}
<!-- alias: loc:EmergencyNumber, dom:Employee_EmergencyNumber -->

**What it is.** Telephone number for the emergency contact.

**Example.** `96555512345`

**Rules.** Optional. Maximum 64 characters.

## EmergencyContactAddress {#Employee.EmergencyContactAddress}
<!-- alias: loc:EmergencyContactAddress, dom:Employee_EmergencyContactAddress -->

**What it is.** Address of the emergency contact, where it differs from the employee's own.

**Example.** `Block 4, Street 12, Building 7, Flat 3, Salmiya`

**Rules.** Optional. Maximum 1000 characters.

## CriticalIllness {#Employee.CriticalIllness}
<!-- alias: loc:CriticalIllness, dom:Employee_CriticalIllness -->

**What it is.** Medical conditions, allergies or medication that first responders or the company doctor should know about.

**Example.** `Type 1 diabetes; allergic to penicillin`

**Why it matters.** Sensitive personal data. Record only what genuinely serves the employee in an emergency, and restrict who can view this screen accordingly.

**Rules.** Optional. Maximum 1000 characters.
