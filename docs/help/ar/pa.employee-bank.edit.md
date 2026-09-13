---
helpId: pa.employee-bank.edit
lang: ar
title: بنوك الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeBank
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/banks/create-or-edit-employee-bank-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeBanks.Create]
---

## Overview

كيف وأين يُصرف راتب الموظف، اعتباراً من تاريخ معين. ولا يجوز أن يبدأ سجلان للموظف نفسه في التاريخ نفسه.

تستخدم ملفات التحويل البنكي السجل الساري في تاريخ نهاية الملف. ولا يؤدي حفظ سجل جديد إلى إغلاق السابق، لذا حدد تاريخ انتهاء السجل السابق بنفسك.

## BankId {#EmployeeBank.BankId}
<!-- alias: loc:BankId, dom:EmployeeBank_BankId -->

**ما هو.** بنك الموظف الذي يستلم الراتب.

**مثال.** بنك الكويت الوطني

**لماذا يهم.** إلزامي عندما يكون نوع الدفع بنكاً، ويمكن تصفية ملفات البنك به.

**القواعد.** اختياري. يُختار من قائمة البنوك.

:::admin
يُدار من البيانات الأساسية العامة ← البنوك.
:::

## PayerBankId {#EmployeeBank.PayerBankId}
<!-- alias: loc:PayerBankId, dom:EmployeeBank_PayerBankId -->

**ما هو.** بنك الشركة الذي يدفع لهذا الموظف، عند توزيع الموظفين على عدة بنوك دافعة.

**مثال.** بنك الخليج

**القواعد.** اختياري. يُختار من قائمة البنوك.

:::admin
إذا حدد سجل أي موظف بنكاً دافعاً، فلا يشمل ملف تحويل ذلك البنك إلا الموظفين الذين يحددونه. وإلا فلا يشمل الملف إلا الموظفين الذين لم يُحدَّد لهم بنك دافع.
:::

## BankCountryId {#EmployeeBank.BankCountryId}
<!-- alias: loc:BankCountryId, dom:EmployeeBank_BankCountryId -->

**ما هو.** بلد بنك الموظف.

**مثال.** الكويت

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه ملفات البنك حالياً.

**القواعد.** اختياري. يُختار من قائمة الدول.

## PaymentType {#EmployeeBank.PaymentType}
<!-- alias: loc:PaymentType, dom:EmployeeBank_PaymentType -->

**ما هو.** طريقة صرف الراتب.

**مثال.** بنك

**لماذا يهم.** يمكن قصر ملفات البنك على نوع دفع واحد، ويجب اختيار بنك عندما يكون النوع بنكاً.

**القواعد.** إلزامي. إحدى القيم: البنك، نقداً، لا ينطبق.

## PaymentCurrencyId {#EmployeeBank.PaymentCurrencyId}
<!-- alias: loc:PaymentCurrencyId, dom:EmployeeBank_PaymentCurrencyId -->

**ما هو.** العملة التي يُصرف بها الراتب.

**مثال.** KWD

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه ملفات البنك حالياً.

**القواعد.** اختياري. يُختار من قائمة العملات.

## AccountNum {#EmployeeBank.AccountNum}
<!-- alias: loc:AccountNum, dom:EmployeeBank_AccountNum -->

**ما هو.** رقم الحساب البنكي للموظف.

**مثال.** 2010123456

**لماذا يهم.** يُكتب في ملف التحويل البنكي.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Iban {#EmployeeBank.IBAN}
<!-- alias: loc:Iban, dom:EmployeeBank_Iban -->

**ما هو.** رقم الحساب البنكي الدولي للموظف.

**مثال.** KW81CBKU0000000000001234560101

**لماذا يهم.** يُكتب في ملف التحويل البنكي. ويحتاج طلب التغيير عبر الخدمة الذاتية إلى رقم حساب أو رقم IBAN.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## BankBranch {#EmployeeBank.BankBranch}
<!-- alias: loc:BankBranch, dom:EmployeeBank_BankBranch -->

**ما هو.** فرع بنك الموظف.

**مثال.** السالمية

**لماذا يهم.** يُدرج في ملفات البنك التي تتضمن الفرع، وفي تقارير الموظفين.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.
