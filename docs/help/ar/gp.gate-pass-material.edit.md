---
helpId: gp.gate-pass-material.edit
lang: ar
title: مواد تصاريح الدخول والخروج — إنشاء / تحرير
entity: HCMv3.GatePassManagement.GatePassMaterial
surfaces:
  - angular/src/app/admin/gate-pass-management/gate-pass-detail/materials/create-or-edit-gate-pass-material-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.GatePassMaterials.Create]
---

## Overview

مادة تمر عبر البوابة بموجب تصريح، والكمية التي أُعيدت منها.

## ItemCode {#GatePassMaterial.ItemCode}
<!-- alias: loc:ItemCode, dom:GatePassMaterial_ItemCode -->

**ما هو.** رمز المادة.

**القواعد.** إلزامي. الحد الأقصى 128 حرفاً.

## ItemName {#GatePassMaterial.ItemName}
<!-- alias: loc:ItemName, dom:GatePassMaterial_ItemName -->

**ما هو.** اسم المادة.

**مثال.** حاسوب محمول

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## Quantity {#GatePassMaterial.Quantity}
<!-- alias: loc:Quantity, dom:GatePassMaterial_Quantity -->

**ما هو.** الكمية التي خرجت.

**مثال.** 2

**القواعد.** إلزامي. رقم صحيح.

## ExpectedReturnDate {#GatePassMaterial.ExpectedReturnDate}
<!-- alias: loc:ExpectedReturnDate, dom:GatePassMaterial_ExpectedReturnDate -->

**ما هو.** متى يجب أن تعود المواد.

**لماذا يهم.** للتسجيل فقط: لا يوجد ما ينبّه عند حلوله.

**القواعد.** إلزامي. تاريخ.

## ReturnedQuantity {#GatePassMaterial.ReturnedQuantity}
<!-- alias: loc:ReturnedQuantity, dom:GatePassMaterial_ReturnedQuantity -->

**ما هو.** الكمية التي عادت.

**مثال.** 1

**القواعد.** إلزامي. رقم صحيح.
