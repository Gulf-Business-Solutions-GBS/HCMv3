---
helpId: sys.self-service-settings.edit
lang: en
title: Self service settings
entity: HCMv3.SystemSettings.ManageSelfServiceSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-self-service-settings/create-or-edit-manage-self-service-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageSelfServiceSettings]
---

## Overview

Settings for employee self-service.

Only **Email Notification** is read at present; the other settings have no effect.

There is one settings record per company; it cannot be deleted.

## RedirectAfterWFApproval {#ManageSelfServiceSettings.RedirectAfterWFApproval}
<!-- alias: loc:RedirectAfterWFApproval, dom:SelfServiceSettings_RedirectAfterWFApproval -->

**What it is.** Where the user goes after approving a workflow request.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Required. One of: Default, ListView, Dashboard.

## DefaultLoginPage {#ManageSelfServiceSettings.DefaultLoginPage}
<!-- alias: loc:DefaultLoginPage, dom:SelfServiceSettings_DefaultLoginPage -->

**What it is.** The page shown after an employee signs in.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional.

## CaptchaKey {#ManageSelfServiceSettings.CaptchaKey}
<!-- alias: loc:CaptchaKey, dom:SelfServiceSettings_CaptchaKey -->

**What it is.** The key for the sign-in captcha.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional.

## EmailNotification {#ManageSelfServiceSettings.EmailNotification}
<!-- alias: loc:EmailNotification, dom:SelfServiceSettings_EmailNotification -->

**What it is.** Whether the system sends emails.

**Why it matters.** Unticked, emails sent through the system's email service -- notification emails and punch emails -- are not sent.

**Rules.** Yes or no.

## ShowEmployeeInfo {#ManageSelfServiceSettings.ShowEmployeeInfo}
<!-- alias: loc:ShowEmployeeInfo, dom:SelfServiceSettings_ShowEmployeeInfo -->

**What it is.** Whether employee information is shown on the self-service home page.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## IsAttachmentAllowed {#ManageSelfServiceSettings.IsAttachmentAllowed}
<!-- alias: loc:IsAttachmentAllowed, dom:SelfServiceSettings_IsAttachmentAllowed -->

**What it is.** Whether attachments can be added to self-service requests.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AllowPunchOutsideRange {#ManageSelfServiceSettings.AllowPunchOutsideRange}
<!-- alias: loc:AllowPunchOutsideRange, dom:SelfServiceSettings_AllowPunchOutsideRange -->

**What it is.** Whether mobile punches are allowed outside the work location range.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AutoLogoutAfterPunch {#ManageSelfServiceSettings.AutoLogoutAfterPunch}
<!-- alias: loc:AutoLogoutAfterPunch, dom:SelfServiceSettings_AutoLogoutAfterPunch -->

**What it is.** Whether the user is signed out after punching.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AllowGeolocationSelfie {#ManageSelfServiceSettings.AllowGeolocationSelfie}
<!-- alias: loc:AllowGeolocationSelfie, dom:SelfServiceSettings_AllowGeolocationSelfie -->

**What it is.** Whether a selfie is taken with a mobile punch.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## HideGeolocationInfo {#ManageSelfServiceSettings.HideGeolocationInfo}
<!-- alias: loc:HideGeolocationInfo, dom:SelfServiceSettings_HideGeolocationInfo -->

**What it is.** Whether location details are hidden.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## HideGeolocationPunchInfo {#ManageSelfServiceSettings.HideGeolocationPunchInfo}
<!-- alias: loc:HideGeolocationPunchInfo, dom:SelfServiceSettings_HideGeolocationPunchInfo -->

**What it is.** Whether location details of punches are hidden.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## ShowLeaveInfo {#ManageSelfServiceSettings.ShowLeaveInfo}
<!-- alias: loc:ShowLeaveInfo, dom:SelfServiceSettings_ShowLeaveInfo -->

**What it is.** Whether leave information is shown.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## ShowCurrentLeaveBalance {#ManageSelfServiceSettings.ShowCurrentLeaveBalance}
<!-- alias: loc:ShowCurrentLeaveBalance, dom:SelfServiceSettings_ShowCurrentLeaveBalance -->

**What it is.** Whether the current leave balance is shown.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## ShowUpdatedLeaveBalance {#ManageSelfServiceSettings.ShowUpdatedLeaveBalance}
<!-- alias: loc:ShowUpdatedLeaveBalance, dom:SelfServiceSettings_ShowUpdatedLeaveBalance -->

**What it is.** Whether the leave balance after the request is shown.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## IsAdditionalUnpaidLeaveAllowed {#ManageSelfServiceSettings.IsAdditionalUnpaidLeaveAllowed}
<!-- alias: loc:IsAdditionalUnpaidLeaveAllowed, dom:SelfServiceSettings_IsAdditionalUnpaidLeaveAllowed -->

**What it is.** Whether extra unpaid leave can be requested.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## ShowPaymentDate {#ManageSelfServiceSettings.ShowPaymentDate}
<!-- alias: loc:ShowPaymentDate, dom:SelfServiceSettings_ShowPaymentDate -->

**What it is.** Whether the leave payment date is shown.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnablePayWithPayroll {#ManageSelfServiceSettings.EnablePayWithPayroll}
<!-- alias: loc:EnablePayWithPayroll, dom:SelfServiceSettings_EnablePayWithPayroll -->

**What it is.** Whether employees can choose to be paid with payroll.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnablePayInAdvance {#ManageSelfServiceSettings.EnablePayInAdvance}
<!-- alias: loc:EnablePayInAdvance, dom:SelfServiceSettings_EnablePayInAdvance -->

**What it is.** Whether employees can choose to be paid in advance.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## IsPassportWithdrawalRequired {#ManageSelfServiceSettings.IsPassportWithdrawalRequired}
<!-- alias: loc:IsPassportWithdrawalRequired, dom:SelfServiceSettings_IsPassportWithdrawalRequired -->

**What it is.** Whether a passport withdrawal is required with leave.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## PassportWithdrawalSubmissionStep {#ManageSelfServiceSettings.PassportWithdrawalSubmissionStep}
<!-- alias: loc:PassportWithdrawalSubmissionStep, dom:SelfServiceSettings_PassportWithdrawalSubmissionStep -->

**What it is.** The workflow step at which the passport withdrawal is submitted.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.
