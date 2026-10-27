---
helpId: pay.account-info.edit
lang: ar
title: بيانات الحسابات — إنشاء / تحرير
entity: HCMv3.PayrollManagement.AccountInfo
surfaces:
  - angular/src/app/admin/payroll-management/account-infos/create-or-edit-account-info-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AccountInfos.Create]
---

## Overview

حساب في دفتر الأستاذ في النظام المالي تُرحَّل إليه الرواتب -- مصروف الرواتب والمستحقات وحساب الرواتب البنكي وغيرها. وتشير حسابات الأجور ومناطق الرواتب وقيد اليومية إلى حسابات من هذه القائمة.

## AccountType {#AccountInfo.AccountTypeId}
<!-- alias: loc:AccountType, dom:AccountInfo_AccountTypeId -->

**ما هو.** نوع الحساب في النظام المالي.

**مثال.** دفتر الأستاذ

**لماذا يهم.** يُطبع نوعاً للحساب بجانب الحساب في كل سطر من أسطر قيد اليومية، للحساب الرئيسي والحساب المقابل على السواء.

**القواعد.** إلزامي. يُختار من قائمة أنواع الحسابات.
