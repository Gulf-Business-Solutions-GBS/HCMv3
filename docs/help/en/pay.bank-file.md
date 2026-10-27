---
helpId: pay.bank-file
lang: en
title: Bank Files
surfaces:
  - angular/src/app/admin/payroll/bank-file/bank-file.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.BankFiles]
---

## Overview

Makes the salary transfer file to send to the bank, in the layout the paying bank expects, from the salary registers of a period.

Choose who is paid (a payroll area, a sponsor, a MOSAL ID or employee codes: at least one is needed), the period, the run type and the **Payment Bank**, then press **Generate Bank File**. The file downloads at once; with **Generate a separate file per employee bank** it is a zip holding one file per employee bank. A summary then shows the format, the period, the number of payees and the total amount.

Only salary registers that have lines are paid, and a register whose net pay (wage type 9000) is zero is left out. Each filter narrows the list further, in this order: payroll area, sponsor, MOSAL ID, payment type, employee bank, payment bank. Every bank, sponsor and org assignment record is read as in force on the period's end date.

When the payroll setting **Lock Salary Register On Payroll Exit** is on, making the file locks every selected salary register that is still unlocked, regular and off-cycle alike, including those the file leaves out (on hold, no account), so they can no longer be changed. The summary says how many were locked.

Every run, successful or not, is logged with its parameters and a zipped copy of the file: see **View Log**.

:::admin
When the FI posting setting *FIPostingAfterPayrollExit* is on and a payroll area is chosen, a file whose end date is after the area's last closed period is refused; the message names the last date allowed. The parameter text on the log is kept in the old system's English wording, because existing reports read it.
:::

## Payroll Area {#pay.bank-file.payroll-area}
<!-- alias:  -->

**What it is.** Pays the employees whose org assignment on the period end is in this payroll area.

**Example.** Kuwait Monthly

**Why it matters.** It also supplies the period when **Current** is chosen, from its payroll control record.

## Sponsor {#pay.bank-file.sponsor}
<!-- alias:  -->

**What it is.** Pays only the employees under this sponsor on the period end.

**Example.** Al-Sabah Trading Co.

**Why it matters.** Choosing a sponsor fills the **Sponsor Bank** list with that sponsor's accounts and clears any earlier choice.

## Sponsor Bank {#pay.bank-file.sponsor-bank}
<!-- alias:  -->

**What it is.** One of the chosen sponsor's bank accounts, shown as bank code and account number. Available only once a sponsor is chosen.

**Example.** NBK (2001234567)

**Why it matters.** Used by the layouts that print the company's account, for example in the file header; other layouts ignore it.

## MOSAL ID {#pay.bank-file.mosal}
<!-- alias:  -->

**What it is.** Pays only the employees whose sponsor on the period end has this MOSAL ID. The list holds the MOSAL IDs of your sponsors.

**Example.** 123456

## Employee Codes (comma separated) {#pay.bank-file.employee-codes}
<!-- alias:  -->

**What it is.** Pays only these employees.

**Example.** `1001, 1002`

**Why it matters.** Useful to remake the file for a few people. The other filters still apply.

## Employee Bank {#pay.bank-file.employee-bank}
<!-- alias:  -->

**What it is.** Pays only the employees whose own bank (where their salary goes) is this bank.

**Example.** Kuwait Finance House

## Payroll Run Type {#pay.bank-file.run-type}
<!-- alias:  -->

**What it is.** Which salary registers to pay: **Regular**, **Off Cycle**, or **Regular + Off Cycle**.

**Example.** Regular + Off Cycle

**Why it matters.** With **Regular + Off Cycle**, off-cycle registers are taken only when their payment date is on or after the cutoff day of the payroll month (payroll setting *Offcycle Bank File Cutoff*; 0 means the 1st).

## Generate a separate file per employee bank {#pay.bank-file.split}
<!-- alias:  -->

**What it is.** Makes one file per employee bank instead of a single file, and downloads them together as a zip.

**Example.** Ticked: one file for NBK payees, one for KFH payees.

## Period {#pay.bank-file.period}
<!-- alias:  -->

**What it is.** **Current** takes the payroll month, year, start and end dates from the payroll area's control record. **Other** lets you type a payroll month and year; the dates are then worked out from the area's payroll period (with no area, that calendar month). The start and end dates are read-only.

**Example.** Other, month 3, year 2026.

**Why it matters.** The dates are worked out on the server, so the file always covers the period exactly. Current needs a payroll area with a control record; otherwise the screen says why and the file cannot be made.

## Payment Type {#pay.bank-file.payment-type}
<!-- alias:  -->

**What it is.** **All**, **Bank** or **Cash**: the payment type on the employee's bank record in force on the period end.

**Example.** Bank

**Why it matters.** Even **All** leaves out employees with no bank record in force on that date.

## Payment Bank {#pay.bank-file.payment-bank}
<!-- alias:  -->

**What it is.** The bank the company pays salaries from. Its file format decides the layout. Required.

**Example.** NBK

**Why it matters.** Employees whose payer bank is this bank are paid; if nobody in the period has a payer bank set, employees with no payer bank are paid. A bank with no file format assigned is refused with a message naming the bank.

## Bank File Format {#pay.bank-file.format}
<!-- alias:  -->

**What it is.** The file formats known for the chosen payment bank, with their code and type: **Text** or **Grid** (a spreadsheet).

**Example.** A bank with one text layout and one spreadsheet layout shows two rows.

**Why it matters.** Grid formats download as .xlsx. For text formats the record count is the number of detail lines in the file.

## Generate Bank File {#pay.bank-file.generate}
<!-- alias:  -->

**What it is.** Makes and downloads the file. Available once a payment bank and a period are set and at least one of payroll area, sponsor, MOSAL ID or employee codes is given.

**Example.** "Total: 245 / 112,450.500"

**Why it matters.** Refused when no employee matches the filters or no salary register is found for them. Needs the right to generate bank files.

## View Log {#pay.bank-file.view-log}
<!-- alias:  -->

**What it is.** Opens the **Bank File Generation Log**: every run with its date and time, user, status, the parameters used and the file. **All**, **Successful** and **Failed** filter it, with counts.

**Example.** Download last month's NBK file again to check what was sent.

**Why it matters.** **Export Log** downloads the list as shown. **Download** gives back the zipped copy of a run's file; because it shows salaries, it needs the right to generate bank files.
