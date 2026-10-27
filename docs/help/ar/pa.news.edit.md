---
helpId: pa.news.edit
lang: ar
title: الإعلانات — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.News
surfaces:
  - angular/src/app/admin/personnel-administration/announcements/create-or-edit-announcement-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.News.Create, Pages.PersonnelAdministration.News.Edit]
---

## Overview

إعلان من الشركة للموظفين.

يرى الموظفون الإعلانات في لوحة الخدمة الذاتية بين تاريخ الإصدار وتاريخ الانتهاء. ويُحفظ خيارا الإشعار والبريد الإلكتروني، لكن الإعلانات لا تُرسل بإشعار أو بريد إلكتروني حالياً.

## Title {#News.Title}
<!-- alias: loc:Title, dom:Announcement_Title -->

**ما هو.** عنوان الإعلان بالإنجليزية.

**مثال.** Office closed for National Day

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## TitleAr {#News.TitleAr}
<!-- alias: loc:TitleAr, dom:Announcement_TitleAr -->

**ما هو.** عنوان الإعلان بالعربية.

**مثال.** إغلاق المكتب بمناسبة العيد الوطني

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## IssueDate {#News.IssueDate}
<!-- alias: loc:IssueDate, dom:Announcement_IssueDate -->

**ما هو.** تاريخ بدء ظهور الإعلان.

**مثال.** 20 فبراير 2027

**لماذا يهم.** لا يجوز أن يأتي بعد تاريخ الانتهاء.

**القواعد.** إلزامي. تاريخ.

## ExpiryDate {#News.ExpiryDate}
<!-- alias: loc:ExpiryDate, dom:Announcement_ExpiryDate -->

**ما هو.** تاريخ توقف ظهور الإعلان.

**مثال.** 27 فبراير 2027

**القواعد.** إلزامي. تاريخ.

## Comments {#News.Comments}

**ما هو.** نص الإعلان بالإنجليزية.

**مثال.** The office will be closed on 25 and 26 February.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## SummaryAr {#News.CommentAr}
<!-- alias: loc:SummaryAr, dom:Announcement_CommentAr -->

**ما هو.** نص الإعلان بالعربية.

**مثال.** سيُغلق المكتب يومي 25 و26 فبراير.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## SendNotification {#News.SendNotification}
<!-- alias: loc:SendNotification, dom:Announcement_SendNotification -->

**ما هو.** هل يُبلَّغ الموظفون داخل التطبيق.

**لماذا يهم.** يُحفظ فقط، ولا يُرسل أي إشعار حالياً.

**القواعد.** نعم أو لا.

## SendEmail {#News.SendEmail}
<!-- alias: loc:SendEmail, dom:Announcement_SendEmail -->

**ما هو.** هل يُرسل بريد إلكتروني للموظفين.

**لماذا يهم.** يُحفظ فقط، ولا يُرسل أي بريد حالياً.

**القواعد.** نعم أو لا.
