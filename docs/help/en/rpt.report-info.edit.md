---
helpId: rpt.report-info.edit
lang: en
title: Report Infos — Create / Edit
entity: HCMv3.ReportManagement.ReportInfo
surfaces:
  - angular/src/app/admin/report-management/report-infos/create-or-edit-report-info-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.ReportInfos.Create]
---

## Overview

A designed report. Part of the report designer carried over from the old system. Not read at present: nothing in HCMv3 runs these definitions, so records here have no effect.

## ReportName {#ReportInfo.ReportName}
<!-- alias: loc:ReportName, dom:ReportInfo_ReportName -->

**What it is.** The report's name.

**Rules.** Required. Maximum 256 characters.

## ReportType {#ReportInfo.ReportTypeId}
<!-- alias: loc:ReportType, dom:ReportInfo_ReportTypeId -->

**What it is.** The kind of report.

**Rules.** Required. Chosen from Report Type master data.

## DomainType {#ReportInfo.DomainTypeId}
<!-- alias: loc:DomainType, dom:ReportInfo_DomainTypeId -->

**What it is.** The area of data.

**Rules.** Required. Chosen from Domain Type master data.

## Description {#ReportInfo.Description}
<!-- alias: loc:Description, dom:ReportInfo_Description -->

**What it is.** A description.

**Rules.** Optional. Maximum 512 characters.

## ReportQuery {#ReportInfo.ReportQuery}
<!-- alias: loc:ReportQuery, dom:ReportInfo_ReportQuery -->

**What it is.** The query behind the report.

**Rules.** Optional. Maximum 1500 characters.

## IsActive {#ReportInfo.IsActive}
<!-- alias: loc:IsActive, dom:ReportInfo_IsActive -->

**What it is.** Whether the report is in use.

**Rules.** Yes or no.
