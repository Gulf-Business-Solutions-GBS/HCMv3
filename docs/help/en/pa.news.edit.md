---
helpId: pa.news.edit
lang: en
title: Announcements — Create / Edit
entity: HCMv3.PersonnelAdministration.News
surfaces:
  - angular/src/app/admin/personnel-administration/announcements/create-or-edit-announcement-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.News.Create, Pages.PersonnelAdministration.News.Edit]
---

## Overview

A company announcement for employees.

Employees see announcements on their self-service dashboard between the issue date and the expiry date. The notification and email options are stored, but announcements are not sent by notification or email at present.

## Title {#News.Title}
<!-- alias: loc:Title, dom:Announcement_Title -->

**What it is.** The announcement title in English.

**Example.** Office closed for National Day

**Rules.** Required. Maximum 256 characters.

## TitleAr {#News.TitleAr}
<!-- alias: loc:TitleAr, dom:Announcement_TitleAr -->

**What it is.** The announcement title in Arabic.

**Example.** إغلاق المكتب بمناسبة العيد الوطني

**Rules.** Optional. Maximum 256 characters.

## IssueDate {#News.IssueDate}
<!-- alias: loc:IssueDate, dom:Announcement_IssueDate -->

**What it is.** The date the announcement starts showing.

**Example.** 20 Feb 2027

**Why it matters.** Cannot be after the expiry date.

**Rules.** Required. Date value.

## ExpiryDate {#News.ExpiryDate}
<!-- alias: loc:ExpiryDate, dom:Announcement_ExpiryDate -->

**What it is.** The date the announcement stops showing.

**Example.** 27 Feb 2027

**Rules.** Required. Date value.

## Comments {#News.Comments}

**What it is.** The announcement text in English.

**Example.** The office will be closed on 25 and 26 February.

**Rules.** Optional. Maximum 500 characters.

## SummaryAr {#News.CommentAr}
<!-- alias: loc:SummaryAr, dom:Announcement_CommentAr -->

**What it is.** The announcement text in Arabic.

**Example.** سيُغلق المكتب يومي 25 و26 فبراير.

**Rules.** Optional. Maximum 500 characters.

## SendNotification {#News.SendNotification}
<!-- alias: loc:SendNotification, dom:Announcement_SendNotification -->

**What it is.** Whether to notify employees in the app.

**Why it matters.** Stored only; no notification is sent at present.

**Rules.** Yes or no.

## SendEmail {#News.SendEmail}
<!-- alias: loc:SendEmail, dom:Announcement_SendEmail -->

**What it is.** Whether to email employees.

**Why it matters.** Stored only; no email is sent at present.

**Rules.** Yes or no.
