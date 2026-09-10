---
helpId: gmi.number-series.edit
lang: ar
title: تسلسل الأرقام — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.NumberSeries
surfaces:
  - angular/src/app/admin/general-master-info/number-series/create-or-edit-number-series-modal.component.html
audience: [admin]
permissions: []
---

## Overview

تسلسلات أرقام لتوليد الرموز. لا تُستخدم حالياً: فرموز الموظفين مثلاً تُمنح بأعلى رقم موجود مضافاً إليه واحد، لا من تسلسل أرقام.

## Name {#NumberSeries.Name}
<!-- alias: loc:Name, dom:NumberSeries_Name -->

**ما هو.** اسم تتعرّف به على التسلسل.

**القواعد.** اختياري.

## Start {#NumberSeries.Start}
<!-- alias: loc:Start, dom:NumberSeries_Start -->

**ما هو.** أول رقم في التسلسل.

**القواعد.** اختياري. رقم صحيح.

## Prefix {#NumberSeries.Prefix}
<!-- alias: loc:Prefix, dom:NumberSeries_Prefix -->

**ما هو.** نص يسبق كل رقم.

**مثال.** `EMP-`

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## Formatting {#NumberSeries.Formatting}
<!-- alias: loc:Formatting, dom:NumberSeries_Formatting -->

**ما هو.** النمط الذي تُكتب به الأرقام.

**القواعد.** إلزامي. الحد الأقصى 64 حرفاً.
