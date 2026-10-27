---
helpId: sec.email-token.edit
lang: en
title: Email Tokens — Create / Edit
entity: HCMv3.SystemSecurity.EmailToken
surfaces:
  - angular/src/app/admin/system-security/email-tokens/create-or-edit-email-token-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.EmailTokens.Create]
---

## Overview

A placeholder in an email template, and where its value comes from. `$#TokenName#$` in the template is replaced with the value; a token with no value is replaced with nothing. Token names must be unique.

## TokenName {#EmailToken.TokenName}
<!-- alias: loc:TokenName, dom:EmailToken_TokenName -->

**What it is.** The placeholder's name, as written between `$#` and `#$`.

**Example.** `EmployeeName`

**Rules.** Required. Maximum 256 characters.

## TokenValue {#EmailToken.TokenValue}
<!-- alias: loc:TokenValue, dom:EmailToken_TokenValue -->

**What it is.** Where the value comes from.

**Example.** `Employee.FullNameEn`

**Why it matters.** A property of the record the email is about (for example `Employee.FullNameEn`), or -- with **Object Key** ticked -- the name of a value the sending feature supplies.

**Rules.** Optional. Maximum 1000 characters.

## EmailTemplate {#EmailToken.EmailTemplateId}
<!-- alias: loc:EmailTemplate, dom:EmailToken_EmailTemplateId -->

**What it is.** The template the token belongs to.

**Rules.** Required. Chosen from Email Template master data.

## IsDateTimeType {#EmailToken.IsDateTimeType}
<!-- alias: loc:IsDateTimeType, dom:EmailToken_IsDateTimeType -->

**What it is.** Whether the value is a date or time.

**Why it matters.** Ticked, the value is formatted with the general settings' date format -- or the time format when the token name contains "Time".

**Rules.** Yes or no.

## ObjectKey {#EmailToken.ObjectKey}
<!-- alias: loc:ObjectKey, dom:EmailToken_ObjectKey -->

**What it is.** Whether the value is supplied by the sending feature rather than read from the record.

**Why it matters.** Ticked, **Token Value** is looked up among the values the feature passes in.

**Rules.** Yes or no.
