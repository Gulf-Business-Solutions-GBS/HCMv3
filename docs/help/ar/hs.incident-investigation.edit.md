---
helpId: hs.incident-investigation.edit
lang: ar
title: تحقيقات الحوادث — إنشاء / تحرير
entity: HCMv3.HealthAndSafetyManagement.IncidentInvestigation
surfaces:
  - angular/src/app/admin/health-and-safety-management/incident-investigations/create-or-edit-incident-investigation-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.IncidentInvestigations.Create]
---

## Overview

تحقيق في حادث وسببه الجذري.

## InvestigationDate {#IncidentInvestigation.InvestigationDate}
<!-- alias: loc:InvestigationDate, dom:IncidentInvestigation_InvestigationDate -->

**ما هو.** متى جرى التحقيق.

**القواعد.** إلزامي. تاريخ.

## InvestigationTime {#IncidentInvestigation.InvestigationTime}
<!-- alias: loc:InvestigationTime, dom:IncidentInvestigation_InvestigationTime -->

**ما هو.** الوقت من اليوم، إن كان معروفاً.

**مثال.** 14:30

**القواعد.** اختياري. تاريخ.

## Incident {#IncidentInvestigation.IncidentId}
<!-- alias: loc:Incident, dom:IncidentInvestigation_IncidentId -->

**ما هو.** الحادث الذي يخصه هذا.

**القواعد.** إلزامي. يُختار من قائمة سجلات الحوادث.

## RootCause {#IncidentInvestigation.RootCauseId}
<!-- alias: loc:RootCause, dom:IncidentInvestigation_RootCauseId -->

**ما هو.** السبب الرئيسي الذي وُجد.

**مثال.** أرضية مبللة دون لافتة

**القواعد.** إلزامي. يُختار من قائمة الأسباب الجذرية للتحقيق.

## RootCauseAnalysis {#IncidentInvestigation.RootCauseAnalysis}
<!-- alias: loc:RootCauseAnalysis, dom:IncidentInvestigation_RootCauseAnalysis -->

**ما هو.** كيف تم التوصل إلى السبب.

**القواعد.** اختياري.
