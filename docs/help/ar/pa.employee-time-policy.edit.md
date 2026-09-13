---
helpId: pa.employee-time-policy.edit
lang: ar
title: سياسات وقت الموظف — إنشاء / تحرير
entity: HCMv3.TimeManagement.EmployeeTimePolicy
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/time-policies/create-or-edit-employee-time-policy-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.EmployeeTimePolicies.Create]
---

## Overview

سياسة الوقت التي تحكم حضور الموظف اعتباراً من تاريخ معين. عند حفظ سياسة جديدة تُغلق السابقة في اليوم السابق لتاريخ البداية الجديد.

يطبّق محرك الوقت السياسة السارية في كل يوم، ويتوقف برسالة «Employee TimePolicy not found» في أي يوم لا تغطيه سياسة. ويقرأ احتساب الأيام في الرواتب حالة إدارة الوقت والسماحية الشهرية في السياسة.

## TimePolicy {#EmployeeTimePolicy.TimePolicyId}
<!-- alias: loc:TimePolicy, dom:EmployeeTimePolicy_TimePolicyId -->

**ما هو.** سياسة الوقت المخصصة اعتباراً من هذا التاريخ.

**مثال.** موظفو المكاتب — البصمة

**القواعد.** إلزامي. يُختار من قائمة سياسات الوقت.

:::admin
تُدار من إدارة الوقت ← سياسات الوقت.
:::
