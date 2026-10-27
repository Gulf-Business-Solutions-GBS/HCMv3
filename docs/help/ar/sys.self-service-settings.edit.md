---
helpId: sys.self-service-settings.edit
lang: ar
title: Self service settings
entity: HCMv3.SystemSettings.ManageSelfServiceSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-self-service-settings/create-or-edit-manage-self-service-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageSelfServiceSettings]
---

## Overview

إعدادات الخدمة الذاتية للموظفين.

لا يُقرأ حالياً إلا **إشعار البريد الإلكتروني**، أما الإعدادات الأخرى فلا أثر لها.

لكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## RedirectAfterWFApproval {#ManageSelfServiceSettings.RedirectAfterWFApproval}
<!-- alias: loc:RedirectAfterWFApproval, dom:SelfServiceSettings_RedirectAfterWFApproval -->

**ما هو.** المكان الذي ينتقل إليه المستخدم بعد اعتماد طلب سير العمل.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** إلزامي. إحدى القيم: افتراضي، ListView، لوحة القيادة.

## DefaultLoginPage {#ManageSelfServiceSettings.DefaultLoginPage}
<!-- alias: loc:DefaultLoginPage, dom:SelfServiceSettings_DefaultLoginPage -->

**ما هو.** الصفحة المعروضة بعد تسجيل دخول الموظف.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري.

## CaptchaKey {#ManageSelfServiceSettings.CaptchaKey}
<!-- alias: loc:CaptchaKey, dom:SelfServiceSettings_CaptchaKey -->

**ما هو.** مفتاح رمز التحقق عند تسجيل الدخول.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري.

## EmailNotification {#ManageSelfServiceSettings.EmailNotification}
<!-- alias: loc:EmailNotification, dom:SelfServiceSettings_EmailNotification -->

**ما هو.** ما إذا كان النظام يرسل رسائل بريد إلكتروني.

**لماذا يهم.** إذا لم يُؤشَّر عليه لا تُرسل الرسائل المرسلة عبر خدمة البريد في النظام -- رسائل الإشعارات ورسائل البصمات.

**القواعد.** نعم أو لا.

## ShowEmployeeInfo {#ManageSelfServiceSettings.ShowEmployeeInfo}
<!-- alias: loc:ShowEmployeeInfo, dom:SelfServiceSettings_ShowEmployeeInfo -->

**ما هو.** ما إذا كانت معلومات الموظف تُعرض في الصفحة الرئيسية للخدمة الذاتية.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## IsAttachmentAllowed {#ManageSelfServiceSettings.IsAttachmentAllowed}
<!-- alias: loc:IsAttachmentAllowed, dom:SelfServiceSettings_IsAttachmentAllowed -->

**ما هو.** ما إذا كان يمكن إضافة مرفقات إلى طلبات الخدمة الذاتية.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## AllowPunchOutsideRange {#ManageSelfServiceSettings.AllowPunchOutsideRange}
<!-- alias: loc:AllowPunchOutsideRange, dom:SelfServiceSettings_AllowPunchOutsideRange -->

**ما هو.** ما إذا كانت بصمات الجوال مسموحة خارج نطاق موقع العمل.

**لماذا يهم.** تقرأه شاشة البصمة الجغرافية في الخدمة الذاتية. عند تفعيله يستطيع الموظف الذي يبعد عن موقع العمل أكثر من النطاق المسموح به أن يسجّل البصمة، وتُسجَّل البصمة على أنها خارج النطاق مع عدد الأمتار الزائدة عن النطاق. وعند إلغائه تُرفض هذه البصمة. ويقيس الخادم المسافة بنفسه.

**القواعد.** نعم أو لا.

## AutoLogoutAfterPunch {#ManageSelfServiceSettings.AutoLogoutAfterPunch}
<!-- alias: loc:AutoLogoutAfterPunch, dom:SelfServiceSettings_AutoLogoutAfterPunch -->

**ما هو.** ما إذا كان المستخدم يُسجَّل خروجه بعد البصمة.

**لماذا يهم.** تقرأه شاشة البصمة الجغرافية في الخدمة الذاتية: عند تفعيله يُسجَّل خروج الموظف من النظام مباشرة بعد تسجيل البصمة بنجاح.

**القواعد.** نعم أو لا.

## AllowGeolocationSelfie {#ManageSelfServiceSettings.AllowGeolocationSelfie}
<!-- alias: loc:AllowGeolocationSelfie, dom:SelfServiceSettings_AllowGeolocationSelfie -->

**ما هو.** ما إذا كانت تُلتقط صورة ذاتية مع بصمة الجوال.

**لماذا يهم.** تقرأه شاشة البصمة الجغرافية في الخدمة الذاتية. عند تفعيله، وكانت سياسة الدوام الخاصة بالموظف تتطلب التعرف على الوجه (ويُعامل الموظف الذي ليست له سياسة دوام على أنها مفعّلة)، يجب التقاط صورة شخصية مع كل بصمة جغرافية. وتُحفظ الصورة مع البصمة، ويمكن للموارد البشرية عرضها من شاشة أحداث الدوام.

**القواعد.** نعم أو لا.

## HideGeolocationInfo {#ManageSelfServiceSettings.HideGeolocationInfo}
<!-- alias: loc:HideGeolocationInfo, dom:SelfServiceSettings_HideGeolocationInfo -->

**ما هو.** ما إذا كانت تفاصيل الموقع مخفية.

**لماذا يهم.** تقرأه شاشة البصمة الجغرافية في الخدمة الذاتية: عند تفعيله تُخفى اللوحة التي تعرض إحداثيات الموظف وموقع العمل والنطاق المسموح به والمسافة. ويبقى التحقق من النطاق مطبقاً.

**القواعد.** نعم أو لا.

## HideGeolocationPunchInfo {#ManageSelfServiceSettings.HideGeolocationPunchInfo}
<!-- alias: loc:HideGeolocationPunchInfo, dom:SelfServiceSettings_HideGeolocationPunchInfo -->

**ما هو.** ما إذا كانت تفاصيل موقع البصمات مخفية.

**لماذا يهم.** تقرأه شاشة البصمة الجغرافية في الخدمة الذاتية: عند تفعيله تُخفى قائمة بصمات اليوم في تلك الشاشة.

**القواعد.** نعم أو لا.

## ShowLeaveInfo {#ManageSelfServiceSettings.ShowLeaveInfo}
<!-- alias: loc:ShowLeaveInfo, dom:SelfServiceSettings_ShowLeaveInfo -->

**ما هو.** ما إذا كانت معلومات الإجازات معروضة.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## ShowCurrentLeaveBalance {#ManageSelfServiceSettings.ShowCurrentLeaveBalance}
<!-- alias: loc:ShowCurrentLeaveBalance, dom:SelfServiceSettings_ShowCurrentLeaveBalance -->

**ما هو.** ما إذا كان رصيد الإجازة الحالي معروضاً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## ShowUpdatedLeaveBalance {#ManageSelfServiceSettings.ShowUpdatedLeaveBalance}
<!-- alias: loc:ShowUpdatedLeaveBalance, dom:SelfServiceSettings_ShowUpdatedLeaveBalance -->

**ما هو.** ما إذا كان رصيد الإجازة بعد الطلب معروضاً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## IsAdditionalUnpaidLeaveAllowed {#ManageSelfServiceSettings.IsAdditionalUnpaidLeaveAllowed}
<!-- alias: loc:IsAdditionalUnpaidLeaveAllowed, dom:SelfServiceSettings_IsAdditionalUnpaidLeaveAllowed -->

**ما هو.** ما إذا كان يمكن طلب إجازة غير مدفوعة إضافية.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## ShowPaymentDate {#ManageSelfServiceSettings.ShowPaymentDate}
<!-- alias: loc:ShowPaymentDate, dom:SelfServiceSettings_ShowPaymentDate -->

**ما هو.** ما إذا كان تاريخ دفع الإجازة معروضاً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnablePayWithPayroll {#ManageSelfServiceSettings.EnablePayWithPayroll}
<!-- alias: loc:EnablePayWithPayroll, dom:SelfServiceSettings_EnablePayWithPayroll -->

**ما هو.** ما إذا كان يمكن للموظفين اختيار الصرف مع الرواتب.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnablePayInAdvance {#ManageSelfServiceSettings.EnablePayInAdvance}
<!-- alias: loc:EnablePayInAdvance, dom:SelfServiceSettings_EnablePayInAdvance -->

**ما هو.** ما إذا كان يمكن للموظفين اختيار الصرف مقدماً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## IsPassportWithdrawalRequired {#ManageSelfServiceSettings.IsPassportWithdrawalRequired}
<!-- alias: loc:IsPassportWithdrawalRequired, dom:SelfServiceSettings_IsPassportWithdrawalRequired -->

**ما هو.** ما إذا كان سحب جواز السفر مطلوباً مع الإجازة.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## PassportWithdrawalSubmissionStep {#ManageSelfServiceSettings.PassportWithdrawalSubmissionStep}
<!-- alias: loc:PassportWithdrawalSubmissionStep, dom:SelfServiceSettings_PassportWithdrawalSubmissionStep -->

**ما هو.** خطوة سير العمل التي يُقدَّم فيها سحب جواز السفر.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## IsWithdrawalReasonMandatory {#ManageSelfServiceSettings.IsWithdrawalReasonMandatory}
<!-- alias: loc:IsWithdrawalReasonMandatory, dom:SelfServiceSettings_IsWithdrawalReasonMandatory -->

**ما هو.** ما إذا كان يجب على الموظف ذكر سبب عند طلب سحب مستند مثل جواز السفر.

**لماذا يهم.** إذا أُشِّر عليه، يُرفض طلب سحب المستند عبر الخدمة الذاتية إذا لم يتضمن سبباً. وإلى أن تُحفظ الإعدادات أول مرة يكون السبب اختيارياً.

**القواعد.** نعم أو لا.
