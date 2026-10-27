---
helpId: pa.employee-contract.edit
lang: ar
title: عقود الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeContract
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/contracts/create-or-edit-employee-contract-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeContracts.Create]
---

## Overview

عقود عمل الموظف والفترات التي تغطيها. لا يؤدي حفظ عقد إلى إغلاق عقد سابق؛ ولا يجوز أن تتداخل فترات عقود الموظف نفسه، ولا أن يبدأ عقدان في التاريخ نفسه.

في أنواع الإجازات التي تتراكم على مدى فترة الخدمة كلها، تمتد فترة التراكم من تاريخ الالتحاق حتى تاريخ انتهاء أحدث عقد للموظف.

## ContractType {#EmployeeContract.ContractTypeId}
<!-- alias: loc:ContractType, dom:EmployeeContract_ContractTypeId -->

**ما هو.** نوع العقد.

**مثال.** محدد المدة — سنتان

**لماذا يهم.** يظهر في تقارير العقود والموظفين.

**القواعد.** إلزامي. يُختار من قائمة أنواع العقود.

:::admin
يُدار من البيانات الأساسية العامة ← أنواع العقود.
:::
