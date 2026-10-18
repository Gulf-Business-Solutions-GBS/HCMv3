---
helpId: rec.applicant.edit
lang: ar
title: المتقدمون — إنشاء / تحرير
entity: HCMv3.Recruitment.Applicant
surfaces:
  - angular/src/app/admin/recruitment/applicants/create-or-edit-applicant-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.Applicants.Create]
---

## Overview

مرشح لوظيفة. ينشئ المرشحون أنفسهم عادةً بالتسجيل في موقع الوظائف وتعبئة ملفاتهم، ويمكن للموارد البشرية أيضاً إدخالهم هنا.

وعندما يُضبط أحد طلباتهم على *التحق*، يُنسخ المتقدم إلى سجل موظف جديد -- الأسماء وتاريخ الميلاد والجنس والجنسية والديانة والحالة الاجتماعية والمؤهل وبيانات الاتصال والرقم المدني وتاريخا الالتحاق ونهاية التجربة -- ما لم يوجد موظف بالأسماء الإنجليزية وتاريخ الميلاد نفسها.

ويجب أن يكون تاريخ الميلاد في الماضي، وتاريخ الالتحاق بعد تاريخ الميلاد، ونهاية التجربة بعد الالتحاق. ولا يكون البريد الإلكتروني إلا لمتقدم واحد.

## Title {#Applicant.TitleId}
<!-- alias: loc:Title, dom:Applicant_TitleId -->

**ما هو.** لقب المتقدم.

**مثال.** السيد

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** اختياري. يُختار من قائمة الألقاب.

## FirstNameEn {#Applicant.FirstNameEn}
<!-- alias: loc:FirstNameEn, dom:Applicant_FirstNameEn -->

**ما هو.** الجزء الأول بالإنجليزية من اسم المتقدم.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** إلزامي.

## SecondNameEn {#Applicant.SecondNameEn}
<!-- alias: loc:SecondNameEn, dom:Applicant_SecondNameEn -->

**ما هو.** الجزء الثاني بالإنجليزية من اسم المتقدم.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** اختياري.

## ThirdNameEn {#Applicant.ThirdNameEn}
<!-- alias: loc:ThirdNameEn, dom:Applicant_ThirdNameEn -->

**ما هو.** الجزء الثالث بالإنجليزية من اسم المتقدم.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** اختياري.

## LastNameEn {#Applicant.LastNameEn}
<!-- alias: loc:LastNameEn, dom:Applicant_LastNameEn -->

**ما هو.** اسم العائلة بالإنجليزية من اسم المتقدم.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** إلزامي.

## FirstNameAr {#Applicant.FirstNameAr}
<!-- alias: loc:FirstNameAr, dom:Applicant_FirstNameAr -->

**ما هو.** الجزء الأول بالعربية من اسم المتقدم.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** إلزامي.

## SecondNameAr {#Applicant.SecondNameAr}
<!-- alias: loc:SecondNameAr, dom:Applicant_SecondNameAr -->

**ما هو.** الجزء الثاني بالعربية من اسم المتقدم.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** اختياري.

## ThirdNameAr {#Applicant.ThirdNameAr}
<!-- alias: loc:ThirdNameAr, dom:Applicant_ThirdNameAr -->

**ما هو.** الجزء الثالث بالعربية من اسم المتقدم.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** اختياري.

## LastNameAr {#Applicant.LastNameAr}
<!-- alias: loc:LastNameAr, dom:Applicant_LastNameAr -->

**ما هو.** اسم العائلة بالعربية من اسم المتقدم.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** إلزامي.

## BirthDate {#Applicant.BirthDate}
<!-- alias: loc:BirthDate, dom:Applicant_BirthDate -->

**ما هو.** تاريخ الميلاد.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم. يُحتسب ضمن قوة الملف التي تُعرض للمرشح في موقع الوظائف. ويعرض تقرير البحث عن المتقدمين العمر منه.

**القواعد.** إلزامي. تاريخ.

## Gender {#Applicant.Gender}
<!-- alias: loc:Gender, dom:Applicant_Gender -->

**ما هو.** ذكر أو أنثى.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم. ويمكن لتقرير البحث عن المتقدمين التصفية به.

**القواعد.** إلزامي. إحدى القيم: ذكر، أنثى.

## MaritalStatus {#Applicant.MaritalStatusId}
<!-- alias: loc:MaritalStatus, dom:Applicant_MaritalStatusId -->

**ما هو.** الحالة الاجتماعية.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم. ويمكن لتقرير البحث عن المتقدمين التصفية به.

**القواعد.** اختياري. يُختار من قائمة الحالات الاجتماعية.

## Nationality {#Applicant.NationalityId}
<!-- alias: loc:Nationality, dom:Applicant_NationalityId -->

**ما هو.** الجنسية.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم. يُحتسب ضمن قوة الملف التي تُعرض للمرشح في موقع الوظائف.

**القواعد.** إلزامي. يُختار من قائمة الجنسيات.

## Religion {#Applicant.ReligionId}
<!-- alias: loc:Religion, dom:Applicant_ReligionId -->

**ما هو.** الديانة.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** اختياري. يُختار من قائمة الديانات.

## Qualification {#Applicant.QualificationId}
<!-- alias: loc:Qualification, dom:Applicant_QualificationId -->

**ما هو.** أعلى مؤهل.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** اختياري. يُختار من قائمة المؤهلات.

## JobTitle {#Applicant.JobTitleId}
<!-- alias: loc:JobTitle, dom:Applicant_JobTitleId -->

**ما هو.** المسمى الوظيفي الذي يبحث عنه المتقدم.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. يُختار من قائمة المسميات الوظيفية.

## BloodGroup {#Applicant.BloodGroup}
<!-- alias: loc:BloodGroup, dom:Applicant_BloodGroup -->

**ما هو.** فصيلة الدم.

**مثال.** `O+`

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** اختياري. الحد الأقصى 50 حرفاً.

## CivilID {#Applicant.CivilID}
<!-- alias: loc:CivilID, dom:Applicant_CivilID -->

**ما هو.** الرقم المدني الكويتي.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم. يُحتسب ضمن قوة الملف التي تُعرض للمرشح في موقع الوظائف.

**القواعد.** إلزامي. رقم صحيح.

## Country {#Applicant.CountryId}
<!-- alias: loc:Country, dom:Applicant_CountryId -->

**ما هو.** بلد الإقامة.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** اختياري. يُختار من قائمة الدول.

## PassportNum {#Applicant.PassportNum}
<!-- alias: loc:PassportNum, dom:Applicant_PassportNum -->

**ما هو.** رقم جواز السفر.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. الحد الأقصى 100 حرف.

## PlaceOfBirth {#Applicant.PlaceOfBirth}
<!-- alias: loc:PlaceOfBirth, dom:Applicant_PlaceOfBirth -->

**ما هو.** مكان الميلاد.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## DrivingLicense {#Applicant.DrivingLicense}
<!-- alias: loc:DrivingLicense, dom:Applicant_DrivingLicense -->

**ما هو.** رقم رخصة القيادة.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. رقم صحيح.

## Photo {#Applicant.PhotoPath}
<!-- alias: loc:Photo, dom:Applicant_Photo -->

**ما هو.** صورة المتقدم.

**لماذا يهم.** يُحتسب ضمن قوة الملف التي تُعرض للمرشح في موقع الوظائف.

**القواعد.** اختياري. الحد الأقصى 512 حرفاً.

## CurrentAddress {#Applicant.CurrentAddress}
<!-- alias: loc:CurrentAddress, dom:Applicant_CurrentAddress -->

**ما هو.** العنوان الحالي.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** اختياري.

## PermanentAddress {#Applicant.PermanentAddress}
<!-- alias: loc:PermanentAddress, dom:Applicant_PermanentAddress -->

**ما هو.** العنوان الدائم.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم. يُحتسب ضمن قوة الملف التي تُعرض للمرشح في موقع الوظائف.

**القواعد.** اختياري.

## Address1 {#Applicant.Address1}
<!-- alias: loc:Address1, dom:Applicant_Address1 -->

**ما هو.** سطر عنوان.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري.

## Address2 {#Applicant.Address2}
<!-- alias: loc:Address2, dom:Applicant_Address2 -->

**ما هو.** سطر عنوان.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري.

## Telephone {#Applicant.Telephone}
<!-- alias: loc:Telephone, dom:Applicant_Telephone -->

**ما هو.** رقم هاتف أرضي.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم.

**القواعد.** اختياري. رقم صحيح.

## Mobile {#Applicant.Mobile}
<!-- alias: loc:Mobile, dom:Applicant_Mobile -->

**ما هو.** رقم الهاتف النقال.

**مثال.** `96590001234`

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم. يُحتسب ضمن قوة الملف التي تُعرض للمرشح في موقع الوظائف.

**القواعد.** اختياري. رقم صحيح.

## Email {#Applicant.Email}
<!-- alias: loc:Email, dom:Applicant_Email -->

**ما هو.** البريد الإلكتروني.

**لماذا يهم.** يُنسخ إلى الموظف الجديد عند التحاق المتقدم. يُحتسب ضمن قوة الملف التي تُعرض للمرشح في موقع الوظائف. ولا يكون البريد إلا لمتقدم واحد.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## JoiningDate {#Applicant.JoiningDate}
<!-- alias: loc:JoiningDate, dom:Applicant_JoiningDate -->

**ما هو.** تاريخ التحاق المتقدم بعد تعيينه.

**لماذا يهم.** يجب ملؤه قبل ضبط الطلب على *التحق*، ويصبح تاريخ التحاق الموظف.

**القواعد.** إلزامي. تاريخ.

## ProbationEndDate {#Applicant.ProbationEndDate}
<!-- alias: loc:ProbationEndDate, dom:Applicant_ProbationEndDate -->

**ما هو.** نهاية فترة التجربة بعد التعيين.

**لماذا يهم.** يجب ملؤه قبل ضبط الطلب على *التحق*.

**القواعد.** اختياري. تاريخ.

## IsAgency {#Applicant.IsAgency}
<!-- alias: loc:IsAgency, dom:Applicant_IsAgency -->

**ما هو.** ما إذا كان المتقدم قد جاء عن طريق وكالة.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** نعم أو لا.

## CompletionPoints {#Applicant.CompletionPoints}
<!-- alias: loc:CompletionPoints, dom:Applicant_CompletionPoints -->

**ما هو.** مدى اكتمال ملف المرشح في بوابة التوظيف، من 60 نقطة. تُحتسب ولا تُدخل يدوياً.

**مثال.** 35

**لماذا يهم.** 5 نقاط لكل من الصورة والبطاقة المدنية والجنسية والعنوان الدائم وتاريخ الميلاد والبريد الإلكتروني والهاتف المحمول، و25 نقطة لوجود سيرة ذاتية واحدة على الأقل. تُحفظ عندما يحفظ المرشح ملفه؛ وهي للعلم فقط.

**القواعد.** اختياري. رقم صحيح.

## IsOnbdStarted {#Applicant.IsOnbdStarted}
<!-- alias: loc:IsOnbdStarted, dom:Applicant_IsOnbdStarted -->

**ما هو.** ما إذا كان يجوز بدء التهيئة الوظيفية لهذا المتقدم.

**لماذا يهم.** يُسجَّل فقط. ولا يقرؤه شيء حالياً.

**القواعد.** نعم أو لا.

## IsOnbdInit {#Applicant.IsOnbdInit}
<!-- alias: loc:IsOnbdInit, dom:Applicant_IsOnbdInit -->

**ما هو.** ما إذا كانت التهيئة الوظيفية قد جُهِّزت لهذا المتقدم.

**لماذا يهم.** يُسجَّل فقط. ولا يقرؤه شيء حالياً.

**القواعد.** نعم أو لا.

## OnbCoordinator {#Applicant.OnbCoordinatorId}
<!-- alias: loc:OnbCoordinator, dom:Applicant_OnbCoordinatorId -->

**ما هو.** الموظف الذي ينسّق التهيئة الوظيفية لهذا المتقدم.

**مثال.** فاطمة الصباح (الموارد البشرية)

**لماذا يهم.** يُسجَّل فقط. ولا يقرؤه شيء حالياً.

**القواعد.** اختياري. يُختار من قائمة.

## OnbBuddy {#Applicant.OnbBuddyId}
<!-- alias: loc:OnbBuddy, dom:Applicant_OnbBuddyId -->

**ما هو.** الموظف الذي يرافق الموظف الجديد أثناء التهيئة الوظيفية.

**مثال.** أحمد خان

**لماذا يهم.** يُسجَّل فقط. ولا يقرؤه شيء حالياً.

**القواعد.** اختياري. يُختار من قائمة.
