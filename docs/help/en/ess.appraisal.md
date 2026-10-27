---
helpId: ess.appraisal
lang: en
title: 360 Degree Appraisal
surfaces:
  - angular/src/app/main/ess/my-appraisal/my-appraisal.component.html
audience: [end-user]
permissions: [Ess.MyAppraisals]
---

## Overview

Everything about performance appraisals in one place: your own appraisals, the ones you have been asked to review, and, if you are a manager, your team's. Pick a list at the top, then press **Details** on a row to open the appraisal. There you see the KPIs, enter your ratings and comments, add peers and submit your review.

What you see and may do in an appraisal depends on two things: your part in it (the employee being appraised, a reviewer, a peer, or an approver on the step the appraisal is waiting at), and what HR has allowed for that step under **Appraisal WF Setup**. So a button you used at one step may be missing at the next. If HR has not set up the step, no review buttons are offered.

Once the appraisal's request is approved, rejected or closed, it can only be read. Approvers who open the appraisal from their **Approvals** see the same review.

## My Appraisals {#ess.appraisal.mine}
<!-- alias: loc:MyAppraisalListScope_Mine -->

**What it is.** The appraisals about you.

**Example.** Your 2026 annual appraisal, waiting at your manager's step.

**Why it matters.** Open one to rate yourself (when your company asks for self-rating), nominate peers when allowed, and follow its status.

## Appraisals to Review {#ess.appraisal.to-review}
<!-- alias: loc:MyAppraisalListScope_ToReview -->

**What it is.** The appraisals you have to act on: those where you are on the reviewer list, and those waiting at a step where you are an approver.

**Example.** A colleague nominated you as a peer: their appraisal appears here.

**Why it matters.** An appraisal stays on this list after you have submitted your review, so you can look back at it.

## Team Appraisals {#ess.appraisal.team}
<!-- alias: loc:MyAppraisalListScope_Team -->

**What it is.** The appraisals of the employees who report to you directly today, one level down.

**Example.** The four people in your section.

**Why it matters.** It lets a manager follow the team's appraisals even at steps where they are not an approver. What you may see inside follows the step's setup.

## Request Number {#ess.appraisal.request-number}
<!-- alias: loc:RequestNumber -->

**What it is.** The workflow request number of the appraisal.

**Example.** 1052

**Why it matters.** Quote it when you ask HR about an appraisal. It is blank for an appraisal that has not been sent into the workflow.

## Employee {#ess.appraisal.employee}
<!-- alias: loc:Employee -->

**What it is.** The employee being appraised, by code and name.

**Example.** 1045 - Fatima Al-Sabah

## Period {#ess.appraisal.period}
<!-- alias: loc:Period -->

**What it is.** The appraisal period (cycle) the appraisal belongs to.

**Example.** Annual 2026

## Form Type {#ess.appraisal.form-type}
<!-- alias: loc:FormType -->

**What it is.** The kind of appraisal form, which also decides its approval chain and the most reviewers it may have.

**Example.** Staff Appraisal

## Created On {#ess.appraisal.created-on}
<!-- alias: loc:CreatedOn -->

**What it is.** The date the appraisal was opened.

**Example.** 2026-12-01

## Request Status {#ess.appraisal.status}
<!-- alias: loc:RequestStatus -->

**What it is.** Where the appraisal's request stands: open (in progress), approved, rejected or closed. **Recorded** means the appraisal is not managed by the workflow.

**Example.** Open

**Why it matters.** Ratings, peers and reviews can only be changed while the request is open (or still a draft).

## Details {#ess.appraisal.details}
<!-- alias: loc:Details -->

**What it is.** Opens the appraisal.

**Why it matters.** All the work on an appraisal happens there; nothing else can be done from the row.

## KPI {#ess.appraisal.kpis}
<!-- alias: loc:KpiInfo -->

**What it is.** The KPIs the employee is appraised on, with their category, description, weight and target.

**Example.** Customer satisfaction, weight 30, target 90%.

**Why it matters.** Shown only where the step allows the KPI list to be seen, and never to a reviewer who is not an approver on the step: they see the KPIs in their own rating lines instead.

## Copy KPI {#ess.appraisal.copy-kpi}
<!-- alias: loc:CopyKpi -->

**What it is.** Fills the appraisal's KPI list from a ready-made set. Offered to the approver on a step that allows it, while the request is open.

**Example.** Copy the KPIs set up for the job title "Accountant".

**Why it matters.** Only KPIs the appraisal does not already have are added, so copying twice does not double them. Every reviewer already on the appraisal gets a rating line for each new KPI. On a step where the approver rates, copying also puts you on the reviewer list. When peer review is switched on in Appraisal Settings, the copy is refused if one more reviewer would go over the form type's reviewer limit.

## KPI From {#ess.appraisal.kpi-from}
<!-- alias: loc:KpiFrom -->

**What it is.** Where Copy KPI takes the KPIs from: a **Job Title** KPI set, an **Organization Unit** KPI set, or another **Employee**'s appraisal for the same period. Then choose the set or the employee in the box beside it.

**Example.** Organization Unit, then "Finance Department".

**Why it matters.** Only employees with an appraisal in the same period that already has KPIs are offered.

## KPI Category {#ess.appraisal.kpi-category}
<!-- alias: loc:KpiCategory -->

**What it is.** The group the KPI belongs to.

**Example.** Financial

## Contribution Review {#ess.appraisal.contribution}
<!-- alias: loc:ContributionReview -->

**What it is.** The KPI itself, with its description underneath.

**Example.** Close the month within 5 working days.

## Weight {#ess.appraisal.weight}
<!-- alias: loc:Weight -->

**What it is.** How much the KPI counts in the score compared with the others.

**Example.** 25

**Why it matters.** Not shown when your company appraises with marks instead of ratings.

## Target {#ess.appraisal.target}
<!-- alias: loc:Target -->

**What it is.** What the employee is expected to achieve on the KPI.

**Example.** 95% on time

## Ratings {#ess.appraisal.ratings}
<!-- alias: loc:Ratings -->

**What it is.** One line per KPI for each reviewer. You always see your own lines; other reviewers' lines are shown only where the step allows it. Pick a rating for each of your lines while your review is not yet submitted.

**Example.** "Exceeds expectations (2/3)": you may give this rating to 2 more KPIs out of a limit of 3.

**Why it matters.** Each rating can be given to only a limited number of your KPIs; a rating with none left is greyed out. Some ratings need a comment: the comment box turns red until you write one.

## Reviewer Name {#ess.appraisal.reviewer-name}
<!-- alias: loc:ReviewerName -->

**What it is.** Whose rating line it is. The column appears only when you can see other reviewers' lines as well as your own.

**Example.** 1020 - Ahmad Al-Kandari

## Marks {#ess.appraisal.marks}
<!-- alias: loc:Marks -->

**What it is.** The marks you give a KPI, used instead of a rating when your company has marks switched on in Appraisal Settings.

**Example.** 8

**Why it matters.** Marks are not required before you submit, and with marks there are no average ratings.

## Comments {#ess.appraisal.comments}
<!-- alias: loc:Comments -->

**What it is.** Your remark on the KPI, up to 4000 characters.

**Example.** Delivered every month-end on time; one late reconciliation in March.

**Why it matters.** Required where the chosen rating asks for a comment; you cannot submit without it.

## Reviewers {#ess.appraisal.reviewers}
<!-- alias: loc:Reviewers -->

**What it is.** Everyone reviewing the appraisal, with their type, scores and whether they have submitted. Shown where the step allows it, and never to a reviewer who is not an approver on the step.

**Example.** The employee, their manager and two peers.

**Why it matters.** Reviewers are mostly added automatically when the appraisal reaches a step, as HR has set it up; peers can also be added with **Add Peer**.

## Add Peer {#ess.appraisal.add-peer}
<!-- alias: loc:AddPeer -->

**What it is.** Adds a colleague as a peer reviewer. Offered to the employee and to the approver on a step that allows peers. Search by code or name, choose the person and press **Add Peer**.

**Example.** The employee adds a colleague from a project team.

**Why it matters.** You cannot add yourself, the employee or someone already reviewing. When the employee is choosing, their manager and people who will be added automatically are not offered either. Adding is refused when the appraisal has reached the peer limit in Appraisal Settings, the form type's reviewer limit, or, for the employee, the number of peers they may nominate on this step.

## Peer {#ess.appraisal.peer}
<!-- alias: loc:Peer -->

**What it is.** The colleague to add, from the search results (active employees, up to 50 at a time).

**Example.** 1102 - Noura Al-Ali

## Reviewer Type {#ess.appraisal.reviewer-type}
<!-- alias: loc:ReviewerType -->

**What it is.** Why the person is reviewing: **Employee** (self-rating), **Employee Manager**, the name of their relationship to the employee, or **Peer**.

**Example.** Employee Manager

## Average Rating {#ess.appraisal.average-rating}
<!-- alias: loc:AverageRating -->

**What it is.** The rating that the reviewer's ratings average out to.

**Example.** Meets expectations

## Average Score {#ess.appraisal.average-score}
<!-- alias: loc:AverageScore -->

**What it is.** The reviewer's score, worked out from their ratings in the same way as on the admin appraisal screen. It is updated every time they save.

**Example.** 3.25

## Total Marks {#ess.appraisal.total-marks}
<!-- alias: loc:TotalMarks -->

**What it is.** The reviewer's marks added up, shown instead of the average when marks are used.

**Example.** 42

## Review Submitted {#ess.appraisal.review-submitted}
<!-- alias: loc:ReviewSubmitted -->

**What it is.** Whether the reviewer has submitted their review.

**Example.** No

**Why it matters.** A submitted review is locked. On some steps the appraisal moves on by itself when the last reviewer submits.

## Save Review {#ess.appraisal.save-review}
<!-- alias: loc:SaveReview -->

**What it is.** Saves your ratings, marks and comments without submitting them.

**Example.** Save half-way and finish tomorrow.

**Why it matters.** You can change a saved review as often as you like until you submit it. The scores are recalculated on every save.

## Submit Review {#ess.appraisal.submit-review}
<!-- alias: loc:SubmitReview -->

**What it is.** Saves and hands in your review. You are asked to confirm, because a submitted review cannot be changed.

**Example.** "Review submitted. All reviews are in, so the step was approved."

**Why it matters.** Every KPI must have a rating, and a comment where the rating needs one. Peers and the employee rating themselves finish with this button; an approver who rates usually finishes by approving from the **Approvals** instead. When the step is set to move on after the last review, submitting the last one approves the step automatically.

## Overall Average Score {#ess.appraisal.overall-average}
<!-- alias: loc:OverallAverageScore -->

**What it is.** The average of all reviewers' scores, to 2 decimals. Shown on the right where the step allows it.

**Example.** 3.40

## Peer Average Score {#ess.appraisal.peer-average}
<!-- alias: loc:PeerAverageScore -->

**What it is.** The average score of every reviewer other than the employee: managers, peers and approvers who rated.

**Example.** 3.55

## Employee Average Score {#ess.appraisal.employee-average}
<!-- alias: loc:EmployeeAverageScore -->

**What it is.** The employee's own score from rating themselves; 0 when they do not self-rate.

**Example.** 3.80
