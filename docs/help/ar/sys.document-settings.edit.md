---
helpId: sys.document-settings.edit
lang: ar
title: Document settings
entity: HCMv3.SystemSettings.ManageDocumentSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-document-settings/create-or-edit-manage-document-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageDocumentSettings]
---

## Overview

مكان حفظ المرفقات وأحجامها المسموح بها.

لا يُقرأ حالياً إلا **حفظ المستندات في نظام الملفات**، ويقرؤه تقرير الحضور المتأخر والمبكر مع الصور، أما حدود الحجم والامتدادات فلا تُطبَّق.

لكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## EnableDocSaveInFileSystem {#ManageDocumentSettings.EnableDocSaveInFileSystem}
<!-- alias: loc:EnableDocSaveInFileSystem, dom:DocumentSettings_EnableDocSaveInFileSystem -->

**ما هو.** ما إذا كانت المستندات تُحفظ في نظام الملفات بدلاً من قاعدة البيانات.

**لماذا يهم.** يقرؤه تقرير الحضور المتأخر والمبكر مع الصور ليعرف مكان صور البصمات.

**القواعد.** نعم أو لا.

## FileStorageRootName {#ManageDocumentSettings.FileStorageRootName}
<!-- alias: loc:FileStorageRootName, dom:DocumentSettings_FileStorageRootName -->

**ما هو.** أيّ مواقع التخزين المعتمدة على هذا الخادم تُكتب فيه ملفات شركتك: المرفقات والمستندات الممسوحة وصور الموظفين وصور البصمة بالموقع. وتختار الموقع بالاسم، ولا تكتب مجلداً.

**مثال.** الافتراضي، أو «الأرشيف» عندما يوفّر الخادم قرصاً ثانياً للسجلات القديمة.

**لماذا يهمّ.** تبقى الملفات المحفوظة سابقاً في مكانها وتظل تعمل بعد التغيير، لأن كل ملف يحتفظ باسم الموقع الذي كُتب فيه. ولا ينتقل إلى الموقع الجديد إلا الملفات الجديدة، فلا يفقد من ينقل شركة إلى قرص آخر ما سبق.

:::admin
تأتي القائمة من `FileStorage:Roots` في ملف `appsettings.json` على الخادم، ويرى مستخدم المضيف أيضاً المجلد الذي يشير إليه كل اسم. ولا شيء يُكتب في هذه الشاشة يمكنه تحديد موضع الكتابة على الخادم، وهذا مقصود: فالمجلد الذي يُكتب هنا يصبح مجلداً يكتب فيه حساب الخدمة الملفات المرفوعة، وتوجيهه إلى موقع الويب أو إلى مجلد مستأجر آخر يفتح ثغرة. أما الحقل النصي القديم **مسار مرفقات نظام الملفات** فلم يكن يُقرأ وقد أُزيل، وبقيت قيمته المخزَّنة في قاعدة البيانات دون تغيير.
:::

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## AllowedFileExtensions {#ManageDocumentSettings.AllowedFileExtensions}
<!-- alias: loc:AllowedFileExtensions, dom:DocumentSettings_AllowedFileExtensions -->

**ما هو.** أنواع الملفات المسموح بإرفاقها.

**مثال.** `.pdf,.jpg,.png`

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. الحد الأقصى 200 حرف.

## MaxAttachmentsPerTransaction {#ManageDocumentSettings.MaxAttachmentsPerTransaction}
<!-- alias: loc:MaxAttachmentsPerTransaction, dom:DocumentSettings_MaxAttachmentsPerTransaction -->

**ما هو.** أكبر عدد من المرفقات في السجل الواحد.

**مثال.** `5`

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## MaxFileSizePerAttachmentMB {#ManageDocumentSettings.MaxFileSizePerAttachmentMB}
<!-- alias: loc:MaxFileSizePerAttachmentMB, dom:DocumentSettings_MaxFileSizePerAttachmentMB -->

**ما هو.** أكبر حجم للمرفق، بالميغابايت.

**مثال.** `10`

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## MaxStorageSizeMB {#ManageDocumentSettings.MaxStorageSizeMB}
<!-- alias: loc:MaxStorageSizeMB, dom:DocumentSettings_MaxStorageSizeMB -->

**ما هو.** مجموع مساحة التخزين المسموح بها، بالميغابايت.

**مثال.** `5000`

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## StorageWarningThresholdMB {#ManageDocumentSettings.StorageWarningThresholdMB}
<!-- alias: loc:StorageWarningThresholdMB, dom:DocumentSettings_StorageWarningThresholdMB -->

**ما هو.** مساحة التخزين المستخدمة التي يُنبَّه عندها، بالميغابايت.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.
