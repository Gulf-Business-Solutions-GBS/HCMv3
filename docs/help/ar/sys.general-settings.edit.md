---
helpId: sys.general-settings.edit
lang: ar
title: General settings
entity: HCMv3.SystemSettings.ManageGeneralSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-general-settings/create-or-edit-manage-general-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageGeneralSettings]
---

## Overview

إعدادات العرض العامة.

لا يُقرأ حالياً إلا تنسيقا التاريخ والوقت -- للتواريخ والأوقات المكتوبة في رسائل البريد الإلكتروني والرسائل المولّدة. أما البقية فلا أثر لها.

لكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## DateEditMask {#ManageGeneralSettings.DateEditMask}
<!-- alias: loc:DateEditMask, dom:GeneralSettings_DateEditMask -->

**ما هو.** تنسيق التاريخ المستخدم في رسائل البريد الإلكتروني والرسائل المولّدة.

**مثال.** `dd/MM/yyyy`

**لماذا يهم.** إذا تُرك فارغاً يُستخدم `dd/MM/yyyy`. أما الشاشات فتستخدم تنسيق لغة المستخدم، لا هذا.

**القواعد.** اختياري.

## DateDisplayFormat {#ManageGeneralSettings.DateDisplayFormat}
<!-- alias: loc:DateDisplayFormat, dom:GeneralSettings_DateDisplayFormat -->

**ما هو.** تنسيق عرض التاريخ.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري.

## TimeEditMask {#ManageGeneralSettings.TimeEditMask}
<!-- alias: loc:TimeEditMask, dom:GeneralSettings_TimeEditMask -->

**ما هو.** تنسيق الوقت المستخدم في رسائل البريد الإلكتروني والرسائل المولّدة.

**مثال.** `HH:mm`

**لماذا يهم.** إذا تُرك فارغاً يُستخدم `HH:mm`.

**القواعد.** اختياري.

## TimeDisplayFormat {#ManageGeneralSettings.TimeDisplayFormat}
<!-- alias: loc:TimeDisplayFormat, dom:GeneralSettings_TimeDisplayFormat -->

**ما هو.** تنسيق عرض الوقت.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري.

## StartupDashboard {#ManageGeneralSettings.StartupDashboard}
<!-- alias: loc:StartupDashboard, dom:GeneralSettings_StartupDashboard -->

**ما هو.** لوحة المعلومات المعروضة بعد تسجيل الدخول.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري.

## AutoFilterColumnCondition {#ManageGeneralSettings.AutoFilterColumnCondition}
<!-- alias: loc:AutoFilterColumnCondition, dom:GeneralSettings_AutoFilterColumnCondition -->

**ما هو.** طريقة مطابقة مرشحات القوائم للنص.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** إلزامي. إحدى القيم: يبدأ بـ، يحتوي على، يساوي.

## DisableAutoEmpcode {#ManageGeneralSettings.DisableAutoEmpcode}
<!-- alias: loc:DisableAutoEmpcode, dom:GeneralSettings_DisableAutoEmpcode -->

**ما هو.** ما إذا كانت رموز الموظفين لم تعد تُمنح تلقائياً.

**لماذا يهم.** لا يُقرأ حالياً: يُمنح رمز الموظف الفارغ الرقم التالي عند حفظ الموظف كما هو.

**القواعد.** نعم أو لا.

## NoticePeriodDaysExcludingSubmissionDate {#ManageGeneralSettings.NoticePeriodDaysExcludingSubmissionDate}
<!-- alias: loc:NoticePeriodDaysExcludingSubmissionDate, dom:GeneralSettings_NoticePeriodDaysExcludingSubmissionDate -->

**ما هو.** ما إذا كانت فترة الإشعار تستثني يوم تقديم الإشعار.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableGridDataAccessModeServer {#ManageGeneralSettings.EnableGridDataAccessModeServer}
<!-- alias: loc:EnableGridDataAccessModeServer, dom:GeneralSettings_EnableGridDataAccessModeServer -->

**ما هو.** ما إذا كانت القوائم تحمّل بياناتها صفحة بصفحة من الخادم.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## StartHangfire {#ManageGeneralSettings.StartHangfire}
<!-- alias: loc:StartHangfire, dom:GeneralSettings_StartHangfire -->

**ما هو.** ما إذا كان مجدول المهام الخلفية يبدأ.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.
