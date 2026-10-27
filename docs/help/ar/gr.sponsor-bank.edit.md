---
helpId: gr.sponsor-bank.edit
lang: ar
title: بنوك الكفلاء — إنشاء / تحرير
entity: HCMv3.GovtRelationsManagement.SponsorBank
surfaces:
  - angular/src/app/admin/govt-relations-management/sponsor-detail/banks/create-or-edit-sponsor-bank-modal.component.html
audience: [admin]
permissions: [Pages.GovtRelationsManagement.SponsorBanks.Create]
---

## Overview

حساب بنكي يدفع منه الكفيل الرواتب. لا يمكن أن يكون تاريخ الانتهاء قبل تاريخ البدء. ويمكن اختيار كل حساب له بنك دفع عند إنشاء ملف البنك.

## EmployeeBankId {#SponsorBank.EmployeeBankId}
<!-- alias: loc:EmployeeBankId, dom:SponsorBank_EmployeeBankId -->

**ما هو.** البنك الذي يتعامل معه الموظفون.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة البنوك.

## PaymentBankId {#SponsorBank.PaymentBankId}
<!-- alias: loc:PaymentBankId, dom:SponsorBank_PaymentBankId -->

**ما هو.** البنك الذي فيه الحساب.

**مثال.** NBK

**القواعد.** إلزامي. يُختار من قائمة البنوك.

## AccountNumber {#SponsorBank.AccountNumber}
<!-- alias: loc:AccountNumber, dom:SponsorBank_AccountNumber -->

**ما هو.** رقم الحساب.

**لماذا يهم.** يُكتب في ترويسة ملف البنك للصيغ التي تتضمن حساب الدفع.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Iban {#SponsorBank.IBAN}
<!-- alias: loc:Iban, dom:SponsorBank_Iban -->

**ما هو.** رقم الآيبان للحساب.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.
