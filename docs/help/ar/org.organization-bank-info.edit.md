---
helpId: org.organization-bank-info.edit
lang: ar
title: المعلومات البنكية للشركة — إنشاء / تحرير
entity: HCMv3.OrganizationManagement.OrganizationBankInfo
surfaces:
  - angular/src/app/admin/organization-management/organization-bank-infos/create-or-edit-organization-bank-info-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.OrganizationBankInfos.Create]
---

## Overview

حساب بنكي لجزء من المنظمة. تأخذ ملفات البنك التي تتضمن حساب الشركة الحساب من هنا: يُستخدم حساب الوحدة في المستوى الأعلى، وإلا فأي حساب مُدخل. ولا يمكن ترك الوحدة التنظيمية والبنك ورقم الحساب فارغة.

## OrgUnit {#OrganizationBankInfo.OrganizationUnitId}
<!-- alias: loc:OrgUnit, dom:OrganizationBankInfo_OrganizationUnitId -->

**ما هو.** جزء المنظمة الذي يملك الحساب.

**القواعد.** اختياري. يُختار من قائمة الوحدات التنظيمية.

## Bank {#OrganizationBankInfo.BankId}
<!-- alias: loc:Bank, dom:OrganizationBankInfo_BankId -->

**ما هو.** البنك.

**مثال.** NBK

**القواعد.** اختياري. يُختار من قائمة البنوك.

## AccountNo {#OrganizationBankInfo.AccountNo}
<!-- alias: loc:AccountNo, dom:OrganizationBankInfo_AccountNo -->

**ما هو.** رقم الحساب.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## SC_Code {#OrganizationBankInfo.SC_Code}
<!-- alias: loc:SC_Code, dom:OrganizationBankInfo_SC_Code -->

**ما هو.** رمز الفرع للحساب.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.
