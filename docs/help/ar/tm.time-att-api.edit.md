---
helpId: tm.time-att-api.edit
lang: ar
title: واجهات ربط أجهزة البصمة — إنشاء / تحرير
entity: HCMv3.TimeManagement.TimeAttAPI
surfaces:
  - angular/src/app/admin/time-management/time-att-apis/create-or-edit-time-att-api-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimeAttAPIs.Create]
---

## Overview

الاتصال بنظام الحضور والانصراف (BioTime) الذي تُستورد منه البصمات. وتظهر الإعدادات نفسها في **مركز التكامل ← BioTime**، الذي يمكنه أيضاً اختبار الاتصال.

## TokenUrl {#TimeAttAPI.TokenUrl}
<!-- alias: loc:TokenUrl, dom:TimeAttAPI_TokenUrl -->

**ما هو.** العنوان المستخدم لتسجيل الدخول إلى نظام الحضور.

**مثال.** `https://biotime.example.com/jwt-api-token-auth/`

**القواعد.** إلزامي. الحد الأقصى 500 حرف.

## TransactionsUrl {#TimeAttAPI.TransactionsUrl}
<!-- alias: loc:TransactionsUrl, dom:TimeAttAPI_TransactionsUrl -->

**ما هو.** العنوان الذي تُقرأ منه البصمات.

**مثال.** `https://biotime.example.com/iclock/api/transactions/`

**القواعد.** إلزامي. الحد الأقصى 500 حرف.

## DevicesUrl {#TimeAttAPI.DevicesUrl}
<!-- alias: loc:DevicesUrl, dom:TimeAttAPI_DevicesUrl -->

**ما هو.** العنوان الذي تُقرأ منه قائمة الأجهزة.

**مثال.** `https://biotime.example.com/iclock/api/terminals/`

**القواعد.** إلزامي. الحد الأقصى 500 حرف.

## Username {#TimeAttAPI.Username}
<!-- alias: loc:Username, dom:TimeAttAPI_Username -->

**ما هو.** الحساب المستخدم لتسجيل الدخول إلى نظام الحضور.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## Password {#TimeAttAPI.Password}
<!-- alias: loc:Password, dom:TimeAttAPI_Password -->

**ما هو.** كلمة المرور لذلك الحساب.

**القواعد.** إلزامي عند إنشاء السجل. الحد الأقصى 128 حرفاً. عند التعديل يظهر الحقل فارغاً لأن كلمة المرور المحفوظة لا تُعرض أبداً؛ اتركه فارغاً للإبقاء عليها، أو أدخل كلمة مرور جديدة لاستبدالها.

:::admin
تُخزَّن كلمة المرور مشفّرة سواء حُفظت من هذا النموذج أو من مركز التكامل. وتبقى كلمة المرور المحفوظة قبل تطبيق التشفير صالحة، وتُشفَّر عند الحفظ التالي للسجل.
:::

## NoOfRecords {#TimeAttAPI.NoOfRecords}
<!-- alias: loc:NoOfRecords, dom:TimeAttAPI_NoOfRecords -->

**ما هو.** عدد البصمات المقروءة في كل طلب.

**مثال.** `500`

**لماذا يهم.** يواصل الاستيراد طلب صفحات بهذا الحجم إلى أن تعود صفحة أقصر.

**القواعد.** إلزامي. رقم صحيح.

## Email {#TimeAttAPI.Email}
<!-- alias: loc:Email, dom:TimeAttAPI_Email -->

**ما هو.** بريد إلكتروني للتواصل بشأن الاتصال.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Company {#TimeAttAPI.Company}
<!-- alias: loc:Company, dom:TimeAttAPI_Company -->

**ما هو.** اسم الشركة في نظام الحضور.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## PunchInCode {#TimeAttAPI.PunchInCode}
<!-- alias: loc:PunchInCode, dom:TimeAttAPI_PunchInCode -->

**ما هو.** حالات البصمة في نظام الحضور التي تعني الدخول، مفصولة بفواصل.

**مثال.** `0`

**القواعد.** اختياري. الحد الأقصى 50 حرفاً.

## PunchOutCode {#TimeAttAPI.PunchOutCode}
<!-- alias: loc:PunchOutCode, dom:TimeAttAPI_PunchOutCode -->

**ما هو.** حالات البصمة التي تعني الخروج.

**مثال.** `1`

**القواعد.** اختياري. الحد الأقصى 50 حرفاً.

## BreakOutCode {#TimeAttAPI.BreakOutCode}
<!-- alias: loc:BreakOutCode, dom:TimeAttAPI_BreakOutCode -->

**ما هو.** حالات البصمة التي تعني الخروج للاستراحة.

**مثال.** `2`

**القواعد.** اختياري. الحد الأقصى 50 حرفاً.

## BreakInCode {#TimeAttAPI.BreakInCode}
<!-- alias: loc:BreakInCode, dom:TimeAttAPI_BreakInCode -->

**ما هو.** حالات البصمة التي تعني العودة من الاستراحة.

**مثال.** `3`

**القواعد.** اختياري. الحد الأقصى 50 حرفاً.
