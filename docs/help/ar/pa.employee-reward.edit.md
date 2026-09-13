---
helpId: pa.employee-reward.edit
lang: ar
title: حوافز الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeReward
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/rewards/create-or-edit-employee-reward-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeRewards.Create]
---

## Overview

جائزة أو تكريم مُنح للموظف.

يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

## RewardType {#EmployeeReward.RewardType}
<!-- alias: loc:RewardType, dom:EmployeeReward_RewardType -->

**ما هو.** نوع الجائزة، كنص حر.

**مثال.** موظف الشهر

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## RewardDate {#EmployeeReward.RewardDate}
<!-- alias: loc:RewardDate, dom:EmployeeReward_RewardDate -->

**ما هو.** تاريخ منح الجائزة.

**مثال.** 30 يونيو 2026

**القواعد.** إلزامي. تاريخ.

## Certificate {#EmployeeReward.Certificate}
<!-- alias: loc:Certificate, dom:EmployeeReward_Certificate -->

**ما هو.** الشهادة الممنوحة مع الجائزة.

**مثال.** شهادة تميز

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.
