---
helpId: hs.site-audit.edit
lang: en
title: Site Audits — Create / Edit
entity: HCMv3.HealthAndSafetyManagement.SiteAudit
surfaces:
  - angular/src/app/admin/health-and-safety-management/site-audits/create-or-edit-site-audit-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.SiteAudits.Create]
---

## Overview

A safety inspection of a site. Its observations are listed as details.

## AuditDate {#SiteAudit.AuditDate}
<!-- alias: loc:AuditDate, dom:SiteAudit_AuditDate -->

**What it is.** When the audit took place.

**Rules.** Required. Date value.

## AuditTime {#SiteAudit.AuditTime}
<!-- alias: loc:AuditTime, dom:SiteAudit_AuditTime -->

**What it is.** The time of day, if known.

**Example.** 14:30

**Rules.** Optional. Date value.

## ReassessmentDate {#SiteAudit.ReassessmentDate}
<!-- alias: loc:ReassessmentDate, dom:SiteAudit_ReassessmentDate -->

**What it is.** When the site should be audited again.

**Why it matters.** A record only: no reminder is sent when it comes round.

**Rules.** Required. Date value.

## Location {#SiteAudit.LocationId}
<!-- alias: loc:Location, dom:SiteAudit_LocationId -->

**What it is.** The work location.

**Example.** Main warehouse

**Rules.** Required. Chosen from Work Location master data.

## ConductedBy {#SiteAudit.ConductedById}
<!-- alias: loc:ConductedBy, dom:SiteAudit_ConductedById -->

**What it is.** Who carried out the audit.

**Rules.** Required. Chosen from the employee list.
