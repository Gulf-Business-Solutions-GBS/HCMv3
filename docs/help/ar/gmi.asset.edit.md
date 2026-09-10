---
helpId: gmi.asset.edit
lang: ar
title: العهد — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.Asset
surfaces:
  - angular/src/app/admin/general-master-info/assets/create-or-edit-asset-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.Assets.Create]
---

## Overview

عُهد الشركة التي يمكن تسليمها للموظفين -- من مركبات ومعدات. تُسجَّل العهد المسلَّمة للموظف في سجله، ويمكن للموظفين طلب العهد من خلال الخدمة الذاتية، التي لا تعرض إلا العهد المفعّلة.

وتُطبع البيانات الوصفية أدناه في تقرير عهد الموظفين.

## Brand {#Asset.Brand}
<!-- alias: loc:Brand, dom:Asset_Brand -->

**ما هو.** الشركة المصنّعة للعهدة.

**مثال.** `Toyota`

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Model {#Asset.Model}
<!-- alias: loc:Model, dom:Asset_Model -->

**ما هو.** طراز العهدة.

**مثال.** `Land Cruiser`

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Color {#Asset.Color}
<!-- alias: loc:Color, dom:Asset_Color -->

**ما هو.** لون العهدة.

**مثال.** `أبيض`

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## VIN_Number {#Asset.VIN_Number}
<!-- alias: loc:VIN_Number, dom:Asset_VinNumber -->

**ما هو.** رقم تعريف المركبة (رقم الشاصي)، للمركبات.

**مثال.** `JTMHV05J804012345`

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## Serial {#Asset.Serial}
<!-- alias: loc:Serial, dom:Asset_Serial -->

**ما هو.** الرقم التسلسلي من الشركة المصنّعة، للمعدات.

**مثال.** `SN-4471902`

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## PlateNumber {#Asset.PlateNumber}
<!-- alias: loc:PlateNumber, dom:Asset_PlateNumber -->

**ما هو.** رقم لوحة المركبة، للمركبات.

**مثال.** `12/34567`

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## Barcode {#Asset.Barcode}
<!-- alias: loc:Barcode, dom:Asset_Barcode -->

**ما هو.** ملصق العهدة أو الرمز الشريطي الذي تثبّته الشركة عليها.

**مثال.** `AST-000312`

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## Year {#Asset.Year}
<!-- alias: loc:Year, dom:Asset_Year -->

**ما هو.** السنة المرتبطة بالعهدة، وهي عادةً سنة صنع المركبة.

**مثال.** `2022`

**القواعد.** اختياري. رقم صحيح.

## ExpiryDate {#Asset.ExpiryDate}
<!-- alias: loc:ExpiryDate, dom:Asset_ExpiryDate -->

**ما هو.** تاريخ انتهاء أمر ما يخص العهدة، مثل ترخيص المركبة أو ضمان المعدة.

**القواعد.** اختياري. تاريخ.

## Value {#Asset.Value}
<!-- alias: loc:Value, dom:Asset_Value -->

**ما هو.** قيمة العهدة.

**مثال.** `8500`

**القواعد.** اختياري. رقم.

## AssetType {#Asset.AssetTypeId}
<!-- alias: loc:AssetType, dom:Asset_AssetTypeId -->

**ما هو.** نوع العهدة، من قائمة أنواع العهد.

**القواعد.** اختياري. يُختار من قائمة أنواع العهد.

:::admin
تُدار أنواع العهد من البيانات الأساسية العامة ← أنواع العهد.
:::

## Status {#Asset.Status}
<!-- alias: loc:Status, dom:Asset_Status -->

**ما هو.** هل العهدة قيد الاستخدام.

**لماذا يهم.** لا تُعرض إلا العهد المفعّلة عندما يطلب الموظفون عهدة من خلال الخدمة الذاتية.

**القواعد.** إلزامي. إحدى القيم: مفعّل، غير مفعّل.

## AssetAllocationStatus {#Asset.AssetAllocationStatus}
<!-- alias: loc:AssetAllocationStatus, dom:Asset_AssetAllocationStatus -->

**ما هو.** هل العهدة مسلَّمة لأحد حالياً.

**لماذا يهم.** تُضبط يدوياً -- فتسليم العهدة لموظف لا يغيّرها. وتظهر في تقرير العهد.

**القواعد.** إلزامي. إحدى القيم: مشغول، غير مشغول.
