using System;
using System.Linq;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Seed helpers shared by the Full &amp; Final tests: an employee with a contract, an org assignment carrying the
/// group / sub group / payroll area (with a country), a 260 KD basic pay, and the Kuwait defaults --
/// <list type="bullet">
/// <item><description>gratuity slabs: 15 days a year to year 5, then 26 on a 26-day basis -- a month (two slabs, as legacy's
/// absolute-change-year walk requires);</description></item>
/// <item><description>employee group cap: 18 months at 100 %;</description></item>
/// <item><description>indemnity pay rules as the Go-Live defaults seed them (legacy CCH): Termination 100 % at every length of service,
/// Resignation 0 / 50 / 66.67 / 100 % for 0-2.99, 3-4.99, 5-9.99 and 10-99 years.</description></item>
/// </list>
/// </summary>
public abstract class FullAndFinalTestBase : AppTestBase
{
    protected const double MonthlyBasic = 260;

    protected int TenantId => AbpSession.TenantId ?? 1;

    protected FullAndFinalTestBase()
    {
        LoginAsDefaultTenantAdmin();
    }

    /// <summary>The ids a scenario needs afterwards.</summary>
    protected class Seeded
    {
        public int EmployeeId { get; set; }
        public int EmployeeGroupId { get; set; }
        public int PayrollAreaId { get; set; }
        public int SeparationId { get; set; }
    }

    /// <summary>
    /// One employee who joined on <paramref name="joiningDate"/> and leaves on <paramref name="lastDayInService"/>
    /// by <paramref name="separationType"/>, with the Kuwait defaults in place.
    /// </summary>
    protected Seeded SeedLeaver(
        DateTime joiningDate,
        DateTime lastDayInService,
        SeparationType separationType,
        double indemnityMaxMonths = 18,
        double indemnityMaxAccruePercentage = 100,
        bool seedKuwaitRules = true)
    {
        var seeded = new Seeded();

        UsingDbContext(context =>
        {
            var country = context.Set<Country>().FirstOrDefault(c => c.Code == "KW");
            if (country == null)
            {
                country = context.Set<Country>().Add(new Country { TenantId = TenantId, Code = "KW", NameEn = "Kuwait", NameAr = "الكويت" }).Entity;
            }

            var currency = context.Set<Currency>().FirstOrDefault(c => c.Code == "KWD")
                           ?? context.Set<Currency>().Add(new Currency { TenantId = TenantId, Code = "KWD", NameEn = "KWD", NameAr = "KWD", DecimalCount = 3 }).Entity;

            var period = context.Set<PayrollPeriod>().FirstOrDefault()
                         ?? context.Set<PayrollPeriod>().Add(new PayrollPeriod { TenantId = TenantId, Code = "Jan-Dec", NameEn = "Jan-Dec", NameAr = "Jan-Dec" }).Entity;
            context.SaveChanges();

            var area = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = "PA" + Guid.NewGuid().ToString("N").Substring(0, 6),
                Name = "Kuwait Area",
                PayrollPeriodId = period.Id,
                CurrencyId = currency.Id,
                CountryId = country.Id,
                CutOffDay = 25
            }).Entity;

            var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup
            {
                TenantId = TenantId,
                NameEn = "Staff",
                NameAr = "موظفون",
                IndemnityMaxNumber = indemnityMaxMonths,
                IndemnityMaxUnit = Unit.Month,
                IndemnityMaxAccruePercentage = indemnityMaxAccruePercentage
            }).Entity;
            context.SaveChanges();

            var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup
            {
                TenantId = TenantId,
                EmployeeGroupId = group.Id,
                NameEn = "Staff SG",
                NameAr = "فرعي"
            }).Entity;

            var nationality = context.Set<Nationality>().Add(new Nationality { TenantId = TenantId, NameEn = "Local", NameAr = "محلي" }).Entity;
            var contractType = context.Set<ContractType>().Add(new ContractType { TenantId = TenantId, NameEn = "Unlimited", NameAr = "غير محدد" }).Entity;
            var reason = context.Set<SeparationReason>().Add(new SeparationReason { TenantId = TenantId, NameEn = "Personal", NameAr = "شخصي" }).Entity;
            context.SaveChanges();

            var employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = "FNF" + Guid.NewGuid().ToString("N").Substring(0, 6),
                FirstNameEn = "Leaver",
                LastNameEn = "Test",
                FirstNameAr = "مغادر",
                LastNameAr = "تجربة",
                FullNameEn = "Leaver Test",
                FullNameAr = "مغادر تجربة",
                NationalityId = nationality.Id,
                JoiningDate = joiningDate,
                Status = EmployeeStatus.Active
            }).Entity;
            context.SaveChanges();

            context.Set<EmployeeContract>().Add(new EmployeeContract
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ContractTypeId = contractType.Id,
                StartDate = joiningDate,
                EndDate = new DateTime(2099, 12, 31)
            });

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = joiningDate,
                EndDate = new DateTime(2099, 12, 31),
                EmployeeGroupId = group.Id,
                EmployeeSubGroupId = subGroup.Id,
                PayrollAreaId = area.Id
            });

            context.Set<EmployeeSubGroupGratuity>().Add(new EmployeeSubGroupGratuity
            {
                TenantId = TenantId, EmployeeSubGroupId = subGroup.Id,
                StartDate = new DateTime(2000, 1, 1), EndDate = new DateTime(9999, 12, 31),
                ChangeYear = 5, GratuityDays = 15, BasisDays = 26
            });
            context.Set<EmployeeSubGroupGratuity>().Add(new EmployeeSubGroupGratuity
            {
                TenantId = TenantId, EmployeeSubGroupId = subGroup.Id,
                StartDate = new DateTime(2000, 1, 1), EndDate = new DateTime(9999, 12, 31),
                ChangeYear = 99, GratuityDays = 26, BasisDays = 26
            });

            var basicWage = WageType(context, "1000", includeInIndemnity: true);
            WageType(context, "1010", fixedDays: 26);
            context.SaveChanges();

            var basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = joiningDate,
                EndDate = new DateTime(2099, 12, 31),
                Amount = MonthlyBasic
            }).Entity;
            context.SaveChanges();

            context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
            {
                TenantId = TenantId,
                EmployeeBasicPayId = basicPay.Id,
                WageTypeId = basicWage.Id,
                Amount = MonthlyBasic,
                CurrencyId = currency.Id
            });

            var separation = context.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                SubmissionDate = lastDayInService.AddMonths(-1),
                LastDayInService = lastDayInService,
                PlannedLastDayInService = lastDayInService,
                SeparationType = separationType,
                SeparationReasonId = reason.Id
            }).Entity;
            context.SaveChanges();

            if (seedKuwaitRules && !context.Set<IndemnityPayRule>().Any(r => r.CountryId == country.Id))
            {
                SeedKuwaitRule(context, country.Id, SeparationType.Termination, (0, 2.99, 100), (3, 4.99, 100), (5, 9.99, 100), (10, 99, 100));
                SeedKuwaitRule(context, country.Id, SeparationType.Resignation, (0, 2.99, 0), (3, 4.99, 50), (5, 9.99, 66.67), (10, 99, 100));
            }

            seeded.EmployeeId = employee.Id;
            seeded.EmployeeGroupId = group.Id;
            seeded.PayrollAreaId = area.Id;
            seeded.SeparationId = separation.Id;
        });

        return seeded;
    }

    private void SeedKuwaitRule(HCMv3.EntityFrameworkCore.HCMv3DbContext context, int countryId, SeparationType type,
        params (double Min, double Max, double Percentage)[] bands)
    {
        var rule = context.Set<IndemnityPayRule>().Add(new IndemnityPayRule
        {
            TenantId = TenantId,
            SeparationType = type,
            StartDate = new DateTime(2000, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            MaxNumber = 18,
            MaxUnit = Unit.Month,
            MaxPayPercentage = 100,
            CountryId = countryId
        }).Entity;
        context.SaveChanges();

        foreach (var band in bands)
        {
            context.Set<IndemnityPayRuleDetail>().Add(new IndemnityPayRuleDetail
            {
                TenantId = TenantId,
                IndemnityPayRuleId = rule.Id,
                MinYearOfService = band.Min,
                MaxYearOfService = band.Max,
                PayPercentage = band.Percentage
            });
        }

        context.SaveChanges();
    }

    private WageType WageType(HCMv3.EntityFrameworkCore.HCMv3DbContext context, string code, bool includeInIndemnity = false, double fixedDays = 0)
    {
        var existing = context.Set<WageType>().FirstOrDefault(r => r.Code == code);
        if (existing != null)
        {
            return existing;
        }

        return context.Set<WageType>().Add(new WageType
        {
            TenantId = TenantId,
            Code = code,
            NameEn = "WT " + code,
            NameAr = "بند " + code,
            FixedDays = fixedDays,
            IncludeInIndemnity = includeInIndemnity,
            PayrollOrder = 1,
            Status = true
        }).Entity;
    }

    /// <summary>The indemnity quota balance the F&amp;F may compensate against.</summary>
    protected void SeedGratuityBalance(int employeeId, double accrued)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeGratuityQuota>().Add(new EmployeeGratuityQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                EntitlementFrom = new DateTime(2000, 1, 1),
                EntitlementTo = new DateTime(2099, 12, 31),
                DeductionFrom = new DateTime(2000, 1, 1),
                DeductionTo = new DateTime(2099, 12, 31),
                QuotaAccrued = accrued
            });
        });
    }

    /// <summary>An annual-leave (code "1") quota with <paramref name="accrued"/> days, less <paramref name="used"/>.</summary>
    protected void SeedLeaveBalance(int employeeId, double accrued, double used = 0)
    {
        UsingDbContext(context =>
        {
            var annual = AnnualLeave(context);

            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = annual.Id,
                EntitlementFrom = new DateTime(2000, 1, 1),
                EntitlementTo = new DateTime(2099, 12, 31),
                DeductionFrom = new DateTime(2000, 1, 1),
                DeductionTo = new DateTime(2099, 12, 31),
                QuotaAccrued = accrued,
                QuotaUsed = used
            });
        });
    }

    /// <summary>The employee's own ticket (code "1") quota.</summary>
    protected void SeedTicketBalance(int employeeId, double accrued)
    {
        UsingDbContext(context =>
        {
            var ticketType = context.Set<TicketType>().FirstOrDefault(t => t.Code == "1")
                             ?? context.Set<TicketType>().Add(new TicketType { TenantId = TenantId, Code = "1", NameEn = "Self", NameAr = "ذاتي" }).Entity;
            context.SaveChanges();

            context.Set<EmployeeTicketQuota>().Add(new EmployeeTicketQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                TicketTypeId = ticketType.Id,
                EntitlementFrom = new DateTime(2000, 1, 1),
                EntitlementTo = new DateTime(2099, 12, 31),
                DeductionFrom = new DateTime(2000, 1, 1),
                DeductionTo = new DateTime(2099, 12, 31),
                QuotaAccrued = accrued
            });
        });
    }

    protected void SetAutoFill(bool autoFill)
    {
        UsingDbContext(context =>
        {
            var settings = context.ManagePayrollSettingses.FirstOrDefault(s => s.TenantId == TenantId);
            if (settings == null)
            {
                context.ManagePayrollSettingses.Add(new ManagePayrollSettings { TenantId = TenantId, AutoFillFnFCompensation = autoFill });
            }
            else
            {
                settings.AutoFillFnFCompensation = autoFill;
            }
        });
    }

    protected AbsenceType AnnualLeave(HCMv3.EntityFrameworkCore.HCMv3DbContext context)
    {
        var annual = context.Set<AbsenceType>().FirstOrDefault(a => a.Code == "1");
        if (annual == null)
        {
            annual = context.Set<AbsenceType>().Add(new AbsenceType { TenantId = TenantId, Code = "1", NameEn = "Annual Leave", NameAr = "إجازة سنوية" }).Entity;
            context.SaveChanges();
        }

        return annual;
    }
}
