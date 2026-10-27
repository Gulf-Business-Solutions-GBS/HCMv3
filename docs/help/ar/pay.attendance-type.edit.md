---
helpId: pay.attendance-type.edit
lang: ar
title: أنواع المواظبة — إنشاء / تحرير
entity: HCMv3.PayrollManagement.AttendanceType
surfaces:
  - angular/src/app/admin/payroll-management/attendance-types/create-or-edit-attendance-type-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AttendanceTypes.Create]
---

## Overview

نوع من المواظبة خارج الروتين المعتاد -- مهمة عمل أو تدريب أو عمل في الموقع -- يُسجَّل مواظبةً للموظف. ويحدد ما إذا كانت تلك الأيام تُحتسب وقتاً مدفوعاً، وما إذا كانت تشمل عطلات نهاية الأسبوع والعطلات، وما إذا كانت تُكسب إجازة.

## WageType {#AttendanceType.WageTypeId}
<!-- alias: loc:WageType, dom:AttendanceType_WageTypeId -->

**ما هو.** نوع الأجر الذي تُصرف تحته المواظبة من هذا النوع.

**مثال.** بدل مهمة عمل

**لماذا يهم.** الموظف الذي لديه مواظبة من هذا النوع في فترة الدفع يُحتسب له نوع الأجر هذا أجراً للحضور.

**القواعد.** اختياري. يُختار من قائمة.

## AccrualPeriod {#AttendanceType.AccrualPeriod}
<!-- alias: loc:AccrualPeriod, dom:AttendanceType_AccrualPeriod -->

**ما هو.** فترة استحقاق لنوع المواظبة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** إلزامي. إحدى القيم: بلا تراكم، السنة الميلادية، السنة المالية، سنة العمل، نصف سنوي، لكل فترة توظيف، مخصص.

## LinkLeaveQuota {#AttendanceType.LinkLeaveQuotaId}
<!-- alias: loc:LinkLeaveQuota, dom:AttendanceType_LinkLeaveQuotaId -->

**ما هو.** الإجازة التي تُضاف إليها أيام هذه المواظبة.

**مثال.** الإجازة التعويضية

**لماذا يهم.** يكون رصيد نوع الإجازة ذاك هو أيام المواظبة المسجلة على الأنواع المرتبطة به، ناقص ما أُخذ منه.

**القواعد.** اختياري. يُختار من قائمة أنواع الإجازات.

## IncludePublicHoliday {#AttendanceType.IncludePublicHoliday}
<!-- alias: loc:IncludePublicHoliday, dom:AttendanceType_IncludePublicHoliday -->

**ما هو.** ما إذا كانت المواظبة في العطلة الرسمية تُحتسب.

**لماذا يهم.** إذا أُشِّر عليه يُحتسب اليوم مواظبةً ولا يعود يُعامل عطلةً في سجل الوقت. وإلا فلا تضيف المواظبة من هذا النوع في العطلة الرسمية أي وقت.

**القواعد.** نعم أو لا.

## HaveQuotaAccrual {#AttendanceType.HaveQuotaAccrual}
<!-- alias: loc:HaveQuotaAccrual, dom:AttendanceType_HaveQuotaAccrual -->

**ما هو.** ما إذا كانت هذه المواظبة تُكسب إجازة.

**لماذا يهم.** إذا أُشِّر عليه يجب ملء **ربط حصة الإجازة**. وتُكتسب الإجازة عبر الربط نفسه: فكل نوع مواظبة له إجازة مرتبطة يضيف إليها.

**القواعد.** نعم أو لا.
