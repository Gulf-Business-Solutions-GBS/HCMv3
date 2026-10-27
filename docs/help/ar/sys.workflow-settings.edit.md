---
helpId: sys.workflow-settings.edit
lang: ar
title: Workflow settings
entity: HCMv3.SystemSettings.ManageWorkflowSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-workflow-settings/create-or-edit-manage-workflow-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageWorkflowSettings]
---

## Overview

إعدادات سير عمل الموافقات.

لا يُقرأ حالياً إلا **تفعيل مجموعة سير العمل**، أما البقية فلا أثر لها.

لكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## EnableWorkflowGroup {#ManageWorkflowSettings.EnableWorkflowGroup}
<!-- alias: loc:EnableWorkflowGroup, dom:WorkflowSettings_EnableWorkflowGroup -->

**ما هو.** ما إذا كانت سلاسل الموافقة تعتمد على مجموعة سير العمل للموظف.

**لماذا يهم.** إذا أُشِّر عليه يُبحث عن إعداد الموافقة للطلب في مجموعة سير العمل في التعيين التنظيمي الحالي للموظف، مع الرجوع إلى المجموعة الافتراضية. وإلا فيحصل كل الموظفين على سلسلة الموافقة نفسها لنوع الطلب.

**القواعد.** نعم أو لا.

## EnableOrgUnitTypeInWorkflow {#ManageWorkflowSettings.EnableOrgUnitTypeInWorkflow}
<!-- alias: loc:EnableOrgUnitTypeInWorkflow, dom:WorkflowSettings_EnableOrgUnitTypeInWorkflow -->

**ما هو.** ما إذا كان سير العمل يستخدم أنواع الوحدات التنظيمية.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## HideLeavePayDetailsInWorkflow {#ManageWorkflowSettings.HideLeavePayDetailsInWorkflow}
<!-- alias: loc:HideLeavePayDetailsInWorkflow, dom:WorkflowSettings_HideLeavePayDetailsInWorkflow -->

**ما هو.** ما إذا كانت تفاصيل أجر الإجازة مخفية عن المعتمدين.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## DynamicWorkflowSteps {#ManageWorkflowSettings.DynamicWorkflowSteps}
<!-- alias: loc:DynamicWorkflowSteps, dom:WorkflowSettings_DynamicWorkflowSteps -->

**ما هو.** ما إذا كانت خطوات الموافقة تُبنى ديناميكياً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## SendBackToCreator {#ManageWorkflowSettings.SendBackToCreator}
<!-- alias: loc:SendBackToCreator, dom:WorkflowSettings_SendBackToCreator -->

**ما هو.** ما إذا كان يمكن إعادة الطلب إلى منشئه.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## DelegateEmployeeListByOrganizationUnitTypeId {#ManageWorkflowSettings.DelegateEmployeeListByOrganizationUnitTypeId}
<!-- alias: loc:DelegateEmployeeListByOrganizationUnitTypeId, dom:WorkflowSettings_DelegateEmployeeListByOrganizationUnitTypeId -->

**ما هو.** نوع الوحدة التنظيمية الذي يحدد من يمكن التفويض إليه.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. يُختار من قائمة.

## EnableTravelCostOnWorkflowstep {#ManageWorkflowSettings.EnableTravelCostOnWorkflowstep}
<!-- alias: loc:EnableTravelCostOnWorkflowstep, dom:WorkflowSettings_EnableTravelCostOnWorkflowstep -->

**ما هو.** ما إذا كانت تكلفة السفر تُعرض في خطوات سير العمل.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## OvertimeRequestModificationStep {#ManageWorkflowSettings.OvertimeRequestModificationStep}
<!-- alias: loc:OvertimeRequestModificationStep, dom:WorkflowSettings_OvertimeRequestModificationStep -->

**ما هو.** خطوة سير العمل التي يمكن فيها تعديل طلب العمل الإضافي.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.
