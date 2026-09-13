---
helpId: pa.employee-absence.edit
lang: ar
title: غيابات الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeAbsence
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/absences/create-or-edit-employee-absence-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAbsences.Create]
---

## Overview

فترة إجازة أو غياب للموظف. ولا يجوز أن تتداخل فترات غياب الموظف نفسه.

تدفع الرواتب أجر الإجازة وفق نوع الدفع وتاريخه، ويحدد محرك الوقت الأيام على أنها إجازة.

الغياب المُدخل في هذا النموذج لا يغيّر رقم المستخدَم في حصة إجازات الموظف، فلا ينقص رصيد الإجازات في الخدمة الذاتية. أما الإجازة المطلوبة والمعتمدة عبر الخدمة الذاتية فتحدّثه.

## AbsenceTypeId {#EmployeeAbsence.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:EmployeeAbsence_AbsenceTypeId -->

**ما هو.** نوع الإجازة.

**مثال.** إجازة سنوية

**لماذا يهم.** يحدد طريقة دفع الأيام والحصة التي تنتمي إليها.

**القواعد.** إلزامي. يُختار من قائمة أنواع الغياب.

:::admin
تُدار من إدارة الرواتب ← أنواع الغياب.
:::

## HalfdayType {#EmployeeAbsence.HalfdayType}
<!-- alias: loc:HalfdayType, dom:EmployeeAbsence_HalfdayType -->

**ما هو.** هل الإجازة نصف يوم، وأي نصف.

**مثال.** بداية الدوام

**لماذا يهم.** يظهر في تقارير الحضور الشهري مع السماحية.

**القواعد.** إلزامي. إحدى القيم: لا شيء، بداية الدوام، نهاية الدوام.

## StartTime {#EmployeeAbsence.StartTime}
<!-- alias: loc:StartTime, dom:EmployeeAbsence_StartTime -->

**ما هو.** وقت بدء الإجازة، للإجازة بالساعات.

**مثال.** 08:00

**القواعد.** اختياري. تاريخ.

:::admin
لا يعامل محركا الوقت والرواتب الغياب على أنه بالساعات إلا عند تحديد ساعات الغياب، وهو ما لا يفعله هذا النموذج. لذلك يُحتسب الغياب المحفوظ هنا أياماً كاملة حتى لو أُدخلت الأوقات.
:::

## EndTime {#EmployeeAbsence.EndTime}
<!-- alias: loc:EndTime, dom:EmployeeAbsence_EndTime -->

**ما هو.** وقت انتهاء الإجازة، للإجازة بالساعات.

**مثال.** 12:00

**القواعد.** اختياري. تاريخ.

## LeavePaymentType {#EmployeeAbsence.LeavePaymentType}
<!-- alias: loc:LeavePaymentType, dom:EmployeeAbsence_LeavePaymentType -->

**ما هو.** متى يُدفع أجر الإجازة.

**مثال.** دفع مقدم

**لماذا يهم.** الدفع المقدم يُصرف في مسيرة رواتب خارج الدورة في تاريخ الدفع. والدفع الكامل مع الرواتب يصرف أجر الإجازة الممتدة إلى أشهر لاحقة في مسيرة الرواتب التي يطابق تاريخ دفعها تاريخ الدفع.

**القواعد.** إلزامي. إحدى القيم: لا شيء، الدفع مع الرواتب، الدفع مقدماً، الدفع كاملاً مع الرواتب.

## PaymentDate {#EmployeeAbsence.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeeAbsence_PaymentDate -->

**ما هو.** تاريخ استحقاق أجر الإجازة.

**مثال.** 25 يونيو 2026

**لماذا يهم.** تطابقه الرواتب مع تاريخ دفع المسيرة التي يجب أن تصرف هذه الإجازة. ويمكن أيضاً توقيت تذكيرات الدفع المقدم منه.

**القواعد.** اختياري. تاريخ.

## ReturnDate {#EmployeeAbsence.ReturnDate}
<!-- alias: loc:ReturnDate, dom:EmployeeAbsence_ReturnDate -->

**ما هو.** التاريخ الذي عاد فيه الموظف من الإجازة فعلياً.

**مثال.** 16 يوليو 2026

**القواعد.** اختياري. تاريخ.

## IsTravelling {#EmployeeAbsence.IsTravelling}
<!-- alias: loc:IsTravelling, dom:EmployeeAbsence_IsTravelling -->

**ما هو.** هل يسافر الموظف خلال الإجازة.

**لماذا يهم.** متاح لقواعد العمل.

**القواعد.** نعم أو لا.

## AdvanceLeavePaid {#EmployeeAbsence.AdvanceLeavePaid}
<!-- alias: loc:AdvanceLeavePaid, dom:EmployeeAbsence_AdvanceLeavePaid -->

**ما هو.** هل صُرف أجر الإجازة المقدم بالفعل.

**لماذا يهم.** تحدده الرواتب عند صرف الدفعة المقدمة. ولا تُرسل تذكيرات الدفع المقدم إلا ما دام غير محدد.

**القواعد.** نعم أو لا.

## IsReturned {#EmployeeAbsence.IsReturned}
<!-- alias: loc:IsReturned, dom:EmployeeAbsence_IsReturned -->

**ما هو.** هل عاد الموظف من هذه الإجازة.

**القواعد.** نعم أو لا.

:::admin
في أنواع الإجازات التي تتطلب تاريخ عودة، يُعامل الغياب المنتهي الذي ليس له تاريخ عودة ولم يُحدد فيه هذا الخيار على أنه عدم عودة من الإجازة: فتُرسل تذكيرات العودة، وتعامل الرواتب الموظف على أنه لم يعد عند تفعيل هذا الإعداد فيها.
:::

## ExtAbsenceTypeId {#EmployeeAbsence.ExtAbsenceTypeId}
<!-- alias: loc:ExtAbsenceTypeId, dom:EmployeeAbsence_ExtAbsenceTypeId -->

**ما هو.** نوع الإجازة الذي يُحوَّل إليه تمديد هذا الغياب -- كالإجازة السنوية الزائدة التي تُحتسب دون أجر.

**مثال.** إجازة بدون أجر

**لماذا يهم.** يُسجَّل فقط، ولا يقرأ أي احتساب التمديد حالياً.

**القواعد.** اختياري. يُختار من قائمة أنواع الغياب.

## ExtStartDate {#EmployeeAbsence.ExtStartDate}
<!-- alias: loc:ExtStartDate, dom:EmployeeAbsence_ExtStartDate -->

**ما هو.** أول يوم في التمديد.

**مثال.** 11 يوليو 2026

**القواعد.** اختياري. تاريخ.

## ExtEndDate {#EmployeeAbsence.ExtEndDate}
<!-- alias: loc:ExtEndDate, dom:EmployeeAbsence_ExtEndDate -->

**ما هو.** آخر يوم في التمديد.

**مثال.** 15 يوليو 2026

**القواعد.** اختياري. تاريخ.
