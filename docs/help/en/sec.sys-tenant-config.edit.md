---
helpId: sec.sys-tenant-config.edit
lang: en
title: Tenant Configs — Create / Edit
entity: HCMv3.SystemSecurity.SysTenantConfig
surfaces:
  - angular/src/app/admin/system-security/sys-tenant-configs/create-or-edit-sys-tenant-config-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.SysTenantConfigs.Create]
---

## Overview

A key and value the system can look up by key, from a given date. At present only the Dynamics 365 Finance connection reads these -- as a fallback when a value is not set in the Integration Center.

## Key {#SysTenantConfig.Key}
<!-- alias: loc:Key, dom:SysTenantConfig_Key -->

**What it is.** The key the value is looked up by.

**Rules.** Required. Maximum 256 characters.

## ConfigCategory {#SysTenantConfig.ConfigCategory}
<!-- alias: loc:ConfigCategory, dom:SysTenantConfig_ConfigCategory -->

**What it is.** The area the setting belongs to, for grouping.

**Example.** Payroll

**Rules.** Required. One of: Employee, Payroll, Time Attendance, Formatting, Report, Geolocation.

## KeyValue {#SysTenantConfig.KeyValue}
<!-- alias: loc:KeyValue, dom:SysTenantConfig_KeyValue -->

**What it is.** The value.

**Rules.** Optional. Maximum 1000 characters.

## ConfigDescription {#SysTenantConfig.ConfigDescription}
<!-- alias: loc:ConfigDescription, dom:SysTenantConfig_ConfigDescription -->

**What it is.** What the setting is for.

**Rules.** Optional. Maximum 1000 characters.
