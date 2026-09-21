---
helpId: org.cost-element.edit
lang: ar
title: عناصر التكلفة — إنشاء / تحرير
entity: HCMv3.OrganizationManagement.CostElement
surfaces:
  - angular/src/app/admin/organization-management/cost-elements/create-or-edit-cost-element-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.CostElements.Create]
---

## Overview

بُعد محاسبي إضافي، مثل رمز قسم أو نشاط، يُحدَّد في حسابات الأجور وتعيينات حسابات الموظفين. ويُكتب رمزه في بنود قيود الرواتب إلى جانب الحساب. ولا يمكن ترك النوع فارغاً.

## CostElementType {#CostElement.CostElementTypeId}
<!-- alias: loc:CostElementType, dom:CostElement_CostElementTypeId -->

**ما هو.** نوع البُعد.

**مثال.** القسم

**القواعد.** اختياري. يُختار من قائمة أنواع عناصر التكلفة.
