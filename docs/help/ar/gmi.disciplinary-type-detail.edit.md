---
helpId: gmi.disciplinary-type-detail.edit
lang: ar
title: تفاصيل أنواع المخالفات — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.DisciplinaryTypeDetail
surfaces:
  - angular/src/app/admin/general-master-info/disciplinary-type-details/create-or-edit-disciplinary-type-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.DisciplinaryTypeDetails.Create]
---

## Overview

درجة واحدة من سُلّم التصعيد لنوع المخالفة: ابتداءً من أي مخالفة ينطبق نوع خصم معيّن.

عند تسجيل مخالفة، يعدّ HCMv3 المخالفات السابقة للموظف من النوع نفسه ضمن مدة التكرار الخاصة بالنوع، مستبعداً ما رُفض منها، ثم يضيف واحداً للمخالفة الجديدة. والدرجة المستخدمة هي صاحبة أعلى عدد مخالفات لا يتجاوز ذلك الرقم. فمع درجات عند 1 و3 و5، تستخدم المخالفتان الأولى والثانية الدرجة 1، والثالثة والرابعة الدرجة 3، وكل مخالفة من الخامسة فصاعداً الدرجة 5.

## DisciplinaryType {#DisciplinaryTypeDetail.DisciplinaryTypeId}
<!-- alias: loc:DisciplinaryType, dom:DisciplinaryTypeDetail_DisciplinaryTypeId -->

**ما هو.** نوع المخالفة الذي تنتمي إليه هذه الدرجة.

**القواعد.** اختياري. يُختار من قائمة أنواع المخالفات.

## DisciplinaryCount {#DisciplinaryTypeDetail.DisciplinaryCount}
<!-- alias: loc:DisciplinaryCount, dom:DisciplinaryTypeDetail_DisciplinaryCount -->

**ما هو.** رقم المخالفة الذي تنطبق منه هذه الدرجة.

**مثال.** `3`، من المخالفة الثالثة فصاعداً

**القواعد.** إلزامي. رقم صحيح.

## DisciplinaryDeductionType {#DisciplinaryTypeDetail.DisciplinaryDeductionTypeId}
<!-- alias: loc:DisciplinaryDeductionType, dom:DisciplinaryTypeDetail_DisciplinaryDeductionTypeId -->

**ما هو.** الخصم الذي ينطبق من هذه المخالفة فصاعداً.

**القواعد.** اختياري. يُختار من قائمة أنواع خصم المخالفات.
