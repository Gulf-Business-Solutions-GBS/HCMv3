---
helpId: pa.employee-org-assignment.edit
lang: ar
title: التعيينات التنظيمية للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeOrgAssignment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/org-assignments/create-or-edit-employee-org-assignment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeOrgAssignments.Create]
---

## Overview

موقع الموظف في الهيكل التنظيمي اعتباراً من تاريخ معين: القسم والمسمى الوظيفي ومركز التكلفة ومكان العمل.

تشكّل التعيينات سجلاً تاريخياً. فعند حفظ تعيين جديد يُغلق التعيين السابق للموظف في اليوم السابق لتاريخ البداية الجديد، فيكون هناك دائماً تعيين واحد ساري. وإذا تُرك حقل فارغاً في تعيين جديد، يُنسخ من التعيين السابق.

يقرأ سير اعتماد الطلبات ومحرك الوقت والتقارير وقواعد العمل التعيينَ الساري في التاريخ الذي تحتاجه.

## JobTitle {#EmployeeOrgAssignment.JobTitleId}
<!-- alias: loc:JobTitle, dom:EmployeeOrgAssignment_JobTitleId -->

**ما هو.** المسمى الوظيفي للموظف اعتباراً من هذا التاريخ.

**مثال.** محاسب أول

**لماذا يهم.** يظهر في التقارير ويمكن لقواعد العمل استخدامه. وإذا تُرك فارغاً في تعيين جديد يُنسخ من التعيين السابق.

**القواعد.** اختياري. يُختار من قائمة المسميات الوظيفية.

:::admin
يُدار من إدارة الهيكل التنظيمي ← المسميات الوظيفية.
:::

## CostCenter {#EmployeeOrgAssignment.CostCenterId}
<!-- alias: loc:CostCenter, dom:EmployeeOrgAssignment_CostCenterId -->

**ما هو.** مركز التكلفة الذي تُحمَّل عليه تكلفة الموظف اعتباراً من هذا التاريخ.

**مثال.** CC-100 المالية

**لماذا يهم.** تُوجَّه خطوات الاعتماد المسندة إلى دور مركز التكلفة إلى معتمِد هذا المركز. وإذا تُرك فارغاً في تعيين جديد يُنسخ من التعيين السابق.

**القواعد.** اختياري. يُختار من قائمة مراكز التكلفة.

## WorkLocation {#EmployeeOrgAssignment.WorkLocationId}
<!-- alias: loc:WorkLocation, dom:EmployeeOrgAssignment_WorkLocationId -->

**ما هو.** مكان العمل المعتاد للموظف اعتباراً من هذا التاريخ.

**مثال.** المكتب الرئيسي

**لماذا يهم.** تُوجَّه خطوات الاعتماد المسندة إلى دور الفرع إلى معتمِد مكان العمل هذا. وإذا تُرك فارغاً في تعيين جديد يُنسخ من التعيين السابق.

**القواعد.** اختياري. يُختار من قائمة أماكن العمل.

## PostingGroup {#EmployeeOrgAssignment.PostingGroupId}
<!-- alias: loc:PostingGroup, dom:EmployeeOrgAssignment_PostingGroupId -->

**ما هو.** مجموعة القيود المحاسبية المسجلة للموظف اعتباراً من هذا التاريخ.

**لماذا يهم.** تُحفظ للرجوع إليها، ولا يقرؤها أي احتساب أو قيد من التعيين حالياً.

**القواعد.** اختياري. يُختار من قائمة مجموعات القيود المحاسبية.

## BudgetingGroup {#EmployeeOrgAssignment.BudgetingGroupId}
<!-- alias: loc:BudgetingGroup, dom:EmployeeOrgAssignment_BudgetingGroupId -->

**ما هو.** مجموعة الموازنة المسجلة للموظف اعتباراً من هذا التاريخ.

**لماذا يهم.** تُحفظ للرجوع إليها، ولا يقرؤها أي احتساب من التعيين حالياً.

**القواعد.** اختياري. يُختار من قائمة مجموعات الموازنة.

## OrganizationUnit {#EmployeeOrgAssignment.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:EmployeeOrgAssignment_OrganizationUnitId -->

**ما هو.** القسم أو الوحدة التي ينتمي إليها الموظف اعتباراً من هذا التاريخ، ويُختار من الشجرة التنظيمية.

**مثال.** المالية › الحسابات الدائنة

**لماذا يهم.** يحدد موقع الموظف في الهيكل التنظيمي، وتُوجَّه خطوات الاعتماد المسندة إلى دور القسم إلى معتمِد هذا القسم. وتُقرَّب مبالغ رواتب الموظف إلى عدد المنازل العشرية لعملة منطقة الرواتب المحددة لهذه الوحدة.

**القواعد.** اختياري. يُختار من قائمة الوحدات التنظيمية.

## EmployeeGroup {#EmployeeOrgAssignment.EmployeeGroupId}
<!-- alias: loc:EmployeeGroup, dom:EmployeeOrgAssignment_EmployeeGroupId -->

**ما هو.** ملف راتب الموظف خلال تواريخ هذا التعيين.

**مثال.** الموظفون الإداريون

**لماذا يهم.** تؤخذ منه قواعد مكافأة نهاية الخدمة والتسوية التي تعتمد على ملف الراتب. وإذا تُرك فارغاً تبقى القيمة الموجودة في التعيين السابق. وإذا اختيرت الدرجة وحدها، يُعبَّأ ملف الراتب منها.

**القواعد.** اختياري. يُختار من قائمة.

## EmployeeSubGroup {#EmployeeOrgAssignment.EmployeeSubGroupId}
<!-- alias: loc:EmployeeSubGroup, dom:EmployeeOrgAssignment_EmployeeSubGroupId -->

**ما هو.** درجة الموظف خلال تواريخ هذا التعيين.

**مثال.** الدرجة 5

**لماذا يهم.** تعتمد معظم الاستحقاقات على الدرجة: أرصدة الإجازات، وتذاكر الطيران، ومكافأة نهاية الخدمة، واقتطاعات الصندوق، ومكافأة الخدمة، واسترداد المصاريف الصحية. ويقرأ محرك الوقت والرواتب الدرجة من التعيين الذي يغطي كل تاريخ. ويجب أن تتبع الدرجة ملف الراتب المختار؛ وإذا تُركت فارغة تبقى درجة التعيين السابق.

**القواعد.** اختياري. يُختار من قائمة.

## PayrollArea {#EmployeeOrgAssignment.PayrollAreaId}
<!-- alias: loc:PayrollArea, dom:EmployeeOrgAssignment_PayrollAreaId -->

**ما هو.** منطقة الرواتب التي يُصرف فيها راتب الموظف خلال تواريخ هذا التعيين.

**مثال.** الكويت - شهري

**لماذا يهم.** تحدد أي تشغيل للرواتب يشمل الموظف، وأي تقويم رواتب وإقفال ينطبقان عليه. وإذا تُركت فارغة تبقى منطقة التعيين السابق، وإلا فالمنطقة المحددة للوحدة التنظيمية.

**القواعد.** اختياري. يُختار من قائمة مناطق الرواتب.

## WorkflowGroup {#EmployeeOrgAssignment.WorkflowGroupId}
<!-- alias: loc:WorkflowGroup, dom:EmployeeOrgAssignment_WorkflowGroupId -->

**ما هو.** مجموعة سير العمل التي تتبع طلبات الموظف سلاسل اعتمادها.

**مثال.** المكتب الرئيسي

**لماذا يهم.** عندما يقدّم الموظف طلباً، تُختار السلسلة من مجموعة سير العمل في تعيين اليوم. وإذا تُركت فارغة تُستخدم مجموعة سير العمل المحددة كافتراضية.

**القواعد.** اختياري. يُختار من قائمة مجموعات سير العمل.
