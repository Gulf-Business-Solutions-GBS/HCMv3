---
helpId: perf.form-type.edit
lang: en
title: Form Types — Create / Edit
entity: HCMv3.PerformanceManagement.FormType
surfaces:
  - angular/src/app/admin/performance-management/form-types/create-or-edit-form-type-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.FormTypes.Create]
---

## Overview

A kind of appraisal form, with limits on how many reviewers an appraisal of this type may have. Every appraisal names its form type.

## MinReviewerLimit {#FormType.MinReviewerLimit}
<!-- alias: loc:MinReviewerLimit, dom:FormType_MinReviewerLimit -->

**What it is.** The fewest reviewers an appraisal should have.

**Why it matters.** Not read at present.

**Rules.** Required. Whole number.

## MaxReviewerLimit {#FormType.MaxReviewerLimit}
<!-- alias: loc:MaxReviewerLimit, dom:FormType_MaxReviewerLimit -->

**What it is.** The most reviewers an appraisal of this type may have.

**Example.** `5`

**Why it matters.** Adding a reviewer beyond it is refused. 0 means no limit.

**Rules.** Required. Whole number.
