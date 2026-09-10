---
helpId: gmi.currency-convertor.edit
lang: en
title: Currency Convertors — Create / Edit
entity: HCMv3.GeneralMasterInfo.CurrencyConvertor
surfaces:
  - angular/src/app/admin/general-master-info/currency-convertors/create-or-edit-currency-convertor-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.CurrencyConvertors.Create]
---

## Overview

Exchange rates between two currencies on a given date. The rates are stored for reference only: no calculation converts between currencies with them at present.

## CalendarDate {#CurrencyConvertor.CalendarDate}
<!-- alias: loc:CalendarDate, dom:CurrencyConvertor_CalendarDate -->

**What it is.** The date the rate applies to.

**Rules.** Required. Date value.

## FromCurr {#CurrencyConvertor.FromCurrId}
<!-- alias: loc:FromCurr, dom:CurrencyConvertor_FromCurrId -->

**What it is.** The currency being converted from.

**Rules.** Required. Chosen from Currency master data.

## ToCurr {#CurrencyConvertor.ToCurrId}
<!-- alias: loc:ToCurr, dom:CurrencyConvertor_ToCurrId -->

**What it is.** The currency being converted to.

**Rules.** Required. Chosen from Currency master data.

## Rate {#CurrencyConvertor.Rate}
<!-- alias: loc:Rate, dom:CurrencyConvertor_Rate -->

**What it is.** The conversion rate between the two currencies on that date.

**Why it matters.** It cannot be zero.

**Rules.** Required. Number.
