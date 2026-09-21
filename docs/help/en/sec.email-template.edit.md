---
helpId: sec.email-template.edit
lang: en
title: Email Templates — Create / Edit
entity: HCMv3.SystemSecurity.EmailTemplate
surfaces:
  - angular/src/app/admin/system-security/email-templates/create-or-edit-email-template-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.EmailTemplates.Create]
---

## Overview

The text of an email the system sends -- notifications, reminders, payslips, bulk passwords. Placeholders written as `$#TokenName#$` in the subject and body are replaced using the template's email tokens.

## SmtpProfile {#EmailTemplate.SmtpProfile}
<!-- alias: loc:SmtpProfile, dom:EmailTemplate_SmtpProfile -->

**What it is.** The mail server profile to send from.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 256 characters.

## Subject {#EmailTemplate.Subject}
<!-- alias: loc:Subject, dom:EmailTemplate_Subject -->

**What it is.** The email's subject. Placeholders are replaced.

**Example.** Leave resumption reminder for $#EmployeeName#$

**Rules.** Optional. Maximum 500 characters.

## EmailBodyHtml {#EmailTemplate.EmailBodyHtml}
<!-- alias: loc:EmailBodyHtml, dom:EmailTemplate_EmailBodyHtml -->

**What it is.** The email's body, as HTML. Placeholders are replaced.

**Rules.** Optional.

## EmailBodyText {#EmailTemplate.EmailBodyText}
<!-- alias: loc:EmailBodyText, dom:EmailTemplate_EmailBodyText -->

**What it is.** A plain-text version of the body.

**Why it matters.** Where filled in, it is used instead of the HTML body for emails sent straight to an address, and the bulk password email needs it.

**Rules.** Optional.
