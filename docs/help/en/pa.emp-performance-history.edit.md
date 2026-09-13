---
helpId: pa.emp-performance-history.edit
lang: en
title: Employee Performance Histories — Create / Edit
entity: HCMv3.SuccessionManagement.EmpPerformanceHistory
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/performance-histories/create-or-edit-employee-performance-history-modal.component.html
audience: [admin]
permissions: [Pages.SuccessionManagement.EmpPerformanceHistories.Create]
---

## Overview

A performance score for the employee over a period, for succession planning. Periods for the same employee cannot overlap.

It is recorded for reference; no calculation or report reads it at present.

## Description {#EmpPerformanceHistory.Description}
<!-- alias: loc:Description, dom:EmpPerformanceHistory_Description -->

**What it is.** Notes on the score.

**Example.** Exceeded sales targets for the year.

**Rules.** Optional. Maximum 2000 characters.

## Score {#EmpPerformanceHistory.Score}
<!-- alias: loc:Score, dom:EmpPerformanceHistory_Score -->

**What it is.** The performance score.

**Example.** 4.2

**Rules.** Required. Number.

## Source {#EmpPerformanceHistory.Source}
<!-- alias: loc:Source, dom:EmpPerformanceHistory_Source -->

**What it is.** Whether the score came from a live appraisal or was entered by hand.

**Example.** Manual

**Rules.** Required. One of: Live, Manual.

## RatingId {#EmpPerformanceHistory.RatingId}
<!-- alias: loc:RatingId, dom:EmpPerformanceHistory_RatingId -->

**What it is.** The succession rating.

**Example.** High Performer

**Rules.** Required. Chosen from Succession Rating master data.

:::admin
Maintained under Succession Management.
:::

## NineBoxRatingId {#EmpPerformanceHistory.NineBoxRatingId}
<!-- alias: loc:NineBoxRatingId, dom:EmpPerformanceHistory_NineBoxRatingId -->

**What it is.** The nine-box rating.

**Example.** Star

**Rules.** Required. Chosen from Nine Box Rating master data.

:::admin
Maintained under Succession Management.
:::
