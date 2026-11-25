namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Output of <c>GetSalaryRegisterForEdit</c>.
/// </summary>
public class GetSalaryRegisterForEditOutput
{
    public CreateOrEditSalaryRegisterDto SalaryRegister { get; set; }

    /// <summary>Display only: the register's employee, so the dialog shows code and name instead of the id.</summary>
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }
}
