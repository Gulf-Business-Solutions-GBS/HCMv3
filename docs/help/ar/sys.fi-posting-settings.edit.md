---
helpId: sys.fi-posting-settings.edit
lang: ar
title: FI posting settings
entity: HCMv3.SystemSettings.ManageFIPostingSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-fi-posting-settings/create-or-edit-manage-fi-posting-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageFIPostingSettings]
---

## Overview

طريقة ترحيل الرواتب إلى النظام المالي.

لكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## SeparateProvisionPosting {#ManageFIPostingSettings.SeparateProvisionPosting}
<!-- alias: loc:SeparateProvisionPosting, dom:FIPostingSettings_SeparateProvisionPosting -->

**ما هو.** ما إذا كانت المخصصات تُرحَّل في قيد يومية منفصل.

**لماذا يهم.** إذا أُشِّر عليه يُقسم ترحيل قيد اليومية إلى قسمين: أسطر الرواتب في قيد، وأسطر المخصصات (الإجازات ومكافأة نهاية الخدمة والتذاكر) في قيد آخر. وإلا فيذهب كل شيء إلى قيد رواتب واحد.

**القواعد.** نعم أو لا.

## EnableAutoWageAccountEntry {#ManageFIPostingSettings.EnableAutoWageAccountEntry}
<!-- alias: loc:EnableAutoWageAccountEntry, dom:FIPostingSettings_EnableAutoWageAccountEntry -->

**ما هو.** ما إذا كانت حسابات الأجور تُنشأ تلقائياً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## AllowDuplicateCodeInCostElement {#ManageFIPostingSettings.AllowDuplicateCodeInCostElement}
<!-- alias: loc:AllowDuplicateCodeInCostElement, dom:FIPostingSettings_AllowDuplicateCodeInCostElement -->

**ما هو.** ما إذا كان يجوز أن يشترك عنصرا تكلفة في رمز واحد.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## FIPostingAfterPayrollExit {#ManageFIPostingSettings.FiPostingAfterPayrollExit}
<!-- alias: loc:FIPostingAfterPayrollExit, dom:FIPostingSettings_FIPostingAfterPayrollExit -->

**ما هو.** ما إذا كان لا يجوز إنتاج ملفات البنك إلا لفترات الرواتب المقفلة.

**لماذا يهم.** رغم اسمه فإنه يتحكم في ملفات البنك: إذا أُشِّر عليه يُرفض ملف البنك الذي تاريخ نهايته بعد آخر فترة مقفلة لمنطقة الرواتب. وإلا فيمكن إنتاج ملفات البنك لأي تواريخ.

**القواعد.** نعم أو لا.
