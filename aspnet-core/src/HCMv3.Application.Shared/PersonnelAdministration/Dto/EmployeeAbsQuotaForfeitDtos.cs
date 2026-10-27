using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeAbsQuotaForfeitDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int AbsenceTypeId { get; set; }
    public string AbsenceTypeName { get; set; }
    public DateTime EffectiveDate { get; set; }
    public double QuotaForfeited { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeAbsQuotaForfeitDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int AbsenceTypeId { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    public double QuotaForfeited { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    /// <summary>
    /// Legacy <c>EmployeeAbsQuotaForfeit.DontUpdateQuotaBalance</c> (<c>[NonPersistent]</c>, legacy lines 60-63):
    /// when ticked, the save records the forfeit without moving the annual-leave quota (and so without the
    /// negative-balance check, legacy line 140-144). Not stored -- the edit form always opens unticked.
    /// </summary>
    public bool DontUpdateQuotaBalance { get; set; }
}

public class GetEmployeeAbsQuotaForfeitForEditOutput
{
    public CreateOrEditEmployeeAbsQuotaForfeitDto EmployeeAbsQuotaForfeit { get; set; }
}

public class GetAllEmployeeAbsQuotaForfeitsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "EffectiveDate DESC";
        }
    }
}
