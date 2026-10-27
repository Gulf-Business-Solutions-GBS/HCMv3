---
helpId: gmi.nationality.edit
lang: en
title: Nationalities — Create / Edit
entity: HCMv3.GeneralMasterInfo.Nationality
surfaces:
  - angular/src/app/admin/general-master-info/nationalities/create-or-edit-nationality-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.Nationalities.Create]
---

## Overview

The nationalities used on employee records. An employee's nationality decides which social security (PIFSS) rules apply in payroll and how they count in the workforce quota.

## NationalityGroup {#Nationality.NationalityGroupId}
<!-- alias: loc:NationalityGroup, dom:Nationality_NationalityGroupId -->

**What it is.** The group this nationality belongs to.

**Why it matters.** For the workforce quota under Government Relations, nationalities whose group is named Citizen count as citizens; all others count as non-citizens.

**Rules.** Optional. Chosen from Nationality Group master data.

:::admin
Groups are maintained under General Master Info → Nationality Groups.
:::
