using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class PayrollDriverListDto : EntityDto
{
    public int CountryId { get; set; }

    public string CountryName { get; set; }

    public bool LeaveDeductionStartMonth { get; set; }
}

public class PayrollDriverEditDto : EntityDto
{
    public int CountryId { get; set; }

    public bool LeaveDeductionStartMonth { get; set; }
}

public class GetPayrollDriversInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}
