---
helpId: mp.position-relationship.edit
lang: en
title: Position Relationships — Create / Edit
entity: HCMv3.ManpowerPlanningManagement.PositionRelationship
surfaces:
  - angular/src/app/admin/manpower-planning-management/position-relationships/create-or-edit-position-relationship-modal.component.html
audience: [admin]
permissions: [Pages.ManpowerPlanningManagement.PositionRelationships.Create]
---

## Overview

An employee holding a position for a date range. The end date cannot be before the start date, and the position's FTE limit cannot be exceeded. The position's Vacant flag is updated, and the holder appears on the Position Chart.

## Position {#PositionRelationship.PositionId}
<!-- alias: loc:Position, dom:PositionRelationship_PositionId -->

**What it is.** The position.

**Rules.** Required. Chosen from Position master data.

## RelationType {#PositionRelationship.RelationType}
<!-- alias: loc:RelationType, dom:PositionRelationship_RelationType -->

**What it is.** How the employee relates to the position. Holder is the only option.

**Rules.** Required. One of: Holder.

## Employee {#PositionRelationship.EmployeeId}
<!-- alias: loc:Employee, dom:PositionRelationship_EmployeeId -->

**What it is.** The employee holding the position.

**Rules.** Required. Chosen from the employee list.
