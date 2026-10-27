---
helpId: ess.bank
lang: en
title: My Bank Details
surfaces:
  - angular/src/app/main/ess/my-bank/my-bank.component.html
audience: [end-user]
permissions: [Ess.MyBank]
---

## Overview

Ask for the account your salary is paid into to be changed, and see the accounts already on your record.

Nothing changes until the request is approved, and the change takes effect from the date you give — so a request raised after payroll has been run for the month is paid into the old account that month. Raise it early.

## Bank {#ess.bank.bank}
<!-- alias: loc:Bank -->

**What it is.** The bank holding the new account, from the banks your company can pay to.

**Example.** National Bank of Kuwait.

**Why it matters.** Salaries go out as one file to each bank, so an account at a bank your company does not pay to cannot be used, however valid the account is.

## Account number {#ess.bank.account-number}
<!-- alias: loc:AccountNumber -->

**What it is.** The account, as the bank writes it — usually the IBAN.

**Example.** KW81CBKU0000000000001234560101

**Why it matters.** **Check it character by character against your own bank statement.** A wrong digit does not bounce politely; it either fails the whole payroll file or pays a stranger, and getting the money back after that is slow and is not certain.

## Branch {#ess.bank.bank-branch}
<!-- alias: loc:BankBranch -->

**What it is.** The branch the account is held at, where your company records branches.

**Example.** Salmiya Branch.

**Why it matters.** Most transfers do not need it — the account number carries the routing. It is recorded because some banks and some statutory returns still ask for it.

## Start date {#ess.bank.start-date}
<!-- alias: loc:StartDate -->

**What it is.** The date the new account takes over.

**Example.** 1 August 2026.

**Why it matters.** Payroll uses whichever account is in force on the day it runs, so this date decides which month is the first paid into the new one. A date in a month already processed changes nothing for that month.
