---
helpId: rpt.report-info.edit
lang: ar
title: بيانات التقارير — إنشاء / تحرير
entity: HCMv3.ReportManagement.ReportInfo
surfaces:
  - angular/src/app/admin/report-management/report-infos/create-or-edit-report-info-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.ReportInfos.Create]
---

## Overview

تقرير مصمم. جزء من مصمم التقارير المنقول من النظام القديم. لا يُقرأ حالياً: لا يوجد في HCMv3 ما يشغّل هذه التعريفات، فلا أثر للسجلات هنا.

## ReportName {#ReportInfo.ReportName}
<!-- alias: loc:ReportName, dom:ReportInfo_ReportName -->

**ما هو.** اسم التقرير.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## ReportType {#ReportInfo.ReportTypeId}
<!-- alias: loc:ReportType, dom:ReportInfo_ReportTypeId -->

**ما هو.** نوع التقرير.

**القواعد.** إلزامي. يُختار من قائمة أنواع التقارير.

## DomainType {#ReportInfo.DomainTypeId}
<!-- alias: loc:DomainType, dom:ReportInfo_DomainTypeId -->

**ما هو.** مجال البيانات.

**القواعد.** إلزامي. يُختار من قائمة أنواع النطاقات.

## Description {#ReportInfo.Description}
<!-- alias: loc:Description, dom:ReportInfo_Description -->

**ما هو.** وصف.

**القواعد.** اختياري. الحد الأقصى 512 حرفاً.

## ReportQuery {#ReportInfo.ReportQuery}
<!-- alias: loc:ReportQuery, dom:ReportInfo_ReportQuery -->

**ما هو.** الاستعلام الذي يقوم عليه التقرير.

**القواعد.** اختياري. الحد الأقصى 1500 حرف.

## IsActive {#ReportInfo.IsActive}
<!-- alias: loc:IsActive, dom:ReportInfo_IsActive -->

**ما هو.** ما إذا كان التقرير مستخدماً.

**القواعد.** نعم أو لا.
