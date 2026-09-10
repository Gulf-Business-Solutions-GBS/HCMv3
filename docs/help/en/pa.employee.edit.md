---
helpId: pa.employee.edit
lang: en
title: Employee — Create / Edit
entity: HCMv3.PersonnelAdministration.Employee
surfaces:
  - angular/src/app/admin/personnel-administration/employees/create-or-edit-employee-modal.component.html
audience: [end-user, admin]
permissions: [Pages.PersonnelAdministration.Employees.Create, Pages.PersonnelAdministration.Employees.Edit]
---

## Overview

The one form for an employee's own particulars. **Create** on the Employees list opens it empty; **Edit** on an employee's Personal Information tab opens it filled in. Because both use the same form, the same checks apply however the record is changed.

The form is split into sections -- Names, Personal info, Address, Contact, Emergency contact and Employment. Fields marked as required must be filled before you can save, and a section that is still missing one shows a count beside its title.

When you save, the system also:

- builds the English and Arabic full names from the name parts you entered
- works out the probation end date as the joining date plus the probation days
- gives the employee the next free code if you left **Code** blank

It refuses to save when the code is already in use, the birth date is today or later, the joining date is not after the birth date, or the probation days are negative. It also refuses a Civil ID or an Access ID that another *active* employee already has, and a Login ID that *any* other employee has.

## FirstNameEn {#Employee.FirstNameEn}
<!-- alias: loc:FirstNameEn, dom:Employee_FirstNameEn -->

**What it is.** The employee's first (given) name in English, spelled as on their passport or Civil ID.

**Example.** `Ahmed`

**Why it matters.** Together with the other English name parts it builds the English full name used on lists, in search, in self-service and on reports. Employees can ask for a correction through self-service (*Update my personal details*); it changes here once approved.

**Rules.** Required. Maximum 128 characters.

## SecondNameEn {#Employee.SecondNameEn}
<!-- alias: loc:SecondNameEn, dom:Employee_SecondNameEn -->

**What it is.** The second part of the English name. In Arabic naming this is usually the father's first name.

**Example.** `Khaled`

**Why it matters.** Part of the English full name. Leave it blank if the employee's documents have no second name -- the full name simply skips empty parts.

**Rules.** Optional. Maximum 128 characters.

## ThirdNameEn {#Employee.ThirdNameEn}
<!-- alias: loc:ThirdNameEn, dom:Employee_ThirdNameEn -->

**What it is.** The third part of the English name. In Arabic naming this is usually the grandfather's first name.

**Example.** `Saleh`

**Why it matters.** Part of the English full name. Leave it blank if the employee's documents have no third name.

**Rules.** Optional. Maximum 128 characters.

## LastNameEn {#Employee.LastNameEn}
<!-- alias: loc:LastNameEn, dom:Employee_LastNameEn -->

**What it is.** The family or tribal name in English, as on the passport or Civil ID.

**Example.** `Al-Mutairi`

**Why it matters.** Ends the English full name. Employees can ask for a correction through self-service; it changes here once approved.

**Rules.** Required. Maximum 128 characters.

## FirstNameAr {#Employee.FirstNameAr}
<!-- alias: loc:FirstNameAr, dom:Employee_FirstNameAr -->

**What it is.** The employee's first name in Arabic, spelled as on the Civil ID.

**Example.** `أحمد`

**Why it matters.** Together with the other Arabic name parts it builds the Arabic full name, which the system shows wherever names appear in Arabic. Employees can ask for a correction through self-service; it changes here once approved.

**Rules.** Required. Maximum 128 characters.

## SecondNameAr {#Employee.SecondNameAr}
<!-- alias: loc:SecondNameAr, dom:Employee_SecondNameAr -->

**What it is.** The second part of the Arabic name -- usually the father's first name.

**Example.** `خالد`

**Why it matters.** Part of the Arabic full name. Leave it blank if the Civil ID has no second name.

**Rules.** Optional. Maximum 128 characters.

## ThirdNameAr {#Employee.ThirdNameAr}
<!-- alias: loc:ThirdNameAr, dom:Employee_ThirdNameAr -->

**What it is.** The third part of the Arabic name -- usually the grandfather's first name.

**Example.** `صالح`

**Why it matters.** Part of the Arabic full name. Leave it blank if the Civil ID has no third name.

**Rules.** Optional. Maximum 128 characters.

## LastNameAr {#Employee.LastNameAr}
<!-- alias: loc:LastNameAr, dom:Employee_LastNameAr -->

**What it is.** The family or tribal name in Arabic, as on the Civil ID.

**Example.** `المطيري`

**Why it matters.** Ends the Arabic full name. Employees can ask for a correction through self-service; it changes here once approved.

**Rules.** Required. Maximum 128 characters.

## FatherNameEn {#Employee.FatherNameEn}
<!-- alias: loc:FatherNameEn, dom:Employee_FatherNameEn -->

**What it is.** The father's full name in English, as it appears on the employee's documents.

**Example.** `Khaled Saleh Al-Mutairi`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a correction through self-service.

**Rules.** Optional. Maximum 128 characters.

## FatherNameAr {#Employee.FatherNameAr}
<!-- alias: loc:FatherNameAr, dom:Employee_FatherNameAr -->

**What it is.** The father's full name in Arabic.

**Example.** `خالد صالح المطيري`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a correction through self-service.

**Rules.** Optional. Maximum 128 characters.

## MotherNameEn {#Employee.MotherNameEn}
<!-- alias: loc:MotherNameEn, dom:Employee_MotherNameEn -->

**What it is.** The mother's full name in English, as it appears on the employee's documents.

**Example.** `Fatima Ali Al-Enezi`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a correction through self-service.

**Rules.** Optional. Maximum 128 characters.

## MotherNameAr {#Employee.MotherNameAr}
<!-- alias: loc:MotherNameAr, dom:Employee_MotherNameAr -->

**What it is.** The mother's full name in Arabic.

**Example.** `فاطمة علي العنزي`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a correction through self-service.

**Rules.** Optional. Maximum 128 characters.

## Photo {#Employee.Photo}
<!-- alias: loc:Photo, dom:Employee_Photo -->

**What it is.** A photograph of the employee. Choose an image file; it is stored with the record when you save the form.

**Example.** A recent head-and-shoulders photo on a plain background, as for a passport.

**Why it matters.** Shown in self-service, on organisation charts, in search results and on the late/early attendance report with photos, and printed on the work-visa request form.

:::admin
Employees cannot change their photo through self-service -- by design it is managed by HR on this form only. The form does not limit the file size, and the photo is stored in the database with the employee, so resize large camera images before uploading.
:::

**Rules.** Optional.

## FlatNumber {#Employee.FlatNumn}
<!-- alias: loc:FlatNumber, dom:Employee_FlatNumn -->

**What it is.** The flat (apartment) number within the building, as in a Kuwait address.

**Example.** `3`

**Why it matters.** Printed on the work-visa request form. Letters are allowed, for flats such as `3A`. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 32 characters.

## FloorNumber {#Employee.FloorNum}
<!-- alias: loc:FloorNumber, dom:Employee_FloorNum -->

**What it is.** The floor the flat is on.

**Example.** `2` (or `G` for ground)

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 32 characters.

## BuildingNumber {#Employee.BuildingNum}
<!-- alias: loc:BuildingNumber, dom:Employee_BuildingNum -->

**What it is.** The building (house) number on the street.

**Example.** `7`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 32 characters.

## StreetNumber {#Employee.StreetNum}
<!-- alias: loc:StreetNumber, dom:Employee_StreetNum -->

**What it is.** The street number or name within the block.

**Example.** `12`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 32 characters.

## BlockNumber {#Employee.BlockNum}
<!-- alias: loc:BlockNumber, dom:Employee_BlockNum -->

**What it is.** The block number of the area, as in a Kuwait address.

**Example.** `4`

**Why it matters.** Printed on the work-visa request form. Employees can ask for a change through self-service.

**Rules.** Optional. Maximum 32 characters.

## LaneNumber {#Employee.LaneNumber}
<!-- alias: loc:LaneNumber, dom:Employee_LaneNumber -->

**What it is.** The lane (*jadda*) number, where the address has one.

**Example.** `5`

**Why it matters.** Completes the structured address. Unlike the other address parts, it is not on the self-service change request, so only HR can update it.

**Rules.** Optional. Maximum 32 characters.

## PermanentTelephone {#Employee.PermanentTelephone}
<!-- alias: loc:PermanentTelephone, dom:Employee_PermanentTelephone -->

**What it is.** A telephone number at the employee's permanent address -- usually the home-country address -- entered as digits without spaces or symbols, including the country code.

**Example.** `20212345678`

**Why it matters.** A way to reach the family at the permanent address when the employee cannot be reached in Kuwait. Only HR can update it; it is not on the self-service change request.

**Rules.** Optional. Whole number.

## LoginID {#Employee.LoginID}
<!-- alias: loc:LoginID, dom:Employee_LoginID -->

**What it is.** The user name this employee signs in to HCMv3 with. It must match their user account's user name exactly.

**Example.** `ahmed.almutairi`

**Why it matters.** Connects the person who signs in to this employee record, so self-service shows them their own records and requests. The administration tools for bulk passwords and workflow also find people by it.

**Rules.** Optional. Maximum 128 characters.

:::admin
A Login ID can belong to only one employee -- including employees who have left, because an old record keeping the same Login ID would make it unclear who is signing in. To reuse a leaver's Login ID, clear it on their record first; the error message names that employee.

Records linked directly to a user account do not depend on this field, but for any record that is not, Login ID is how the system finds the employee. If a user account is renamed, update the Login ID to match, or that employee will stop finding their own record in self-service.
:::

## AccessID {#Employee.AccessID}
<!-- alias: loc:AccessID, dom:Employee_AccessID -->

**What it is.** The employee's PIN or enrolment number on the attendance and access-control devices.

**Example.** `10457`

**Why it matters.** Every punch imported from the devices is matched to an employee by this number. If it is blank or does not match the device, the employee's punches are not recorded against them and their attendance will show gaps.

**Rules.** Optional. Maximum 128 characters.

:::admin
No two *active* employees can share an Access ID, so a leaver's number can be given to someone new. Clear it on the leaver's record first all the same: the attendance import matches punches by this number without checking whether the employee is still active, so a punch could be recorded against the leaver instead. The attendance resync tools only include employees who have an Access ID.
:::
