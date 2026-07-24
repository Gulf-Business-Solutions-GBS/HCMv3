using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee loan installment schedule row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.LoanInstallment</c>, Wave 2 deferred bucket).
/// Unlike its sibling entities, the legacy <c>EmployeeLoan</c> reference on this business object has no
/// <c>[Association]</c> attribute (an unassociated plain FK reference), and <see cref="InstallmentAmount"/>
/// is declared <c>decimal</c> in the legacy source (not <c>double</c> like the other monetary fields in this
/// module) -- both preserved faithfully here.
/// </summary>
public class LoanInstallment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeLoanId { get; set; }

    public virtual EmployeeLoan EmployeeLoan { get; set; }

    public virtual DateTime InstallmentDate { get; set; }

    public virtual decimal InstallmentAmount { get; set; }

    public virtual InstallmentStatus InstallmentStatus { get; set; }
}
