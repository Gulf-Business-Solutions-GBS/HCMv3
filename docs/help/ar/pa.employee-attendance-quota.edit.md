---
helpId: pa.employee-attendance-quota.edit
lang: ar
title: حصص حضور الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeAttendanceQuota
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/attendance-quotas/create-or-edit-employee-attendance-quota-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAttendanceQuotas.Create]
---

## Overview

استحقاق حضور للموظف خلال فترة.

يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب حالياً.

## EntitlementFrom {#EmployeeAttendanceQuota.EntitlementFrom}
<!-- alias: loc:EntitlementFrom, dom:EmployeeAttendanceQuota_EntitlementFrom -->

**ما هو.** أول يوم في فترة الاستحقاق.

**مثال.** 1 يناير 2026

**القواعد.** إلزامي. تاريخ.

## EntitlementTo {#EmployeeAttendanceQuota.EntitlementTo}
<!-- alias: loc:EntitlementTo, dom:EmployeeAttendanceQuota_EntitlementTo -->

**ما هو.** آخر يوم في فترة الاستحقاق.

**مثال.** 31 ديسمبر 2026

**القواعد.** إلزامي. تاريخ.

## DeductionFrom {#EmployeeAttendanceQuota.DeductionFrom}
<!-- alias: loc:DeductionFrom, dom:EmployeeAttendanceQuota_DeductionFrom -->

**ما هو.** أول يوم يمكن فيه استخدام الاستحقاق.

**مثال.** 1 يناير 2026

**القواعد.** إلزامي. تاريخ.

## DeductionTo {#EmployeeAttendanceQuota.DeductionTo}
<!-- alias: loc:DeductionTo, dom:EmployeeAttendanceQuota_DeductionTo -->

**ما هو.** آخر يوم يمكن فيه استخدام الاستحقاق.

**مثال.** 31 ديسمبر 2026

**القواعد.** إلزامي. تاريخ.

## QuotaAccrued {#EmployeeAttendanceQuota.QuotaAccrued}
<!-- alias: loc:QuotaAccrued, dom:EmployeeAttendanceQuota_QuotaAccrued -->

**ما هو.** المقدار المكتسب في الفترة.

**مثال.** 10

**القواعد.** اختياري. رقم.

## QuotaUsed {#EmployeeAttendanceQuota.QuotaUsed}
<!-- alias: loc:QuotaUsed, dom:EmployeeAttendanceQuota_QuotaUsed -->

**ما هو.** المقدار المستخدم في الفترة.

**مثال.** 4

**القواعد.** اختياري. رقم.
