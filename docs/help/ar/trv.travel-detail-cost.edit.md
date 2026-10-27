---
helpId: trv.travel-detail-cost.edit
lang: ar
title: تكاليف تفاصيل السفر — إنشاء / تحرير
entity: HCMv3.TravelManagement.TravelDetailCost
surfaces:
  - angular/src/app/admin/travel-management/travel-detail-costs/create-or-edit-travel-detail-cost-modal.component.html
audience: [admin]
permissions: [Pages.TravelManagement.TravelDetailCosts.Create]
---

## Overview

تكلفة واحدة لمرحلة من الرحلة، مثل تذكرة الطيران أو الفندق. وتُجمع تكاليف الرحلة للمعتمد ولعرض الموظف في الخدمة الذاتية.

## TravelDetail {#TravelDetailCost.TravelDetailId}
<!-- alias: loc:TravelDetail, dom:TravelDetailCost_TravelDetailId -->

**ما هو.** المرحلة.

**القواعد.** إلزامي. يُختار من قائمة تفاصيل السفر.

## ExpenseType {#TravelDetailCost.ExpenseTypeId}
<!-- alias: loc:ExpenseType, dom:TravelDetailCost_ExpenseTypeId -->

**ما هو.** نوع التكلفة.

**مثال.** تذكرة طيران

**القواعد.** إلزامي. يُختار من قائمة أنواع المصاريف.

## Amount {#TravelDetailCost.Amount}
<!-- alias: loc:Amount, dom:TravelDetailCost_Amount -->

**ما هو.** المبلغ.

**مثال.** 180.000

**القواعد.** إلزامي. رقم.
