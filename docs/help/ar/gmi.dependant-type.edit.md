---
helpId: gmi.dependant-type.edit
lang: ar
title: أنواع صفات العائل — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.DependantType
surfaces:
  - angular/src/app/admin/general-master-info/dependant-types/create-or-edit-dependant-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.DependantTypes.Create]
---

## Overview

صفات العائل التي يمكن أن تكون للموظف -- زوج أو زوجة، ابن، ابنة، ونحو ذلك. لكل عائل مسجَّل على الموظف صفة من هذه الصفات، وتُحدَّد حدود استرداد المصاريف الصحية لكل صفة عائل.

## Gender {#DependantType.Gender}
<!-- alias: loc:Gender, dom:DependantType_Gender -->

**ما هو.** الجنس الذي تقتضيه صفة العائل -- فالابن ذكر والابنة أنثى.

**لماذا يهم.** يُسجَّل للعلم، ولا يطابق شيءٌ جنسَ العائل به.

**القواعد.** إلزامي. إحدى القيم: ذكر، أنثى.

## IsEmployee {#DependantType.IsEmployee}
<!-- alias: loc:IsEmployee, dom:DependantType_IsEmployee -->

**ما هو.** يميّز الصفة التي تمثّل الموظف نفسه لا أحد أقاربه.

**لماذا يهم.** يستخدمها استرداد المصاريف الصحية للتمييز بين حد الموظف نفسه وحدود عائليه. ولا يجوز تحديدها إلا لصفة عائل واحدة.

**القواعد.** نعم أو لا.
