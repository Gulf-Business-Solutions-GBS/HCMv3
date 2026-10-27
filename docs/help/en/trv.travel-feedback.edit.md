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

A travel expense claim. Claims an employee raises in self-service go to approval: the amount must be above 0, and the employee cannot already have another claim for the trip, unless that one was rejected or sent back. Claims are not paid through payroll.

**Employee or Applicant** at the top says whose claim it is: an employee's, or a job applicant's. A claim is for exactly one of the two, and only that person's trips are offered under **Travel**; switching clears the other person and the trip.

An employee's claim entered here goes to approval when travel claims are set up for approval, and is saved directly when they are not. An applicant's claim is always saved directly, because approval is routed through the employee a request is about and an applicant has no manager or unit to route by. A claim that has been sent for approval cannot later be moved from the employee to an applicant.

## Employee {#TravelFeedback.EmployeeId}
<!-- alias: loc:Employee, dom:TravelFeedback_EmployeeId -->

**What it is.** The employee claiming.

**Rules.** Required. Chosen from the employee list.

## Applicant {#TravelFeedback.ApplicantId}
<!-- alias: loc:Applicant, dom:TravelFeedback_ApplicantId -->

**What it is.** The job applicant claiming. Shown instead of Employee when **Applicant** is chosen above.

**Example.** APP-0042 - Sara Ahmed

**Why it matters.** Choosing the applicant narrows **Travel** to that applicant's trips. The claim is saved at once, with no approval.

**Rules.** Required. Chosen from Applicant master data.

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
