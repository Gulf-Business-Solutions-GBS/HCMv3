---
helpId: rec.interview-details.edit
lang: en
title: Interview Details — Create / Edit
entity: HCMv3.Recruitment.InterviewDetails
surfaces:
  - angular/src/app/admin/recruitment/interview-details/create-or-edit-interview-detail-modal.component.html
audience: [admin]
permissions: []
---

## Overview

An interview for an application: when, where, who interviewed and what they said. Stored with the application; nothing reads it at present.

## ApplicationStatuses {#InterviewDetails.ApplicationStatusId}
<!-- alias: loc:ApplicationStatuses, dom:InterviewDetails_ApplicationStatusId -->

**What it is.** The application.

**Rules.** Required. Chosen from Application Status master data.

## InterviewDate {#InterviewDetails.InterviewDate}
<!-- alias: loc:InterviewDate, dom:InterviewDetails_InterviewDate -->

**What it is.** The date.

**Example.** `2026-04-20`

**Rules.** Optional. Date value.

## InterviewTime {#InterviewDetails.InterviewTime}
<!-- alias: loc:InterviewTime, dom:InterviewDetails_InterviewTime -->

**What it is.** The time.

**Example.** `10:00`

**Rules.** Optional. Date value.

## Place {#InterviewDetails.Place}
<!-- alias: loc:Place, dom:InterviewDetails_Place -->

**What it is.** Where it is held.

**Example.** Head Office, 3rd floor

**Rules.** Optional. Maximum 256 characters.

## Interviewer1 {#InterviewDetails.Interviewer1Id}
<!-- alias: loc:Interviewer1, dom:InterviewDetails_Interviewer1Id -->

**What it is.** The first interviewer.

**Rules.** Required. Whole number.

## Interviewer2 {#InterviewDetails.Interviewer2Id}
<!-- alias: loc:Interviewer2, dom:InterviewDetails_Interviewer2Id -->

**What it is.** The second interviewer.

**Rules.** Optional. Whole number.

## Interviewer3 {#InterviewDetails.Interviewer3Id}
<!-- alias: loc:Interviewer3, dom:InterviewDetails_Interviewer3Id -->

**What it is.** The third interviewer.

**Rules.** Optional. Whole number.

## Comments1 {#InterviewDetails.Comments1}
<!-- alias: loc:Comments1, dom:InterviewDetails_Comments1 -->

**What it is.** The first interviewer's comments.

**Rules.** Optional.

## Comments2 {#InterviewDetails.Comments2}
<!-- alias: loc:Comments2, dom:InterviewDetails_Comments2 -->

**What it is.** The second interviewer's comments.

**Rules.** Optional.

## Comments3 {#InterviewDetails.Comments3}
<!-- alias: loc:Comments3, dom:InterviewDetails_Comments3 -->

**What it is.** The third interviewer's comments.

**Rules.** Optional.
