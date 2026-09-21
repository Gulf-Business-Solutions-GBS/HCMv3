---
helpId: sec.sys-object.edit
lang: ar
title: كائنات النظام — إنشاء / تحرير
entity: HCMv3.SystemSecurity.SysObject
surfaces:
  - angular/src/app/admin/system-security/sys-objects/create-or-edit-sys-object-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.SysObjects.Create]
---

## Overview

قائمة النظام بأنواع السجلات، ومدى حساسية كل منها للرواتب والوقت. وتأشير نوع بأنه *حساس للرواتب* هو ما يُدخله تحت إقفال الرواتب وتتبع الأثر الرجعي: فأثناء إقفال منطقة الرواتب لا يمكن تعديل سجلات موظفيها من هذا النوع، والتعديل بتاريخ سابق يحدد التاريخ الذي يعيد نظام الرواتب الاحتساب منه.

## Name {#SysObject.Name}
<!-- alias: loc:Name, dom:SysObject_Name -->

**ما هو.** اسم نوع السجل.

**مثال.** EmployeeBasicPay

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## FullName {#SysObject.FullName}
<!-- alias: loc:FullName, dom:SysObject_FullName -->

**ما هو.** الاسم الكامل لصنف نوع السجل.

**مثال.** `HCMv3.PersonnelAdministration.EmployeeBasicPay`

**لماذا يهم.** بهذا يطابق النظام السطر مع السجلات -- فيجب أن يكون دقيقاً.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## IsPayrollSensitive {#SysObject.IsPayrollSensitive}
<!-- alias: loc:IsPayrollSensitive, dom:SysObject_IsPayrollSensitive -->

**ما هو.** ما إذا كانت تغييرات هذا النوع تؤثر في الرواتب.

**لماذا يهم.** إذا أُشِّر عليه تُرفض التغييرات أثناء إقفال منطقة رواتب الموظف، والتعديل بتاريخ سابق يُسجَّل ويجعل نظام الرواتب يعيد الاحتساب من ذلك التاريخ.

**القواعد.** نعم أو لا.

## RespectPermission {#SysObject.RespectPermission}
<!-- alias: loc:RespectPermission, dom:SysObject_RespectPermission -->

**ما هو.** ما إذا كانت الصلاحيات يُتحقق منها لهذا النوع.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## MustRetroRun {#SysObject.MustRetroRun}
<!-- alias: loc:MustRetroRun, dom:SysObject_MustRetroRun -->

**ما هو.** ما إذا كانت التعديلات بتاريخ سابق من هذا النوع مسموحة دائماً.

**لماذا يهم.** إذا أُشِّر عليه لا يمنع إعداد الرواتب *تعطيل الاحتساب بأثر رجعي* هذا النوع.

**القواعد.** نعم أو لا.

## IsTimeSensitive {#SysObject.IsTimeSensitive}
<!-- alias: loc:IsTimeSensitive, dom:SysObject_IsTimeSensitive -->

**ما هو.** ما إذا كانت تغييرات هذا النوع تؤثر في احتساب الوقت.

**لماذا يهم.** إذا أُشِّر عليه يجعل التعديلُ بتاريخ سابق محركَ الوقت يعيد احتساب الموظف من ذلك التاريخ.

**القواعد.** نعم أو لا.
