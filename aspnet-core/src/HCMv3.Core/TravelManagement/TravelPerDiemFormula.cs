using System;

namespace HCMv3.TravelManagement;

/// <summary>
/// The per-diem arithmetic of legacy <c>HCM10.Module.BusinessObjects.TravelManagement.TravelDetail</c>, in one
/// place so the screen (<c>TravelDetailsAppService.CalculateExpectedPerDiemAsync</c>) and the payroll rule
/// (<c>TravelPerDiemPayWageTypeCalculator</c>) cannot drift apart.
///
/// <para>Only the arithmetic lives here. Finding the rate (a <see cref="TravelPerDiem"/> row for the leg's
/// Departure/Arrival and the traveller's EmployeeGroup/EmployeeSubGroup) needs repositories, and the two callers
/// get the group/sub-group differently: the screen is handed them, payroll reads the employee's org assignment
/// as of the leg's departure date, exactly as legacy's non-persistent <c>PerDiemRate</c> getter did.</para>
/// </summary>
public static class TravelPerDiemFormula
{
    /// <summary>
    /// Legacy <c>TravelDetail.PerDiemAmount</c> (legacy <c>TravelDetail.cs</c> lines 135-143):
    /// <c>(Travel.Employee != null) ? Math.Round(TotalNights * PerDiemRate, 3) : 0</c>. An applicant's trip
    /// (no employee) has no per diem.
    /// </summary>
    public static double PerDiemAmount(bool hasEmployee, int totalNights, double perDiemRate)
    {
        return hasEmployee ? Math.Round(totalNights * perDiemRate, 3) : 0;
    }

    /// <summary>
    /// The paid share of one leg, legacy payroll <c>GetTravelPerDiemPay</c>
    /// (<c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> line 4187):
    /// <c>(PerDiemAmount * PerDiemPayPercentage) / 100</c>. NOT rounded again -- legacy sums the raw products.
    /// </summary>
    public static double PerDiemPayAmount(double perDiemAmount, double perDiemPayPercentage)
    {
        return (perDiemAmount * perDiemPayPercentage) / 100;
    }
}
