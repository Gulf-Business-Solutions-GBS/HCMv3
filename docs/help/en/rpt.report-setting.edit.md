---
helpId: rpt.report-setting.edit
lang: en
title: Report Settings — Create / Edit
entity: HCMv3.ReportManagement.ReportSetting
surfaces:
  - angular/src/app/admin/report-management/report-settings/create-or-edit-report-setting-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.ReportSettings.Create]
---

## Overview

A saved column layout for a report level. Part of the report designer carried over from the old system. Not read at present: nothing in HCMv3 runs these definitions, so records here have no effect.

## ReportId {#ReportSetting.ReportId}
<!-- alias: loc:ReportId, dom:ReportSetting_ReportId -->

**What it is.** The report.

**Rules.** Required. Chosen from a list.

## Level {#ReportSetting.Level}
<!-- alias: loc:Level, dom:ReportSetting_Level -->

**What it is.** The level it is for.

**Rules.** Required. Whole number.
