---
helpId: rec.cvlanguage.edit
lang: en
title: CV Languages — Create / Edit
entity: HCMv3.Recruitment.CVLanguage
surfaces:
  - angular/src/app/admin/recruitment/applicant-detail/cv/languages/create-or-edit-cv-language-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.CVLanguages.Create]
---

## Overview

A language on a CV.

## LanguageInfo {#CVLanguage.LanguageInfoId}
<!-- alias: loc:LanguageInfo, dom:CVLanguage_LanguageInfoId -->

**What it is.** The language.

**Example.** English

**Rules.** Optional. Chosen from Language master data.

## Proficiency {#CVLanguage.Proficiency}
<!-- alias: loc:Proficiency, dom:CVLanguage_Proficiency -->

**What it is.** How well the candidate knows it.

**Example.** Advanced

**Rules.** Required. One of: Novice, Intermediate, Advanced.
