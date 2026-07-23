namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="WageTypeRegister"/>. <c>Id == null</c> means create; otherwise
/// update. No business validation (legacy has none beyond plain CRUD).
/// </summary>
public class CreateOrEditWageTypeRegisterDto
{
    public int? Id { get; set; }

    public int? EmployeeId { get; set; }

    public int ForPeriod { get; set; }

    public int InPeriod { get; set; }
}
