---
helpId: pa.employee-certification.edit
lang: en
title: Employee Certifications — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeCertification
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/certifications/create-or-edit-employee-certification-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeCertifications.Create]
---

## Overview

A course or professional certification the employee has completed.

It is recorded for reference; no calculation or report reads it at present. Training certifications can be linked from the employee's training records.

## Course {#EmployeeCertification.Course}
<!-- alias: loc:Course, dom:EmployeeCertification_Course -->

**What it is.** The name of the course.

**Example.** Certified Payroll Professional

**Rules.** Required. Maximum 256 characters.

## Institution {#EmployeeCertification.InstitutionId}
<!-- alias: loc:Institution, dom:EmployeeCertification_InstitutionId -->

**What it is.** The institution that ran the course.

**Example.** Kuwait Institute for Scientific Research

**Rules.** Required. Chosen from Institution master data.

## Duration {#EmployeeCertification.Duration}
<!-- alias: loc:Duration, dom:EmployeeCertification_Duration -->

**What it is.** How long the course lasted, as free text.

**Example.** 3 months

**Rules.** Optional. Maximum 64 characters.

## Certificate {#EmployeeCertification.Certificate}
<!-- alias: loc:Certificate, dom:EmployeeCertification_Certificate -->

**What it is.** The certificate awarded.

**Example.** CPP

**Rules.** Optional. Maximum 256 characters.

## Grade {#EmployeeCertification.Grade}
<!-- alias: loc:Grade, dom:EmployeeCertification_Grade -->

**What it is.** The grade achieved.

**Example.** Pass

**Rules.** Optional. Maximum 64 characters.

## CertificationType {#EmployeeCertification.CertificationType}
<!-- alias: loc:CertificationType, dom:EmployeeCertification_CertificationType -->

**What it is.** Whether this is a professional certification or one gained through training.

**Example.** Training

**Why it matters.** Only Training certifications can be linked from the employee's training records. Records that existed before this field was added are Professional.

**Rules.** Required. One of: Professional, Training.
