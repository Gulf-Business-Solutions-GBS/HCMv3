---
helpId: cd.competency.edit
lang: en
title: Competencies — Create / Edit
entity: HCMv3.CompetencyDevelopment.Competency
surfaces:
  - angular/src/app/admin/competency-development/competencies/create-or-edit-competency-modal.component.html
audience: [admin]
permissions: [Pages.CompetencyDevelopment.Competencies.Create]
---

## Overview

A skill or behaviour employees are assessed against.

## CompetencyType {#Competency.CompetencyTypeId}
<!-- alias: loc:CompetencyType, dom:Competency_CompetencyTypeId -->

**What it is.** The group the competency belongs to.

**Example.** Core

**Rules.** Required. Chosen from Competency Type master data.

## Active {#Competency.Active}
<!-- alias: loc:Active, dom:Competency_Active -->

**What it is.** Whether the competency is in use.

**Why it matters.** Not read at present: inactive competencies can still be chosen.

**Rules.** Yes or no.
