using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers the Workflow Setup grid -- the wrapper that configures a transaction from one row instead of
/// four screens.
///
/// <para>
/// Most of these are about what it REFUSES. The grid has three approver columns and a handful of switches,
/// while the engine underneath supports parallel approvers, per-group chains, signatory steps and shared
/// chains. Saving a flat row over any of those would delete a working configuration, and the person doing
/// it would have no way to know until requests started going to the wrong people.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowSetup_Tests : AppTestBase
{
    private readonly IWorkflowSetupAppService _setup;

    public WorkflowSetup_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _setup = Resolve<IWorkflowSetupAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string ShortCode() => Guid.NewGuid().ToString("N").Substring(0, 6);

    private const string Subject = "EmployeeDelegation";

    private async Task<WorkflowSetupRowDto> RowAsync(string code = Subject)
    {
        var grid = await _setup.GetGrid();

        return grid.Rows.Single(r => r.Code == code);
    }

    /// <summary>A role with one holder whose assignment covers today.</summary>
    private int CreateRoleWithHolder()
    {
        var employeeId = CreateEmployee();
        var roleId = 0;

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = ShortCode(),
                NameEn = "Approvers",
                NameAr = "Approvers"
            }).Entity;

            context.SaveChanges();
            roleId = role.Id;

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });
        });

        return roleId;
    }

    private int CreateEmployee()
    {
        var code = ShortCode();
        var employeeId = 0;

        UsingDbContext(context =>
        {
            var nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = ShortCode(),
                NameEn = "N",
                NameAr = "N"
            }).Entity;

            context.SaveChanges();

            var employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "T",
                LastNameAr = "T",
                FullNameEn = code + " T",
                FullNameAr = "T T",
                JoiningDate = new DateTime(2020, 1, 1),
                Status = EmployeeStatus.Active,
                NationalityId = nationality.Id
            }).Entity;

            context.SaveChanges();
            employeeId = employee.Id;
        });

        return employeeId;
    }

    /// <summary>Points the subject transaction at a hand-made chain, as the detailed screens would.</summary>
    private int CreateChainFor(string code, Action<RequestMaster, List<RequestDetail>> configure)
    {
        var masterId = 0;

        UsingDbContext(context =>
        {
            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "HAND-" + ShortCode(),
                NameEn = "Hand made",
                NameAr = "Hand made",
                WorkflowSteps = 1
            }).Entity;

            context.SaveChanges();
            masterId = master.Id;

            var details = new List<RequestDetail>();
            configure(master, details);

            foreach (var detail in details)
            {
                detail.TenantId = TenantId;
                detail.RequestMasterId = master.Id;
                context.Set<RequestDetail>().Add(detail);
            }

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == code);
            type.DefaultRequestMasterId = master.Id;
            type.IsActive = true;
        });

        return masterId;
    }

    // ------------------------------------------------------------------ reading

    [Fact]
    public async Task Should_Show_Every_Catalogued_Transaction_As_A_Row()
    {
        var grid = await _setup.GetGrid();

        grid.Rows.ShouldNotBeEmpty();
        grid.Rows.ShouldContain(r => r.Code == Subject);
        grid.Rows.ShouldContain(r => r.Code == "EmployeeAbsence");
    }

    /// <summary>A freshly seeded catalog is entirely off, and every row is editable because nothing is set.</summary>
    [Fact]
    public async Task Should_Start_With_Everything_Off_And_Nothing_Locked()
    {
        var row = await RowAsync();

        row.IsWorkflowEnabled.ShouldBeFalse();
        row.IsEssEnabled.ShouldBeFalse();
        row.Steps.ShouldBeEmpty();
        row.IsLocked.ShouldBeFalse();
    }

    // ------------------------------------------------------------------ writing

    [Fact]
    public async Task Should_Build_A_One_Step_Manager_Chain_From_A_Row()
    {
        await _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsEssEnabled = true,
            IsActive = true,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.PersonManager }
            }
        });

        var row = await RowAsync();

        row.IsWorkflowEnabled.ShouldBeTrue();
        row.IsEssEnabled.ShouldBeTrue();
        row.Steps.Count.ShouldBe(1);
        row.Steps[0].ApproverPerson.ShouldBe(WorkflowApproverPersonType.PersonManager);

        // The catalog must actually point at the chain, or nothing routes.
        UsingDbContext(context =>
        {
            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == Subject);
            type.DefaultRequestMasterId.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Should_Build_A_Three_Step_Chain_In_Order()
    {
        var roleId = CreateRoleWithHolder();

        await _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsActive = true,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.PersonManager },
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.ManagerLevel2 },
                new WorkflowSetupStepDto
                {
                    ApproverPerson = WorkflowApproverPersonType.Person,
                    WorkflowRoleId = roleId,
                    ApprovalRequire = ApprovalRequire.Single
                }
            }
        });

        var row = await RowAsync();

        row.Steps.Select(s => s.ApproverPerson).ShouldBe(new[]
        {
            WorkflowApproverPersonType.PersonManager,
            WorkflowApproverPersonType.ManagerLevel2,
            WorkflowApproverPersonType.Person
        });

        row.Steps[2].WorkflowRoleId.ShouldBe(roleId);
    }

    /// <summary>
    /// Saving again is how every edit arrives, so the second save must leave one chain rather than two
    /// sets of steps on top of each other.
    /// </summary>
    [Fact]
    public async Task Should_Replace_The_Steps_On_A_Second_Save_Rather_Than_Add_To_Them()
    {
        var input = new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsActive = true,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.PersonManager },
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.ManagerLevel2 }
            }
        };

        await _setup.SaveRow(input);

        input.Steps = new List<WorkflowSetupStepDto>
        {
            new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.PersonManager }
        };

        await _setup.SaveRow(input);

        var row = await RowAsync();

        row.Steps.Count.ShouldBe(1);
    }

    /// <summary>
    /// The creator row: ApproverPerson None carrying a Person scope. It says who may RAISE the request and
    /// is not one of the approver cells, so it must not show up as a fourth approver.
    /// </summary>
    [Fact]
    public async Task Should_Write_Who_May_Raise_It_Without_Counting_As_An_Approver()
    {
        await _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsActive = true,
            RaisedBy = WorkflowPersonScope.SelfSubordinates,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.PersonManager }
            }
        });

        var row = await RowAsync();

        row.RaisedBy.ShouldBe(WorkflowPersonScope.SelfSubordinates);
        row.Steps.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Refuse_A_Person_Step_With_No_Role()
    {
        var act = () => _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsActive = true,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.Person }
            }
        });

        await Should.ThrowAsync<UserFriendlyException>(act);
    }

    /// <summary>
    /// Routing with nothing to route to fails at the first real request, and nobody connects that failure
    /// back to this screen. Refused here instead.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_To_Switch_Routing_On_With_No_Approvers()
    {
        var act = () => _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsActive = true,
            Steps = new List<WorkflowSetupStepDto>()
        });

        await Should.ThrowAsync<UserFriendlyException>(act);
    }

    // ------------------------------------------------------------------ the locks

    /// <summary>
    /// Two transactions sharing one chain. Rewriting it for one silently changes the other, so the grid
    /// will not.
    /// </summary>
    [Fact]
    public async Task Should_Lock_A_Row_Whose_Chain_Is_Shared_With_Another_Transaction()
    {
        var masterId = CreateChainFor(Subject, (master, details) =>
        {
            details.Add(new RequestDetail
            {
                Step = 1,
                ApproverPerson = WorkflowApproverPersonType.PersonManager,
                ApprovalRequire = ApprovalRequire.Single
            });
        });

        UsingDbContext(context =>
        {
            var other = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAsset");
            other.DefaultRequestMasterId = masterId;
        });

        var row = await RowAsync();

        row.IsLocked.ShouldBeTrue();
        row.LockedReason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Lock_A_Row_With_More_Approvers_Than_The_Grid_Shows()
    {
        CreateChainFor(Subject, (master, details) =>
        {
            for (var step = 1; step <= 4; step++)
            {
                details.Add(new RequestDetail
                {
                    Step = step,
                    ApproverPerson = WorkflowApproverPersonType.PersonManager,
                    ApprovalRequire = ApprovalRequire.Single
                });
            }
        });

        var row = await RowAsync();

        row.IsLocked.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Lock_A_Row_Whose_Step_Carries_Settings_The_Grid_Cannot_Show()
    {
        CreateChainFor(Subject, (master, details) =>
        {
            details.Add(new RequestDetail
            {
                Step = 1,
                ApproverPerson = WorkflowApproverPersonType.PersonManager,
                ApprovalRequire = ApprovalRequire.Single,

                // No column for this anywhere in the grid.
                SignatoryStep = true
            });
        });

        var row = await RowAsync();

        row.IsLocked.ShouldBeTrue();
    }

    /// <summary>
    /// The lock has to hold on the way in as well as on the way out. A browser that never rendered the
    /// lock, or one left open from before the chain became shared, must not be able to save over it.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_To_Save_A_Locked_Row()
    {
        CreateChainFor(Subject, (master, details) =>
        {
            details.Add(new RequestDetail
            {
                Step = 1,
                ApproverPerson = WorkflowApproverPersonType.PersonManager,
                ApprovalRequire = ApprovalRequire.Single,
                SignatoryStep = true
            });
        });

        var act = () => _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsActive = true,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.PersonManager }
            }
        });

        await Should.ThrowAsync<UserFriendlyException>(act);
    }

    /// <summary>
    /// A chain the grid built is its own to rewrite, even though it sets escalation on its steps. Without
    /// this, every row would lock itself the moment escalation was used once.
    /// </summary>
    [Fact]
    public async Task Should_Not_Lock_A_Chain_The_Grid_Built_Itself()
    {
        await _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsActive = true,
            EscalateAfterDays = 3,
            NotifyByEmail = true,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.PersonManager }
            }
        });

        var row = await RowAsync();

        row.IsLocked.ShouldBeFalse();
        row.EscalateAfterDays.ShouldBe(3);
        row.NotifyByEmail.ShouldBeTrue();
    }

    // ------------------------------------------------------------------ warnings

    [Fact]
    public async Task Should_Warn_When_A_Chosen_Role_Has_Nobody_In_It()
    {
        var emptyRoleId = 0;

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = ShortCode(),
                NameEn = "Nobody",
                NameAr = "Nobody"
            }).Entity;

            context.SaveChanges();
            emptyRoleId = role.Id;
        });

        await _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsActive = true,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto
                {
                    ApproverPerson = WorkflowApproverPersonType.Person,
                    WorkflowRoleId = emptyRoleId,
                    ApprovalRequire = ApprovalRequire.Single
                }
            }
        });

        var row = await RowAsync();

        row.Warnings.ShouldNotBeEmpty();
    }

    // ------------------------------------------------------------------ roles

    [Fact]
    public async Task Should_Create_A_Role_With_Its_Members_From_The_Grid()
    {
        var employeeId = CreateEmployee();

        var role = await _setup.CreateRole(new CreateWorkflowSetupRoleInput
        {
            Code = ShortCode(),
            NameEn = "Finance",
            NameAr = "Finance",
            EmployeeIds = new List<int> { employeeId }
        });

        role.Id.ShouldBeGreaterThan(0);
        role.CurrentHolderCount.ShouldBe(1);

        var members = await _setup.GetRoleMembers(role.Id);

        members.Count.ShouldBe(1);
        members[0].EmployeeId.ShouldBe(employeeId);
    }

    /// <summary>
    /// The new role has to be usable straight away -- a role created to finish a chain that does not
    /// resolve until tomorrow would look broken on the first test request.
    /// </summary>
    [Fact]
    public async Task Should_Make_A_New_Role_Resolve_Today()
    {
        var employeeId = CreateEmployee();

        var role = await _setup.CreateRole(new CreateWorkflowSetupRoleInput
        {
            Code = ShortCode(),
            NameEn = "Finance",
            NameAr = "Finance",
            EmployeeIds = new List<int> { employeeId }
        });

        var grid = await _setup.GetGrid();

        grid.Roles.Single(r => r.Id == role.Id).CurrentHolderCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Refuse_A_Duplicate_Role_Code()
    {
        var code = ShortCode();

        await _setup.CreateRole(new CreateWorkflowSetupRoleInput
        {
            Code = code,
            NameEn = "Finance",
            NameAr = "Finance"
        });

        var act = () => _setup.CreateRole(new CreateWorkflowSetupRoleInput
        {
            Code = code,
            NameEn = "Finance again",
            NameAr = "Finance again"
        });

        await Should.ThrowAsync<UserFriendlyException>(act);
    }

    // ------------------------------------------------------------------ the default chain

    private async Task DefineDefaultChainAsync(params WorkflowApproverPersonType[] approvers)
    {
        await _setup.SaveDefaultChain(new SaveWorkflowSetupDefaultChainInput
        {
            Steps = approvers.Select(a => new WorkflowSetupStepDto { ApproverPerson = a }).ToList()
        });
    }

    [Fact]
    public async Task Should_Define_A_Three_Step_Default_Chain()
    {
        await DefineDefaultChainAsync(
            WorkflowApproverPersonType.PersonManager,
            WorkflowApproverPersonType.ManagerLevel2,
            WorkflowApproverPersonType.ManagerLevel3);

        var grid = await _setup.GetGrid();

        grid.DefaultChainSteps.Count.ShouldBe(3);
        grid.DefaultChainSteps[0].ApproverPerson.ShouldBe(WorkflowApproverPersonType.PersonManager);
        grid.DefaultChainSteps[2].ApproverPerson.ShouldBe(WorkflowApproverPersonType.ManagerLevel3);
    }

    [Fact]
    public async Task Should_Point_A_Transaction_At_The_Default_Chain()
    {
        await DefineDefaultChainAsync(WorkflowApproverPersonType.PersonManager);

        await _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            UseDefaultChain = true,
            IsWorkflowEnabled = true,
            IsActive = true
        });

        var row = await RowAsync();

        row.UsesDefaultChain.ShouldBeTrue();

        // The row shows the default's approvers, so the grid still answers "who approves this".
        row.Steps.Count.ShouldBe(1);
        row.Steps[0].ApproverPerson.ShouldBe(WorkflowApproverPersonType.PersonManager);
    }

    /// <summary>
    /// The whole point of a default: edit once, every follower changes. Anything else would be a template,
    /// which is a different and much less useful thing.
    /// </summary>
    [Fact]
    public async Task Should_Change_Every_Follower_When_The_Default_Changes()
    {
        await DefineDefaultChainAsync(WorkflowApproverPersonType.PersonManager);

        foreach (var code in new[] { Subject, "EmployeeAsset" })
        {
            await _setup.SaveRow(new SaveWorkflowSetupRowInput
            {
                Code = code,
                UseDefaultChain = true,
                IsWorkflowEnabled = true,
                IsActive = true
            });
        }

        await DefineDefaultChainAsync(
            WorkflowApproverPersonType.PersonManager,
            WorkflowApproverPersonType.ManagerLevel2);

        var grid = await _setup.GetGrid();

        grid.DefaultChainUsedByCount.ShouldBe(2);

        foreach (var code in new[] { Subject, "EmployeeAsset" })
        {
            grid.Rows.Single(r => r.Code == code).Steps.Count.ShouldBe(2);
        }
    }

    /// <summary>
    /// A shared chain normally locks a row, because rewriting it would change the other transactions using
    /// it. The default is shared deliberately, so that reason does not apply -- without this exemption
    /// every row would lock itself the moment a second transaction followed the default.
    /// </summary>
    [Fact]
    public async Task Should_Not_Lock_Rows_Merely_For_Sharing_The_Default_Chain()
    {
        await DefineDefaultChainAsync(WorkflowApproverPersonType.PersonManager);

        foreach (var code in new[] { Subject, "EmployeeAsset" })
        {
            await _setup.SaveRow(new SaveWorkflowSetupRowInput
            {
                Code = code,
                UseDefaultChain = true,
                IsWorkflowEnabled = true,
                IsActive = true
            });
        }

        var grid = await _setup.GetGrid();

        grid.Rows.Single(r => r.Code == Subject).IsLocked.ShouldBeFalse();
        grid.Rows.Single(r => r.Code == "EmployeeAsset").IsLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Refuse_To_Follow_A_Default_Chain_That_Does_Not_Exist_Yet()
    {
        var act = () => _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            UseDefaultChain = true,
            IsWorkflowEnabled = true,
            IsActive = true
        });

        await Should.ThrowAsync<UserFriendlyException>(act);
    }

    /// <summary>
    /// Emptying a default that transactions still follow would leave them routing to nobody, which fails
    /// at the first request rather than at the moment somebody caused it.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_To_Empty_A_Default_Chain_Still_In_Use()
    {
        await DefineDefaultChainAsync(WorkflowApproverPersonType.PersonManager);

        await _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            UseDefaultChain = true,
            IsWorkflowEnabled = true,
            IsActive = true
        });

        var act = () => _setup.SaveDefaultChain(new SaveWorkflowSetupDefaultChainInput
        {
            Steps = new List<WorkflowSetupStepDto>()
        });

        await Should.ThrowAsync<UserFriendlyException>(act);
    }

    /// <summary>Coming off the default gives the transaction its own chain, leaving the default alone.</summary>
    [Fact]
    public async Task Should_Give_A_Transaction_Its_Own_Chain_When_It_Stops_Following_The_Default()
    {
        await DefineDefaultChainAsync(WorkflowApproverPersonType.PersonManager);

        await _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            UseDefaultChain = true,
            IsWorkflowEnabled = true,
            IsActive = true
        });

        await _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            UseDefaultChain = false,
            IsWorkflowEnabled = true,
            IsActive = true,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.ManagerLevel3 }
            }
        });

        var grid = await _setup.GetGrid();
        var row = grid.Rows.Single(r => r.Code == Subject);

        row.UsesDefaultChain.ShouldBeFalse();
        row.Steps[0].ApproverPerson.ShouldBe(WorkflowApproverPersonType.ManagerLevel3);

        // The default itself is untouched by one follower leaving.
        grid.DefaultChainSteps[0].ApproverPerson.ShouldBe(WorkflowApproverPersonType.PersonManager);
    }

    // ------------------------------------------------------------------ approver ordering

    /// <summary>
    /// A hole in the middle of a route is a mistake, not shorthand. Filtering the blank out would close it
    /// silently and save the approvers one position earlier than they were typed, so the screen and the
    /// saved route would disagree with nothing to say why.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_A_Route_With_A_Gap_Between_Approvers()
    {
        var act = () => _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsActive = true,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.PersonManager },
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.None },
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.ManagerLevel3 }
            }
        });

        await Should.ThrowAsync<UserFriendlyException>(act);
    }

    /// <summary>
    /// The common case, and it must stay legal: most routes are one or two deep, so trailing blanks are
    /// the normal shape rather than an incomplete one.
    /// </summary>
    [Fact]
    public async Task Should_Allow_Trailing_Blank_Approvers()
    {
        await _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsActive = true,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.PersonManager },
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.ManagerLevel2 },
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.None }
            }
        });

        var row = await RowAsync();

        row.Steps.Count.ShouldBe(2);
    }

    /// <summary>A blank first slot with anything after it is the same mistake, caught at approver 2.</summary>
    [Fact]
    public async Task Should_Refuse_A_Route_That_Starts_With_A_Blank()
    {
        var act = () => _setup.SaveRow(new SaveWorkflowSetupRowInput
        {
            Code = Subject,
            IsWorkflowEnabled = true,
            IsActive = true,
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.None },
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.PersonManager }
            }
        });

        await Should.ThrowAsync<UserFriendlyException>(act);
    }

    /// <summary>The default route has the same three slots, so it gets the same rule.</summary>
    [Fact]
    public async Task Should_Refuse_A_Gap_In_The_Default_Route_Too()
    {
        var act = () => _setup.SaveDefaultChain(new SaveWorkflowSetupDefaultChainInput
        {
            Steps = new List<WorkflowSetupStepDto>
            {
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.PersonManager },
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.None },
                new WorkflowSetupStepDto { ApproverPerson = WorkflowApproverPersonType.ManagerLevel2 }
            }
        });

        await Should.ThrowAsync<UserFriendlyException>(act);
    }
}
