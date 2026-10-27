---
helpId: rpt.print-report.edit
lang: ar
title: طباعة التقارير — إنشاء / تحرير
entity: HCMv3.ReportManagement.PrintReport
surfaces:
  - angular/src/app/admin/report-management/print-reports/create-or-edit-print-report-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.PrintReports.Create]
---

## Overview

تقرير واحد في قائمة طباعة التقارير. تُعطى كل شركة القائمة القياسية عند إنشائها، والتقرير الذي تعيد تسميته أو توقفه أو تحذفه يبقى كما ضبطته. ولا يمكن تشغيل التقرير إلا إذا كان مبنياً في المنتج -- وإلا فإنه يظهر في القائمة دون إمكانية تشغيله.

## ReportName {#PrintReport.ReportName}
<!-- alias: loc:ReportName, dom:PrintReport_ReportName -->

**ما هو.** اسم التقرير المبني الذي يشغّله هذا القيد.

**لماذا يهم.** يجب أن يطابق تقريراً مبنياً تماماً، وإلا لا يمكن تشغيل القيد. لا تغيّره إلا لتوجيهه إلى تقرير مبني آخر.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## ReportCategory {#PrintReport.ReportCategory}
<!-- alias: loc:ReportCategory, dom:PrintReport_ReportCategory -->

**ما هو.** ما إذا كان هذا تقريراً قياسياً أم مخصصاً.

**لماذا يهم.** يظهر كعمود في قائمة طباعة التقارير، ولا يغيّر شيئاً آخر.

**القواعد.** إلزامي. إحدى القيم: افتراضي، مخصص.

## CustomPercentage {#PrintReport.CustomPercentage}
<!-- alias: loc:CustomPercentage, dom:PrintReport_CustomPercentage -->

**ما هو.** مدى تخصيص التقرير، كنسبة مئوية.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم.

## ReportCreatedOn {#PrintReport.ReportCreatedOn}
<!-- alias: loc:ReportCreatedOn, dom:PrintReport_ReportCreatedOn -->

**ما هو.** متى أُنشئ التقرير.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. تاريخ.

## IsActive {#PrintReport.IsActive}
<!-- alias: loc:IsActive, dom:PrintReport_IsActive -->

**ما هو.** ما إذا كان التقرير يظهر في القائمة.

**لماذا يهم.** التقارير غير المحددة لا تظهر في قائمة طباعة التقارير لأي أحد.

**القواعد.** نعم أو لا.
