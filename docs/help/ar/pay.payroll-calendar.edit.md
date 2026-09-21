---
helpId: pay.payroll-calendar.edit
lang: ar
title: تقاويم الرواتب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.PayrollCalendar
surfaces:
  - angular/src/app/admin/payroll-management/payroll-calendars/create-or-edit-payroll-calendar-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PayrollCalendars.Create]
---

## Overview

شهر رواتب واحد لمنطقة رواتب، مع تاريخي بدايته ونهايته.

ويُرفض تشغيل الرواتب إذا لم يوجد في التقويم سطر لشهر وسنة تاريخ الدفع. ويستخدم تقرير مكافأة نهاية الخدمة تاريخ نهاية السطر فترةً للتقرير.

## PayrollAreaId {#PayrollCalendar.PayrollAreaId}
<!-- alias: loc:PayrollAreaId, dom:PayrollCalendar_PayrollAreaId -->

**ما هو.** منطقة الرواتب التي يتبعها هذا الشهر من التقويم.

**مثال.** Kuwait Monthly

**القواعد.** إلزامي. يُختار من قائمة مناطق الرواتب.

:::admin
يبحث تشغيل الرواتب عن السطر بالشهر والسنة فقط، ولا يتحقق من أنه يتبع المنطقة الجاري تشغيلها، فأي سطر لأي منطقة يسمح ببدء التشغيل. وهذا مطابق للنظام السابق وقد أُبقي عليه عمداً.
:::

## PayrollMonth {#PayrollCalendar.PayrollMonth}
<!-- alias: loc:PayrollMonth, dom:PayrollCalendar_PayrollMonth -->

**ما هو.** رقم الشهر الذي يغطيه هذا السطر، من 1 إلى 12.

**مثال.** `4`

**لماذا يهم.** يطابقه تشغيل الرواتب مع الشهر الميلادي لتاريخ الدفع.

**القواعد.** إلزامي. رقم صحيح.

:::admin
هذا هو الشهر الميلادي، لا الشهر المالي المستخدم في سجل ضبط الرواتب. ويتطابق الاثنان في المنطقة التي سنتها يناير–ديسمبر، ويختلفان في أي سنة مالية أخرى.
:::

## PayrollYear {#PayrollCalendar.PayrollYear}
<!-- alias: loc:PayrollYear, dom:PayrollCalendar_PayrollYear -->

**ما هو.** السنة التي يغطيها هذا السطر.

**مثال.** `2026`

**لماذا يهم.** يطابقها تشغيل الرواتب مع السنة الميلادية لتاريخ الدفع.

**القواعد.** إلزامي. رقم صحيح.
