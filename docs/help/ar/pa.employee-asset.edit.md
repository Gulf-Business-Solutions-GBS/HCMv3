---
helpId: pa.employee-asset.edit
lang: ar
title: عهد الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeAsset
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/assets/create-or-edit-employee-asset-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAssets.Create]
---

## Overview

أصل من أصول الشركة سُلِّم للموظف خلال فترة -- حاسوب محمول أو هاتف أو سيارة.

لا يجوز تخصيص الأصل نفسه لفترات متداخلة، أياً كان الموظف الذي يحمله. ويمكن للموظفين طلب الأصول عبر الخدمة الذاتية.

## AssetId {#EmployeeAsset.AssetId}
<!-- alias: loc:AssetId, dom:EmployeeAsset_AssetId -->

**ما هو.** الأصل المسلَّم.

**مثال.** حاسوب محمول — Dell 5540 (SN 7GH21)

**لماذا يهم.** لا يجوز تخصيصه لأي شخص آخر لفترة متداخلة.

**القواعد.** إلزامي. يُختار من قائمة العهد.

:::admin
تُدار من البيانات الأساسية العامة ← العهد.
:::

## AssetDeductionMode {#EmployeeAsset.AssetDeductionMode}
<!-- alias: loc:AssetDeductionMode, dom:EmployeeAsset_AssetDeductionMode -->

**ما هو.** هل يدفع الموظف فواتير الأصل، وما مقدارها.

**مثال.** ما يزيد على البدل

**لماذا يهم.** يستخدمه تقرير فواتير الأصول لاحتساب حصة الموظف، ولا يُقتطع منه شيء في الرواتب.

**القواعد.** إلزامي. إحدى القيم: لا شيء، كامل، ما يزيد عن البدل.
