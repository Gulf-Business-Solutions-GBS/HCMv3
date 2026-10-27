using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Payroll;
using HCMv3.Dashboards.Payroll.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Dashboards;

/// <summary>Counting rules of the Payroll Dashboard. Pure in-memory. Period: 1 Jan to 31 Mar 2026.</summary>
public class PayrollDashboardCalculator_Tests
{
    private const int Basic = 1;
    private const int Housing = 2;
    private const int NetPay = 3;
    private const int Loan = 4;
    private const int EmployeePifss = 5;
    private const int EmployerPifss = 6;
    private const int IndemnityProvision = 7;
    private const int LeaveProvision = 8;
    private const int IndemnityBalance = 9;
    private const int LeaveBalance = 10;
    private const int AbsentDays = 11;

    private static readonly DateTime Jan = new(2026, 1, 31);
    private static readonly DateTime Feb = new(2026, 2, 28);

    private readonly DashboardOrgTree _tree = new(new[]
    {
        new DashboardOrgUnit(1, null, "Company"),
        new DashboardOrgUnit(2, 1, "Finance"),
        new DashboardOrgUnit(3, 1, "Operations")
    });

    private readonly List<PayrollEmployee> _employees = new()
    {
        new(1, "E1", "Amal", null, 2, new DateTime(2020, 1, 1)),
        new(2, "E2", "Badr", null, 2, new DateTime(2021, 1, 1)),
        new(3, "E3", "Chris", null, 3, new DateTime(2022, 1, 1))
    };

    private static readonly List<PayrollWageInfo> WageTypes = new()
    {
        new(Basic, "1000", "Basic", null, BillingType.Earning, false),
        new(Housing, "1002", "Housing", null, BillingType.Earning, false),
        new(NetPay, "9000", "Net Pay", null, BillingType.Earning, false),
        new(Loan, "2010", "Loan", null, BillingType.Deduction, false),
        new(EmployeePifss, "1", "PIFSS employee", null, BillingType.Deduction, false),
        new(EmployerPifss, "3", "PIFSS employer", null, BillingType.Deduction, true),
        new(IndemnityProvision, "3000", "Indemnity provision", null, BillingType.Provision, false),
        new(LeaveProvision, "3001", "Leave provision", null, BillingType.Provision, false),
        new(IndemnityBalance, "3003", "Indemnity total", null, BillingType.Provision, false),
        new(LeaveBalance, "3004", "Leave total", null, BillingType.Provision, false),
        new(AbsentDays, "4024", "Absent days", null, BillingType.TimeType, false)
    };

    private PayrollDashboardCalculator Calculator() =>
        new(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31), _tree, _employees, WageTypes);

    private static PayrollLine L(int wageType, double amount, double number = 0) => new(wageType, amount, number);

    /// <summary>Basic 1000 + housing 200, loan 100 + PIFSS 50, net 1050; employer PIFSS 90, provisions 60 + 40.</summary>
    private static PayrollPayslip Standard(int employee, DateTime month, PayrollRunType runType = PayrollRunType.Regular) =>
        new(employee, month, runType, new[]
        {
            L(Basic, 1000), L(Housing, 200), L(Loan, 100), L(EmployeePifss, 50), L(NetPay, 1050),
            L(EmployerPifss, 90), L(IndemnityProvision, 60), L(LeaveProvision, 40),
            L(IndemnityBalance, 5000, 120), L(LeaveBalance, 2000, 30), L(AbsentDays, 2, 2)
        });

    [Fact]
    public void Totals_Should_Follow_The_Net_Pay_Rule_And_Add_Employer_Cost_On_Top()
    {
        var t = Calculator().Totals(Standard(1, Jan));

        t.Basic.ShouldBe(1000);
        t.Gross.ShouldBe(1200);           // net pay and time types are not earnings
        t.Deductions.ShouldBe(150);       // employer PIFSS does not touch net pay
        t.Net.ShouldBe(1050);
        t.EmployerCost.ShouldBe(190);     // 90 + 60 + 40; balances are not a monthly cost
        t.CostToCompany.ShouldBe(1390);
    }

    [Fact]
    public void Totals_Should_Take_Gross_Less_Deductions_When_A_Payslip_Has_No_Net_Pay_Line()
    {
        var payslip = new PayrollPayslip(1, Jan, PayrollRunType.OffCycle, new[] { L(Housing, 500), L(Loan, 100) });

        Calculator().Totals(payslip).Net.ShouldBe(400);
    }

    [Fact]
    public void Kpis_Should_Add_Up_The_Period_And_Count_The_Latest_Months_Employees()
    {
        var payslips = new[] { Standard(1, Jan), Standard(2, Jan), Standard(1, Feb), Standard(3, Feb, PayrollRunType.OffCycle) };

        var kpis = Calculator().Kpis(payslips);

        kpis.EmployeesPaid.ShouldBe(1);   // February's only regular payslip is employee 1's
        kpis.Gross.ShouldBe(4800);
        kpis.Net.ShouldBe(4200);
        kpis.CostToCompany.ShouldBe(5560);
        kpis.AverageMonthlyCostPerEmployee.ShouldBe(1853.333); // 5560 ÷ 3 regular payslip-months
        kpis.OffCyclePayments.ShouldBe(1);
    }

    [Fact]
    public void Kpis_Should_Leave_The_Average_Empty_When_Nobody_Was_Paid()
    {
        Calculator().Kpis(new List<PayrollPayslip>()).AverageMonthlyCostPerEmployee.ShouldBeNull();
    }

    [Fact]
    public void Trend_Should_Give_Every_Month_Of_The_Period()
    {
        var trend = Calculator().Trend(new[] { Standard(1, Jan), Standard(2, Jan) });

        trend.Select(p => p.MonthStart.Month).ShouldBe(new[] { 1, 2, 3 }, ignoreOrder: false);
        trend[0].Employees.ShouldBe(2);
        trend[0].CostToCompany.ShouldBe(2780);
        trend[1].CostToCompany.ShouldBe(0);
    }

    [Fact]
    public void CostMix_Should_Split_Cost_To_Company_And_Leave_Out_Empty_Parts()
    {
        var mix = Calculator().CostMix(new[] { Standard(1, Jan) });

        mix.Select(c => (c.Component, c.Amount)).ShouldBe(new[]
        {
            (PayrollCostComponent.Basic, 1000.0),
            (PayrollCostComponent.OtherEarnings, 200.0),
            (PayrollCostComponent.EmployerSocialSecurity, 90.0),
            (PayrollCostComponent.IndemnityProvision, 60.0),
            (PayrollCostComponent.LeaveProvision, 40.0)
        }, ignoreOrder: false);
        mix.Sum(c => c.Amount).ShouldBe(1390);
    }

    [Fact]
    public void ByOrgUnit_Should_Put_Cost_In_The_Unit_One_Level_Down_Highest_First()
    {
        var buckets = Calculator().ByOrgUnit(new[] { Standard(1, Jan), Standard(2, Jan), Standard(3, Jan) }, 1);

        buckets[0].Name.ShouldBe("Finance");
        buckets[0].Employees.ShouldBe(2);
        buckets[0].CostToCompany.ShouldBe(2780);
        buckets.Single(b => b.OrganizationUnitId == 3).Net.ShouldBe(1050);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(299.999, 0)]
    [InlineData(300, 1)]
    [InlineData(300.5, 1)]
    [InlineData(999, 2)]
    [InlineData(1500, 4)]
    [InlineData(25000, 5)]
    public void BandOf_Should_Leave_No_Gaps(double basic, int band)
    {
        PayrollDashboardCalculator.BandOf(basic).ShouldBe(band);
    }

    [Fact]
    public void BasicBands_Should_Count_The_Latest_Month_And_Keep_Every_Band()
    {
        var low = new PayrollPayslip(2, Feb, PayrollRunType.Regular, new[] { L(Basic, 450) });

        var bands = Calculator().BasicBands(new[] { Standard(1, Jan), Standard(1, Feb), low });

        bands.Count.ShouldBe(PayrollDashboardCalculator.BasicBandBounds.Length);
        bands[1].Employees.ShouldBe(1);   // 450
        bands[3].Employees.ShouldBe(1);   // 1000 in February; January is not counted again
        bands.Last().To.ShouldBeNull();
    }

    [Fact]
    public void Earnings_And_Deductions_Should_Rank_Pay_Items_Leaving_Out_Net_Pay_And_Employer_Items()
    {
        var calculator = Calculator();
        var payslips = new[] { Standard(1, Jan), Standard(2, Jan) };

        calculator.Earnings(payslips).Select(w => (w.Code, w.Amount)).ShouldBe(new[] { ("1000", 2000.0), ("1002", 400.0) }, ignoreOrder: false);
        calculator.Deductions(payslips).Select(w => w.Code).ShouldBe(new[] { "2010", "1" }, ignoreOrder: false);
        calculator.Earnings(payslips)[0].Employees.ShouldBe(2);
    }

    [Fact]
    public void PaymentMethods_Should_Group_The_Latest_Month_With_No_Method_On_File_Apart()
    {
        var methods = Calculator().PaymentMethods(new[] { Standard(1, Feb), Standard(2, Feb), Standard(3, Feb) },
            new Dictionary<int, PaymentType> { [1] = PaymentType.Bank, [2] = PaymentType.Bank });

        methods[0].PaymentType.ShouldBe(PaymentType.Bank);
        methods[0].Employees.ShouldBe(2);
        methods[1].PaymentType.ShouldBeNull();
        methods[1].Net.ShouldBe(1050);
    }

    [Fact]
    public void RunTypes_Should_Count_Payslips_And_Employees()
    {
        var runs = Calculator().RunTypes(new[] { Standard(1, Jan), Standard(1, Feb), Standard(1, Feb, PayrollRunType.OffCycle) });

        runs[0].RunType.ShouldBe(PayrollRunType.Regular);
        runs[0].Payslips.ShouldBe(2);
        runs[0].Employees.ShouldBe(1);
        runs[1].Payslips.ShouldBe(1);
    }

    [Fact]
    public void Provisions_Should_Be_The_Latest_Months_Balances_Not_A_Sum_Over_Months()
    {
        var provisions = Calculator().Provisions(new[] { Standard(1, Jan), Standard(1, Feb), Standard(2, Feb) });

        provisions.Indemnity.ShouldBe(10000);
        provisions.Leave.ShouldBe(4000);
        provisions.Total.ShouldBe(14000);
    }

    [Fact]
    public void CostCard_Should_List_Each_Line_Under_Earnings_Deductions_Or_Employer_Cost()
    {
        var card = Calculator().CostCard(1, Standard(1, Jan));

        card.Month.ShouldBe(new DateTime(2026, 1, 1));
        card.Earnings.Select(l => l.Code).ShouldBe(new[] { "1000", "1002" }, ignoreOrder: false);
        card.Deductions.Select(l => l.Code).ShouldBe(new[] { "1", "2010" }, ignoreOrder: false);
        card.EmployerCosts.Select(l => l.Code).ShouldBe(new[] { "3", "3000", "3001" }, ignoreOrder: false);
        card.CostToCompany.ShouldBe(1390);
        card.AnnualCostToCompany.ShouldBe(16680);
        card.Balances.Indemnity.ShouldBe(5000);
        card.IndemnityDays.ShouldBe(120);
        card.LeaveDays.ShouldBe(30);
        card.OrganizationUnitName.ShouldBe("Finance");
    }

    [Fact]
    public void CostCard_Should_Show_The_Employee_Without_Figures_When_There_Is_No_Payslip()
    {
        var card = Calculator().CostCard(3, null);

        card.EmployeeCode.ShouldBe("E3");
        card.Month.ShouldBeNull();
        card.Earnings.ShouldBeEmpty();
    }
}
