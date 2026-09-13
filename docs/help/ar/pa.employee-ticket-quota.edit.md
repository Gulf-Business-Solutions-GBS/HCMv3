---
helpId: pa.employee-ticket-quota.edit
lang: ar
title: حصص تذاكر الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeTicketQuota
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/ticket-quotas/create-or-edit-employee-ticket-quota-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeTicketQuotas.Create]
---

## Overview

استحقاق الموظف من تذاكر الطيران لنوع تذكرة واحد خلال فترة استحقاق.

يظهر في تقرير رصيد حصص تذاكر الطيران، ورصيده هو المتراكم مطروحاً منه المستخدم والمعدَّل. ويحسب محرك الوقت استحقاق التذاكر من قواعد التذاكر في المجموعة الفرعية والتذاكر المصروفة، ولا يقرأ هذه الحصص حالياً.

## TicketTypeId {#EmployeeTicketQuota.TicketTypeId}
<!-- alias: loc:TicketTypeId, dom:EmployeeTicketQuota_TicketTypeId -->

**ما هو.** نوع التذكرة الذي تخصه هذه الحصة.

**مثال.** الموظف

**القواعد.** إلزامي. يُختار من قائمة أنواع التذاكر.

## EntitlementFrom {#EmployeeTicketQuota.EntitlementFrom}
<!-- alias: loc:EntitlementFrom, dom:EmployeeTicketQuota_EntitlementFrom -->

**ما هو.** أول يوم في فترة الاستحقاق.

**مثال.** 1 يناير 2026

**لماذا يهم.** يستخدم التقرير الحصة التي تغطي تاريخه.

**القواعد.** إلزامي. تاريخ.

## EntitlementTo {#EmployeeTicketQuota.EntitlementTo}
<!-- alias: loc:EntitlementTo, dom:EmployeeTicketQuota_EntitlementTo -->

**ما هو.** آخر يوم في فترة الاستحقاق.

**مثال.** 31 ديسمبر 2027

**القواعد.** إلزامي. تاريخ.

## DeductionFrom {#EmployeeTicketQuota.DeductionFrom}
<!-- alias: loc:DeductionFrom, dom:EmployeeTicketQuota_DeductionFrom -->

**ما هو.** أول يوم يمكن فيه أخذ تذاكر من هذه الحصة.

**مثال.** 1 يناير 2026

**القواعد.** إلزامي. تاريخ.

## DeductionTo {#EmployeeTicketQuota.DeductionTo}
<!-- alias: loc:DeductionTo, dom:EmployeeTicketQuota_DeductionTo -->

**ما هو.** آخر يوم يمكن فيه أخذ تذاكر من هذه الحصة.

**مثال.** 31 ديسمبر 2027

**القواعد.** إلزامي. تاريخ.

## QuotaAccrued {#EmployeeTicketQuota.QuotaAccrued}
<!-- alias: loc:QuotaAccrued, dom:EmployeeTicketQuota_QuotaAccrued -->

**ما هو.** التذاكر المكتسبة في الفترة.

**مثال.** 1

**القواعد.** اختياري. رقم.

## QuotaUsed {#EmployeeTicketQuota.QuotaUsed}
<!-- alias: loc:QuotaUsed, dom:EmployeeTicketQuota_QuotaUsed -->

**ما هو.** التذاكر المأخوذة في الفترة.

**مثال.** 0

**القواعد.** اختياري. رقم.

## QuotaAdjusted {#EmployeeTicketQuota.QuotaAdjusted}
<!-- alias: loc:QuotaAdjusted, dom:EmployeeTicketQuota_QuotaAdjusted -->

**ما هو.** التذاكر المعدَّلة في الفترة.

**مثال.** 0

**القواعد.** اختياري. رقم.
