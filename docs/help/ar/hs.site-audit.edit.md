---
helpId: hs.site-audit.edit
lang: ar
title: تدقيق المواقع — إنشاء / تحرير
entity: HCMv3.HealthAndSafetyManagement.SiteAudit
surfaces:
  - angular/src/app/admin/health-and-safety-management/site-audits/create-or-edit-site-audit-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.SiteAudits.Create]
---

## Overview

تفتيش سلامة على موقع. وتُدرج ملاحظاته كتفاصيل.

## AuditDate {#SiteAudit.AuditDate}
<!-- alias: loc:AuditDate, dom:SiteAudit_AuditDate -->

**ما هو.** متى جرى التدقيق.

**القواعد.** إلزامي. تاريخ.

## AuditTime {#SiteAudit.AuditTime}
<!-- alias: loc:AuditTime, dom:SiteAudit_AuditTime -->

**ما هو.** الوقت من اليوم، إن كان معروفاً.

**مثال.** 14:30

**القواعد.** اختياري. تاريخ.

## ReassessmentDate {#SiteAudit.ReassessmentDate}
<!-- alias: loc:ReassessmentDate, dom:SiteAudit_ReassessmentDate -->

**ما هو.** متى يجب تدقيق الموقع مرة أخرى.

**لماذا يهم.** للتسجيل فقط: لا يُرسل تذكير عند حلوله.

**القواعد.** إلزامي. تاريخ.

## Location {#SiteAudit.LocationId}
<!-- alias: loc:Location, dom:SiteAudit_LocationId -->

**ما هو.** موقع العمل.

**مثال.** المستودع الرئيسي

**القواعد.** إلزامي. يُختار من قائمة أماكن العمل.

## ConductedBy {#SiteAudit.ConductedById}
<!-- alias: loc:ConductedBy, dom:SiteAudit_ConductedById -->

**ما هو.** من أجرى التدقيق.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.
