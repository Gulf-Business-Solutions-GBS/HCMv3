---
helpId: pa.employee-reward.edit
lang: en
title: Employee Rewards — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeReward
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/rewards/create-or-edit-employee-reward-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeRewards.Create]
---

## Overview

An award or recognition given to the employee.

It is recorded for reference; no calculation or report reads it at present.

## RewardType {#EmployeeReward.RewardType}
<!-- alias: loc:RewardType, dom:EmployeeReward_RewardType -->

**What it is.** The kind of reward, as free text.

**Example.** Employee of the Month

**Rules.** Required. Maximum 256 characters.

## RewardDate {#EmployeeReward.RewardDate}
<!-- alias: loc:RewardDate, dom:EmployeeReward_RewardDate -->

**What it is.** The date the reward was given.

**Example.** 30 Jun 2026

**Rules.** Required. Date value.

## Certificate {#EmployeeReward.Certificate}
<!-- alias: loc:Certificate, dom:EmployeeReward_Certificate -->

**What it is.** The certificate given with the reward.

**Example.** Certificate of Excellence

**Rules.** Optional. Maximum 256 characters.
