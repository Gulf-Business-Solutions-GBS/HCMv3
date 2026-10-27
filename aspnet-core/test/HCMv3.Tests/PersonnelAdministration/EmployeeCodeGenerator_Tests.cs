using System.Linq;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Legacy <c>NumberSeries.GetCode(EmploymentType)</c>, called from <c>Employee.OnSaving</c>. Built over in-memory
/// rows. Several tests pin legacy behaviour that looks odd but is what legacy did (ALL wins, Start ignored).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeCodeGenerator_Tests
{
    private static IRepository<TEntity> ListRepository<TEntity>(params TEntity[] rows)
        where TEntity : class, IEntity<int>
    {
        var repository = Substitute.For<IRepository<TEntity>>();
        repository.GetAll().Returns(_ => rows.AsQueryable());
        return repository;
    }

    private static EmployeeCodeGenerator Generator(NumberSeries[] series, params Employee[] employees)
    {
        return new EmployeeCodeGenerator(ListRepository(employees), ListRepository(series));
    }

    private static Employee Existing(int id, string code, EmploymentType type = EmploymentType.Permanent)
    {
        return new Employee { Id = id, Code = code, EmploymentType = type };
    }

    private static NumberSeries Series(EmploymentType type, string prefix, string formatting, long start = 0)
    {
        return new NumberSeries { Id = (int)type + 1, Code = type.ToString(), EmploymentType = type, Prefix = prefix, Formatting = formatting, Start = start };
    }

    private static string CodeFor(EmployeeCodeGenerator generator, EmploymentType type)
    {
        var employee = new Employee { EmploymentType = type };
        generator.AssignCodeIfEmpty(employee);
        return employee.Code;
    }

    [Fact]
    public void Should_Fall_Back_To_Highest_Numeric_Code_Plus_One_When_No_Series_Is_Set_Up()
    {
        var generator = Generator(new NumberSeries[0], Existing(1, "7"), Existing(2, "ABC"), Existing(3, "12", EmploymentType.Temporary));

        CodeFor(generator, EmploymentType.Permanent).ShouldBe("13");
    }

    [Fact]
    public void Should_Number_Over_All_Employees_With_The_ALL_Series_Prefix_And_Format()
    {
        var generator = Generator(
            new[] { Series(EmploymentType.ALL, "EMP", "0000") },
            Existing(1, "EMP0007"),
            Existing(2, "EMP0009", EmploymentType.Temporary),
            Existing(3, "99"));

        // "99" has no EMP prefix so it does not count; EMP0009 counts although that employee is Temporary.
        CodeFor(generator, EmploymentType.Permanent).ShouldBe("EMP0010");
    }

    [Fact]
    public void Should_Number_Over_Employees_Of_The_Same_Employment_Type_Only()
    {
        var generator = Generator(
            new[] { Series(EmploymentType.Permanent, "P-", "000"), Series(EmploymentType.Temporary, "T-", "000") },
            Existing(1, "P-005"),
            Existing(2, "T-009", EmploymentType.Temporary),
            Existing(3, "P-020", EmploymentType.Temporary));

        // P-020 belongs to a Temporary employee, so it does not count towards the Permanent series.
        CodeFor(generator, EmploymentType.Permanent).ShouldBe("P-006");
        CodeFor(generator, EmploymentType.Temporary).ShouldBe("T-010");
    }

    [Fact]
    public void Should_Use_The_ALL_Series_Even_When_A_Series_For_The_Type_Also_Exists()
    {
        // Pinned legacy behaviour: GetCode looks for an ALL series first and never reaches the per-type one.
        var generator = Generator(
            new[] { Series(EmploymentType.Permanent, "P", "0"), Series(EmploymentType.ALL, "A", "00") },
            Existing(1, "A05"));

        CodeFor(generator, EmploymentType.Permanent).ShouldBe("A06");
    }

    [Fact]
    public void Should_Ignore_Start()
    {
        // Pinned legacy behaviour: NumberSeries.Start is never read; the first code of a new series is 1.
        var generator = Generator(new[] { Series(EmploymentType.ALL, "E", "00000", start: 1000) });

        CodeFor(generator, EmploymentType.Permanent).ShouldBe("E00001");
    }

    [Fact]
    public void Should_Not_Prefix_When_The_Series_Has_No_Prefix()
    {
        var generator = Generator(new[] { Series(EmploymentType.ALL, null, "000") }, Existing(1, "041"), Existing(2, "X50"));

        CodeFor(generator, EmploymentType.Temporary).ShouldBe("042");
    }

    [Fact]
    public void Should_Ignore_Codes_That_Are_Not_Numbers_After_The_Prefix_And_Employees_Without_A_Code()
    {
        var generator = Generator(
            new[] { Series(EmploymentType.ALL, "E", "0") },
            Existing(1, "E12"),
            Existing(2, "E12A"),
            Existing(3, "EX"),
            Existing(4, null));

        CodeFor(generator, EmploymentType.Permanent).ShouldBe("E13");
    }

    [Fact]
    public void Should_Fall_Back_When_Only_A_Series_For_Another_Employment_Type_Exists()
    {
        // Legacy refused to save here ("NumberSeries not found"); HCMv3 keeps its previous max + 1 instead.
        var generator = Generator(new[] { Series(EmploymentType.Permanent, "P", "000") }, Existing(1, "P001"), Existing(2, "4"));

        CodeFor(generator, EmploymentType.Temporary).ShouldBe("5");
    }

    [Fact]
    public void Should_Keep_A_Code_That_Was_Typed_In()
    {
        var generator = Generator(new[] { Series(EmploymentType.ALL, "E", "000") });
        var employee = new Employee { Code = "MANUAL-1" };

        generator.AssignCodeIfEmpty(employee);

        employee.Code.ShouldBe("MANUAL-1");
    }

    [Fact]
    public void Should_Recode_An_Employee_Whose_Employment_Type_Changed()
    {
        var employee = Existing(1, "P-001");
        var generator = Generator(
            new[] { Series(EmploymentType.Permanent, "P-", "000"), Series(EmploymentType.Temporary, "T-", "000") },
            employee,
            Existing(2, "T-004", EmploymentType.Temporary));

        employee.EmploymentType = EmploymentType.Temporary;
        generator.ReassignCodeIfEmploymentTypeChanged(employee, EmploymentType.Permanent);

        employee.Code.ShouldBe("T-005");
    }

    [Fact]
    public void Should_Recode_Under_The_ALL_Series_Counting_The_Employees_Own_Current_Code()
    {
        // Pinned legacy behaviour: with an ALL series a type change still takes a new number, and the employee's
        // own (highest) code is part of the list it is counted over.
        var employee = Existing(1, "E009");
        var generator = Generator(new[] { Series(EmploymentType.ALL, "E", "000") }, employee, Existing(2, "E003"));

        employee.EmploymentType = EmploymentType.Temporary;
        generator.ReassignCodeIfEmploymentTypeChanged(employee, EmploymentType.Permanent);

        employee.Code.ShouldBe("E010");
    }

    [Fact]
    public void Should_Not_Recode_When_The_Employment_Type_Is_Unchanged()
    {
        var employee = Existing(1, "P-001");
        var generator = Generator(new[] { Series(EmploymentType.Permanent, "P-", "000") }, employee, Existing(2, "P-007"));

        generator.ReassignCodeIfEmploymentTypeChanged(employee, EmploymentType.Permanent);

        employee.Code.ShouldBe("P-001");
    }

    [Fact]
    public void Should_Keep_The_Code_When_No_Series_Applies_To_The_New_Type()
    {
        var employee = Existing(1, "P-001");
        var generator = Generator(new[] { Series(EmploymentType.Permanent, "P-", "000") }, employee);

        employee.EmploymentType = EmploymentType.Temporary;
        generator.ReassignCodeIfEmploymentTypeChanged(employee, EmploymentType.Permanent);

        employee.Code.ShouldBe("P-001");
    }
}
