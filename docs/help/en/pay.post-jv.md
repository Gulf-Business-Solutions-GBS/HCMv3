---
helpId: pay.post-jv
lang: en
title: Post JV
surfaces:
  - angular/src/app/admin/payroll-management/post-jv/post-jv.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PostJv]
---

## Overview

Posts a period's payroll journal voucher (JV) to Dynamics 365 Finance. The JV is built from the cost registers made on the **Cost Engine** screen, so generate the costing file first.

Choose the payroll area, run type, period and posting date, press **Journal Voucher** to preview the JV, check that it balances, then press **Post JV** and confirm. The posting runs in the background: the status panel follows it and **Show details** lists every journal header and line batch with Dynamics 365's reply.

Posting is refused, both before the run is queued and again just before the first journal is sent, when:

- the payroll area has no FI company or no FI bank;
- posting to Dynamics 365 Finance is switched off in the **Integration Center**;
- the JV has not been previewed, has no lines, or does not balance;
- the JV has changed since it was previewed ("Preview it again before posting").

Only one finance run (Cost Engine or Post JV) can be in progress at a time.

## Post JV {#pay.post-jv.post}
<!-- alias:  -->

**What it is.** Sends the previewed JV to Dynamics 365 Finance, after you confirm. Available only when a balanced preview is on screen and the area has its FI company and FI bank; otherwise hovering over the button shows why.

**Example.** Regular run for April: one salary journal, "Payroll/Salary JV Posted Successfully.", with its journal batch number.

**Why it matters.** A **Regular** run posts the area as one journal, or two (salary and provision) when *Separate Provision Posting* is on. An **Off Cycle** run posts one journal per employee who has an off-cycle cost register for the period, each with its own header and log line. If a journal fails part-way, what has already reached Dynamics 365 stays there and is not reversed: the failure line in **Show details** names the journals that had already been posted, so finance can finish or reverse them by hand. Needs the right to post JVs.
