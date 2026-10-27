using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Sponsor bank-account record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.SponsorBank</c> -- legacy misfiles this Sponsor
/// satellite entity under the PayrollManagement business-object namespace/module, despite it being purely
/// Sponsor-related data; this port places it alongside <see cref="Sponsor"/> and its other satellite
/// entities (<see cref="SponsorJobTitle"/>, <see cref="SponsorSignatory"/>,
/// <see cref="SponsorGovtRepresentative"/>, <see cref="SponsorLicenseInfo"/>) in
/// <c>HCMv3.GovtRelationsManagement</c> instead).
///
/// <see cref="EmployeeBankId"/>/<see cref="PaymentBankId"/> both FK <c>HCMv3.GeneralMasterInfo.Bank</c>
/// (already ported).
///
/// MULTI-CASCADE-PATH FK RISK: this entity has TWO FKs to the same <c>Banks</c> table
/// (<see cref="EmployeeBankId"/> and <see cref="PaymentBankId"/>). SQL Server will reject multiple cascade
/// delete paths to the same table by default -- when this entity is wired into the batched DbContext, both
/// relationships (or at minimum one of them) must be configured with an explicit
/// <c>.OnDelete(DeleteBehavior.Restrict)</c> in <c>OnModelCreating</c> to avoid a
/// "may cause cycles or multiple cascade paths" migration error. NOT configured here since this wave does
/// not touch <c>HCMv3DbContext</c>.
/// </summary>
public class SponsorBank : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SponsorId { get; set; }

    public virtual Sponsor Sponsor { get; set; }

    public virtual int? EmployeeBankId { get; set; }

    public virtual Bank EmployeeBank { get; set; }

    public virtual int PaymentBankId { get; set; }

    public virtual Bank PaymentBank { get; set; }

    [StringLength(256)]
    public virtual string AccountNumber { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    [StringLength(64)]
    public virtual string IBAN { get; set; }
}
