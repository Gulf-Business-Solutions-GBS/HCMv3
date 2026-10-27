---
helpId: rpt.report-detail.edit
lang: ar
title: تفاصيل التقارير — إنشاء / تحرير
entity: HCMv3.ReportManagement.ReportDetail
surfaces:
  - angular/src/app/admin/report-management/report-details/create-or-edit-report-detail-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.ReportDetails.Create]
---

## Overview

مستوى واحد من تقرير تفصيلي متدرج. جزء من مصمم التقارير المنقول من النظام القديم. لا يُقرأ حالياً: لا يوجد في HCMv3 ما يشغّل هذه التعريفات، فلا أثر للسجلات هنا.

## ReportMaster {#ReportDetail.ReportMasterId}
<!-- alias: loc:ReportMaster, dom:ReportDetail_ReportMasterId -->

**ما هو.** التقرير التفصيلي المتدرج.

**القواعد.** إلزامي. يُختار من قائمة التقارير الرئيسية.

## ReportSetting {#ReportDetail.ReportSettingId}
<!-- alias: loc:ReportSetting, dom:ReportDetail_ReportSettingId -->

**ما هو.** تخطيط الأعمدة لهذا المستوى.

**القواعد.** اختياري. يُختار من قائمة إعدادات التقارير.

## DrillDownLevel {#ReportDetail.DrillDownLevel}
<!-- alias: loc:DrillDownLevel, dom:ReportDetail_DrillDownLevel -->

**ما هو.** رقم المستوى، بدءاً من 1.

**القواعد.** إلزامي. رقم صحيح.
