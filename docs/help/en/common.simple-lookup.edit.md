---
helpId: common.simple-lookup.edit
lang: en
title: Simple Lists — Create / Edit
entity: HCMv3.GeneralMasterInfo.SimpleLookupEntity
surfaces:
  - angular/src/app/admin/general-master-info/shared/simple-lookup-modal.component.html
audience: [admin]
permissions: []
---

## Overview

The shared form for HCMv3's simple lists -- lists whose entries have only a code, an English name and an Arabic name, such as titles, countries, religions, marital statuses and job titles. More than ninety lists across every module use it, so the fields and rules on this form are the same whichever list you are editing.

Left blank, the code is filled with the next number when the entry is saved. The code, the English name and the Arabic name must each be unique within the list.
