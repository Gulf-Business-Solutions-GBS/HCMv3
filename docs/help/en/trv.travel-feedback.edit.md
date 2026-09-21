---
helpId: trv.travel-feedback.edit
lang: en
title: Travel Feedbacks — Create / Edit
entity: HCMv3.TravelManagement.TravelFeedback
surfaces:
  - angular/src/app/admin/travel-management/travel-feedbacks/create-or-edit-travel-feedback-modal.component.html
audience: [admin]
permissions: [Pages.TravelManagement.TravelFeedbacks.Create]
---

## Overview

A travel expense claim. Claims an employee raises in self-service go to approval: the amount must be above 0, and the employee cannot already have another claim for the trip, unless that one was rejected or sent back. A claim entered here is saved directly. Claims are not paid through payroll.

## Employee {#TravelFeedback.EmployeeId}
<!-- alias: loc:Employee, dom:TravelFeedback_EmployeeId -->

**What it is.** The employee claiming.

**Rules.** Required. Chosen from the employee list.

## Travel {#TravelFeedback.TravelId}
<!-- alias: loc:Travel, dom:TravelFeedback_TravelId -->

**What it is.** The trip the claim is for, if any.

**Rules.** Optional. Chosen from Travel master data.

## ClaimDate {#TravelFeedback.ClaimDate}
<!-- alias: loc:ClaimDate, dom:TravelFeedback_ClaimDate -->

**What it is.** When the claim was made.

**Rules.** Required. Date value.

## ClaimAmount {#TravelFeedback.ClaimAmount}
<!-- alias: loc:ClaimAmount, dom:TravelFeedback_ClaimAmount -->

**What it is.** The amount claimed.

**Example.** 75.500

**Rules.** Required. Number.
