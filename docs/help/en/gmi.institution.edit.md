---
helpId: gmi.institution.edit
lang: en
title: Institutions — Create / Edit
entity: HCMv3.GeneralMasterInfo.Institution
surfaces:
  - angular/src/app/admin/general-master-info/institutions/create-or-edit-institution-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.Institutions.Create]
---

## Overview

Educational and training institutions, used on employees' education and certification records.

## University {#Institution.UniversityId}
<!-- alias: loc:University, dom:Institution_UniversityId -->

**What it is.** The university the institution belongs to, from the universities list.

**Why it matters.** Recorded for reference; nothing else uses it.

**Rules.** Optional. Chosen from University master data.

## Country {#Institution.CountryId}
<!-- alias: loc:Country, dom:Institution_CountryId -->

**What it is.** The country the institution is in.

**Example.** United Kingdom

**Why it matters.** Recorded for reference; nothing else uses it.

**Rules.** Optional. Chosen from Country master data.
