---
helpId: rpt.report-info-detail.edit
lang: en
title: Report Info Details — Create / Edit
entity: HCMv3.ReportManagement.ReportInfoDetail
surfaces:
  - angular/src/app/admin/report-management/report-info-details/create-or-edit-report-info-detail-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.ReportInfoDetails.Create]
---

## Overview

One field chosen for a designed report. Part of the report designer carried over from the old system. Not read at present: nothing in HCMv3 runs these definitions, so records here have no effect.

## ReportInfo {#ReportInfoDetail.ReportInfoId}
<!-- alias: loc:ReportInfo, dom:ReportInfoDetail_ReportInfoId -->

**What it is.** The designed report.

**Rules.** Required. Chosen from Report Info master data.

## NodeId {#ReportInfoDetail.NodeId}
<!-- alias: loc:NodeId, dom:ReportInfoDetail_NodeId -->

**What it is.** The field's position in the designer.

**Rules.** Optional. Maximum 256 characters.

## NodeAlias {#ReportInfoDetail.NodeAlias}
<!-- alias: loc:NodeAlias, dom:ReportInfoDetail_NodeAlias -->

**What it is.** A short name for the field.

**Rules.** Optional. Maximum 256 characters.

## EntityName {#ReportInfoDetail.EntityName}
<!-- alias: loc:EntityName, dom:ReportInfoDetail_EntityName -->

**What it is.** The record the field is on.

**Rules.** Optional. Maximum 256 characters.

## PropertyName {#ReportInfoDetail.PropertyName}
<!-- alias: loc:PropertyName, dom:ReportInfoDetail_PropertyName -->

**What it is.** The field.

**Rules.** Optional. Maximum 256 characters.

## PropertyDisplayName {#ReportInfoDetail.PropertyDisplayName}
<!-- alias: loc:PropertyDisplayName, dom:ReportInfoDetail_PropertyDisplayName -->

**What it is.** The field's caption.

**Rules.** Optional. Maximum 256 characters.

## PropertyType {#ReportInfoDetail.PropertyType}
<!-- alias: loc:PropertyType, dom:ReportInfoDetail_PropertyType -->

**What it is.** The kind of value.

**Rules.** Optional. Maximum 256 characters.

## BaseType {#ReportInfoDetail.BaseType}
<!-- alias: loc:BaseType, dom:ReportInfoDetail_BaseType -->

**What it is.** The underlying kind of value.

**Rules.** Optional. Maximum 256 characters.

## AssemblyName {#ReportInfoDetail.AssemblyName}
<!-- alias: loc:AssemblyName, dom:ReportInfoDetail_AssemblyName -->

**What it is.** Where the record is defined in the old system.

**Rules.** Optional. Maximum 512 characters.

## ParentEntity {#ReportInfoDetail.ParentEntity}
<!-- alias: loc:ParentEntity, dom:ReportInfoDetail_ParentEntity -->

**What it is.** The record above it.

**Rules.** Optional. Maximum 256 characters.

## ParentAssembly {#ReportInfoDetail.ParentAssembly}
<!-- alias: loc:ParentAssembly, dom:ReportInfoDetail_ParentAssembly -->

**What it is.** Where the record above is defined in the old system.

**Rules.** Optional. Maximum 512 characters.

## IsAssociate {#ReportInfoDetail.IsAssociate}
<!-- alias: loc:IsAssociate, dom:ReportInfoDetail_IsAssociate -->

**What it is.** Whether the field comes through a link to another record.

**Rules.** Yes or no.

## AssociateEntity {#ReportInfoDetail.AssociateEntity}
<!-- alias: loc:AssociateEntity, dom:ReportInfoDetail_AssociateEntity -->

**What it is.** The linked record.

**Rules.** Optional. Maximum 256 characters.

## AssociateProperty {#ReportInfoDetail.AssociateProperty}
<!-- alias: loc:AssociateProperty, dom:ReportInfoDetail_AssociateProperty -->

**What it is.** The field on the linked record.

**Rules.** Optional. Maximum 256 characters.
