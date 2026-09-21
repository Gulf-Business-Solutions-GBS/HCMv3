---
helpId: sec.sys-tenant-config.edit
lang: ar
title: إعدادات المستأجر — إنشاء / تحرير
entity: HCMv3.SystemSecurity.SysTenantConfig
surfaces:
  - angular/src/app/admin/system-security/sys-tenant-configs/create-or-edit-sys-tenant-config-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.SysTenantConfigs.Create]
---

## Overview

مفتاح وقيمة يمكن للنظام البحث عنهما بالمفتاح، اعتباراً من تاريخ معيّن. ولا يقرؤها حالياً إلا اتصال Dynamics 365 Finance -- احتياطاً إذا لم تُضبط القيمة في مركز التكامل.

## Key {#SysTenantConfig.Key}
<!-- alias: loc:Key, dom:SysTenantConfig_Key -->

**ما هو.** المفتاح الذي يُبحث بالقيمة من خلاله.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## ConfigCategory {#SysTenantConfig.ConfigCategory}
<!-- alias: loc:ConfigCategory, dom:SysTenantConfig_ConfigCategory -->

**ما هو.** المجال الذي يتبعه الإعداد، للتجميع.

**مثال.** الرواتب

**القواعد.** إلزامي. إحدى القيم: الموظف، الرواتب، الحضور والانصراف، تنسيق، التقرير، الموقع الجغرافي.

## KeyValue {#SysTenantConfig.KeyValue}
<!-- alias: loc:KeyValue, dom:SysTenantConfig_KeyValue -->

**ما هو.** القيمة.

**القواعد.** اختياري. الحد الأقصى 1000 حرف.

## ConfigDescription {#SysTenantConfig.ConfigDescription}
<!-- alias: loc:ConfigDescription, dom:SysTenantConfig_ConfigDescription -->

**ما هو.** الغرض من الإعداد.

**القواعد.** اختياري. الحد الأقصى 1000 حرف.
