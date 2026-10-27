using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TempBenefitType"/>.
/// </summary>
public class TempBenefitTypeDto : EntityDto<int>
{
    public string Grade { get; set; }

    public string Ticket { get; set; }

    public string SimCard { get; set; }

    public string Phone { get; set; }

    public string Incentive { get; set; }

    public string BonusSIP { get; set; }

    public string Uniform { get; set; }

    public string MedicalInsurance { get; set; }

    public string LeaveDays { get; set; }

    public string MedicalLeave { get; set; }

    public string MaternityLeave { get; set; }

    public string StaffDiscount { get; set; }

    public string SpecialAllowance { get; set; }

    public string ManagerAllowance { get; set; }

    public string CompanyAsset { get; set; }

    public string SchoolAllowance { get; set; }

    public string NoticePeriod { get; set; }

    public string Indemnity { get; set; }

    public string Retirement { get; set; }

    public string MinSalary { get; set; }

    public string MidSalary { get; set; }

    public string MaxSalary { get; set; }

    public string TransportationAllowance { get; set; }

    public string HousingAllowance { get; set; }

    public string OtherAllowance { get; set; }
}
