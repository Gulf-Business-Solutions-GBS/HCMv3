---
helpId: pay.absence-violation-rule-exception.edit
lang: en
title: Absence Violation Rule Exceptions — Create / Edit
entity: HCMv3.PayrollManagement.AbsenceViolationRuleException
surfaces:
  - angular/src/app/admin/payroll-management/absence-violation-rule-exceptions/create-or-edit-absence-violation-rule-exception-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceViolationRuleExceptions.Create]
---

## Overview

An employee excused from an absence violation rule for a period.

Not run at present: no process escalates absences using these rules. They are saved and kept for reference, and nothing happens when an employee is absent.

## AbsenceViolationRules {#AbsenceViolationRuleException.AbsenceViolationRuleId}
<!-- alias: loc:AbsenceViolationRules, dom:AbsenceViolationRuleException_AbsenceViolationRuleId -->

**What it is.** The rule the employee is excused from.

**Example.** Unauthorised Absence

**Rules.** Required. Chosen from Absence Violation Rule master data.

## Employee {#AbsenceViolationRuleException.EmployeeId}
<!-- alias: loc:Employee, dom:AbsenceViolationRuleException_EmployeeId -->

**What it is.** The employee excused.

**Why it matters.** An employee's exception periods for the same rule cannot overlap.

**Rules.** Required. Chosen from the employee list.
