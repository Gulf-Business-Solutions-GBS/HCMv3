using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TempBenefitType"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditTempBenefitTypeDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string Grade { get; set; }

    [StringLength(256)]
    public string Ticket { get; set; }

    [StringLength(256)]
    public string SimCard { get; set; }

    [StringLength(256)]
    public string Phone { get; set; }

    [StringLength(256)]
    public string Incentive { get; set; }

    [StringLength(256)]
    public string BonusSIP { get; set; }

    [StringLength(256)]
    public string Uniform { get; set; }

    [StringLength(256)]
    public string MedicalInsurance { get; set; }

    [StringLength(256)]
    public string LeaveDays { get; set; }

    [StringLength(256)]
    public string MedicalLeave { get; set; }

    [StringLength(256)]
    public string MaternityLeave { get; set; }

    [StringLength(256)]
    public string StaffDiscount { get; set; }

    [StringLength(256)]
    public string SpecialAllowance { get; set; }

    [StringLength(256)]
    public string ManagerAllowance { get; set; }

    [StringLength(256)]
    public string CompanyAsset { get; set; }

    [StringLength(256)]
    public string SchoolAllowance { get; set; }

    [StringLength(256)]
    public string NoticePeriod { get; set; }

    [StringLength(256)]
    public string Indemnity { get; set; }

    [StringLength(256)]
    public string Retirement { get; set; }

    [StringLength(256)]
    public string MinSalary { get; set; }

    [StringLength(256)]
    public string MidSalary { get; set; }

    [StringLength(256)]
    public string MaxSalary { get; set; }

    [StringLength(256)]
    public string TransportationAllowance { get; set; }

    [StringLength(256)]
    public string HousingAllowance { get; set; }

    [StringLength(256)]
    public string OtherAllowance { get; set; }
}
