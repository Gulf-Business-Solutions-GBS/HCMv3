---
helpId: pay.social-security.edit
lang: ar
title: التأمينات الاجتماعية — إنشاء / تحرير
entity: HCMv3.PayrollManagement.SocialSecurity
surfaces:
  - angular/src/app/admin/payroll-management/social-securities/create-or-edit-social-security-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SocialSecurities.Create]
---

## Overview

إعداد التأمينات الاجتماعية لجنسية واحدة، اعتباراً من تاريخ معيّن: شرائح الاشتراك والعلاوات التي تُضاف إلى الأجر الذي تُحتسب عليه.

لا تنطبق التأمينات الاجتماعية على الموظف إلا إذا كان له سجل تأمينات اجتماعية مؤشَّر فيه على *اشتراك التأمينات الاجتماعية* للشهر، وكان هناك إعداد تأمينات اجتماعية لجنسيته سارياً في تاريخ الدفع.

ويحدد إعداد الرواتب *قاعدة نسبة التأمينات الاجتماعية* أجر الموظف للتأمينات الاجتماعية: المجموع الشهري لأنواع الأجور المؤشَّر عليها *إدراج في التأمينات الاجتماعية* (*الأجر والراتب*)، أو راتب العقد في سجل الكفيل للموظف (*راتب العقد*). وتُضاف إليه علاوات الحالة الاجتماعية والمؤهل الدراسي والأبناء، وتعطي الشريحة التي يقع فيها النسب، ويُصرف نصيبا الموظف وصاحب العمل على نوعي الأجر `1` و`3`.

## NationalityId {#SocialSecurity.NationalityId}
<!-- alias: loc:NationalityId, dom:SocialSecurity_NationalityId -->

**ما هو.** الجنسية التي يخصها هذا الإعداد.

**مثال.** كويتي

**لماذا يهم.** لا يشمل الموظفين من الجنسيات الأخرى.

**القواعد.** إلزامي. يُختار من قائمة الجنسيات.

## EarningsCap {#SocialSecurity.EarningsCap}
<!-- alias: loc:EarningsCap, dom:SocialSecurity_EarningsCap -->

**ما هو.** أقصى ما يمكن خصمه من التأمينات الاجتماعية في الشهر.

**مثال.** `450`

**لماذا يهم.** رغم اسمه فإنه يحدّ الخصم لا الاستحقاقات: فالخصم المحتسب فوق هذا المبلغ يُخفَّض إليه. والقيمة 0 تعني بلا حد.

**القواعد.** اختياري. رقم.

## ChildAllowance {#SocialSecurity.ChildAllowance}
<!-- alias: loc:ChildAllowance, dom:SocialSecurity_ChildAllowance -->

**ما هو.** المبلغ المضاف إلى الأجر عن كل ابن.

**مثال.** `50`

**لماذا يهم.** يُحتسب للموظفين الذكور فقط، عن كل عائل مؤشَّر عليه *إدراج في التأمينات الاجتماعية* وليس من ذوي الاحتياجات الخاصة.

**القواعد.** اختياري. رقم.

## SpecialNeedChildAllowance {#SocialSecurity.SpecialNeedChildAllowance}
<!-- alias: loc:SpecialNeedChildAllowance, dom:SocialSecurity_SpecialNeedChildAllowance -->

**ما هو.** المبلغ المضاف إلى الأجر عن كل ابن من ذوي الاحتياجات الخاصة.

**مثال.** `75`

**لماذا يهم.** يُحتسب للموظفين الذكور فقط، عن كل عائل مؤشَّر عليه *إدراج في التأمينات الاجتماعية* و*من ذوي الاحتياجات الخاصة*.

**القواعد.** اختياري. رقم.
