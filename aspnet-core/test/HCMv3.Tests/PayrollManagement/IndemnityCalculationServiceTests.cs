using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Tests for <see cref="IIndemnityCalculationService"/> (PayrollEngine wave P3 -- indemnity/gratuity).
///
/// <para>These cover end-of-service benefit money rules, so the assertions are deliberately written against
/// legacy's EXACT arithmetic -- including its rounding digit counts and its off-by-one slab-walk quirk --
/// rather than against "what the number ought to be". Where a legacy behaviour looks like a bug it is
/// asserted as-is and called out in the test name/comment.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class IndemnityCalculationServiceTests : AppTestBase
{
    private readonly IIndemnityCalculationService _indemnityService;

    private int TenantId => AbpSession.TenantId ?? 1;

    public IndemnityCalculationServiceTests()
    {
        LoginAsDefaultTenantAdmin();
        _indemnityService = Resolve<IIndemnityCalculationService>();
    }

    #region << Seed helpers >>

    private int CreateEmployee(DateTime joiningDate)
    {
        return UsingDbContext(context =>
        {
            var nationality = new Nationality { TenantId = TenantId, NameEn = "Local", NameAr = "محلي" };
            context.Set<Nationality>().Add(nationality);
            context.SaveChanges();

            var employee = new Employee
            {
                TenantId = TenantId,
                FirstNameEn = "Test",
                LastNameEn = "Employee",
                FirstNameAr = "تجربة",
                LastNameAr = "موظف",
                NationalityId = nationality.Id,
                JoiningDate = joiningDate
            };
            context.Set<Employee>().Add(employee);
            context.SaveChanges();
            return employee.Id;
        });
    }

    /// <summary>Creates a wage type, or returns the existing one when the code was already seeded.</summary>
    private int CreateWageType(string code, bool includeInIndemnity = false, double fixedDays = 0)
    {
        return UsingDbContext(context =>
        {
            var existing = context.Set<WageType>().FirstOrDefault(r => r.Code == code);
            if (existing != null)
            {
                return existing.Id;
            }

            var wageType = new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "WT " + code,
                NameAr = "بند " + code,
                FixedDays = fixedDays,
                IncludeInIndemnity = includeInIndemnity,
                PayrollOrder = 1,
                Status = true
            };
            context.Set<WageType>().Add(wageType);
            context.SaveChanges();
            return wageType.Id;
        });
    }

    private int CreatePayrollReasonType()
    {
        return UsingDbContext(context =>
        {
            var existing = context.Set<PayrollReasonType>().FirstOrDefault();
            if (existing != null)
            {
                return existing.Id;
            }

            var reason = new PayrollReasonType { TenantId = TenantId, NameEn = "Monthly", NameAr = "شهري" };
            context.Set<PayrollReasonType>().Add(reason);
            context.SaveChanges();
            return reason.Id;
        });
    }

    private int CreateSalaryRegister(int employeeId, DateTime forPeriod, DateTime? inPeriod = null, PayrollRunType runType = PayrollRunType.Regular)
    {
        var reasonTypeId = CreatePayrollReasonType();

        return UsingDbContext(context =>
        {
            var register = new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                ForPeriod = forPeriod,
                InPeriod = inPeriod ?? forPeriod,
                PayrollRunType = runType,
                PayrollReasonTypeId = reasonTypeId
            };
            context.Set<SalaryRegister>().Add(register);
            context.SaveChanges();
            return register.Id;
        });
    }

    private int AddRegisterLine(int salaryRegisterId, string wageCode, double rate = 0, double number = 0, double amount = 0, bool includeInIndemnity = false, double fixedDays = 0)
    {
        var wageTypeId = CreateWageType(wageCode, includeInIndemnity, fixedDays);

        return UsingDbContext(context =>
        {
            var detail = new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = salaryRegisterId,
                WageTypeId = wageTypeId,
                Rate = rate,
                Number = number,
                Amount = amount
            };
            context.Set<SalaryRegisterDetail>().Add(detail);
            context.SaveChanges();
            return detail.Id;
        });
    }

    private void SetPayrollSettings(bool enableUnpaidDedSlabWise = false, int decimalInSalaryRegister = 0)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManagePayrollSettings>().FirstOrDefault();
            if (settings == null)
            {
                context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings
                {
                    TenantId = TenantId,
                    EnableUnpaidDedSlabWise = enableUnpaidDedSlabWise,
                    DecimalInSalaryRegister = decimalInSalaryRegister
                });
            }
            else
            {
                settings.EnableUnpaidDedSlabWise = enableUnpaidDedSlabWise;
                settings.DecimalInSalaryRegister = decimalInSalaryRegister;
            }

            context.SaveChanges();
        });
    }

    private void CreateContract(int employeeId, DateTime startDate, DateTime endDate)
    {
        UsingDbContext(context =>
        {
            var contractType = context.Set<ContractType>().FirstOrDefault();
            if (contractType == null)
            {
                contractType = new ContractType { TenantId = TenantId, NameEn = "Fixed", NameAr = "ثابت" };
                context.Set<ContractType>().Add(contractType);
                context.SaveChanges();
            }

            context.Set<EmployeeContract>().Add(new EmployeeContract
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                ContractTypeId = contractType.Id,
                StartDate = startDate,
                EndDate = endDate
            });
            context.SaveChanges();
        });
    }

    private int CreateEmployeeSubGroup()
    {
        return UsingDbContext(context =>
        {
            var group = new EmployeeGroup { TenantId = TenantId, NameEn = "G1", NameAr = "ج1" };
            context.Set<EmployeeGroup>().Add(group);
            context.SaveChanges();

            var subGroup = new EmployeeSubGroup { TenantId = TenantId, EmployeeGroupId = group.Id, NameEn = "SG1", NameAr = "ف1" };
            context.Set<EmployeeSubGroup>().Add(subGroup);
            context.SaveChanges();
            return subGroup.Id;
        });
    }

    /// <summary>
    /// Adds an <see cref="EmployeeSubGroupGratuity"/> change-year slab. Date range is deliberately wide so
    /// that the <c>StartDate &lt;= payDate &lt;= EndDate</c> filter never interferes with the ChangeYear walk
    /// being tested.
    /// </summary>
    private void AddSubGroupGratuitySlab(int employeeSubGroupId, double changeYear, double basisDays, double gratuityDays = 15, bool impactFromDateOfJoining = false)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeSubGroupGratuity>().Add(new EmployeeSubGroupGratuity
            {
                TenantId = TenantId,
                EmployeeSubGroupId = employeeSubGroupId,
                StartDate = new DateTime(2000, 1, 1),
                EndDate = new DateTime(2100, 12, 31),
                ChangeYear = changeYear,
                GratuityDays = gratuityDays,
                BasisDays = basisDays,
                ImpactFromDateOfJoining = impactFromDateOfJoining
            });
            context.SaveChanges();
        });
    }

    private void AddTimeRegister(int employeeId, DateTime dayDate, double unpaidLeaveDay = 0, double absentDay = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<TimeRegister>().Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DayDate = dayDate,
                UnpaidLeaveDay = unpaidLeaveDay,
                AbsentDay = absentDay
            });
            context.SaveChanges();
        });
    }

    private void AddGratuityReset(int employeeId, DateTime gratuityPaidDate, bool resetGratuityCalc)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeGratuity>().Add(new EmployeeGratuity
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                GratuityPaidDate = gratuityPaidDate,
                ResetGratuityCalc = resetGratuityCalc
            });
            context.SaveChanges();
        });
    }

    private void AddBasicPay(int employeeId, DateTime startDate, DateTime endDate, double amount)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = startDate,
                EndDate = endDate,
                Amount = amount
            });
            context.SaveChanges();
        });
    }

    /// <summary>
    /// The pay date that makes legacy's inclusive service-day count
    /// (<c>payDate.Subtract(start).TotalDays + 1</c>) come out at exactly <paramref name="serviceDays"/>.
    /// </summary>
    private static DateTime PayDateForServiceDays(DateTime start, int serviceDays)
    {
        return start.AddDays(serviceDays - 1);
    }

    private static IndemnitySlab Slab(int srNo, double indemnityDays, double basisDays, DateTime? start = null, DateTime? end = null)
    {
        return new IndemnitySlab
        {
            SrNo = srNo,
            StartDate = start ?? new DateTime(2000, 1, 1),
            EndDate = end ?? new DateTime(2100, 12, 31),
            IndemnityDays = indemnityDays,
            BasisDays = basisDays
        };
    }

    #endregion

    #region << GetIndemnityBifurcateAmount -- flat path (legacy EmployeeCalculatedPayService.cs:2677) >>

    [Fact]
    public async Task GetIndemnityBifurcateAmountAsync_Should_Use_Flat_Formula_When_SlabWise_Disabled()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var result = await _indemnityService.GetIndemnityBifurcateAmountAsync(
            employeeId, new DateTime(2024, 6, 30), indemnityDays: 15, totalBasicPay: 900, fixedDays: 30,
            PayrollRunType.Regular, offCycleReason: null, isMonthly: false, employeeSubGroupId: null,
            indemnitySlabs: null);

        // Legacy: (totalBasicPay / fixedDays) * indemnityDays -- NO rounding applied anywhere.
        result.Amount.ShouldBe((900d / 30d) * 15d);
    }

    [Fact]
    public async Task GetIndemnityBifurcateAmountAsync_Should_Return_Zero_When_FixedDays_Is_Zero_And_SlabWise_Disabled()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var result = await _indemnityService.GetIndemnityBifurcateAmountAsync(
            employeeId, new DateTime(2024, 6, 30), indemnityDays: 15, totalBasicPay: 900, fixedDays: 0,
            PayrollRunType.Regular, offCycleReason: null, isMonthly: false, employeeSubGroupId: null,
            indemnitySlabs: null);

        // Legacy guards the division: fixedDays != 0 ? ... : 0
        result.Amount.ShouldBe(0);
    }

    [Fact]
    public async Task GetIndemnityBifurcateAmountAsync_Should_Not_Round_The_Flat_Amount()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        // 1000/30 is a repeating decimal; legacy returns it at full double precision.
        var result = await _indemnityService.GetIndemnityBifurcateAmountAsync(
            employeeId, new DateTime(2024, 6, 30), indemnityDays: 1, totalBasicPay: 1000, fixedDays: 30,
            PayrollRunType.Regular, offCycleReason: null, isMonthly: false, employeeSubGroupId: null,
            indemnitySlabs: null);

        result.Amount.ShouldBe((1000d / 30d) * 1d);
        result.Amount.ShouldNotBe(Math.Round((1000d / 30d) * 1d, 3));
    }

    #endregion

    #region << GetIndemnityBifurcateAmount -- slab-wise path >>

    [Fact]
    public async Task GetIndemnityBifurcateAmountAsync_Should_Consume_Days_Across_Slabs_In_SrNo_Order_When_Not_Monthly()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: true);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        // Slabs deliberately supplied out of order to prove the OrderBy(SrNo).
        var slabs = new List<IndemnitySlab>
        {
            Slab(srNo: 2, indemnityDays: 20, basisDays: 30),
            Slab(srNo: 1, indemnityDays: 10, basisDays: 26)
        };

        var result = await _indemnityService.GetIndemnityBifurcateAmountAsync(
            employeeId, new DateTime(2024, 6, 30), indemnityDays: 25, totalBasicPay: 780, fixedDays: 30,
            PayrollRunType.Regular, offCycleReason: null, isMonthly: false, employeeSubGroupId: null,
            indemnitySlabs: slabs);

        // Slab 1 absorbs 10 of the 25 days at 26 basis days, slab 2 absorbs the remaining 15 at 30.
        var expected = (780d / 26d) * 10d + (780d / 30d) * 15d;
        result.Amount.ShouldBe(expected);
    }

    [Fact]
    public async Task GetIndemnityBifurcateAmountAsync_Should_Fall_Back_To_FixedDays_When_Slab_BasisDays_Is_Zero()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: true);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var slabs = new List<IndemnitySlab> { Slab(srNo: 1, indemnityDays: 10, basisDays: 0) };

        var result = await _indemnityService.GetIndemnityBifurcateAmountAsync(
            employeeId, new DateTime(2024, 6, 30), indemnityDays: 10, totalBasicPay: 900, fixedDays: 30,
            PayrollRunType.Regular, offCycleReason: null, isMonthly: false, employeeSubGroupId: null,
            indemnitySlabs: slabs);

        result.Amount.ShouldBe((900d / 30d) * 10d);
    }

    [Fact]
    public async Task GetIndemnityBifurcateAmountAsync_Should_Stop_Accruing_Once_IndemnityDays_Are_Exhausted()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: true);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var slabs = new List<IndemnitySlab>
        {
            Slab(srNo: 1, indemnityDays: 10, basisDays: 26),
            Slab(srNo: 2, indemnityDays: 20, basisDays: 30)
        };

        // Only 4 days to spread -- slab 1 takes all of them, slab 2 contributes exactly 0.
        var result = await _indemnityService.GetIndemnityBifurcateAmountAsync(
            employeeId, new DateTime(2024, 6, 30), indemnityDays: 4, totalBasicPay: 780, fixedDays: 30,
            PayrollRunType.Regular, offCycleReason: null, isMonthly: false, employeeSubGroupId: null,
            indemnitySlabs: slabs);

        result.Amount.ShouldBe((780d / 26d) * 4d);
    }

    [Theory]
    // payDate exactly on the slab StartDate, exactly on the EndDate, and one day either side.
    [InlineData("2024-06-01", true)]   // exactly StartDate       -> inclusive
    [InlineData("2024-06-30", true)]   // exactly EndDate         -> inclusive
    [InlineData("2024-05-31", false)]  // one day before StartDate
    [InlineData("2024-07-01", false)]  // one day after EndDate
    public async Task GetIndemnityBifurcateAmountAsync_Monthly_Should_Match_Slab_On_Inclusive_Date_Boundaries(string payDateText, bool shouldMatch)
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: true);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var payDate = DateTime.Parse(payDateText);

        var slabs = new List<IndemnitySlab>
        {
            Slab(srNo: 1, indemnityDays: 10, basisDays: 26,
                 start: new DateTime(2024, 6, 1), end: new DateTime(2024, 6, 30))
        };

        var result = await _indemnityService.GetIndemnityBifurcateAmountAsync(
            employeeId, payDate, indemnityDays: 2.5, totalBasicPay: 780, fixedDays: 30,
            PayrollRunType.Regular, offCycleReason: null, isMonthly: true, employeeSubGroupId: null,
            indemnitySlabs: slabs);

        // Monthly: the slab's own IndemnityDays cap is NOT applied (legacy commented that line out) --
        // ALL remaining indemnityDays go against the matched slab.
        result.Amount.ShouldBe(shouldMatch ? (780d / 26d) * 2.5d : 0d);
    }

    [Fact]
    public async Task GetIndemnityBifurcateAmountAsync_Monthly_Should_Ignore_The_Slabs_Own_IndemnityDays_Cap()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: true);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var payDate = new DateTime(2024, 6, 15);

        var slabs = new List<IndemnitySlab>
        {
            Slab(srNo: 1, indemnityDays: 1, basisDays: 26,
                 start: new DateTime(2024, 6, 1), end: new DateTime(2024, 6, 30))
        };

        // 9 days requested against a slab that only "holds" 1 -- legacy still pays all 9 in the monthly path.
        var result = await _indemnityService.GetIndemnityBifurcateAmountAsync(
            employeeId, payDate, indemnityDays: 9, totalBasicPay: 780, fixedDays: 30,
            PayrollRunType.Regular, offCycleReason: null, isMonthly: true, employeeSubGroupId: null,
            indemnitySlabs: slabs);

        result.Amount.ShouldBe((780d / 26d) * 9d);
    }

    [Fact]
    public async Task GetIndemnityBifurcateAmountAsync_Should_Return_Zero_When_SlabWise_Enabled_And_No_Slabs_On_Regular_Run()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: true);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        // Not an End-Of-Service off-cycle run, so legacy never rebuilds the slab table -> nothing accrues.
        var result = await _indemnityService.GetIndemnityBifurcateAmountAsync(
            employeeId, new DateTime(2024, 6, 30), indemnityDays: 15, totalBasicPay: 900, fixedDays: 30,
            PayrollRunType.Regular, offCycleReason: null, isMonthly: false, employeeSubGroupId: null,
            indemnitySlabs: null);

        result.Amount.ShouldBe(0);
    }

    #endregion

    #region << GetIndemnityBasisDays -- ChangeYear slab boundaries (legacy Program.cs:4138) >>

    /// <summary>
    /// Seeds an employee whose net service period lands exactly where the caller asks, then asks for the
    /// basis days. Slabs: ChangeYear 5 -> 26 basis days, ChangeYear 100 -> 30 basis days.
    /// </summary>
    private async Task<double> BasisDaysForServiceDaysAsync(int serviceDays, int unpaidDays = 0, bool unpaidWageTypeIncludeInIndemnity = true)
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);

        var contractStart = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(contractStart);
        var subGroupId = CreateEmployeeSubGroup();

        var payDate = PayDateForServiceDays(contractStart, serviceDays);
        CreateContract(employeeId, contractStart, new DateTime(2030, 12, 31));

        AddSubGroupGratuitySlab(subGroupId, changeYear: 5, basisDays: 26);
        AddSubGroupGratuitySlab(subGroupId, changeYear: 100, basisDays: 30);

        if (unpaidDays > 0)
        {
            // Wage code "4001" gates the unpaid-leave deduction on IncludeInIndemnity.
            CreateWageType("4001", includeInIndemnity: unpaidWageTypeIncludeInIndemnity);

            for (var i = 0; i < unpaidDays; i++)
            {
                AddTimeRegister(employeeId, contractStart.AddDays(i), unpaidLeaveDay: 1);
            }
        }

        return await _indemnityService.GetIndemnityBasisDaysAsync(employeeId, payDate, fixedDays: 99, employeeSubGroupId: subGroupId);
    }

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Stay_In_First_Slab_Exactly_At_The_ChangeYear_Boundary()
    {
        // 5 * 365 = 1825 -- legacy's test is `remainingPeriod <= (ChangeYear - prevChangeYear) * 365`,
        // so exactly-on-the-boundary belongs to the FIRST slab.
        var basisDays = await BasisDaysForServiceDaysAsync(serviceDays: 1825);

        basisDays.ShouldBe(26);
    }

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Stay_In_First_Slab_One_Day_Below_The_ChangeYear_Boundary()
    {
        var basisDays = await BasisDaysForServiceDaysAsync(serviceDays: 1824);

        basisDays.ShouldBe(26);
    }

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Move_To_Second_Slab_One_Day_Above_The_ChangeYear_Boundary()
    {
        var basisDays = await BasisDaysForServiceDaysAsync(serviceDays: 1826);

        basisDays.ShouldBe(30);
    }

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Fall_Back_To_FixedDays_When_No_Slabs_Are_Configured()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);

        var contractStart = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(contractStart);
        var subGroupId = CreateEmployeeSubGroup();
        CreateContract(employeeId, contractStart, new DateTime(2030, 12, 31));

        var basisDays = await _indemnityService.GetIndemnityBasisDaysAsync(
            employeeId, new DateTime(2024, 6, 30), fixedDays: 30, employeeSubGroupId: subGroupId);

        basisDays.ShouldBe(30);
    }

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Return_FixedDays_Unconditionally_When_SlabWise_Enabled()
    {
        // Legacy's body has no `else` for the enableUnpaidDedSlabWise == true case: BasisDays stays 0 and the
        // FixedDays fallback wins, even though matching slabs exist.
        SetPayrollSettings(enableUnpaidDedSlabWise: true);

        var contractStart = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(contractStart);
        var subGroupId = CreateEmployeeSubGroup();
        CreateContract(employeeId, contractStart, new DateTime(2030, 12, 31));
        AddSubGroupGratuitySlab(subGroupId, changeYear: 5, basisDays: 26);

        var basisDays = await _indemnityService.GetIndemnityBasisDaysAsync(
            employeeId, PayDateForServiceDays(contractStart, 1000), fixedDays: 30, employeeSubGroupId: subGroupId);

        basisDays.ShouldBe(30);
    }

    #endregion

    #region << GetIndemnityBasisDays -- unpaid/absent deductions (legacy Program.cs:3473/3494/3726/3747) >>

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Deduct_UnpaidLeave_And_Drop_Back_A_Slab_When_WageType_4001_IncludeInIndemnity_Is_True()
    {
        // 1826 raw service days would land in slab 2 (30 basis days); one deducted unpaid day pulls it back
        // to exactly 1825 -- the slab boundary -- and therefore back into slab 1 (26 basis days).
        var basisDays = await BasisDaysForServiceDaysAsync(
            serviceDays: 1826, unpaidDays: 1, unpaidWageTypeIncludeInIndemnity: true);

        basisDays.ShouldBe(26);
    }

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Not_Deduct_UnpaidLeave_When_WageType_4001_IncludeInIndemnity_Is_False()
    {
        // Identical data to the test above, but the "4001" wage type is excluded from indemnity, so the
        // deduction is gated off and the employee stays in slab 2.
        var basisDays = await BasisDaysForServiceDaysAsync(
            serviceDays: 1826, unpaidDays: 1, unpaidWageTypeIncludeInIndemnity: false);

        basisDays.ShouldBe(30);
    }

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Deduct_AbsentDays_When_WageType_4029_IncludeInIndemnity_Is_True()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);

        var contractStart = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(contractStart);
        var subGroupId = CreateEmployeeSubGroup();
        CreateContract(employeeId, contractStart, new DateTime(2030, 12, 31));
        AddSubGroupGratuitySlab(subGroupId, changeYear: 5, basisDays: 26);
        AddSubGroupGratuitySlab(subGroupId, changeYear: 100, basisDays: 30);

        CreateWageType("4029", includeInIndemnity: true);
        // Absent days are only counted on rows with UnpaidLeaveDay == 0.
        AddTimeRegister(employeeId, contractStart, unpaidLeaveDay: 0, absentDay: 1);

        var basisDays = await _indemnityService.GetIndemnityBasisDaysAsync(
            employeeId, PayDateForServiceDays(contractStart, 1826), fixedDays: 99, employeeSubGroupId: subGroupId);

        basisDays.ShouldBe(26);
    }

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Not_Deduct_AbsentDays_When_WageType_4029_IncludeInIndemnity_Is_False()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);

        var contractStart = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(contractStart);
        var subGroupId = CreateEmployeeSubGroup();
        CreateContract(employeeId, contractStart, new DateTime(2030, 12, 31));
        AddSubGroupGratuitySlab(subGroupId, changeYear: 5, basisDays: 26);
        AddSubGroupGratuitySlab(subGroupId, changeYear: 100, basisDays: 30);

        CreateWageType("4029", includeInIndemnity: false);
        AddTimeRegister(employeeId, contractStart, unpaidLeaveDay: 0, absentDay: 1);

        var basisDays = await _indemnityService.GetIndemnityBasisDaysAsync(
            employeeId, PayDateForServiceDays(contractStart, 1826), fixedDays: 99, employeeSubGroupId: subGroupId);

        basisDays.ShouldBe(30);
    }

    #endregion

    #region << GetIndemnityBasisDays -- reset (ResetGratuityCalc / GratuityPaidDate) >>

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Restart_The_Service_Period_From_GratuityPaidDate_When_ResetGratuityCalc_Is_Set()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);

        var contractStart = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(contractStart);
        var subGroupId = CreateEmployeeSubGroup();
        CreateContract(employeeId, contractStart, new DateTime(2035, 12, 31));
        AddSubGroupGratuitySlab(subGroupId, changeYear: 5, basisDays: 26);
        AddSubGroupGratuitySlab(subGroupId, changeYear: 100, basisDays: 30);

        // 3000 raw days from contract start would be slab 2; resetting the clock so that only 1825 days
        // remain puts the employee back at the slab-1 boundary.
        var payDate = PayDateForServiceDays(contractStart, 3000);
        var resetDate = PayDateForServiceDays(contractStart, 3000).AddDays(-1824);
        AddGratuityReset(employeeId, resetDate, resetGratuityCalc: true);

        var basisDays = await _indemnityService.GetIndemnityBasisDaysAsync(
            employeeId, payDate, fixedDays: 99, employeeSubGroupId: subGroupId);

        basisDays.ShouldBe(26);
    }

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Ignore_GratuityPaidDate_When_ResetGratuityCalc_Is_Not_Set()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);

        var contractStart = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(contractStart);
        var subGroupId = CreateEmployeeSubGroup();
        CreateContract(employeeId, contractStart, new DateTime(2035, 12, 31));
        AddSubGroupGratuitySlab(subGroupId, changeYear: 5, basisDays: 26);
        AddSubGroupGratuitySlab(subGroupId, changeYear: 100, basisDays: 30);

        var payDate = PayDateForServiceDays(contractStart, 3000);
        // Gratuity was PAID but the calculation was not reset -> the clock keeps running from contract start.
        AddGratuityReset(employeeId, payDate.AddDays(-1824), resetGratuityCalc: false);

        var basisDays = await _indemnityService.GetIndemnityBasisDaysAsync(
            employeeId, payDate, fixedDays: 99, employeeSubGroupId: subGroupId);

        basisDays.ShouldBe(30);
    }

    [Fact]
    public async Task GetIndemnityBasisDaysAsync_Should_Ignore_A_Reset_Dated_After_The_PayDate()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);

        var contractStart = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(contractStart);
        var subGroupId = CreateEmployeeSubGroup();
        CreateContract(employeeId, contractStart, new DateTime(2035, 12, 31));
        AddSubGroupGratuitySlab(subGroupId, changeYear: 5, basisDays: 26);
        AddSubGroupGratuitySlab(subGroupId, changeYear: 100, basisDays: 30);

        var payDate = PayDateForServiceDays(contractStart, 3000);
        AddGratuityReset(employeeId, payDate.AddDays(1), resetGratuityCalc: true);

        var basisDays = await _indemnityService.GetIndemnityBasisDaysAsync(
            employeeId, payDate, fixedDays: 99, employeeSubGroupId: subGroupId);

        basisDays.ShouldBe(30);
    }

    #endregion

    #region << GetIndemnityDays (legacy Program.cs:3760) >>

    [Fact]
    public async Task GetIndemnityDaysAsync_Should_Return_The_Delta_Against_The_Previous_Months_Register()
    {
        SetPayrollSettings();
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var prevRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 5, 31));
        AddRegisterLine(prevRegisterId, "4025", number: 7);
        AddRegisterLine(prevRegisterId, "5025", number: 1);

        var currentRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        AddRegisterLine(currentRegisterId, "4025", number: 10);
        AddRegisterLine(currentRegisterId, "5025", number: 2);

        var days = await _indemnityService.GetIndemnityDaysAsync(
            employeeId, currentRegisterId, new DateTime(2024, 6, 30), null, null, null);

        // (10 + 2) - (7 + 1)
        days.ShouldBe(4);
    }

    [Fact]
    public async Task GetIndemnityDaysAsync_Should_Return_The_Raw_Closing_Days_On_The_Employees_First_Register()
    {
        SetPayrollSettings();
        var employeeId = CreateEmployee(new DateTime(2024, 6, 1));

        var currentRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        AddRegisterLine(currentRegisterId, "4025", number: 2.5);

        var days = await _indemnityService.GetIndemnityDaysAsync(
            employeeId, currentRegisterId, new DateTime(2024, 6, 30), null, null, null);

        // No previous register AND no earlier register at all -> legacy subtracts nothing.
        days.ShouldBe(2.5);
    }

    [Fact]
    public async Task GetIndemnityDaysAsync_Should_Fall_Back_To_The_Current_Accrual_When_The_Delta_Is_Negative_And_Indemnity_Was_Reset_In_Period()
    {
        SetPayrollSettings();
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var prevRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 5, 31));
        AddRegisterLine(prevRegisterId, "4025", number: 10);

        var currentRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        AddRegisterLine(currentRegisterId, "4025", number: 2);

        // Reset dated inside the pay period (2024-06-01 .. 2024-06-30).
        AddGratuityReset(employeeId, new DateTime(2024, 6, 15), resetGratuityCalc: true);

        var days = await _indemnityService.GetIndemnityDaysAsync(
            employeeId, currentRegisterId, new DateTime(2024, 6, 30), null, null, null);

        // Delta is 2 - 10 = -8; the reset correction replaces it with this month's raw accrual.
        days.ShouldBe(2);
    }

    [Fact]
    public async Task GetIndemnityDaysAsync_Should_Keep_The_Negative_Delta_When_There_Was_No_Reset_In_Period()
    {
        SetPayrollSettings();
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var prevRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 5, 31));
        AddRegisterLine(prevRegisterId, "4025", number: 10);

        var currentRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        AddRegisterLine(currentRegisterId, "4025", number: 2);

        var days = await _indemnityService.GetIndemnityDaysAsync(
            employeeId, currentRegisterId, new DateTime(2024, 6, 30), null, null, null);

        days.ShouldBe(-8);
    }

    [Fact]
    public async Task GetIndemnityDaysAsync_Should_Ignore_A_Reset_Dated_Outside_The_Pay_Period()
    {
        SetPayrollSettings();
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var prevRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 5, 31));
        AddRegisterLine(prevRegisterId, "4025", number: 10);

        var currentRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        AddRegisterLine(currentRegisterId, "4025", number: 2);

        // One day before the pay period start.
        AddGratuityReset(employeeId, new DateTime(2024, 5, 31), resetGratuityCalc: true);

        var days = await _indemnityService.GetIndemnityDaysAsync(
            employeeId, currentRegisterId, new DateTime(2024, 6, 30), null, null, null);

        days.ShouldBe(-8);
    }

    [Fact]
    public async Task GetIndemnityDaysAsync_Should_Snap_To_Exactly_Zero_When_The_Delta_Rounds_To_Zero_At_The_Configured_Decimals()
    {
        // decimalInSalaryRegister == 0 is coerced to legacy's default of 6.
        SetPayrollSettings(decimalInSalaryRegister: 0);
        var employeeId = CreateEmployee(new DateTime(2024, 6, 1));

        var currentRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        // 1e-7 rounds to 0 at 6 decimals.
        AddRegisterLine(currentRegisterId, "4025", number: 0.0000001);

        var days = await _indemnityService.GetIndemnityDaysAsync(
            employeeId, currentRegisterId, new DateTime(2024, 6, 30), null, null, null);

        days.ShouldBe(0);
    }

    [Fact]
    public async Task GetIndemnityDaysAsync_Should_Not_Round_The_Returned_Value()
    {
        SetPayrollSettings(decimalInSalaryRegister: 0); // -> 6 decimals for the comparisons only
        var employeeId = CreateEmployee(new DateTime(2024, 6, 1));

        var currentRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        AddRegisterLine(currentRegisterId, "4025", number: 0.1234567);

        var days = await _indemnityService.GetIndemnityDaysAsync(
            employeeId, currentRegisterId, new DateTime(2024, 6, 30), null, null, null);

        // Legacy uses Math.Round for the < 0 and == 0 tests ONLY; the value itself is returned unrounded.
        days.ShouldBe(0.1234567);
        days.ShouldNotBe(Math.Round(0.1234567, 6));
    }

    [Fact]
    public async Task GetIndemnityDaysAsync_Should_Respect_A_Configured_DecimalInSalaryRegister_Of_Two()
    {
        SetPayrollSettings(decimalInSalaryRegister: 2);
        var employeeId = CreateEmployee(new DateTime(2024, 6, 1));

        var currentRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        // 0.004 rounds to 0.00 at 2 decimals (but would survive at 6).
        AddRegisterLine(currentRegisterId, "4025", number: 0.004);

        var days = await _indemnityService.GetIndemnityDaysAsync(
            employeeId, currentRegisterId, new DateTime(2024, 6, 30), null, null, null);

        days.ShouldBe(0);
    }

    #endregion

    #region << UpdateEmployeeIndemnityQuota (legacy Program.cs:2774) >>

    [Fact]
    public async Task UpdateEmployeeIndemnityQuotaAsync_Should_Round_Both_Quota_Fields_To_Exactly_Three_Digits()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var quotaId = UsingDbContext(context =>
        {
            var quota = new EmployeeGratuityQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                EntitlementFrom = new DateTime(2024, 1, 1),
                EntitlementTo = new DateTime(2024, 12, 31),
                DeductionFrom = new DateTime(2024, 1, 1),
                DeductionTo = new DateTime(2024, 12, 31)
            };
            context.Set<EmployeeGratuityQuota>().Add(quota);
            context.SaveChanges();
            return quota.Id;
        });

        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        AddRegisterLine(registerId, "4025", number: 12.3456789);
        AddRegisterLine(registerId, "4005", number: 3.9995);

        var result = await _indemnityService.UpdateEmployeeIndemnityQuotaAsync(employeeId, registerId);

        result.Updated.ShouldBeTrue();
        result.EmployeeGratuityQuotaId.ShouldBe(quotaId);

        // Legacy's literal Math.Round(x, 3) -- banker's rounding, 3 digits, on BOTH fields.
        result.QuotaAccrued.ShouldBe(Math.Round(12.3456789, 3));
        result.QuotaAccrued.ShouldBe(12.346);
        result.QuotaUsed.ShouldBe(Math.Round(3.9995, 3));

        UsingDbContext(context =>
        {
            var persisted = context.Set<EmployeeGratuityQuota>().First(r => r.Id == quotaId);
            persisted.QuotaAccrued.ShouldBe(12.346);
            persisted.QuotaUsed.ShouldBe(Math.Round(3.9995, 3));
        });
    }

    [Fact]
    public async Task UpdateEmployeeIndemnityQuotaAsync_Should_Return_Not_Updated_When_The_Employee_Has_No_Quota_Row()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        AddRegisterLine(registerId, "4025", number: 5);

        var result = await _indemnityService.UpdateEmployeeIndemnityQuotaAsync(employeeId, registerId);

        // Legacy returned silently when emp.IndemnityQuota.FirstOrDefault() was null.
        result.Updated.ShouldBeFalse();
        result.EmployeeGratuityQuotaId.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateEmployeeIndemnityQuotaAsync_Should_Write_Zeroes_When_The_Wage_Lines_Are_Absent()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        UsingDbContext(context =>
        {
            context.Set<EmployeeGratuityQuota>().Add(new EmployeeGratuityQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                EntitlementFrom = new DateTime(2024, 1, 1),
                EntitlementTo = new DateTime(2024, 12, 31),
                QuotaAccrued = 99,
                QuotaUsed = 99
            });
            context.SaveChanges();
        });

        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));

        var result = await _indemnityService.UpdateEmployeeIndemnityQuotaAsync(employeeId, registerId);

        // Legacy used FirstOrDefault().Number, i.e. 0 when the wage line is missing -- it does NOT preserve
        // the previous quota values.
        result.Updated.ShouldBeTrue();
        result.QuotaAccrued.ShouldBe(0);
        result.QuotaUsed.ShouldBe(0);
    }

    #endregion

    #region << GetProvisionChangeIndemnity (legacy EmployeeCalculatedPayService.cs:2857) >>

    [Fact]
    public async Task GetProvisionChangeIndemnityAsync_Should_Subtract_The_Previous_Months_Closing_And_This_Months_Posted()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var prevRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 5, 31));
        AddRegisterLine(prevRegisterId, "3003", amount: 1000);

        var difference = await _indemnityService.GetProvisionChangeIndemnityAsync(
            employeeId, new DateTime(2024, 6, 30), currIndemnityClosingAmount: 1500, currIndemnityMonthlyPosted: 100);

        // 1500 - 100 - 1000
        difference.ShouldBe(400);
    }

    [Fact]
    public async Task GetProvisionChangeIndemnityAsync_Should_Return_Zero_When_There_Is_No_Previous_Regular_Register()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var difference = await _indemnityService.GetProvisionChangeIndemnityAsync(
            employeeId, new DateTime(2024, 6, 30), currIndemnityClosingAmount: 1500, currIndemnityMonthlyPosted: 100);

        difference.ShouldBe(0);
    }

    [Fact]
    public async Task GetProvisionChangeIndemnityAsync_Should_Ignore_A_Retro_Previous_Register_Where_ForPeriod_And_InPeriod_Differ()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        // ForPeriod is the previous month but it was paid in the current month -> a retro run, which legacy
        // excludes via the ForPeriod == InPeriod test.
        var retroRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 5, 31), inPeriod: new DateTime(2024, 6, 30));
        AddRegisterLine(retroRegisterId, "3003", amount: 1000);

        var difference = await _indemnityService.GetProvisionChangeIndemnityAsync(
            employeeId, new DateTime(2024, 6, 30), currIndemnityClosingAmount: 1500, currIndemnityMonthlyPosted: 100);

        difference.ShouldBe(0);
    }

    [Fact]
    public async Task GetProvisionChangeIndemnityAsync_Should_Ignore_An_OffCycle_Previous_Register()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var offCycleRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 5, 31), runType: PayrollRunType.OffCycle);
        AddRegisterLine(offCycleRegisterId, "3003", amount: 1000);

        var difference = await _indemnityService.GetProvisionChangeIndemnityAsync(
            employeeId, new DateTime(2024, 6, 30), currIndemnityClosingAmount: 1500, currIndemnityMonthlyPosted: 100);

        difference.ShouldBe(0);
    }

    #endregion

    #region << GetIndemnityPay (legacy EmployeeCalculatedPayService.cs:1716) >>

    [Fact]
    public async Task GetIndemnityPayAsync_Should_Post_Rate_Number_And_Bifurcated_Amount()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));

        // Basic pay counts towards the indemnity basis; the allowance does not.
        AddRegisterLine(registerId, "1010", rate: 900, includeInIndemnity: true, fixedDays: 30);
        AddRegisterLine(registerId, "2000", rate: 100, includeInIndemnity: false);
        AddRegisterLine(registerId, "4005", number: 15);

        // No EmployeeContract -> GetIndemnityBasisDays falls straight through to the FixedDays of "1010".
        var indemnityPayWageTypeId = CreateWageType("4010");

        var result = await _indemnityService.GetIndemnityPayAsync(
            employeeId, registerId, indemnityPayWageTypeId, new DateTime(2024, 6, 30),
            PayrollRunType.OffCycle, "End Of Service", employeeSubGroupId: null, indemnitySlabs: null);

        result.Rate.ShouldBe(900);
        result.Number.ShouldBe(15);
        result.Amount.ShouldBe((900d / 30d) * 15d);
        result.SalaryRegisterDetailCreated.ShouldBeTrue();
        result.SalaryRegisterDetailId.ShouldNotBeNull();

        UsingDbContext(context =>
        {
            var posted = context.Set<SalaryRegisterDetail>().First(r => r.Id == result.SalaryRegisterDetailId.Value);
            posted.Rate.ShouldBe(900);
            posted.Number.ShouldBe(15);
            posted.Amount.ShouldBe((900d / 30d) * 15d);
        });
    }

    [Fact]
    public async Task GetIndemnityPayAsync_Should_Update_An_Existing_Line_Instead_Of_Inserting_A_Second_One()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));

        AddRegisterLine(registerId, "1010", rate: 900, includeInIndemnity: true, fixedDays: 30);
        AddRegisterLine(registerId, "4005", number: 15);
        var existingLineId = AddRegisterLine(registerId, "4010", rate: 1, number: 1, amount: 1);

        var indemnityPayWageTypeId = CreateWageType("4010");

        var result = await _indemnityService.GetIndemnityPayAsync(
            employeeId, registerId, indemnityPayWageTypeId, new DateTime(2024, 6, 30),
            PayrollRunType.OffCycle, "End Of Service", employeeSubGroupId: null, indemnitySlabs: null);

        result.SalaryRegisterDetailUpdated.ShouldBeTrue();
        result.SalaryRegisterDetailCreated.ShouldBeFalse();
        result.SalaryRegisterDetailId.ShouldBe(existingLineId);

        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>()
                .Count(r => r.SalaryRegisterId == registerId && r.WageTypeId == indemnityPayWageTypeId)
                .ShouldBe(1);
        });
    }

    [Fact]
    public async Task GetIndemnityPayAsync_Should_Write_Nothing_When_There_Are_No_Indemnity_Paid_Days()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));

        AddRegisterLine(registerId, "1010", rate: 900, includeInIndemnity: true, fixedDays: 30);
        AddRegisterLine(registerId, "4005", number: 0);

        var indemnityPayWageTypeId = CreateWageType("4010");

        var result = await _indemnityService.GetIndemnityPayAsync(
            employeeId, registerId, indemnityPayWageTypeId, new DateTime(2024, 6, 30),
            PayrollRunType.OffCycle, "End Of Service", employeeSubGroupId: null, indemnitySlabs: null);

        result.Rate.ShouldBe(0);
        result.Number.ShouldBe(0);
        result.Amount.ShouldBe(0);
        result.SalaryRegisterDetailId.ShouldBeNull();

        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>()
                .Any(r => r.SalaryRegisterId == registerId && r.WageTypeId == indemnityPayWageTypeId)
                .ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetIndemnityPayAsync_Should_Write_Nothing_When_The_Computed_Amount_Is_Zero()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));

        // Paid days exist but nothing is included in the indemnity basis -> amount 0 -> legacy's
        // `if (amount != 0)` write guard suppresses the row.
        AddRegisterLine(registerId, "1010", rate: 0, includeInIndemnity: true, fixedDays: 30);
        AddRegisterLine(registerId, "4005", number: 15);

        var indemnityPayWageTypeId = CreateWageType("4010");

        var result = await _indemnityService.GetIndemnityPayAsync(
            employeeId, registerId, indemnityPayWageTypeId, new DateTime(2024, 6, 30),
            PayrollRunType.OffCycle, "End Of Service", employeeSubGroupId: null, indemnitySlabs: null);

        result.Number.ShouldBe(15);
        result.Amount.ShouldBe(0);
        result.SalaryRegisterDetailId.ShouldBeNull();
    }

    #endregion

    #region << GetIndemnityProvision / GetTotalIndemnityProvision (legacy 2610 / 2643) >>

    [Fact]
    public async Task GetIndemnityProvisionAsync_Should_Post_From_WageCode_4004_And_Always_Return_Zero()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));

        AddRegisterLine(registerId, "1010", rate: 900, includeInIndemnity: true, fixedDays: 30);
        AddRegisterLine(registerId, "4004", number: 2.5);
        AddRegisterLine(registerId, "4006", number: 100); // must be ignored by the MONTHLY provision
        var provisionLineId = AddRegisterLine(registerId, "3000", fixedDays: 30);

        var result = await _indemnityService.GetIndemnityProvisionAsync(
            employeeId, provisionLineId, new DateTime(2024, 6, 30),
            PayrollRunType.Regular, offCycleReason: null, employeeSubGroupId: null, indemnitySlabs: null);

        result.Rate.ShouldBe(900);
        result.Number.ShouldBe(2.5);
        result.Amount.ShouldBe((900d / 30d) * 2.5d);

        // LEGACY QUIRK, preserved: GetIndemnityProvision returns its never-assigned local, i.e. always 0.
        result.LegacyReturnValue.ShouldBe(0);
    }

    [Fact]
    public async Task GetTotalIndemnityProvisionAsync_Should_Post_From_WageCode_4006_And_Return_The_Posted_Amount()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));

        AddRegisterLine(registerId, "1010", rate: 900, includeInIndemnity: true, fixedDays: 30);
        AddRegisterLine(registerId, "4004", number: 2.5); // must be ignored by the TOTAL provision
        AddRegisterLine(registerId, "4006", number: 100);
        var provisionLineId = AddRegisterLine(registerId, "3003", fixedDays: 30);

        var result = await _indemnityService.GetTotalIndemnityProvisionAsync(
            employeeId, provisionLineId, new DateTime(2024, 6, 30),
            PayrollRunType.Regular, offCycleReason: null, employeeSubGroupId: null, indemnitySlabs: null);

        result.Rate.ShouldBe(900);
        result.Number.ShouldBe(100);
        result.Amount.ShouldBe((900d / 30d) * 100d);

        // Unlike GetIndemnityProvision, this one really does return the posted amount.
        result.LegacyReturnValue.ShouldBe(result.Amount);
    }

    [Fact]
    public async Task GetIndemnityProvisionAsync_Should_Zero_The_Amount_When_The_Source_Wage_Line_Is_Absent()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: false);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));

        AddRegisterLine(registerId, "1010", rate: 900, includeInIndemnity: true, fixedDays: 30);
        // No "4004" line at all.
        var provisionLineId = AddRegisterLine(registerId, "3000", rate: 7, number: 7, amount: 777, fixedDays: 30);

        var result = await _indemnityService.GetIndemnityProvisionAsync(
            employeeId, provisionLineId, new DateTime(2024, 6, 30),
            PayrollRunType.Regular, offCycleReason: null, employeeSubGroupId: null, indemnitySlabs: null);

        // Legacy's else-branch zeroes ONLY the Amount; Rate and Number keep their previous values.
        result.Amount.ShouldBe(0);
        result.Rate.ShouldBe(7);
        result.Number.ShouldBe(7);
        result.LegacyReturnValue.ShouldBe(0);

        UsingDbContext(context =>
        {
            var persisted = context.Set<SalaryRegisterDetail>().First(r => r.Id == provisionLineId);
            persisted.Amount.ShouldBe(0);
            persisted.Rate.ShouldBe(7);
            persisted.Number.ShouldBe(7);
        });
    }

    [Fact]
    public async Task GetTotalIndemnityProvisionAsync_Should_Bifurcate_Across_Slabs_When_SlabWise_Is_Enabled()
    {
        SetPayrollSettings(enableUnpaidDedSlabWise: true);
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));

        AddRegisterLine(registerId, "1010", rate: 780, includeInIndemnity: true, fixedDays: 30);
        AddRegisterLine(registerId, "4006", number: 25);
        var provisionLineId = AddRegisterLine(registerId, "3003", fixedDays: 30);

        var slabs = new List<IndemnitySlab>
        {
            Slab(srNo: 1, indemnityDays: 10, basisDays: 26),
            Slab(srNo: 2, indemnityDays: 20, basisDays: 30)
        };

        var result = await _indemnityService.GetTotalIndemnityProvisionAsync(
            employeeId, provisionLineId, new DateTime(2024, 6, 30),
            PayrollRunType.Regular, offCycleReason: null, employeeSubGroupId: null, indemnitySlabs: slabs);

        result.Amount.ShouldBe((780d / 26d) * 10d + (780d / 30d) * 15d);
    }

    #endregion

    #region << GetIndemnityProvisionDayWiseEntry (legacy EmployeeCalculatedPayService.cs:2743) >>

    [Fact]
    public async Task GetIndemnityProvisionDayWiseEntryAsync_Should_Delete_Existing_Rows_And_Return_Early_When_There_Was_No_Increment_This_Period()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        var provisionLineId = AddRegisterLine(registerId, "3000", rate: 10, number: 1, amount: 100);

        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDaysDetail>().Add(new SalaryRegisterDaysDetail
            {
                TenantId = TenantId,
                SalaryRegisterDetailId = provisionLineId,
                Period = new DateTime(2024, 5, 31),
                Amount = 1
            });
            context.Set<SalaryRegisterDaysDetail>().Add(new SalaryRegisterDaysDetail
            {
                TenantId = TenantId,
                SalaryRegisterDetailId = provisionLineId,
                Period = new DateTime(2024, 6, 30),
                Amount = 2
            });
            context.SaveChanges();
        });

        // No EmployeeBasicPay starting inside June 2024 -> legacy bails out, leaving the rows deleted.
        var result = await _indemnityService.GetIndemnityProvisionDayWiseEntryAsync(
            employeeId, registerId, new DateTime(2024, 6, 30));

        result.DeletedDaysDetailCount.ShouldBe(2);
        result.CreatedDaysDetailCount.ShouldBe(0);
        result.ProvisionChangeAmount.ShouldBe(0);

        UsingDbContext(context =>
        {
            // SalaryRegisterDaysDetail is a FullAuditedEntity, so the delete is a SOFT delete; the raw test
            // DbContext sees the tombstones, hence the explicit IsDeleted filter.
            context.Set<SalaryRegisterDaysDetail>()
                .Any(r => r.SalaryRegisterDetailId == provisionLineId && !r.IsDeleted)
                .ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetIndemnityProvisionDayWiseEntryAsync_Should_Return_Early_When_The_Increment_Is_The_Employees_Very_First_BasicPay()
    {
        var employeeId = CreateEmployee(new DateTime(2024, 6, 1));
        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        AddRegisterLine(registerId, "3000", rate: 10, number: 1, amount: 100);

        // A basic pay starting in-period, but nothing before it -> not an increment.
        AddBasicPay(employeeId, new DateTime(2024, 6, 10), new DateTime(2030, 12, 31), 900);

        var result = await _indemnityService.GetIndemnityProvisionDayWiseEntryAsync(
            employeeId, registerId, new DateTime(2024, 6, 30));

        result.CreatedDaysDetailCount.ShouldBe(0);
        result.ProvisionChangeAmount.ShouldBe(0);
    }

    [Fact]
    public async Task GetIndemnityProvisionDayWiseEntryAsync_Should_Split_The_Provision_And_Add_The_Previous_Period_Difference()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        // Previous month's regular register carries the prior closing provision.
        var prevRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 5, 31));
        AddRegisterLine(prevRegisterId, "3003", amount: 1000);

        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        AddRegisterLine(registerId, "3003", amount: 1500);
        var provisionLineId = AddRegisterLine(registerId, "3000", rate: 10, number: 1, amount: 100);

        // An increment inside June, with an earlier basic pay to prove it IS an increment.
        AddBasicPay(employeeId, new DateTime(2020, 1, 1), new DateTime(2024, 6, 14), 800);
        AddBasicPay(employeeId, new DateTime(2024, 6, 15), new DateTime(2030, 12, 31), 900);

        var result = await _indemnityService.GetIndemnityProvisionDayWiseEntryAsync(
            employeeId, registerId, new DateTime(2024, 6, 30));

        // 1500 (current closing) - 100 (current monthly posted) - 1000 (previous closing)
        result.ProvisionChangeAmount.ShouldBe(400);

        // One row for the current period (100) and one catch-up row for the previous period (400).
        result.CreatedDaysDetailCount.ShouldBe(2);
        result.PostedAmount.ShouldBe(500);
        result.SalaryRegisterDetailId.ShouldBe(provisionLineId);

        UsingDbContext(context =>
        {
            var line = context.Set<SalaryRegisterDetail>().First(r => r.Id == provisionLineId);
            line.Amount.ShouldBe(500);
            line.Rate.ShouldBe(10);
            line.Number.ShouldBe(1);

            var dayRows = context.Set<SalaryRegisterDaysDetail>()
                .Where(r => r.SalaryRegisterDetailId == provisionLineId && !r.IsDeleted)
                .OrderBy(r => r.Period)
                .ToList();

            dayRows.Count.ShouldBe(2);
            dayRows[0].Period.ShouldBe(new DateTime(2024, 5, 31));
            dayRows[0].Amount.ShouldBe(400);
            dayRows[1].Period.ShouldBe(new DateTime(2024, 6, 30));
            dayRows[1].Amount.ShouldBe(100);
        });
    }

    [Fact]
    public async Task GetIndemnityProvisionDayWiseEntryAsync_Should_Not_Split_When_The_Provision_Change_Is_Zero()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var prevRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 5, 31));
        AddRegisterLine(prevRegisterId, "3003", amount: 1400);

        var registerId = CreateSalaryRegister(employeeId, new DateTime(2024, 6, 30));
        AddRegisterLine(registerId, "3003", amount: 1500);
        var provisionLineId = AddRegisterLine(registerId, "3000", rate: 10, number: 1, amount: 100);

        AddBasicPay(employeeId, new DateTime(2020, 1, 1), new DateTime(2024, 6, 14), 800);
        AddBasicPay(employeeId, new DateTime(2024, 6, 15), new DateTime(2030, 12, 31), 900);

        var result = await _indemnityService.GetIndemnityProvisionDayWiseEntryAsync(
            employeeId, registerId, new DateTime(2024, 6, 30));

        // 1500 - 100 - 1400 == 0 -> legacy writes no day-wise rows and leaves the line untouched.
        result.ProvisionChangeAmount.ShouldBe(0);
        result.CreatedDaysDetailCount.ShouldBe(0);
        result.PostedAmount.ShouldBe(100);

        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>().First(r => r.Id == provisionLineId).Amount.ShouldBe(100);
            context.Set<SalaryRegisterDaysDetail>().Any(r => r.SalaryRegisterDetailId == provisionLineId && !r.IsDeleted).ShouldBeFalse();
        });
    }

    #endregion
}
