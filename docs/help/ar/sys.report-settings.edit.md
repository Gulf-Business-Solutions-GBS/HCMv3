---
helpId: sys.report-settings.edit
lang: ar
title: Report Settings
entity: HCMv3.SystemSettings.ManageReportSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-report-settings/create-or-edit-manage-report-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageReportSettings]
---

## Overview

إعدادات التقارير وكشوف الرواتب.

لا يُقرأ حالياً إلا إعدادا تقرير كشف الرواتب، أما البقية فلا أثر لها.

لكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## ShowSubordinatesPayslip {#ManageReportSettings.ShowSubordinatesPayslip}
<!-- alias: loc:ShowSubordinatesPayslip, dom:ReportSettings_ShowSubordinatesPayslip -->

**ما هو.** ما إذا كان المديرون يستطيعون رؤية كشوف رواتب فرقهم.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## ShowPayslipOnDay {#ManageReportSettings.ShowPayslipOnDay}
<!-- alias: loc:ShowPayslipOnDay, dom:ReportSettings_ShowPayslipOnDay -->

**ما هو.** يوم الشهر الذي تصبح فيه كشوف الرواتب مرئية.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## ShowPayslipFromDate {#ManageReportSettings.ShowPayslipFromDate}
<!-- alias: loc:ShowPayslipFromDate, dom:ReportSettings_ShowPayslipFromDate -->

**ما هو.** أقدم كشف راتب يستطيع الموظفون رؤيته.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. تاريخ.

## ExportGridReportWithHeader {#ManageReportSettings.ExportGridReportWithHeader}
<!-- alias: loc:ExportGridReportWithHeader, dom:ReportSettings_ExportGridReportWithHeader -->

**ما هو.** ما إذا كانت التقارير المصدَّرة تتضمن ترويسة.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## ExportGridReportWithFooter {#ManageReportSettings.ExportGridReportWithFooter}
<!-- alias: loc:ExportGridReportWithFooter, dom:ReportSettings_ExportGridReportWithFooter -->

**ما هو.** ما إذا كانت التقارير المصدَّرة تتضمن تذييلاً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## DecimalInReports {#ManageReportSettings.DecimalInReports}
<!-- alias: loc:DecimalInReports, dom:ReportSettings_DecimalInReports -->

**ما هو.** المنازل العشرية المعروضة في التقارير.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## ShowPeriodInPayrollSheet {#ManageReportSettings.ShowPeriodInPayrollSheet}
<!-- alias: loc:ShowPeriodInPayrollSheet, dom:ReportSettings_ShowPeriodInPayrollSheet -->

**ما هو.** ما إذا كان تقرير كشف الرواتب يمكن أن يغطي نطاقاً من الأشهر.

**لماذا يهم.** إذا أُشِّر عليه يُستخدم الشهر والسنة "إلى" في التقرير، فيمكن تشغيله على عدة أشهر. وإلا فيغطي الشهر "من" فقط.

**القواعد.** نعم أو لا.

## ShowPeriodInLeaveProvision {#ManageReportSettings.ShowPeriodInLeaveProvision}
<!-- alias: loc:ShowPeriodInLeaveProvision, dom:ReportSettings_ShowPeriodInLeaveProvision -->

**ما هو.** ما إذا كان تقرير مخصص الإجازات يمكن أن يغطي نطاقاً من الأشهر.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## ShowPeriodInIndemnityProvision {#ManageReportSettings.ShowPeriodInIndemnityProvision}
<!-- alias: loc:ShowPeriodInIndemnityProvision, dom:ReportSettings_ShowPeriodInIndemnityProvision -->

**ما هو.** ما إذا كان تقرير مخصص مكافأة نهاية الخدمة يمكن أن يغطي نطاقاً من الأشهر.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## PeriodMonthLimitInPayrollSheet {#ManageReportSettings.PeriodMonthLimitInPayrollSheet}
<!-- alias: loc:PeriodMonthLimitInPayrollSheet, dom:ReportSettings_PeriodMonthLimitInPayrollSheet -->

**ما هو.** أكبر عدد من الأشهر يجوز أن يغطيه تقرير كشف الرواتب.

**مثال.** `12`

**لماذا يهم.** يُرفض النطاق الأطول. والقيمة 0 تعني بلا حد.

**القواعد.** اختياري. رقم صحيح.

## PeriodMonthLimitInLeaveProvision {#ManageReportSettings.PeriodMonthLimitInLeaveProvision}
<!-- alias: loc:PeriodMonthLimitInLeaveProvision, dom:ReportSettings_PeriodMonthLimitInLeaveProvision -->

**ما هو.** أكبر عدد من الأشهر يجوز أن يغطيه تقرير مخصص الإجازات.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## PeriodMonthLimitInIndemnityProvision {#ManageReportSettings.PeriodMonthLimitInIndemnityProvision}
<!-- alias: loc:PeriodMonthLimitInIndemnityProvision, dom:ReportSettings_PeriodMonthLimitInIndemnityProvision -->

**ما هو.** أكبر عدد من الأشهر يجوز أن يغطيه تقرير مخصص مكافأة نهاية الخدمة.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.
