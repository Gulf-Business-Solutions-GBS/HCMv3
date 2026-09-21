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

## FileSystemAttachmentPath {#ManageDocumentSettings.FileSystemAttachmentPath}
<!-- alias: loc:FileSystemAttachmentPath, dom:DocumentSettings_FileSystemAttachmentPath -->

**ما هو.** المجلد الذي تُحفظ فيه المستندات.

**مثال.** `D:\HCMv3\Attachments`

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري.

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
