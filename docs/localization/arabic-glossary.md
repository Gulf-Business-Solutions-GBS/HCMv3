# Arabic terminology glossary (ar-KW)

The vocabulary HCMv3 uses in Arabic, and where it comes from.

## Where these terms came from

The legacy Pinnacle HRMS has Arabic in three places:

| Source | Contents |
|---|---|
| `ModelDifferenceAspect` table, `ar-KW` row, in each customer database | ~10,560 captions — the live translations a customer actually sees |
| `HCM10.Web/Model_ar-KW.xafml` | ~10,593 captions — the shipped baseline the database layers over |
| `HCM10.ESS/App_GlobalResources/EssResource.ar-KW.resx` and the Recruiting equivalent | ESS and careers-portal page text |

Those hold **2,075 distinct identifiers** with Arabic, of which **589 match an HCMv3
localization key by name**.

**We take the terminology, not the text.** People have read these words for years, so
the vocabulary is worth inheriting even where a more literary alternative exists. But
the legacy Arabic is a mix of careful human translation and machine output, and a
number of entries are outright wrong. Those are corrected here rather than imported.

## Register

Modern Standard Arabic (فصحى) with Kuwaiti terminology. Not dialect: the strings that
were already in HCMv3-ar.xml are فصحى, and payroll and labour-law terms have no dialect
form, so a dialect pass would clash with both.

## Core terms

| English | Arabic | Note |
|---|---|---|
| Employee | الموظف | |
| Organization / Company | الشركة | legacy uses الشركة, not المنشأة |
| Organization Unit | وحدة الشركة | |
| Cost Center | مركز التكلفة | |
| Job Title | المسمى الوظيفي | |
| Work Location | مكان العمل | |
| Access Unit | وحدة الوصول | |
| Sponsor | الكفيل | Kuwait-specific and important |
| Civil ID | البطاقة المدنية | |
| PACI | الهيئة العامة للمعلومات المدنية | |
| MOSAL ID | رقم الشؤون | |
| Payroll Area | منطقة الرواتب | |
| Loan | القرض | legacy term; NOT سلفة |
| Document | الوثيقة | |
| Dependant | العائل | DependantType = صفة العائل |
| Leave | الإجازة | |
| Leave Quota | الحصة | Accrued = المتراكمة, Used = المستخدمة |
| Accrual | التراكم | |
| Attendance (hours) | الحضور | |
| Attendance (type) | المواظبة | |
| Overtime | العمل الإضافي | |
| Duty / Shift | الدوام / الوردية | Roster = ورديات العمل |
| Separation | الفصل | |
| Clearance | إخلاء الطرف | |
| Warning Letter | لفت نظر | legacy term, widely understood |
| Disciplinary | تأديبي / مخالفة | |
| Indemnity / Gratuity | مكافأة نهاية الخدمة | see correction below |
| Air Ticket | تذكرة الطيران | |
| Probation | فترة الاختبار | |
| Notice Period | فترة الإشعار | |

## Legacy entries deliberately NOT imported

Each of these is in the legacy Arabic and is wrong. They are listed so nobody
"restores" them later thinking a term was missed.

| Key | Legacy Arabic | What it actually says | Used instead |
|---|---|---|---|
| `Mobile` | الجنسية | "nationality" | الهاتف المتنقل |
| `ThirdNameEn` | اسم المستخدم | "user name" | الاسم الثالث بالإنجليزية |
| `Description` | اسم المستخدم | "user name" | الوصف |
| `JobTitle` | تاريخ التحميل | "upload date" | المسمى الوظيفي |
| `University` | النسبة المئوية | "percentage" | الجامعة |
| `Default` | اهمال | "neglect" | الافتراضي |
| `Amount` | كمية | "quantity" | المبلغ |
| `FullNameEn` | السيرة الذاتية لمقدم الطلب | "applicant's CV" | الاسم الكامل بالإنجليزية |
| `RestDays` | الأيام المتبقية | "remaining days" | أيام الراحة |
| `GratuityPaidDate` | البقشيش المدفوع التاريخ | بقشيش is a waiter's tip | تاريخ صرف مكافأة نهاية الخدمة |
| `GratuityPaidDays` | البقشيش المدفوعة أيام | as above | أيام مكافأة نهاية الخدمة المدفوعة |
| `BreakEarlyOut` | كسر الخروج المبكر | "fracture", not rest break | الخروج المبكر من الاستراحة |
| `AdvLeaveRecoveryDays` | أيام الإجازة المرضية المتقدمة | says "sick", means advance | أيام استرداد الإجازة المقدمة |
| `IsWeeklyOff` | هو أسبوعيا معطل | machine output | عطلة أسبوعية |
| `EffectiveDate` | تاريخ التعين | means "appointment", misspelled | تاريخ السريان |

## Spelling corrected on import

Legacy has recurring orthographic slips. Corrected, not carried over:

- كلمه → **كلمة** (`Password`)
- العهده → **العهدة** (`AssetType`)
- أعدادات → **إعدادات** (`NotificationConfig`)
- ارسل بريد الكتروني → **إرسال بريد إلكتروني** (`SendEmail`)
- أيقاف → **إيقاف** (`OnHoldLoanDeduction`)
- Trailing colons stripped from labels (`LoanDeductionType:`, `OvertimeRate:`, `AdjustmentType:`)

## Still to be reviewed by a native speaker

This glossary is a starting point, not an authority. Terms tied to Kuwait Labour Law
— indemnity calculation, MOSAL wording, leave categories — carry legal weight, and a
subtly wrong term on a payslip screen is a different class of error from a mistranslated
button. A native Arabic HR reviewer should pass over the payroll and end-of-service
screens before go-live.
