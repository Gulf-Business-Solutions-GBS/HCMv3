using System;
using System.Linq;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// The comparison behind "what did this request change": the proposal against the personnel file, field by
/// field. No database -- the rule is pure, and the cases worth pinning are the ones about emptiness.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PersonalDataChangeFields_Tests
{
    private static Employee AnEmployee()
    {
        return new Employee
        {
            FirstNameEn = "Ahmad",
            Mobile = 55550000,
            NationalityId = 3,
            Gender = Gender.Male,
            BirthDate = new DateTime(1980, 3, 2),
            Email = "ahmad@example.com"
        };
    }

    [Fact]
    public void Should_Cover_Forty_Fields_Each_Once()
    {
        PersonalDataChangeFields.All.Count.ShouldBe(40);
        PersonalDataChangeFields.All.Select(f => f.Name).Distinct().Count().ShouldBe(40);
        PersonalDataChangeFields.All.ShouldAllBe(f => typeof(EmployeePersonalDataChange).GetProperty(f.Name) != null);
    }

    [Fact]
    public void Should_Not_Report_A_Field_The_Employee_Did_Not_Propose()
    {
        var changes = PersonalDataChangeFields.Changes(
            new EmployeePersonalDataChange(),
            PersonalDataChangeFields.Read(AnEmployee()));

        changes.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Not_Report_A_Field_Sent_Back_Unchanged()
    {
        var proposal = new EmployeePersonalDataChange
        {
            FirstNameEn = "Ahmad",
            Mobile = 55550000,
            NationalityId = 3,
            Gender = Gender.Male,
            BirthDate = new DateTime(1980, 3, 2, 9, 30, 0)
        };

        PersonalDataChangeFields.Changes(proposal, PersonalDataChangeFields.Read(AnEmployee())).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Treat_Empty_And_Missing_As_The_Same_Thing()
    {
        // A box that was empty and stayed empty has not changed, whichever of the two the client sent.
        var proposal = new EmployeePersonalDataChange { BloodGroup = string.Empty };

        PersonalDataChangeFields.Changes(proposal, PersonalDataChangeFields.Read(AnEmployee())).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Report_A_Field_That_Was_Blanked()
    {
        var proposal = new EmployeePersonalDataChange { Email = string.Empty };

        var change = PersonalDataChangeFields.Changes(proposal, PersonalDataChangeFields.Read(AnEmployee())).Single();

        change.Field.Name.ShouldBe(nameof(EmployeePersonalDataChange.Email));
        change.Previous.ShouldBe("ahmad@example.com");
        change.Proposed.ShouldBe(string.Empty);
    }

    [Fact]
    public void Should_Report_Each_Changed_Field_With_What_It_Held_Before()
    {
        var proposal = new EmployeePersonalDataChange
        {
            Mobile = 66660000,
            NationalityId = 9,
            Gender = Gender.Female,
            BirthDate = new DateTime(1985, 6, 15)
        };

        var changes = PersonalDataChangeFields.Changes(proposal, PersonalDataChangeFields.Read(AnEmployee()))
            .ToDictionary(c => c.Field.Name);

        changes.Count.ShouldBe(4);
        changes[nameof(EmployeePersonalDataChange.Mobile)].Previous.ShouldBe("55550000");
        changes[nameof(EmployeePersonalDataChange.Mobile)].Proposed.ShouldBe("66660000");
        changes[nameof(EmployeePersonalDataChange.NationalityId)].Previous.ShouldBe("3");
        changes[nameof(EmployeePersonalDataChange.NationalityId)].Proposed.ShouldBe("9");
        changes[nameof(EmployeePersonalDataChange.Gender)].Previous.ShouldBe("Male");
        changes[nameof(EmployeePersonalDataChange.Gender)].Proposed.ShouldBe("Female");
        changes[nameof(EmployeePersonalDataChange.BirthDate)].Previous.ShouldBe("1980-03-02");
        changes[nameof(EmployeePersonalDataChange.BirthDate)].Proposed.ShouldBe("1985-06-15");
    }

    [Fact]
    public void Kept_Values_Should_Survive_Being_Stored_As_Json()
    {
        var employee = AnEmployee();
        employee.FirstNameAr = "أحمد";

        var kept = PersonalDataChangeFields.Deserialize(PersonalDataChangeFields.Serialize(employee));

        kept.ShouldBe(PersonalDataChangeFields.Read(employee));
        PersonalDataChangeFields.Deserialize(null).ShouldBeNull();
    }
}
