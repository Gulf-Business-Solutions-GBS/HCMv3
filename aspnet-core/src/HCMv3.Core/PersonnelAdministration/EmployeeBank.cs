using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee bank-account assignment record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeBank</c>, Wave 4 deferred bucket).
///
/// <see cref="BankId"/>/<see cref="PayerBankId"/> both FK <c>HCMv3.GeneralMasterInfo.Bank</c> (already
/// ported). <see cref="PaymentCurrencyId"/> FKs <c>HCMv3.GeneralMasterInfo.Currency</c>. The legacy
/// <c>BankCountry</c> FK (<c>GeneralMasterInfo.Country</c>) is ported as <see cref="BankCountryId"/>.
///
/// MULTI-CASCADE-PATH FK RISK: this entity has TWO FKs to the same <c>Banks</c> table
/// (<see cref="BankId"/> and <see cref="PayerBankId"/>), same pattern already flagged for Wave 3's
/// <c>SponsorBank</c>. SQL Server will reject multiple cascade delete paths to the same table by default --
/// when this entity is wired into the batched DbContext, at least one of the two relationships must be
/// configured with an explicit <c>.OnDelete(DeleteBehavior.Restrict)</c> in <c>OnModelCreating</c> to avoid
/// a "may cause cycles or multiple cascade paths" migration error. NOT configured here since this wave does
/// not touch <c>HCMv3DbContext</c>.
///
/// DELIBERATE SIMPLIFICATION: the legacy auto-EndDate cascade (each new/edited/deleted EmployeeBank record
/// recomputes sibling records' <c>EndDate</c> so periods never overlap) is NOT ported -- only the essential
/// no-duplicate-StartDate-per-employee validation is (see <c>EmployeeBanksAppService</c>, same
/// cross-record-cascade omission approach as every other ledger entity in this port).
/// </summary>
public class EmployeeBank : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int? BankId { get; set; }

    public virtual Bank Bank { get; set; }

    /// <summary>Legacy display name "Effective Date".</summary>
    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int? BankCountryId { get; set; }

    public virtual Country BankCountry { get; set; }

    public virtual PaymentType PaymentType { get; set; }

    [StringLength(256)]
    public virtual string AccountNum { get; set; }

    public virtual int? PaymentCurrencyId { get; set; }

    public virtual Currency PaymentCurrency { get; set; }

    [StringLength(256)]
    public virtual string IBAN { get; set; }

    [StringLength(256)]
    public virtual string BankBranch { get; set; }

    /// <summary>
    /// Legacy comment: "for 19-Recafco client, to fix the employees from Payer bank". See class remarks
    /// for the multi-cascade-path FK risk this creates alongside <see cref="BankId"/>.
    /// </summary>
    public virtual int? PayerBankId { get; set; }

    public virtual Bank PayerBank { get; set; }
}
