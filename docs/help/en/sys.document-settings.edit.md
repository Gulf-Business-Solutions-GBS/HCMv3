---
helpId: sys.document-settings.edit
lang: en
title: Document settings
entity: HCMv3.SystemSettings.ManageDocumentSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-document-settings/create-or-edit-manage-document-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageDocumentSettings]
---

## Overview

Where attachments are kept and how large they may be.

Only **Save Documents In File System** is read at present, by the late/early attendance report with photos; the size and extension limits are not enforced.

There is one settings record per company; it cannot be deleted.

## EnableDocSaveInFileSystem {#ManageDocumentSettings.EnableDocSaveInFileSystem}
<!-- alias: loc:EnableDocSaveInFileSystem, dom:DocumentSettings_EnableDocSaveInFileSystem -->

**What it is.** Whether documents are kept in the file system instead of the database.

**Why it matters.** The late/early attendance report with photos reads it to know where punch photos are.

**Rules.** Yes or no.

## FileSystemAttachmentPath {#ManageDocumentSettings.FileSystemAttachmentPath}
<!-- alias: loc:FileSystemAttachmentPath, dom:DocumentSettings_FileSystemAttachmentPath -->

**What it is.** The folder documents are kept in.

**Example.** `D:\HCMv3\Attachments`

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional.

## AllowedFileExtensions {#ManageDocumentSettings.AllowedFileExtensions}
<!-- alias: loc:AllowedFileExtensions, dom:DocumentSettings_AllowedFileExtensions -->

**What it is.** The file types that may be attached.

**Example.** `.pdf,.jpg,.png`

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Maximum 200 characters.

## MaxAttachmentsPerTransaction {#ManageDocumentSettings.MaxAttachmentsPerTransaction}
<!-- alias: loc:MaxAttachmentsPerTransaction, dom:DocumentSettings_MaxAttachmentsPerTransaction -->

**What it is.** The most attachments on one record.

**Example.** `5`

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## MaxFileSizePerAttachmentMB {#ManageDocumentSettings.MaxFileSizePerAttachmentMB}
<!-- alias: loc:MaxFileSizePerAttachmentMB, dom:DocumentSettings_MaxFileSizePerAttachmentMB -->

**What it is.** The largest attachment, in MB.

**Example.** `10`

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## MaxStorageSizeMB {#ManageDocumentSettings.MaxStorageSizeMB}
<!-- alias: loc:MaxStorageSizeMB, dom:DocumentSettings_MaxStorageSizeMB -->

**What it is.** The total storage allowed, in MB.

**Example.** `5000`

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## StorageWarningThresholdMB {#ManageDocumentSettings.StorageWarningThresholdMB}
<!-- alias: loc:StorageWarningThresholdMB, dom:DocumentSettings_StorageWarningThresholdMB -->

**What it is.** The storage used at which a warning is given, in MB.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.
