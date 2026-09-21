---
helpId: sys.security-settings.edit
lang: ar
title: Security settings
entity: HCMv3.SystemSettings.ManageSecuritySettings
surfaces:
  - angular/src/app/admin/system-settings/manage-security-settings/create-or-edit-manage-security-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageSecuritySettings]
---

## Overview

إعدادات الأمان والتدقيق.

لكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## EnableRowLevelSecurity {#ManageSecuritySettings.EnableRowLevelSecurity}
<!-- alias: loc:EnableRowLevelSecurity, dom:SecuritySettings_EnableRowLevelSecurity -->

**ما هو.** ما إذا كان أمان مستوى السجلات مطبقاً.

**لماذا يهم.** إذا أُشِّر عليه تقصر المواضع التي تطبّق أمان مستوى السجلات المستخدمين على الموظفين الذين تسمح بهم تعييناتهم الأمنية، ويُقيَّد مستلمو التقارير المجدولة بالطريقة نفسها. وإلا فلا يُطبَّق أي قيد على مستوى السجلات.

**القواعد.** نعم أو لا.

## EnableReportExportPwd {#ManageSecuritySettings.EnableReportExportPwd}
<!-- alias: loc:EnableReportExportPwd, dom:SecuritySettings_EnableReportExportPwd -->

**ما هو.** ما إذا كانت التقارير المصدَّرة محمية بكلمة مرور.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableAuditLogInTimeEngine {#ManageSecuritySettings.EnableAuditLogInTimeEngine}
<!-- alias: loc:EnableAuditLogInTimeEngine, dom:SecuritySettings_EnableAuditLogInTimeEngine -->

**ما هو.** ما إذا كانت تغييرات محرك الوقت تُكتب في سجل التدقيق.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableAuditLogInPayrollEngine {#ManageSecuritySettings.EnableAuditLogInPayrollEngine}
<!-- alias: loc:EnableAuditLogInPayrollEngine, dom:SecuritySettings_EnableAuditLogInPayrollEngine -->

**ما هو.** ما إذا كانت تغييرات محرك الرواتب تُكتب في سجل التدقيق.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableAuditLogInProjectCostEngine {#ManageSecuritySettings.EnableAuditLogInProjectCostEngine}
<!-- alias: loc:EnableAuditLogInProjectCostEngine, dom:SecuritySettings_EnableAuditLogInProjectCostEngine -->

**ما هو.** ما إذا كانت تغييرات محرك تكاليف المشاريع تُكتب في سجل التدقيق.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableAuditLogInCostEngine {#ManageSecuritySettings.EnableAuditLogInCostEngine}
<!-- alias: loc:EnableAuditLogInCostEngine, dom:SecuritySettings_EnableAuditLogInCostEngine -->

**ما هو.** ما إذا كانت تغييرات محرك التكلفة تُكتب في سجل التدقيق.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableAuditLogInGridReport {#ManageSecuritySettings.EnableAuditLogInGridReport}
<!-- alias: loc:EnableAuditLogInGridReport, dom:SecuritySettings_EnableAuditLogInGridReport -->

**ما هو.** ما إذا كان تشغيل التقرير يُسجَّل.

**لماذا يهم.** إذا أُشِّر عليه يكتب كل تشغيل للتقرير قيداً في سجل التنفيذ. وإلا فلا تُسجَّل تشغيلات التقارير.

**القواعد.** نعم أو لا.

## EnableAuditLogInPrintReport {#ManageSecuritySettings.EnableAuditLogInPrintReport}
<!-- alias: loc:EnableAuditLogInPrintReport, dom:SecuritySettings_EnableAuditLogInPrintReport -->

**ما هو.** ما إذا كانت طباعة التقرير تُسجَّل.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.
