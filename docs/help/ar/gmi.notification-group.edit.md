---
helpId: gmi.notification-group.edit
lang: ar
title: مجموعات الإخطار — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.NotificationGroup
surfaces:
  - angular/src/app/admin/general-master-info/notification-groups/create-or-edit-notification-group-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.NotificationGroups.Create]
---

## Overview

مجموعات مرتبطة بإعدادات إخطار. لا تُستخدم حالياً: فإرسال الإخطارات إلى المجموعات لم يُنقل إلى HCMv3.

## NotificationConfig {#NotificationGroup.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:NotificationGroup_NotificationConfigId -->

**ما هو.** إعدادات الإخطار التي تنتمي إليها المجموعة.

**القواعد.** اختياري. يُختار من قائمة إعدادات الإخطار.

## NotifyRoleHolders {#NotificationGroup.WorkflowRoleId}
<!-- alias: loc:NotifyRoleHolders, dom:NotificationGroup_WorkflowRoleId -->

**ما هو.** دور سير عمل يُبلَّغ شاغلوه عند تنفيذ هذا الإخطار.

**مثال.** شريك أعمال الموارد البشرية

**لماذا يهم.** يُضاف إلى المستلمين كل من يشغل الدور في ذلك اليوم. والمجموعة التي ليس لها دور لا تضيف أحداً. وإذا أُشِّر على **إرسال إلى المعتمِد** في الإعداد، فلا تُستخدم المجموعات إطلاقاً ولا يُبلَّغ إلا المعتمِدون.

**القواعد.** اختياري. يُختار من قائمة أدوار سير العمل.

## NarrowToRequesterOrgScope {#NotificationGroup.WorkflowRoleGroup}
<!-- alias: loc:NarrowToRequesterOrgScope, dom:NotificationGroup_WorkflowRoleGroup -->

**ما هو.** يقصر شاغلي الدور على من هم في الجزء نفسه من المؤسسة الذي ينتمي إليه مقدّم الطلب.

**مثال.** القسم

**لماذا يهم.** مع «القسم» مثلاً، لا يُبلَّغ إلا شاغلو الدور في قسم مقدّم الطلب. و«لا شيء» يبلّغ جميع الشاغلين. وإذا لم يبقَ أحد بعد هذا القصر، يُبلَّغ جميع شاغلي الدور بدلاً من ذلك، فلا يضيع الإخطار.

**القواعد.** إلزامي. إحدى القيم: لا شيء، الشركة، الإدارة، الفرع، مركز التكلفة.
