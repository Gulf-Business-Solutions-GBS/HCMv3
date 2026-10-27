---
helpId: gmi.number-series.edit
lang: ar
title: تسلسل الأرقام — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.NumberSeries
surfaces:
  - angular/src/app/admin/general-master-info/number-series/create-or-edit-number-series-modal.component.html
audience: [admin]
permissions: []
---

## Overview

تسلسلات أرقام لرموز الموظفين. يُرقَّم الموظف الجديد الذي يُحفظ دون رمز من تسلسل نوع توظيفه: أعلى رقم مستخدم مع بادئة التسلسل مضافاً إليه واحد، مكتوباً بتنسيق التسلسل ومسبوقاً بالبادئة. وإذا لم يوجد أي تسلسل كانت رموز الموظفين أعلى رقم موجود مضافاً إليه واحد.

## Name {#NumberSeries.Name}
<!-- alias: loc:Name, dom:NumberSeries_Name -->

**ما هو.** اسم تتعرّف به على التسلسل.

**القواعد.** اختياري.

## Start {#NumberSeries.Start}
<!-- alias: loc:Start, dom:NumberSeries_Start -->

**ما هو.** الرقم الذي كان يُفترض أن يبدأ منه التسلسل.

**لماذا يهم.** لا يُقرأ حالياً: يبدأ التسلسل الجديد دائماً من 1.

**القواعد.** اختياري. رقم صحيح.

## Prefix {#NumberSeries.Prefix}
<!-- alias: loc:Prefix, dom:NumberSeries_Prefix -->

**ما هو.** نص يسبق كل رقم.

**مثال.** `EMP-`

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## Formatting {#NumberSeries.Formatting}
<!-- alias: loc:Formatting, dom:NumberSeries_Formatting -->

**ما هو.** النمط الذي تُكتب به الأرقام.

**القواعد.** إلزامي. الحد الأقصى 64 حرفاً.

## EmploymentType {#NumberSeries.EmploymentType}
<!-- alias: loc:EmploymentType, dom:NumberSeries_EmploymentType -->

**ما هو.** الموظفون الذين يرقّمهم التسلسل: نوع توظيف واحد، أو «الكل» لجميع الموظفين.

**مثال.** دائم -- يُرقَّم الموظفون الدائمون `EMP-0001` و`EMP-0002`…، بينما يرقّم تسلسل «مؤقت» الموظفين المؤقتين على حدة.

**لماذا يهم.** يحصل الموظف الجديد الذي يُحفظ دون رمز على الرقم التالي من تسلسل نوع توظيفه، محسوباً على رموز موظفي ذلك النوع. ويتقدم تسلسل «الكل» على غيره ويُحسب على جميع الموظفين. وإذا غُيِّر نوع توظيف الموظف إلى نوع له تسلسل خاص به حصل على رمز جديد منه. وإذا لم يوجد تسلسل مطابق كان الرمز أعلى رقم موجود مضافاً إليه واحد. ولا يجوز إلا تسلسل واحد لكل نوع توظيف، ويجب أن يكون تسلسل «الكل» هو التسلسل الوحيد.

**القواعد.** إلزامي. إحدى القيم: دائم، مؤقت، الكل.
