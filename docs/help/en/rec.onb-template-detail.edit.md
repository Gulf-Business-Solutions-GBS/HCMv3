---
helpId: rec.onb-template-detail.edit
lang: en
title: Onb Template Detail
entity: HCMv3.Recruitment.OnbTemplateDetails
surfaces:
  - angular/src/app/admin/recruitment/onb-template-details/create-or-edit-onb-template-detail-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.OnbTasks.Create]
---

## Overview

The content of an onboarding template.

## OnbTemplates {#OnbTemplateDetails.OnbTemplateId}
<!-- alias: loc:OnbTemplates, dom:OnbTemplateDetail_OnbTemplateId -->

**What it is.** The template.

**Rules.** Required. Chosen from Onboarding Template master data.

## TemplateId {#OnbTemplateDetails.TemplateId}
<!-- alias: loc:TemplateId, dom:OnbTemplateDetail_TemplateId -->

**What it is.** The template identifier.

**Rules.** Required. Chosen from a list.

## TemplateData {#OnbTemplateDetails.TemplateData}
<!-- alias: loc:TemplateData, dom:OnbTemplateDetail_TemplateData -->

**What it is.** The template's content.

**Rules.** Optional. Maximum 5000 characters.
