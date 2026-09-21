---
helpId: tm.time-att-api.edit
lang: en
title: Time Attendance APIs — Create / Edit
entity: HCMv3.TimeManagement.TimeAttAPI
surfaces:
  - angular/src/app/admin/time-management/time-att-apis/create-or-edit-time-att-api-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimeAttAPIs.Create]
---

## Overview

The connection to the time-attendance system (BioTime) that punches are imported from. The same settings appear under **Integration Center → BioTime**, which can also test the connection.

## TokenUrl {#TimeAttAPI.TokenUrl}
<!-- alias: loc:TokenUrl, dom:TimeAttAPI_TokenUrl -->

**What it is.** The address used to sign in to the attendance system.

**Example.** `https://biotime.example.com/jwt-api-token-auth/`

**Rules.** Required. Maximum 500 characters.

## TransactionsUrl {#TimeAttAPI.TransactionsUrl}
<!-- alias: loc:TransactionsUrl, dom:TimeAttAPI_TransactionsUrl -->

**What it is.** The address punches are read from.

**Example.** `https://biotime.example.com/iclock/api/transactions/`

**Rules.** Required. Maximum 500 characters.

## DevicesUrl {#TimeAttAPI.DevicesUrl}
<!-- alias: loc:DevicesUrl, dom:TimeAttAPI_DevicesUrl -->

**What it is.** The address the list of devices is read from.

**Example.** `https://biotime.example.com/iclock/api/terminals/`

**Rules.** Required. Maximum 500 characters.

## Username {#TimeAttAPI.Username}
<!-- alias: loc:Username, dom:TimeAttAPI_Username -->

**What it is.** The account used to sign in to the attendance system.

**Rules.** Required. Maximum 256 characters.

## Password {#TimeAttAPI.Password}
<!-- alias: loc:Password, dom:TimeAttAPI_Password -->

**What it is.** The password for that account.

**Rules.** Required when you create the record. Maximum 128 characters. When you edit, the field is empty because the saved password is never shown; leave it blank to keep it, or type a new one to replace it.

:::admin
The password is stored encrypted, whether it is saved from this form or from Integration Center. A password saved before encryption was introduced still works and is encrypted the next time the record is saved.
:::

## NoOfRecords {#TimeAttAPI.NoOfRecords}
<!-- alias: loc:NoOfRecords, dom:TimeAttAPI_NoOfRecords -->

**What it is.** How many punches are read per request.

**Example.** `500`

**Why it matters.** The import keeps requesting pages of this size until a page comes back short.

**Rules.** Required. Whole number.

## Email {#TimeAttAPI.Email}
<!-- alias: loc:Email, dom:TimeAttAPI_Email -->

**What it is.** A contact email for the connection.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 256 characters.

## Company {#TimeAttAPI.Company}
<!-- alias: loc:Company, dom:TimeAttAPI_Company -->

**What it is.** The company name in the attendance system.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 256 characters.

## PunchInCode {#TimeAttAPI.PunchInCode}
<!-- alias: loc:PunchInCode, dom:TimeAttAPI_PunchInCode -->

**What it is.** The attendance-system punch states that mean clock in, separated by commas.

**Example.** `0`

**Rules.** Optional. Maximum 50 characters.

## PunchOutCode {#TimeAttAPI.PunchOutCode}
<!-- alias: loc:PunchOutCode, dom:TimeAttAPI_PunchOutCode -->

**What it is.** The punch states that mean clock out.

**Example.** `1`

**Rules.** Optional. Maximum 50 characters.

## BreakOutCode {#TimeAttAPI.BreakOutCode}
<!-- alias: loc:BreakOutCode, dom:TimeAttAPI_BreakOutCode -->

**What it is.** The punch states that mean out for a break.

**Example.** `2`

**Rules.** Optional. Maximum 50 characters.

## BreakInCode {#TimeAttAPI.BreakInCode}
<!-- alias: loc:BreakInCode, dom:TimeAttAPI_BreakInCode -->

**What it is.** The punch states that mean back from a break.

**Example.** `3`

**Rules.** Optional. Maximum 50 characters.
