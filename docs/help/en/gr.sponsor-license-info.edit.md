---
helpId: gr.sponsor-license-info.edit
lang: en
title: Sponsor License Infos — Create / Edit
entity: HCMv3.GovtRelationsManagement.SponsorLicenseInfo
surfaces:
  - angular/src/app/admin/govt-relations-management/sponsor-detail/license-infos/create-or-edit-sponsor-license-info-modal.component.html
audience: [admin]
permissions: [Pages.GovtRelationsManagement.SponsorLicenseInfos.Create]
---

## Overview

A trade licence held by a sponsor. The expiry date cannot be before the issue date, and a licence number can be entered once per sponsor. Kept as a record: nothing warns when it expires.

## TradeLicenseNum {#SponsorLicenseInfo.TradeLicenseNum}
<!-- alias: loc:TradeLicenseNum, dom:SponsorLicenseInfo_TradeLicenseNum -->

**What it is.** The licence number.

**Rules.** Required. Maximum 256 characters.

## CivilId {#SponsorLicenseInfo.CivilId}
<!-- alias: loc:CivilId, dom:SponsorLicenseInfo_CivilId -->

**What it is.** The civil ID of the licence holder.

**Rules.** Required. Chosen from a list.

## LicenseIssueDate {#SponsorLicenseInfo.LicenseIssueDate}
<!-- alias: loc:LicenseIssueDate, dom:SponsorLicenseInfo_LicenseIssueDate -->

**What it is.** When the licence was issued.

**Rules.** Required. Date value.

## LicenseExpiryDate {#SponsorLicenseInfo.LicenseExpiryDate}
<!-- alias: loc:LicenseExpiryDate, dom:SponsorLicenseInfo_LicenseExpiryDate -->

**What it is.** When the licence expires.

**Rules.** Required. Date value.

## ActivityType {#SponsorLicenseInfo.ActivityType}
<!-- alias: loc:ActivityType, dom:SponsorLicenseInfo_ActivityType -->

**What it is.** The licensed activity.

**Example.** General trading

**Rules.** Optional. Maximum 256 characters.
