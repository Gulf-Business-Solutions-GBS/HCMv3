---
helpId: hs.incident-claim.edit
lang: ar
title: مطالبات الحوادث — إنشاء / تحرير
entity: HCMv3.HealthAndSafetyManagement.IncidentClaim
surfaces:
  - angular/src/app/admin/health-and-safety-management/incident-claims/create-or-edit-incident-claim-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.IncidentClaims.Create]
---

## Overview

مطالبة تأمين أو تعويض ناشئة عن حادث. تُحفظ كسجل، ولا تُدفع عبر الرواتب.

## ClaimDate {#IncidentClaim.ClaimDate}
<!-- alias: loc:ClaimDate, dom:IncidentClaim_ClaimDate -->

**ما هو.** متى قُدمت المطالبة.

**القواعد.** إلزامي. تاريخ.

## Incident {#IncidentClaim.IncidentId}
<!-- alias: loc:Incident, dom:IncidentClaim_IncidentId -->

**ما هو.** الحادث الذي يخصه هذا.

**القواعد.** إلزامي. يُختار من قائمة سجلات الحوادث.

## Amount {#IncidentClaim.Amount}
<!-- alias: loc:Amount, dom:IncidentClaim_Amount -->

**ما هو.** المبلغ المطالب به.

**مثال.** 1,500.000

**القواعد.** إلزامي. رقم.

## Currency {#IncidentClaim.CurrencyId}
<!-- alias: loc:Currency, dom:IncidentClaim_CurrencyId -->

**ما هو.** عملة المبلغ.

**مثال.** KWD

**القواعد.** إلزامي. يُختار من قائمة العملات.

## PaymentDate {#IncidentClaim.PaymentDate}
<!-- alias: loc:PaymentDate, dom:IncidentClaim_PaymentDate -->

**ما هو.** متى دُفعت أو يُتوقع دفعها.

**القواعد.** إلزامي. تاريخ.

## ClaimStatus {#IncidentClaim.ClaimStatusId}
<!-- alias: loc:ClaimStatus, dom:IncidentClaim_ClaimStatusId -->

**ما هو.** حالة المطالبة.

**مثال.** مقدَّمة

**القواعد.** اختياري. يُختار من قائمة حالات مطالبات الحوادث.
