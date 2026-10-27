---
helpId: perf.parating.edit
lang: en
title: PA Ratings — Create / Edit
entity: HCMv3.PerformanceManagement.PARating
surfaces:
  - angular/src/app/admin/performance-management/pa-ratings/create-or-edit-pa-rating-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PARatings.Create]
---

## Overview

A rating reviewers can give, with the score range it stands for.

Its **Code** is its value: the scorecard averages the codes of the ratings given, so codes should be numbers, with the best rating highest. The **From** and **To** range bands a final score into this rating on the scorecard and manager evaluation reports.

## FromRange {#PARating.FromRange}
<!-- alias: loc:FromRange, dom:PARating_FromRange -->

**What it is.** The lowest final score that counts as this rating.

**Example.** `70`

**Rules.** Required. Number.

## ToRange {#PARating.ToRange}
<!-- alias: loc:ToRange, dom:PARating_ToRange -->

**What it is.** The highest final score that counts as this rating.

**Example.** `85`

**Rules.** Required. Number.

## MaxLimit {#PARating.MaxLimit}
<!-- alias: loc:MaxLimit, dom:PARating_MaxLimit -->

**What it is.** The most times the rating may be given on one form.

**Why it matters.** Not read at present.

**Rules.** Required. Whole number.

## DescriptionEn {#PARating.DescriptionEn}
<!-- alias: loc:DescriptionEn, dom:PARating_DescriptionEn -->

**What it is.** A description of the rating in English.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 4000 characters.

## DescriptionAr {#PARating.DescriptionAr}
<!-- alias: loc:DescriptionAr, dom:PARating_DescriptionAr -->

**What it is.** A description of the rating in Arabic.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 4000 characters.

## IsCommentRequired {#PARating.IsCommentRequired}
<!-- alias: loc:IsCommentRequired, dom:PARating_IsCommentRequired -->

**What it is.** Whether giving this rating needs a comment.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## Status {#PARating.Status}
<!-- alias: loc:Status, dom:PARating_Status -->

**What it is.** Whether the rating is active.

**Why it matters.** Not read at present.

**Rules.** Yes or no.
