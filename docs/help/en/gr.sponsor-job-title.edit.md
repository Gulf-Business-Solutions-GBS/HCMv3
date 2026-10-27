---
helpId: gr.sponsor-job-title.edit
lang: en
title: Sponsor Job Titles — Create / Edit
entity: HCMv3.GovtRelationsManagement.SponsorJobTitle
surfaces:
  - angular/src/app/admin/govt-relations-management/sponsor-detail/job-titles/create-or-edit-sponsor-job-title-modal.component.html
audience: [admin]
permissions: [Pages.GovtRelationsManagement.SponsorJobTitles.Create]
---

## Overview

How many employees a sponsor may have under one government job title, split between citizens and non-citizens. The two percentages must add up to 100. Allowed places are worked out on save: citizens get the number of positions times their percentage rounded down, non-citizens rounded up. An employee sponsor record can only be added for a job title the sponsor has a row for. Remaining places are shown on the Sponsor Job Title Quota Report; nothing stops the quota being exceeded.

## GovtJobTitle {#SponsorJobTitle.GovtJobTitleId}
<!-- alias: loc:GovtJobTitle, dom:SponsorJobTitle_GovtJobTitleId -->

**What it is.** The government job title.

**Example.** Accountant

**Rules.** Required. Chosen from Govt Job Title master data.

## NoofPosition {#SponsorJobTitle.NoofPosition}
<!-- alias: loc:NoofPosition, dom:SponsorJobTitle_NoofPosition -->

**What it is.** The total number of positions allowed.

**Example.** 20

**Rules.** Required. Whole number.

## CitizenPercent {#SponsorJobTitle.CitizenPercent}
<!-- alias: loc:CitizenPercent, dom:SponsorJobTitle_CitizenPercent -->

**What it is.** The share of positions for citizens.

**Example.** 30

**Rules.** Required. Whole number.

## CitizenQuotaUsed {#SponsorJobTitle.CitizenQuotaUsed}
<!-- alias: loc:CitizenQuotaUsed, dom:SponsorJobTitle_CitizenQuotaUsed -->

**What it is.** How many places are taken by citizens.

**Why it matters.** Goes up by one when an employee sponsor record is added for an employee whose nationality group is Citizen, and down by one when one is deleted. Changing a record does not recount it.

**Rules.** Required. Whole number.

## NonCitizenPercent {#SponsorJobTitle.NonCitizenPercent}
<!-- alias: loc:NonCitizenPercent, dom:SponsorJobTitle_NonCitizenPercent -->

**What it is.** The share of positions for non-citizens.

**Example.** 70

**Rules.** Required. Whole number.

## NonCitizenQuotaUsed {#SponsorJobTitle.NonCitizenQuotaUsed}
<!-- alias: loc:NonCitizenQuotaUsed, dom:SponsorJobTitle_NonCitizenQuotaUsed -->

**What it is.** How many places are taken by non-citizens.

**Why it matters.** Goes up and down the same way, for employees in any other nationality group.

**Rules.** Required. Whole number.
