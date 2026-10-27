using Abp.Application.Services.Dto;

namespace HCMv3.GatePassManagement.Dto;

public class GatePassPersonDto : EntityDto<int>
{
    public int GatePassId { get; set; }

    public int PersonTypeId { get; set; }

    public string PersonTypeNameEn { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeFullName { get; set; }

    public string PersonFullName { get; set; }

    public string PersonCivildID { get; set; }

    public string PersonPassportNumber { get; set; }

    public int GatePassVendorId { get; set; }

    public string VendorNameEn { get; set; }
}
