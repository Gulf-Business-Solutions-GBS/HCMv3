---
helpId: pa.employee-action.edit
lang: ar
title: إجراءات الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeAction
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/actions/create-or-edit-employee-action-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeActions.Create]
---

## Overview

إجراء شخصي للموظف -- ترقية أو نقل أو إعادة تصنيف أو إنهاء خدمة -- مع سببه وتاريخه.

يمكن توجيه الإجراءات الشخصية للاعتماد؛ ويُنشئها قسم الموارد البشرية لا الموظف. ولا يجوز تسجيل الإجراء نفسه مرتين في اليوم نفسه للموظف نفسه.

## PersonalAction {#EmployeeAction.PersonalActionId}
<!-- alias: loc:PersonalAction, dom:EmployeeAction_PersonalActionId -->

**ما هو.** نوع الإجراء.

**مثال.** ترقية

**القواعد.** إلزامي. يُختار من قائمة الإجراءات الشخصية.

:::admin
تُدار من البيانات الأساسية العامة ← الإجراءات الشخصية. ولا تُطبَّق خيارات المتطلبات فيها هنا حالياً.
:::

## ActionReason {#EmployeeAction.ActionReasonId}
<!-- alias: loc:ActionReason, dom:EmployeeAction_ActionReasonId -->

**ما هو.** سبب اتخاذ الإجراء.

**مثال.** تقييم الأداء السنوي

**القواعد.** إلزامي. يُختار من قائمة أسباب الإجراءات الشخصية.

:::admin
تُدار من البيانات الأساسية العامة ← أسباب الإجراءات الشخصية.
:::

## RecommendedBy {#EmployeeAction.RecommendedBy}
<!-- alias: loc:RecommendedBy, dom:EmployeeAction_RecommendedBy -->

**ما هو.** من أوصى بالإجراء، كنص حر.

**مثال.** رئيس قسم المالية

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.
