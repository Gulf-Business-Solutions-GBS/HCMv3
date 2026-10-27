---
helpId: sys.appraisal-settings.edit
lang: ar
title: Appraisal settings
entity: HCMv3.SystemSettings.ManageAppraisalSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-appraisal-settings/create-or-edit-manage-appraisal-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageAppraisalSettings]
---

## Overview

طريقة إجراء تقييمات الأداء: الدورة، وطريقة منح الدرجات، وتقييم الزملاء.

لا يُقرأ أي من هذه الإعدادات حالياً: فالميزات التي تتحكم فيها لم تُنقل من النظام السابق، لذا لا أثر لتغييرها.

لكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## AppraisalCycle {#ManageAppraisalSettings.AppraisalCycle}
<!-- alias: loc:AppraisalCycle, dom:AppraisalSettings_AppraisalCycle -->

**ما هو.** عدد مرات إجراء التقييم.

**مثال.** سنوياً

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** إلزامي. إحدى القيم: ربع سنوي، نصف سنوي، سنوياً.

## MarksOn {#ManageAppraisalSettings.MarksOn}
<!-- alias: loc:MarksOn, dom:AppraisalSettings_MarksOn -->

**ما هو.** ما إذا كانت درجات التقييم على أساس التقديرات أو الأوزان.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** إلزامي. إحدى القيم: التقديرات، الوزن.

## EnablePAPeerReview {#ManageAppraisalSettings.EnablePAPeerReview}
<!-- alias: loc:EnablePAPeerReview, dom:AppraisalSettings_EnablePAPeerReview -->

**ما هو.** ما إذا كان يمكن للزملاء تقييم بعضهم.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableMarksInPAReview {#ManageAppraisalSettings.EnableMarksInPAReview}
<!-- alias: loc:EnableMarksInPAReview, dom:AppraisalSettings_EnableMarksInPAReview -->

**ما هو.** ما إذا كانت الدرجات تُدخل في المراجعة.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## SetPeerMinLimit {#ManageAppraisalSettings.SetPeerMinLimit}
<!-- alias: loc:SetPeerMinLimit, dom:AppraisalSettings_SetPeerMinLimit -->

**ما هو.** أقل عدد من الزملاء يُرشَّح.

**مثال.** `2`

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## SetPeerMaxLimit {#ManageAppraisalSettings.SetPeerMaxLimit}
<!-- alias: loc:SetPeerMaxLimit, dom:AppraisalSettings_SetPeerMaxLimit -->

**ما هو.** أكبر عدد من الزملاء يُرشَّح.

**مثال.** `5`

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.
