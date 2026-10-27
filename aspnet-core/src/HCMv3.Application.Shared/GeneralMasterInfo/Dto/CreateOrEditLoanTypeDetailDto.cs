namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="LoanTypeDetail"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditLoanTypeDetailDto
{
    public int? Id { get; set; }

    public int? LoanTypeId { get; set; }

    /// <summary>The register line the percentage is taken of. Required; checked in the app service.</summary>
    public int? WageTypeId { get; set; }

    public double Percent { get; set; }
}
