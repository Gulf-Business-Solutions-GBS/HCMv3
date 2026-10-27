---
helpId: hs.safety-checklist.edit
lang: ar
title: قوائم فحص السلامة — إنشاء / تحرير
entity: HCMv3.HealthAndSafetyManagement.SafetyChecklist
surfaces:
  - angular/src/app/admin/health-and-safety-management/safety-checklists/create-or-edit-safety-checklist-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.SafetyChecklists.Create]
---

## Overview

فحص يُجرى على منطقة أو أصل سلامة. اترك الرمز فارغاً ليُعطى الرقم التالي.

## SafetyAreaAsset {#SafetyChecklist.SafetyAreaAssetId}
<!-- alias: loc:SafetyAreaAsset, dom:SafetyChecklist_SafetyAreaAssetId -->

**ما هو.** المنطقة أو الأصل الذي يُفحص.

**مثال.** طفايات الحريق

**القواعد.** إلزامي. يُختار من قائمة أصول مناطق السلامة.

## IsActive {#SafetyChecklist.IsActive}
<!-- alias: loc:IsActive, dom:SafetyChecklist_IsActive -->

**ما هو.** ما إذا كان الفحص لا يزال مستخدماً.

**القواعد.** نعم أو لا.

## Description {#SafetyChecklist.Description}
<!-- alias: loc:Description, dom:SafetyChecklist_Description -->

**ما هو.** ما الذي يُفحص.

**القواعد.** اختياري.
