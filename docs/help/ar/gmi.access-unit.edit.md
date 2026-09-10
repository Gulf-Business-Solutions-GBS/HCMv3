---
helpId: gmi.access-unit.edit
lang: ar
title: وحدات الوصول — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.AccessUnit
surfaces:
  - angular/src/app/admin/general-master-info/access-units/create-or-edit-access-unit-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.AccessUnits.Create]
---

## Overview

وحدة الوصول هي جهاز واحد من أجهزة الحضور أو التحكم في الدخول. وكل بصمة تُستورد من الأجهزة تُسجِّل وحدة الوصول التي جاءت منها، ويُطابَق ذلك بعنوان الجهاز على الشبكة.

وإذا وردت بصمات من عنوان لا تحمله أي وحدة وصول، تُنشأ وحدة جديدة تلقائياً باسم الجهاز -- أو باسم عنوانه إن كان الاسم مستخدماً لوحدة ذات عنوان مختلف. وهكذا تنمو القائمة من تلقاء نفسها كلما أُضيفت أجهزة.

## IPAddress {#AccessUnit.IPAddress}
<!-- alias: loc:IPAddress, dom:AccessUnit_IPAddress -->

**ما هو.** عنوان الجهاز على الشبكة، كما يراه استيراد الحضور.

**مثال.** `192.168.1.50`

**لماذا يهم.** به تُطابَق البصمات المستوردة مع هذه الوحدة. فإذا تغيّر عنوان الجهاز، فغيّره هنا أيضاً -- وإلا أنشأ الاستيراد التالي وحدة ثانية للعنوان الجديد.

**القواعد.** اختياري.
