---
helpId: pay.employee-sub-group.edit
lang: ar
title: المجموعات الفرعية للموظفين — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeSubGroup
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-groups/create-or-edit-employee-sub-group-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroups.Create]
---

## Overview

تصنيف أدق للموظفين تحت مجموعة موظفين، وهو المكان الذي تتعلق به معظم قواعد المزايا: الإجازات ومكافأة نهاية الخدمة وتذاكر الطيران واسترداد المصاريف الصحية والمكافآت وغيرها من الاستحقاقات، وكلها تُضبط لكل مجموعة فرعية في تبويباتها. ويحدد التعيين التنظيمي لكل موظف مجموعته الفرعية.

والحقول في هذا النموذج هي الإعدادات العامة للمجموعة الفرعية الخاصة بالعمل الإضافي وتذاكر الطيران واسترداد المصاريف الصحية.

## EmployeeGroup {#EmployeeSubGroup.EmployeeGroupId}
<!-- alias: loc:EmployeeGroup, dom:EmployeeSubGroup_EmployeeGroupId -->

**ما هو.** مجموعة الموظفين التي تتبعها هذه المجموعة الفرعية.

**مثال.** الموظفون الإداريون

**لماذا يهم.** تُجمِّع المجموعات الفرعية، ويمكن تصفية قائمة المجموعات الفرعية بها.

**القواعد.** إلزامي. يُختار من قائمة.

## TicketClass {#EmployeeSubGroup.TicketClassId}
<!-- alias: loc:TicketClass, dom:EmployeeSubGroup_TicketClassId -->

**ما هو.** درجة السفر الجوي التي تستحقها المجموعة الفرعية.

**مثال.** الدرجة السياحية

**لماذا يهم.** إذا كان نوع دفع تذكرة الطيران *FareAmount*، تُسعَّر التذكرة من أجرة هذه الدرجة على مسار تذكرة الموظف. وإذا تُركت فارغة كانت قيمة التذكرة المسعَّرة بالأجرة صفراً.

**القواعد.** اختياري. يُختار من قائمة.

## PayOvertime {#EmployeeSubGroup.PayOvertime}
<!-- alias: loc:PayOvertime, dom:EmployeeSubGroup_PayOvertime -->

**ما هو.** ما إذا كان موظفو هذه المجموعة الفرعية يتقاضون أجر العمل الإضافي.

**لماذا يهم.** إذا أُشِّر عليها يحوّل محرك الوقت العمل الإضافي الذي يجده في يوم ما إلى سجل عمل إضافي للموظف، ثم يُعتمد ويُصرف. وإذا لم يُؤشَّر عليها لا يُنشأ سجل عمل إضافي، فلا يُصرف أي عمل إضافي مهما طالت ساعات العمل.

**القواعد.** نعم أو لا.

:::admin
عند التأشير عليها يحتاج اليوم الذي فيه عمل إضافي إلى نوع عمل إضافي -- يُحدَّد في الدوام، أو في العطلة إذا كان اليوم عطلة رسمية. وبدونه يُظهر محرك الوقت خطأً لذلك اليوم يطلب تحديده.
:::

## AirTicketPayType {#EmployeeSubGroup.AirTicketPayType}
<!-- alias: loc:AirTicketPayType, dom:EmployeeSubGroup_AirTicketPayType -->

**ما هو.** طريقة احتساب قيمة تذكرة الطيران الواحدة لهذه المجموعة الفرعية.

**مثال.** مبلغ ثابت

**لماذا يهم.** *FareAmount* يسعّر التذكرة من جدول الأجور بحسب درجة التذكرة ومسار الموظف. و*مبلغ ثابت* يستخدم المبلغ الثابت في قاعدة التذاكر للمجموعة الفرعية. و*المبلغ الفعلي* يقسم ما دُفع فعلاً على عدد التذاكر، ويعود إلى الحد الأقصى في القاعدة إذا لم يُدفع شيء. و*ActualAmountWithinMaxlimit* يجري القسمة نفسها دون أن يتجاوز الحد الأقصى في القاعدة. وبهذه الطريقة يُسعَّر نوع أجر متوسط التذكرة.

**القواعد.** إلزامي. إحدى القيم: FareAmount، مبلغ ثابت، المبلغ الفعلي، ActualAmountWithinMaxlimit.

## AutoPayAirTicket {#EmployeeSubGroup.AutoPayAirTicket}
<!-- alias: loc:AutoPayAirTicket, dom:EmployeeSubGroup_AutoPayAirTicket -->

**ما هو.** ما إذا كانت تذاكر الطيران تُصرف تلقائياً عند استحقاقها.

**لماذا يهم.** إذا أُشِّر عليها يضيف محرك الوقت تذكرة للموظف لكل نوع تذكرة في أول يوم من فترة استحقاقها -- تذكرة الموظف نفسه وتذاكر العائلين -- دون أن يُدخلها أحد. ويُراعى استثناء المزايا للموظف أو للعائل، والاستثناء بمبلغ صفر يعني عدم الاستحقاق.

**القواعد.** نعم أو لا.

:::admin
نوع التذكرة الذي ليس له قاعدة تذاكر في المجموعة الفرعية لا يُصرف تلقائياً إلا إذا كان للموظف استثناء له.
:::

## MaxDependantAirTicketCount {#EmployeeSubGroup.MaxDependantAirTicketCount}
<!-- alias: loc:MaxDependantAirTicketCount, dom:EmployeeSubGroup_MaxDependantAirTicketCount -->

**ما هو.** أكبر عدد من تذاكر العائلين يُصرف تلقائياً في المرة الواحدة.

**مثال.** `3`

**لماذا يهم.** عندما يبلغ الصرف التلقائي لتذاكر الطيران هذا العدد من تذاكر العائلين يتوقف عن إضافة المزيد. والقيمة 0 تعني بلا حد.

**القواعد.** اختياري. رقم صحيح.

## MaxHealthReimburseAmount {#EmployeeSubGroup.MaxHealthReimburseAmount}
<!-- alias: loc:MaxHealthReimburseAmount, dom:EmployeeSubGroup_MaxHealthReimburseAmount -->

**ما هو.** أقصى ما يمكن أن يُسترد لموظف في هذه المجموعة الفرعية من المصاريف الصحية في سنة التأمين الصحي الواحدة.

**مثال.** `500`

**لماذا يهم.** يحدّ رصيد استرداد المصاريف الصحية للموظف، بعد إضافة أي رصيد مُرحَّل من السنة السابقة. والقيمة 0 تعني بلا حد.

**القواعد.** اختياري. رقم.

## HealthInsurancePeriod {#EmployeeSubGroup.HealthInsurancePeriod}
<!-- alias: loc:HealthInsurancePeriod, dom:EmployeeSubGroup_HealthInsurancePeriod -->

**ما هو.** الشهر الذي تبدأ فيه سنة التأمين الصحي.

**مثال.** يناير

**لماذا يهم.** يُعاد رصيد الاسترداد كل سنة من أول يوم في هذا الشهر. واختيار *لا شيء* يجعل سنة كل موظف تبدأ من تاريخ التحاقه بدلاً من ذلك.

**القواعد.** إلزامي. إحدى القيم: لا شيء، يناير، فبراير، مارس، أبريل، مايو، يونيو، يوليو، أغسطس، سبتمبر، أكتوبر، نوفمبر، ديسمبر.

## MaxDependantHealthReimburseCount {#EmployeeSubGroup.MaxDependantHealthReimburseCount}
<!-- alias: loc:MaxDependantHealthReimburseCount, dom:EmployeeSubGroup_MaxDependantHealthReimburseCount -->

**ما هو.** أكبر عدد من العائلين الذين يُحتسب استحقاقهم في استرداد المصاريف الصحية ضمن مبلغ الموظف.

**مثال.** `4`

**لماذا يهم.** يجمع المبلغ المستحق استحقاقات المجموعة الفرعية لكل عائل من عائلي الموظف، حتى هذا العدد. والقيمة 0 تعني بلا حد.

**القواعد.** اختياري. رقم صحيح.
