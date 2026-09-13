---
helpId: pa.employee-fund-detail.edit
lang: ar
title: تفاصيل صناديق الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeFundDetail
surfaces:
  - angular/src/app/admin/personnel-administration/employee-fund-details/create-or-edit-employee-fund-detail-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeFundDetails.Create]
---

## Overview

معاملة على حساب صندوق الموظف: رصيد افتتاحي، أو اقتطاع، أو صرف، أو اقتطاع خارجي.

تصرف الرواتب أسطر الصرف المؤرخة في الشهر الذي تعالجه. كما تعيد بناء أسطر الاقتطاع والرصيد الافتتاحي للشهر في كل تشغيل: فتُحذف الأسطر من هذين النوعين المؤرخة في ذلك الشهر ويُعاد احتسابها.

## EmployeeFundId {#EmployeeFundDetail.EmployeeFundId}
<!-- alias: loc:EmployeeFundId, dom:EmployeeFundDetail_EmployeeFundId -->

**ما هو.** حساب الصندوق الذي تنتمي إليه المعاملة.

**القواعد.** إلزامي. يُختار من قائمة.

## TransactionDate {#EmployeeFundDetail.TransactionDate}
<!-- alias: loc:TransactionDate, dom:EmployeeFundDetail_TransactionDate -->

**ما هو.** تاريخ المعاملة.

**مثال.** 30 سبتمبر 2026

**لماذا يهم.** تصرف الرواتب أسطر الصرف المؤرخة في شهرها.

**القواعد.** إلزامي. تاريخ.

## Amount {#EmployeeFundDetail.Amount}
<!-- alias: loc:Amount, dom:EmployeeFundDetail_Amount -->

**ما هو.** مبلغ المعاملة.

**مثال.** 75.000

**لماذا يهم.** تُضاف أسطر الرصيد الافتتاحي والاقتطاع إلى الصندوق، وتُصرف أسطر الصرف منه.

**القواعد.** اختياري. رقم.

## FundPayType {#EmployeeFundDetail.FundPayType}
<!-- alias: loc:FundPayType, dom:EmployeeFundDetail_FundPayType -->

**ما هو.** نوع المعاملة.

**مثال.** صرف

**القواعد.** إلزامي. إحدى القيم: خصم، دفعات، رصيد افتتاحي، خصم خارجي.

:::admin
تحذف الرواتب أسطر الاقتطاع والرصيد الافتتاحي المؤرخة في شهر تعالجه وتعيد احتسابها. أدخل الاشتراكات اليدوية لتلك الأشهر على أنها اقتطاع خارجي.
:::
