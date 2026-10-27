---
helpId: gmi.currency.edit
lang: en
title: Currencies — Create / Edit
entity: HCMv3.GeneralMasterInfo.Currency
surfaces:
  - angular/src/app/admin/general-master-info/currencies/create-or-edit-currency-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.Currencies.Create]
---

## Overview

The currencies pay is made in. Each payroll area has a currency, and payroll rounds amounts to that currency's number of decimal places.

## DecimalCount {#Currency.DecimalCount}
<!-- alias: loc:DecimalCount, dom:Currency_DecimalCount -->

**What it is.** How many decimal places the currency uses.

**Example.** `3` for the Kuwaiti dinar

**Why it matters.** Payroll rounds amounts to this many places for payroll areas paid in this currency.

**Rules.** Required. Whole number.

## CurrencyShortFormEn {#Currency.CurrencyShortFormEn}
<!-- alias: loc:CurrencyShortFormEn, dom:Currency_CurrencyShortFormEn -->

**What it is.** The currency's short form in English.

**Example.** `KWD`

**Why it matters.** Shown next to amounts on reports such as the employee information and basic pay reports, and used in journal vouchers.

**Rules.** Optional.

## CurrencyDenominationFormEn {#Currency.CurrencyDenominationFormEn}
<!-- alias: loc:CurrencyDenominationFormEn, dom:Currency_CurrencyDenominationFormEn -->

**What it is.** The currency's denomination form in English.

**Why it matters.** Not used by any report or calculation at present.

**Rules.** Optional.

## CurrencyShortFormAr {#Currency.CurrencyShortFormAr}
<!-- alias: loc:CurrencyShortFormAr, dom:Currency_CurrencyShortFormAr -->

**What it is.** The currency's short form in Arabic.

**Example.** `د.ك`

**Why it matters.** Shown instead of the English short form when a report is produced with Arabic names.

**Rules.** Optional.

## CurrencyDenominationFormAr {#Currency.CurrencyDenominationFormAr}
<!-- alias: loc:CurrencyDenominationFormAr, dom:Currency_CurrencyDenominationFormAr -->

**What it is.** The currency's denomination form in Arabic.

**Why it matters.** Not used by any report or calculation at present.

**Rules.** Optional.
