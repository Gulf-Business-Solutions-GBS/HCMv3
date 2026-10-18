---
helpId: gmi.loan-type.edit
lang: ar
title: أنواع القروض — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.LoanType
surfaces:
  - angular/src/app/admin/general-master-info/loan-types/create-or-edit-loan-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.LoanTypes.Create]
---

## Overview

أنواع القروض التي يمكن للموظفين الحصول عليها. يسمّي كل قرض للموظف نوعاً من هذه الأنواع، ويسترد احتساب الرواتب الأقساط وفق نوع الخصم وخيار الاسترداد الخاصين بالنوع.

إعدادات الأهلية محفوظة لكنها غير مطبّقة حالياً: فلا يُرفض القرض إذا تجاوزها.

## LoanDeductionType {#LoanType.LoanDeductionType}
<!-- alias: loc:LoanDeductionType, dom:LoanType_LoanDeductionType -->

**ما هو.** طريقة احتساب كل قسط.

**لماذا يهم.** مع «ثابت»، يخصم احتساب الرواتب مبلغ القسط المحدد في قرض الموظف، ولا يتجاوز أبداً الرصيد المتبقي.

**القواعد.** إلزامي. إحدى القيم: ثابت، متغير.

:::admin
لا تستخدم «متغير» بعدُ. فاحتساب الرواتب لا يدعمه، ويتوقف بخطأ لأي موظف لديه قرض من نوع متغير بدلاً من ألا يخصم شيئاً.
:::

## RecoveryOption {#LoanType.RecoveryOption}
<!-- alias: loc:RecoveryOption, dom:LoanType_RecoveryOption -->

**ما هو.** طريقة سداد القرض.

**لماذا يهم.** يسترد احتساب الرواتب القروض التي نوعها «عن طريق الرواتب» أو «الجميع». أما مع «عن طريق جهة خارجية» فلا يخصمها احتساب الرواتب، ويجري السداد خارج HCMv3.

**القواعد.** إلزامي. إحدى القيم: الجميع، عن طريق الرواتب، عن طريق جهة خارجية.

## WageType {#LoanType.WageTypeId}
<!-- alias: loc:WageType, dom:LoanType_WageTypeId -->

**ما هو.** نوع أجر الخصم الذي تسترد عليه الرواتب أقساط القروض من هذا النوع.

**مثال.** 2001 - خصم استرداد القرض

**لماذا يهم.** لا تسترد الرواتب القرض إلا عن طريق نوع الأجر هذا: ففي كل شهر تُضاف الأقساط المستحقة على قروض الموظف من هذا النوع إلى هذا السطر من كشف الراتب. وإذا تُرك فارغاً فلا تُخصم قروض هذا النوع أبداً. ويجوز أن تشترك عدة أنواع قروض في نوع أجر واحد، فتُجمع أقساطها معاً. ولا تعرض القائمة إلا أنواع أجور الخصم الفعّالة التي ليست من الراتب الأساسي أو الأجور المحتسبة أو الفروقات، وغير المستخدمة لنوع إجازة أو عمل إضافي أو مخالفة.

**القواعد.** اختياري. يُختار من قائمة أنواع الأجور.

## EligiblePeriod {#LoanType.EligiblePeriod}
<!-- alias: loc:EligiblePeriod, dom:LoanType_EligiblePeriod -->

**ما هو.** مدة الخدمة التي يحتاجها الموظف قبل أن يصبح مؤهلاً لهذا القرض.

**لماذا يهم.** غير مطبّق حالياً.

**القواعد.** إلزامي. رقم صحيح.

## EligibleCountPerYear {#LoanType.EligibleCountPerYear}
<!-- alias: loc:EligibleCountPerYear, dom:LoanType_EligibleCountPerYear -->

**ما هو.** عدد القروض من هذا النوع التي يجوز للموظف الحصول عليها في السنة.

**لماذا يهم.** غير مطبّق حالياً.

**القواعد.** إلزامي. رقم صحيح.

## MaxInstallments {#LoanType.MaxInstallments}
<!-- alias: loc:MaxInstallments, dom:LoanType_MaxInstallments -->

**ما هو.** الحد الأقصى لعدد الأقساط التي يُسدَّد عليها قرض من هذا النوع.

**لماذا يهم.** غير مطبّق حالياً.

**القواعد.** إلزامي. رقم صحيح.

## EligibleAmount {#LoanType.EligibleAmount}
<!-- alias: loc:EligibleAmount, dom:LoanType_EligibleAmount -->

**ما هو.** الأساس الذي يُحتسب عليه المبلغ المؤهَّل: مبلغ افتراضي، أو الراتب الأساسي للموظف.

**لماذا يهم.** غير مطبّق حالياً.

**القواعد.** إلزامي. إحدى القيم: افتراضي، الراتب الأساسي.

## EligibleAmountNumber {#LoanType.EligibleAmountNumber}
<!-- alias: loc:EligibleAmountNumber, dom:LoanType_EligibleAmountNumber -->

**ما هو.** الرقم المرافق للأساس المذكور أعلاه.

**لماذا يهم.** غير مطبّق حالياً.

**القواعد.** إلزامي. رقم.
