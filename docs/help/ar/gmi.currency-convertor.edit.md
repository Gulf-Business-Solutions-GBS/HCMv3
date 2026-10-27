---
helpId: gmi.currency-convertor.edit
lang: ar
title: محولات العملات — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.CurrencyConvertor
surfaces:
  - angular/src/app/admin/general-master-info/currency-convertors/create-or-edit-currency-convertor-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.CurrencyConvertors.Create]
---

## Overview

أسعار الصرف بين عملتين في تاريخ معيّن. وهي محفوظة للرجوع إليها فقط: لا يحوّل أي احتساب بين العملات بها حالياً.

## CalendarDate {#CurrencyConvertor.CalendarDate}
<!-- alias: loc:CalendarDate, dom:CurrencyConvertor_CalendarDate -->

**ما هو.** التاريخ الذي ينطبق عليه السعر.

**القواعد.** إلزامي. تاريخ.

## FromCurr {#CurrencyConvertor.FromCurrId}
<!-- alias: loc:FromCurr, dom:CurrencyConvertor_FromCurrId -->

**ما هو.** العملة المحوَّل منها.

**القواعد.** إلزامي. يُختار من قائمة العملات.

## ToCurr {#CurrencyConvertor.ToCurrId}
<!-- alias: loc:ToCurr, dom:CurrencyConvertor_ToCurrId -->

**ما هو.** العملة المحوَّل إليها.

**القواعد.** إلزامي. يُختار من قائمة العملات.

## Rate {#CurrencyConvertor.Rate}
<!-- alias: loc:Rate, dom:CurrencyConvertor_Rate -->

**ما هو.** سعر التحويل بين العملتين في ذلك التاريخ.

**لماذا يهم.** لا يجوز أن يكون صفراً.

**القواعد.** إلزامي. رقم.
