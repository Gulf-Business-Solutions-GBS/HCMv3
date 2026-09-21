---
helpId: sec.payslip-config.edit
lang: en
title: Payslip Configs — Create / Edit
entity: HCMv3.SystemSecurity.PayslipConfig
surfaces:
  - angular/src/app/admin/system-security/payslip-configs/create-or-edit-payslip-config-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.PayslipConfigs.Create]
---

## Overview

Which email template payslip emails use. Only one record is allowed.

Not read at present: payslip emails do not take their template from here.

## EmailTemplate {#PayslipConfig.EmailTemplateId}
<!-- alias: loc:EmailTemplate, dom:PayslipConfig_EmailTemplateId -->

**What it is.** The template.

**Rules.** Required. Chosen from Email Template master data.
