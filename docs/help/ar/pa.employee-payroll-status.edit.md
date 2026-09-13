---
helpId: pa.employee-payroll-status.edit
lang: ar
title: حالات رواتب الموظف — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeePayrollStatus
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/payroll-status/create-or-edit-employee-payroll-status-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeePayrollStatuses.Create]
---

## Overview

سجل حالة رواتب الموظف. لا يجوز أن يكون للموظف أكثر من سجل واحد.

ينشئ محركا الرواتب والوقت هذا السجل ويحدّثانه بنفسيهما لتتبّع المدى الذي وصلت إليه معالجة الموظف. أما الخياران في هذا النموذج فيُحفظان ولا يقرؤهما أيٌّ من المحركين حالياً.

## PersonLocked {#EmployeePayrollStatus.PersonLocked}
<!-- alias: loc:PersonLocked, dom:EmployeePayrollStatus_PersonLocked -->

**ما هو.** يحدد الموظف مقفلاً للرواتب.

**لماذا يهم.** يُحفظ فقط؛ ولا تتخطى الرواتب حالياً الموظف المقفل.

**القواعد.** نعم أو لا.

## PayrollCorrection {#EmployeePayrollStatus.PayrollCorrection}
<!-- alias: loc:PayrollCorrection, dom:EmployeePayrollStatus_PayrollCorrection -->

**ما هو.** يحدد أن الموظف يحتاج إلى تصحيح في الرواتب.

**لماذا يهم.** يُحفظ فقط؛ ولا يقرؤه أي محرك حالياً.

**القواعد.** نعم أو لا.
