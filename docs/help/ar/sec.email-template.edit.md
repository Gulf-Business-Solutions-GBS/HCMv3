---
helpId: sec.email-template.edit
lang: ar
title: قوالب البريد الإلكتروني — إنشاء / تحرير
entity: HCMv3.SystemSecurity.EmailTemplate
surfaces:
  - angular/src/app/admin/system-security/email-templates/create-or-edit-email-template-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.EmailTemplates.Create]
---

## Overview

نص رسالة بريد إلكتروني يرسلها النظام -- الإشعارات والتذكيرات وكشوف الرواتب وكلمات المرور الجماعية. وتُستبدل العناصر النائبة المكتوبة بالشكل `$#TokenName#$` في الموضوع والنص باستخدام رموز البريد الخاصة بالنموذج.

## SmtpProfile {#EmailTemplate.SmtpProfile}
<!-- alias: loc:SmtpProfile, dom:EmailTemplate_SmtpProfile -->

**ما هو.** ملف خادم البريد الذي يُرسل منه.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Subject {#EmailTemplate.Subject}
<!-- alias: loc:Subject, dom:EmailTemplate_Subject -->

**ما هو.** موضوع الرسالة. وتُستبدل العناصر النائبة.

**مثال.** تذكير بالعودة من الإجازة لـ $#EmployeeName#$

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## EmailBodyHtml {#EmailTemplate.EmailBodyHtml}
<!-- alias: loc:EmailBodyHtml, dom:EmailTemplate_EmailBodyHtml -->

**ما هو.** نص الرسالة بصيغة HTML. وتُستبدل العناصر النائبة.

**القواعد.** اختياري.

## EmailBodyText {#EmailTemplate.EmailBodyText}
<!-- alias: loc:EmailBodyText, dom:EmailTemplate_EmailBodyText -->

**ما هو.** نسخة نصية عادية من نص الرسالة.

**لماذا يهم.** إذا مُلئت تُستخدم بدلاً من نص HTML في الرسائل المرسلة مباشرة إلى عنوان، ويحتاجها بريد كلمات المرور الجماعية.

**القواعد.** اختياري.
