---
helpId: gmi.currency.edit
lang: ar
title: العملات — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.Currency
surfaces:
  - angular/src/app/admin/general-master-info/currencies/create-or-edit-currency-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.Currencies.Create]
---

## Overview

العملات التي تُدفع بها الرواتب. لكل منطقة رواتب عملة، وتقرّب الرواتب المبالغ إلى عدد المنازل العشرية لتلك العملة.

## DecimalCount {#Currency.DecimalCount}
<!-- alias: loc:DecimalCount, dom:Currency_DecimalCount -->

**ما هو.** عدد المنازل العشرية التي تستخدمها العملة.

**مثال.** `3` للدينار الكويتي

**لماذا يهم.** تقرّب الرواتب المبالغ إلى هذا العدد من المنازل لمناطق الرواتب التي تُدفع بهذه العملة.

**القواعد.** إلزامي. رقم صحيح.

## CurrencyShortFormEn {#Currency.CurrencyShortFormEn}
<!-- alias: loc:CurrencyShortFormEn, dom:Currency_CurrencyShortFormEn -->

**ما هو.** الاختصار الإنجليزي للعملة.

**مثال.** `KWD`

**لماذا يهم.** يظهر بجوار المبالغ في تقارير مثل تقريري بيانات الموظفين والراتب الأساسي، ويُستخدم في قيود اليومية.

**القواعد.** اختياري.

## CurrencyDenominationFormEn {#Currency.CurrencyDenominationFormEn}
<!-- alias: loc:CurrencyDenominationFormEn, dom:Currency_CurrencyDenominationFormEn -->

**ما هو.** صيغة فئة العملة بالإنجليزية.

**لماذا يهم.** لا يستخدمها أي تقرير أو احتساب حالياً.

**القواعد.** اختياري.

## CurrencyShortFormAr {#Currency.CurrencyShortFormAr}
<!-- alias: loc:CurrencyShortFormAr, dom:Currency_CurrencyShortFormAr -->

**ما هو.** الاختصار العربي للعملة.

**مثال.** `د.ك`

**لماذا يهم.** يظهر بدلاً من الاختصار الإنجليزي عندما يُصدر التقرير بالأسماء العربية.

**القواعد.** اختياري.

## CurrencyDenominationFormAr {#Currency.CurrencyDenominationFormAr}
<!-- alias: loc:CurrencyDenominationFormAr, dom:Currency_CurrencyDenominationFormAr -->

**ما هو.** صيغة فئة العملة بالعربية.

**لماذا يهم.** لا يستخدمها أي تقرير أو احتساب حالياً.

**القواعد.** اختياري.
