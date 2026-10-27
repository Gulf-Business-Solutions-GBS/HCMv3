---
helpId: sec.email-token.edit
lang: ar
title: رموز البريد الإلكتروني — إنشاء / تحرير
entity: HCMv3.SystemSecurity.EmailToken
surfaces:
  - angular/src/app/admin/system-security/email-tokens/create-or-edit-email-token-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.EmailTokens.Create]
---

## Overview

عنصر نائب في نموذج البريد، ومصدر قيمته. ويُستبدل `$#TokenName#$` في النموذج بالقيمة، والرمز الذي ليس له قيمة يُستبدل بلا شيء. ويجب ألا تتكرر أسماء الرموز.

## TokenName {#EmailToken.TokenName}
<!-- alias: loc:TokenName, dom:EmailToken_TokenName -->

**ما هو.** اسم العنصر النائب كما يُكتب بين `$#` و`#$`.

**مثال.** `EmployeeName`

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## TokenValue {#EmailToken.TokenValue}
<!-- alias: loc:TokenValue, dom:EmailToken_TokenValue -->

**ما هو.** مصدر القيمة.

**مثال.** `Employee.FullNameEn`

**لماذا يهم.** خاصية من السجل الذي تخصه الرسالة (مثل `Employee.FullNameEn`)، أو -- مع التأشير على **مفتاح الكائن** -- اسم قيمة تقدمها الميزة المرسِلة.

**القواعد.** اختياري. الحد الأقصى 1000 حرف.

## EmailTemplate {#EmailToken.EmailTemplateId}
<!-- alias: loc:EmailTemplate, dom:EmailToken_EmailTemplateId -->

**ما هو.** النموذج الذي يتبعه الرمز.

**القواعد.** إلزامي. يُختار من قائمة قوالب البريد الإلكتروني.

## IsDateTimeType {#EmailToken.IsDateTimeType}
<!-- alias: loc:IsDateTimeType, dom:EmailToken_IsDateTimeType -->

**ما هو.** ما إذا كانت القيمة تاريخاً أو وقتاً.

**لماذا يهم.** إذا أُشِّر عليه تُنسَّق القيمة بتنسيق التاريخ في الإعدادات العامة -- أو بتنسيق الوقت إذا احتوى اسم الرمز على "Time".

**القواعد.** نعم أو لا.

## ObjectKey {#EmailToken.ObjectKey}
<!-- alias: loc:ObjectKey, dom:EmailToken_ObjectKey -->

**ما هو.** ما إذا كانت القيمة تقدمها الميزة المرسِلة بدلاً من قراءتها من السجل.

**لماذا يهم.** إذا أُشِّر عليه يُبحث عن **قيمة الرمز** بين القيم التي تمررها الميزة.

**القواعد.** نعم أو لا.
