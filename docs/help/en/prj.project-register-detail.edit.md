---
helpId: prj.project-register-detail.edit
lang: en
title: Project Register Details — Create / Edit
entity: HCMv3.ProjectManagement.ProjectRegisterDetail
surfaces:
  - angular/src/app/admin/project-management/project-register-details/create-or-edit-project-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.ProjectRegisterDetails.Create]
---

## Overview

One wagetype's cost charged to one project. Written by the Project Cost Engine; there is normally no reason to add or change entries by hand.

## ProjectRegister {#ProjectRegisterDetail.ProjectRegisterId}
<!-- alias: loc:ProjectRegister, dom:ProjectRegisterDetail_ProjectRegisterId -->

**What it is.** The employee's project register for the period.

**Rules.** Required. Chosen from Project Register master data.

## Project {#ProjectRegisterDetail.ProjectId}
<!-- alias: loc:Project, dom:ProjectRegisterDetail_ProjectId -->

**What it is.** The project.

**Rules.** Required. Chosen from Project master data.

## WageType {#ProjectRegisterDetail.WageTypeId}
<!-- alias: loc:WageType, dom:ProjectRegisterDetail_WageTypeId -->

**What it is.** The wagetype.

**Rules.** Required. Chosen from Wagetype master data.

## Number {#ProjectRegisterDetail.Number}
<!-- alias: loc:Number, dom:ProjectRegisterDetail_Number -->

**What it is.** The share of the pay line's quantity, such as days.

**Example.** 12

**Rules.** Required. Number.

## Amount {#ProjectRegisterDetail.Amount}
<!-- alias: loc:Amount, dom:ProjectRegisterDetail_Amount -->

**What it is.** The share of the pay line's amount.

**Example.** 450.000

**Rules.** Required. Number.
