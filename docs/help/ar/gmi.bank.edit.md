---
helpId: gmi.bank.edit
lang: ar
title: البنوك — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.Bank
surfaces:
  - angular/src/app/admin/general-master-info/banks/create-or-edit-bank-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.Banks.Create]
---

## Overview

البنوك التي تُحوَّل إليها رواتب الموظفين، والبنوك التي تدفع منها الشركة. يسمّي كل حساب بنكي للموظف أحد هذه البنوك، ويُنشأ ملف تحويل الرواتب لبنك دافع واحد في كل مرة.

## ExternalCode {#Bank.ExternalCode}
<!-- alias: loc:ExternalCode, dom:Bank_ExternalCode -->

**ما هو.** معرّف لهذا البنك يُستخدم خارج HCMv3.

**لماذا يهم.** عندما يدفع هذا البنك الرواتب، يُكتب رمزه الخارجي في ملف تحويل الرواتب.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## SWIFTCode {#Bank.SWIFTCode}
<!-- alias: loc:SWIFTCode, dom:Bank_SWIFTCode -->

**ما هو.** رمز السويفت (BIC) الخاص بالبنك.

**مثال.** `NBOKKWKW`

**لماذا يهم.** يُكتب في ملف تحويل الرواتب للموظفين الذين تُحوَّل رواتبهم إلى هذا البنك.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## CompanyCode {#Bank.CompanyCode}
<!-- alias: loc:CompanyCode, dom:Bank_CompanyCode -->

**ما هو.** رمز الشركة لدى هذا البنك، كما يتطلبه تنسيق ملف التحويل لديه.

**لماذا يهم.** عندما يدفع هذا البنك الرواتب، يُكتب هذا الرمز في ملف تحويل الرواتب.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## BankFile {#Bank.BankFileId}
<!-- alias: loc:BankFile, dom:Bank_BankFileId -->

**ما هو.** تنسيق ملف بنكي مرتبط بهذا البنك.

**لماذا يهم.** لا يُقرأ حالياً -- فملف تحويل الرواتب لا يستخدم هذا الإعداد.

**القواعد.** اختياري. يُختار من قائمة ملفات البنك.

:::admin
الرموز التي يستخدمها البنك الدافع للبنوك الأخرى في ملف التحويل تُضبط في تفاصيل بنوك المستفيدين.
:::
