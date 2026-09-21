---
helpId: pay.absence-violation-rule-exception.edit
lang: ar
title: استثناءات قواعد مخالفات الغياب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.AbsenceViolationRuleException
surfaces:
  - angular/src/app/admin/payroll-management/absence-violation-rule-exceptions/create-or-edit-absence-violation-rule-exception-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceViolationRuleExceptions.Create]
---

## Overview

موظف مُعفى من قاعدة مخالفة غياب لفترة.

لا يُشغَّل حالياً: لا توجد عملية تصعّد حالات الغياب باستخدام هذه القواعد. فهي تُحفظ للرجوع إليها، ولا يحدث شيء عندما يتغيب الموظف.

## AbsenceViolationRules {#AbsenceViolationRuleException.AbsenceViolationRuleId}
<!-- alias: loc:AbsenceViolationRules, dom:AbsenceViolationRuleException_AbsenceViolationRuleId -->

**ما هو.** القاعدة المُعفى منها الموظف.

**مثال.** الغياب دون إذن

**القواعد.** إلزامي. يُختار من قائمة قواعد مخالفات الغياب.

## Employee {#AbsenceViolationRuleException.EmployeeId}
<!-- alias: loc:Employee, dom:AbsenceViolationRuleException_EmployeeId -->

**ما هو.** الموظف المُعفى.

**لماذا يهم.** لا يجوز أن تتداخل فترات استثناء الموظف للقاعدة نفسها.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.
