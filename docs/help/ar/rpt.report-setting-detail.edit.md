---
helpId: rpt.report-setting-detail.edit
lang: ar
title: تفاصيل إعدادات التقارير — إنشاء / تحرير
entity: HCMv3.ReportManagement.ReportSettingDetail
surfaces:
  - angular/src/app/admin/report-management/report-setting-details/create-or-edit-report-setting-detail-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.ReportSettingDetails.Create]
---

## Overview

عمود واحد من تخطيط محفوظ. جزء من مصمم التقارير المنقول من النظام القديم. لا يُقرأ حالياً: لا يوجد في HCMv3 ما يشغّل هذه التعريفات، فلا أثر للسجلات هنا.

## ReportSetting {#ReportSettingDetail.ReportSettingId}
<!-- alias: loc:ReportSetting, dom:ReportSettingDetail_ReportSettingId -->

**ما هو.** التخطيط.

**القواعد.** إلزامي. يُختار من قائمة إعدادات التقارير.

## FieldName {#ReportSettingDetail.FieldName}
<!-- alias: loc:FieldName, dom:ReportSettingDetail_FieldName -->

**ما هو.** الحقل المعروض.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## CaptionEn {#ReportSettingDetail.CaptionEn}
<!-- alias: loc:CaptionEn, dom:ReportSettingDetail_CaptionEn -->

**ما هو.** عنوان العمود بالإنجليزية.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## CaptionAr {#ReportSettingDetail.CaptionAr}
<!-- alias: loc:CaptionAr, dom:ReportSettingDetail_CaptionAr -->

**ما هو.** عنوان العمود بالعربية.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Order {#ReportSettingDetail.Order}
<!-- alias: loc:Order, dom:ReportSettingDetail_Order -->

**ما هو.** موقع العمود.

**القواعد.** إلزامي. رقم صحيح.

## ColumnDataType {#ReportSettingDetail.ColumnDataType}
<!-- alias: loc:ColumnDataType, dom:ReportSettingDetail_ColumnDataType -->

**ما هو.** نوع القيمة.

**القواعد.** إلزامي. إحدى القيم: عدد صحيح، عدد عشري، نص، تاريخ ووقت، قيمة منطقية.

## Width {#ReportSettingDetail.Width}
<!-- alias: loc:Width, dom:ReportSettingDetail_Width -->

**ما هو.** عرض العمود.

**القواعد.** اختياري. رقم.

## DataHAlignment {#ReportSettingDetail.DataHAlignment}
<!-- alias: loc:DataHAlignment, dom:ReportSettingDetail_DataHAlignment -->

**ما هو.** محاذاة القيم.

**القواعد.** إلزامي. إحدى القيم: يسار، وسط، يمين.

## DisplayFormat {#ReportSettingDetail.DisplayFormat}
<!-- alias: loc:DisplayFormat, dom:ReportSettingDetail_DisplayFormat -->

**ما هو.** تنسيق القيم.

**القواعد.** اختياري. الحد الأقصى 50 حرفاً.

## VisibleInEn {#ReportSettingDetail.VisibleInEn}
<!-- alias: loc:VisibleInEn, dom:ReportSettingDetail_VisibleInEn -->

**ما هو.** ما إذا كان العمود يظهر بالإنجليزية.

**القواعد.** نعم أو لا.

## VisibleInAr {#ReportSettingDetail.VisibleInAr}
<!-- alias: loc:VisibleInAr, dom:ReportSettingDetail_VisibleInAr -->

**ما هو.** ما إذا كان العمود يظهر بالعربية.

**القواعد.** نعم أو لا.
