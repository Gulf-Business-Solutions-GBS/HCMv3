---
helpId: rpt.report-setting-detail.edit
lang: en
title: Report Setting Details — Create / Edit
entity: HCMv3.ReportManagement.ReportSettingDetail
surfaces:
  - angular/src/app/admin/report-management/report-setting-details/create-or-edit-report-setting-detail-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.ReportSettingDetails.Create]
---

## Overview

One column of a saved layout. Part of the report designer carried over from the old system. Not read at present: nothing in HCMv3 runs these definitions, so records here have no effect.

## ReportSetting {#ReportSettingDetail.ReportSettingId}
<!-- alias: loc:ReportSetting, dom:ReportSettingDetail_ReportSettingId -->

**What it is.** The layout.

**Rules.** Required. Chosen from Report Setting master data.

## FieldName {#ReportSettingDetail.FieldName}
<!-- alias: loc:FieldName, dom:ReportSettingDetail_FieldName -->

**What it is.** The field shown.

**Rules.** Required. Maximum 256 characters.

## CaptionEn {#ReportSettingDetail.CaptionEn}
<!-- alias: loc:CaptionEn, dom:ReportSettingDetail_CaptionEn -->

**What it is.** The English column heading.

**Rules.** Optional. Maximum 256 characters.

## CaptionAr {#ReportSettingDetail.CaptionAr}
<!-- alias: loc:CaptionAr, dom:ReportSettingDetail_CaptionAr -->

**What it is.** The Arabic column heading.

**Rules.** Optional. Maximum 256 characters.

## Order {#ReportSettingDetail.Order}
<!-- alias: loc:Order, dom:ReportSettingDetail_Order -->

**What it is.** The column's position.

**Rules.** Required. Whole number.

## ColumnDataType {#ReportSettingDetail.ColumnDataType}
<!-- alias: loc:ColumnDataType, dom:ReportSettingDetail_ColumnDataType -->

**What it is.** The kind of value.

**Rules.** Required. One of: Int, Double, String, Date Time, Boolean.

## Width {#ReportSettingDetail.Width}
<!-- alias: loc:Width, dom:ReportSettingDetail_Width -->

**What it is.** The column width.

**Rules.** Optional. Number.

## DataHAlignment {#ReportSettingDetail.DataHAlignment}
<!-- alias: loc:DataHAlignment, dom:ReportSettingDetail_DataHAlignment -->

**What it is.** How values line up.

**Rules.** Required. One of: Left, Center, Right.

## DisplayFormat {#ReportSettingDetail.DisplayFormat}
<!-- alias: loc:DisplayFormat, dom:ReportSettingDetail_DisplayFormat -->

**What it is.** How values are formatted.

**Rules.** Optional. Maximum 50 characters.

## VisibleInEn {#ReportSettingDetail.VisibleInEn}
<!-- alias: loc:VisibleInEn, dom:ReportSettingDetail_VisibleInEn -->

**What it is.** Whether the column shows in English.

**Rules.** Yes or no.

## VisibleInAr {#ReportSettingDetail.VisibleInAr}
<!-- alias: loc:VisibleInAr, dom:ReportSettingDetail_VisibleInAr -->

**What it is.** Whether the column shows in Arabic.

**Rules.** Yes or no.
