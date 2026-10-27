---
helpId: gmi.payee-bank-detail.edit
lang: ar
title: تفاصيل بنوك المستفيدين — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.PayeeBankDetail
surfaces:
  - angular/src/app/admin/general-master-info/payee-bank-details/create-or-edit-payee-bank-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.PayeeBankDetails.Create]
---

## Overview

للبنك الدافع، الرمز الذي يستخدمه لكل بنك آخر في ملف تحويل الرواتب. وعند إنشاء ملف التحويل لبنك دافع، يُبحث هنا عن بنك كل موظف لمعرفة الرمز الذي يُكتب.

## Bank {#PayeeBankDetail.BankId}
<!-- alias: loc:Bank, dom:PayeeBankDetail_BankId -->

**ما هو.** البنك الدافع.

**القواعد.** اختياري. يُختار من قائمة البنوك.

## PayeeBank {#PayeeBankDetail.PayeeBankId}
<!-- alias: loc:PayeeBank, dom:PayeeBankDetail_PayeeBankId -->

**ما هو.** بنك الموظف -- أي البنك الذي يُحوَّل إليه.

**القواعد.** اختياري. يُختار من قائمة البنوك.

## BankCode {#PayeeBankDetail.BankCode}
<!-- alias: loc:BankCode, dom:PayeeBankDetail_BankCode -->

**ما هو.** الرمز الذي يستخدمه البنك الدافع لبنك الموظف.

**لماذا يهم.** يُكتب في ملف تحويل الرواتب للبنك الدافع لكل موظف تُحوَّل رواتبه إلى ذلك البنك.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## BankFile {#PayeeBankDetail.BankFileId}
<!-- alias: loc:BankFile, dom:PayeeBankDetail_BankFileId -->

**ما هو.** تنسيق ملف بنكي لهذا الزوج من البنوك.

**لماذا يهم.** لا يُقرأ حالياً -- فملف تحويل الرواتب لا يستخدمه.

**القواعد.** اختياري. يُختار من قائمة ملفات البنك.
