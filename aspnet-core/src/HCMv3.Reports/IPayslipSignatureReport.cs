namespace HCMv3.Reports
{
    /// <summary>
    /// A payslip layout with the Approved-By / Reviewed-By signature block. Legacy showed or hid that block
    /// from <c>ManagePayrollSettings.PayslipSignature</c> in each payslip's constructor; here the caller that
    /// builds the report (the report storage in Web.Host, the go-live payslip PDF) reads the setting and
    /// applies it, because a report constructed by DevExpress has no DI container of its own.
    /// </summary>
    public interface IPayslipSignatureReport
    {
        void ApplyPayslipSignature(bool visible);
    }
}
