---
helpId: pa.employee-talent-flag.edit
lang: ar
title: مؤشرات مواهب الموظف — إنشاء / تحرير
entity: HCMv3.SuccessionManagement.EmployeeTalentFlag
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/talent-flags/create-or-edit-employee-talent-flag-modal.component.html
audience: [admin]
permissions: [Pages.SuccessionManagement.EmployeeTalentFlags.Create]
---

## Overview

يحدد الموظف موضع اهتمام من حيث الاستبقاء خلال فترة: مدى احتمال مغادرته، ومدى أثر ذلك، وسببه.

يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

## RiskOfLossId {#EmployeeTalentFlag.RiskOfLossId}
<!-- alias: loc:RiskOfLossId, dom:EmployeeTalentFlag_RiskOfLossId -->

**ما هو.** مدى احتمال مغادرة الموظف.

**مثال.** مرتفع

**القواعد.** إلزامي. يُختار من قائمة مخاطر فقدان الكفاءات.

:::admin
تُدار من إدارة التعاقب الوظيفي.
:::

## ImpactOfLossId {#EmployeeTalentFlag.ImpactOfLossId}
<!-- alias: loc:ImpactOfLossId, dom:EmployeeTalentFlag_ImpactOfLossId -->

**ما هو.** مدى أثر فقدان الموظف.

**مثال.** حرج

**القواعد.** إلزامي. يُختار من قائمة أثر فقدان الكفاءات.

:::admin
تُدار من إدارة التعاقب الوظيفي.
:::

## ReasonForLeavingId {#EmployeeTalentFlag.ReasonForLeavingId}
<!-- alias: loc:ReasonForLeavingId, dom:EmployeeTalentFlag_ReasonForLeavingId -->

**ما هو.** السبب المحتمل للمغادرة.

**مثال.** الأجر

**القواعد.** إلزامي. يُختار من قائمة أسباب ترك العمل.

:::admin
تُدار من إدارة التعاقب الوظيفي.
:::

## TalentPoolId {#EmployeeTalentFlag.TalentPoolId}
<!-- alias: loc:TalentPoolId, dom:EmployeeTalentFlag_TalentPoolId -->

**ما هو.** مجموعة المواهب التي ينتمي إليها الموظف.

**مثال.** قادة المستقبل

**القواعد.** إلزامي. يُختار من قائمة مجموعات المواهب.

:::admin
تُدار من إدارة التعاقب الوظيفي.
:::
