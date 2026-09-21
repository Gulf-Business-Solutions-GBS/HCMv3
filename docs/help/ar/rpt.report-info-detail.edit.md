---
helpId: rpt.report-info-detail.edit
lang: ar
title: تفاصيل بيانات التقارير — إنشاء / تحرير
entity: HCMv3.ReportManagement.ReportInfoDetail
surfaces:
  - angular/src/app/admin/report-management/report-info-details/create-or-edit-report-info-detail-modal.component.html
audience: [admin]
permissions: [Pages.ReportManagement.ReportInfoDetails.Create]
---

## Overview

حقل واحد اختير لتقرير مصمم. جزء من مصمم التقارير المنقول من النظام القديم. لا يُقرأ حالياً: لا يوجد في HCMv3 ما يشغّل هذه التعريفات، فلا أثر للسجلات هنا.

## ReportInfo {#ReportInfoDetail.ReportInfoId}
<!-- alias: loc:ReportInfo, dom:ReportInfoDetail_ReportInfoId -->

**ما هو.** التقرير المصمم.

**القواعد.** إلزامي. يُختار من قائمة بيانات التقارير.

## NodeId {#ReportInfoDetail.NodeId}
<!-- alias: loc:NodeId, dom:ReportInfoDetail_NodeId -->

**ما هو.** موقع الحقل في المصمم.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## NodeAlias {#ReportInfoDetail.NodeAlias}
<!-- alias: loc:NodeAlias, dom:ReportInfoDetail_NodeAlias -->

**ما هو.** اسم مختصر للحقل.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## EntityName {#ReportInfoDetail.EntityName}
<!-- alias: loc:EntityName, dom:ReportInfoDetail_EntityName -->

**ما هو.** السجل الذي فيه الحقل.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## PropertyName {#ReportInfoDetail.PropertyName}
<!-- alias: loc:PropertyName, dom:ReportInfoDetail_PropertyName -->

**ما هو.** الحقل.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## PropertyDisplayName {#ReportInfoDetail.PropertyDisplayName}
<!-- alias: loc:PropertyDisplayName, dom:ReportInfoDetail_PropertyDisplayName -->

**ما هو.** عنوان الحقل.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## PropertyType {#ReportInfoDetail.PropertyType}
<!-- alias: loc:PropertyType, dom:ReportInfoDetail_PropertyType -->

**ما هو.** نوع القيمة.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## BaseType {#ReportInfoDetail.BaseType}
<!-- alias: loc:BaseType, dom:ReportInfoDetail_BaseType -->

**ما هو.** النوع الأساسي للقيمة.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## AssemblyName {#ReportInfoDetail.AssemblyName}
<!-- alias: loc:AssemblyName, dom:ReportInfoDetail_AssemblyName -->

**ما هو.** مكان تعريف السجل في النظام القديم.

**القواعد.** اختياري. الحد الأقصى 512 حرفاً.

## ParentEntity {#ReportInfoDetail.ParentEntity}
<!-- alias: loc:ParentEntity, dom:ReportInfoDetail_ParentEntity -->

**ما هو.** السجل الأعلى منه.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## ParentAssembly {#ReportInfoDetail.ParentAssembly}
<!-- alias: loc:ParentAssembly, dom:ReportInfoDetail_ParentAssembly -->

**ما هو.** مكان تعريف السجل الأعلى في النظام القديم.

**القواعد.** اختياري. الحد الأقصى 512 حرفاً.

## IsAssociate {#ReportInfoDetail.IsAssociate}
<!-- alias: loc:IsAssociate, dom:ReportInfoDetail_IsAssociate -->

**ما هو.** ما إذا كان الحقل يأتي عبر ربط بسجل آخر.

**القواعد.** نعم أو لا.

## AssociateEntity {#ReportInfoDetail.AssociateEntity}
<!-- alias: loc:AssociateEntity, dom:ReportInfoDetail_AssociateEntity -->

**ما هو.** السجل المرتبط.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## AssociateProperty {#ReportInfoDetail.AssociateProperty}
<!-- alias: loc:AssociateProperty, dom:ReportInfoDetail_AssociateProperty -->

**ما هو.** الحقل في السجل المرتبط.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.
