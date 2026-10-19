---
helpId: adm.user-credential-reset
lang: en
title: Reset User Passwords
surfaces:
  - angular/src/app/admin/administration/user-credential-reset/user-credential-reset.component.html
audience: [admin]
permissions: [Pages.Administration.Tools.ResetUserCredentials]
---

## Overview

Gives the users you pick, or everyone in a role, a new temporary password by email. Each user signs in with it once and must then choose a password of their own. Typical uses are go-live, when every account needs its first password, and a suspected leak.

Only active users of your company are listed, and never administrators (holders of the Admin role) or your own account. A user without an email address is listed but cannot be ticked, since there is nowhere to send the password.

Each user is done separately, in this order: a new password is made and checked against your password rules, emailed, and only then saved. So if the email fails, the user's old password goes on working and nobody is locked out with a password they never received. Every run is confirmed first, and does at most 200 users; do a larger group in several runs. Outbound email must be switched on in the Integration Center, or the run is refused before anything changes.

:::admin
The email is the standard account email: user name, company and the new password, with an email confirmation link. It goes through the company's own email settings, which each company sets under Settings.
:::

## Role {#adm.user-credential-reset.role}
<!-- alias:  -->

**What it is.** Narrows the list to the users holding one role. **All roles** lists everybody who can be reset. The Admin role is not offered.

**Example.** Employees

**Why it matters.** With a role chosen, **Reset everyone in** that role appears beside the reset button. It does every eligible user in the role, not only the ones on the current page. Ticked users are not part of that run.

## Users {#adm.user-credential-reset.users}
<!-- alias:  -->

**What it is.** The users who can be reset, with their user name, full name, email address and, where the account is linked to one, the employee code. Search finds a user by user name, name, surname or email address.

**Example.** Tick three users on page 1 and two on page 2: the count above the list reads "5 user(s) selected".

**Why it matters.** Ticks are kept while you page and search; the box in the heading ticks or clears the whole page, and **Clear** empties the selection.

## ResetAndEmailPasswords {#adm.user-credential-reset.reset}
<!-- alias:  -->

**What it is.** Resets the ticked users after you confirm. Each one gets a new temporary password by email and must change it at their next sign-in.

**Why it matters.** From that moment the old password of everyone reset stops working. Tell people before you press it, and make sure their email addresses are right — a password sent to an old address cannot be recovered from this screen.

## Result {#adm.user-credential-reset.result}
<!-- alias:  -->

**What it is.** What the last run did: how many passwords were reset and how many emails were sent, then one line per user. Lines in red are users whose password was not changed — no email address, refused by the password rules, the email failed, the password could not be saved after the email went out, or not a user this tool may reset.

**Example.** "Completed. 41 password(s) reset, 41 e-mail(s) sent." with one red line for a user whose email failed.

**Why it matters.** A red line means the user still has their old password; fix the cause and reset them again.
