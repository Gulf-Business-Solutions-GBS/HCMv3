---
helpId: gr.sponsor-license-info.edit
lang: ar
title: بيانات تراخيص الكفيل — إنشاء / تحرير
entity: HCMv3.GovtRelationsManagement.SponsorLicenseInfo
surfaces:
  - angular/src/app/admin/govt-relations-management/sponsor-detail/license-infos/create-or-edit-sponsor-license-info-modal.component.html
audience: [admin]
permissions: [Pages.GovtRelationsManagement.SponsorLicenseInfos.Create]
---

## Overview

رخصة تجارية يحملها كفيل. لا يمكن أن يكون تاريخ الانتهاء قبل تاريخ الإصدار، ولا يُدخل رقم الرخصة إلا مرة واحدة لكل كفيل. تُحفظ كسجل، ولا يوجد ما ينبّه عند انتهائها.

## TradeLicenseNum {#SponsorLicenseInfo.TradeLicenseNum}
<!-- alias: loc:TradeLicenseNum, dom:SponsorLicenseInfo_TradeLicenseNum -->

**ما هو.** رقم الرخصة.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## CivilId {#SponsorLicenseInfo.CivilId}
<!-- alias: loc:CivilId, dom:SponsorLicenseInfo_CivilId -->

**ما هو.** الرقم المدني لحامل الرخصة.

**القواعد.** إلزامي. يُختار من قائمة.

## LicenseIssueDate {#SponsorLicenseInfo.LicenseIssueDate}
<!-- alias: loc:LicenseIssueDate, dom:SponsorLicenseInfo_LicenseIssueDate -->

**ما هو.** متى صدرت الرخصة.

**القواعد.** إلزامي. تاريخ.

## LicenseExpiryDate {#SponsorLicenseInfo.LicenseExpiryDate}
<!-- alias: loc:LicenseExpiryDate, dom:SponsorLicenseInfo_LicenseExpiryDate -->

**ما هو.** متى تنتهي الرخصة.

**القواعد.** إلزامي. تاريخ.

## ActivityType {#SponsorLicenseInfo.ActivityType}
<!-- alias: loc:ActivityType, dom:SponsorLicenseInfo_ActivityType -->

**ما هو.** النشاط المرخّص.

**مثال.** تجارة عامة

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.
