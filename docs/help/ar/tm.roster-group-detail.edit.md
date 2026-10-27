---
helpId: tm.roster-group-detail.edit
lang: ar
title: تفاصيل مجموعات الجداول — إنشاء / تحرير
entity: HCMv3.TimeManagement.RosterGroupDetail
surfaces:
  - angular/src/app/admin/time-management/roster-group-details/create-or-edit-roster-group-detail-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.RosterGroupDetails.Create]
---

## Overview

عضوية الموظف في مجموعة مناوبة لفترة. ويتبع الموظف جدول المجموعة في التواريخ المشمولة، ما لم تكن له فترة دوام خاصة أو دوام بديل.

## RosterGroup {#RosterGroupDetail.RosterGroupId}
<!-- alias: loc:RosterGroup, dom:RosterGroupDetail_RosterGroupId -->

**ما هو.** مجموعة المناوبة.

**مثال.** الفريق أ

**القواعد.** إلزامي. يُختار من قائمة مجموعات الجداول.

## Employee {#RosterGroupDetail.EmployeeId}
<!-- alias: loc:Employee, dom:RosterGroupDetail_EmployeeId -->

**ما هو.** الموظف.

**لماذا يهم.** لا يجوز أن تتداخل فترات الموظف في مجموعة المناوبة نفسها.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.
