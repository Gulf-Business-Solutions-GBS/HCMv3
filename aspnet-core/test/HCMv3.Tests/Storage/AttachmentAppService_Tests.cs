using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.UI;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.Storage.Dto;
using HCMv3.SystemSettings;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Storage;

/// <summary>
/// Files attached to records, and who may see and change them.
///
/// <para>
/// Read the access tests first. Each one signs in as an ordinary user with no permissions and a linked
/// employee, because the tenant admin holds every permission and would pass any check. The owner may add
/// files only while their request is undecided; an approver may look but not change; a colleague sees
/// nothing; HR sees everything through the admin permission.
/// </para>
///
/// <para>
/// The storage tests then pin the legacy limits (type, size, count per record, total storage) and the one
/// thing this port adds: the bytes are on disk, and the database holds only the path.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class AttachmentAppService_Tests : AppTestBase
{
    private const string DocType = EmployeeDocumentAttachmentOwnerPolicy.Type;

    private readonly IAttachmentAppService _attachments;
    private readonly ITempFileCacheManager _tempFiles;

    public AttachmentAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _attachments = Resolve<IAttachmentAppService>();
        _tempFiles = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- seeding

    /// <summary>A user with no roles, hence no permissions, linked to a new employee.</summary>
    private (long userId, string userName, int employeeId) CreatePlainEmployeeUser(string prefix)
    {
        var userName = prefix + Guid.NewGuid().ToString("N")[..8];
        User user = null;
        UsingDbContext(context =>
        {
            user = new User
            {
                TenantId = TenantId,
                UserName = userName,
                Name = "Plain",
                Surname = "User",
                EmailAddress = userName + "@gbskw.com",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
            };
            user.SetNormalizedNames();
            context.Users.Add(user);
            context.SaveChanges();
        });

        return (user.Id, userName, CreateEmployee(userName, user.Id));
    }

    private int CreateEmployee(string code, long? userId)
    {
        Employee employee = null;
        UsingDbContext(context =>
        {
            var nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N")[..8],
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>A document row as the single-table workflow leaves it, with an optional request.</summary>
    private int CreateDocument(int employeeId, RequestStatus? status, long? creatorUserId = null, int? workflowRequestId = null)
    {
        EmployeeDocument document = null;
        UsingDbContext(context =>
        {
            var type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N")[..8],
                NameEn = "Passport",
                NameAr = "جواز"
            }).Entity;
            context.SaveChanges();

            document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DocumentTypeId = type.Id,
                DocumentNum = "P" + Guid.NewGuid().ToString("N")[..6],
                IssueDate = DateTime.Today.AddYears(-1),
                ExpiryDate = DateTime.Today.AddYears(4),
                WorkflowStatus = status,
                WorkflowRequestId = workflowRequestId,
                CreatorUserId = creatorUserId
            }).Entity;
        });

        return document.Id;
    }

    private int CreateRequestWithApprover(int requesterEmployeeId, int approverEmployeeId)
    {
        var requestId = 0;
        UsingDbContext(context =>
        {
            var request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = requesterEmployeeId,
                RequestType = "EmployeeDocument",
                RequestNumber = "DOC-" + Guid.NewGuid().ToString("N")[..6],
                RequestStatus = RequestStatus.Open
            }).Entity;
            context.SaveChanges();

            context.Set<WorkFlowApprover>().Add(new WorkFlowApprover
            {
                TenantId = TenantId,
                WorkflowRequestId = request.Id,
                ApproverId = approverEmployeeId
            });
            requestId = request.Id;
        });

        return requestId;
    }

    private void SetDocumentSettings(Action<ManageDocumentSettings> configure)
    {
        UsingDbContext(context =>
        {
            var settings = new ManageDocumentSettings { TenantId = TenantId };
            configure(settings);
            context.Set<ManageDocumentSettings>().Add(settings);
        });
    }

    private string Upload(string fileName, int size = 16)
    {
        var token = Guid.NewGuid().ToString("N");
        var bytes = Enumerable.Range(0, size).Select(i => (byte)(i % 251)).ToArray();
        _tempFiles.SetFile(token, new TempFileInfo(fileName, "application/octet-stream", bytes));

        return token;
    }

    private Task<AttachmentDto> Attach(string entityType, int entityId, string fileName = "scan.pdf", int size = 16) =>
        _attachments.AddAttachment(new AddAttachmentInput
        {
            EntityType = entityType,
            EntityId = entityId.ToString(),
            FileToken = Upload(fileName, size)
        });

    private Task<GetAttachmentsOutput> List(string entityType, int entityId) =>
        _attachments.GetAttachments(new AttachmentOwnerInput { EntityType = entityType, EntityId = entityId.ToString() });

    private AppAttachment LoadRow(int id)
    {
        AppAttachment row = null;
        UsingDbContext(context => row = context.Set<AppAttachment>().First(a => a.Id == id));

        return row;
    }

    // ---------------------------------------------------------------- access

    [Fact]
    public async Task Owner_Should_Attach_To_Their_Own_Open_Request()
    {
        var me = CreatePlainEmployeeUser("own");
        var docId = CreateDocument(me.employeeId, RequestStatus.Open);
        LoginAsTenant("Default", me.userName);

        var added = await Attach(DocType, docId);
        var list = await List(DocType, docId);

        list.CanEdit.ShouldBeTrue();
        list.Items.Single().Id.ShouldBe(added.Id);
        list.Items.Single().FileName.ShouldBe("scan.pdf");
    }

    [Theory]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    public async Task Owner_Should_Not_Change_Files_Once_The_Request_Is_Decided(RequestStatus status)
    {
        var me = CreatePlainEmployeeUser("dec");
        var docId = CreateDocument(me.employeeId, status);
        LoginAsTenant("Default", me.userName);

        (await List(DocType, docId)).CanEdit.ShouldBeFalse();
        await Should.ThrowAsync<AbpAuthorizationException>(() => Attach(DocType, docId));
    }

    [Fact]
    public async Task Owner_Should_Attach_To_A_Workflow_Off_Row_They_Created()
    {
        var me = CreatePlainEmployeeUser("wfo");
        var docId = CreateDocument(me.employeeId, status: null, creatorUserId: me.userId);
        LoginAsTenant("Default", me.userName);

        (await Attach(DocType, docId)).Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Owner_Should_Not_Change_Files_On_A_Row_HR_Entered()
    {
        var me = CreatePlainEmployeeUser("hre");
        var docId = CreateDocument(me.employeeId, status: null, creatorUserId: AbpSession.UserId);
        LoginAsTenant("Default", me.userName);

        await Should.ThrowAsync<AbpAuthorizationException>(() => Attach(DocType, docId));
    }

    [Fact]
    public async Task A_Colleague_Should_Not_See_Another_Employees_Files()
    {
        var owner = CreatePlainEmployeeUser("own");
        var colleague = CreatePlainEmployeeUser("col");
        var docId = CreateDocument(owner.employeeId, RequestStatus.Open);
        LoginAsTenant("Default", colleague.userName);

        await Should.ThrowAsync<AbpAuthorizationException>(() => List(DocType, docId));
    }

    [Fact]
    public async Task An_Approver_Should_See_But_Not_Change_The_Files()
    {
        var owner = CreatePlainEmployeeUser("own");
        var approver = CreatePlainEmployeeUser("apr");
        var requestId = CreateRequestWithApprover(owner.employeeId, approver.employeeId);
        var docId = CreateDocument(owner.employeeId, RequestStatus.Open, workflowRequestId: requestId);

        LoginAsTenant("Default", owner.userName);
        await Attach(DocType, docId);

        LoginAsTenant("Default", approver.userName);
        var list = await List(DocType, docId);

        list.Items.Count.ShouldBe(1);
        list.CanEdit.ShouldBeFalse();
        list.Items.Single().CanDelete.ShouldBeFalse();
        (await _attachments.GetAttachmentDownload(new EntityDto(list.Items.Single().Id))).FileToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task HR_With_The_Permission_Should_See_And_Change_Any_Files()
    {
        var owner = CreatePlainEmployeeUser("own");
        var docId = CreateDocument(owner.employeeId, RequestStatus.Approved);

        // Tenant admin: holds the Employee Documents permissions.
        var list = await List(DocType, docId);
        list.CanEdit.ShouldBeTrue();
        (await Attach(DocType, docId)).Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task An_Entity_Type_With_No_Policy_Should_Be_Refused()
    {
        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            _attachments.GetAttachments(new AttachmentOwnerInput { EntityType = "Payroll", EntityId = "1" }));
    }

    [Fact]
    public async Task A_Record_That_Does_Not_Exist_Should_Be_Refused()
    {
        await Should.ThrowAsync<AbpAuthorizationException>(() => List(DocType, 999999));
    }

    [Fact]
    public async Task Owner_Should_Attach_Proof_To_Their_Personal_Data_Change()
    {
        var me = CreatePlainEmployeeUser("pdc");
        EmployeePersonalDataChange change = null;
        UsingDbContext(context =>
        {
            change = context.Set<EmployeePersonalDataChange>().Add(new EmployeePersonalDataChange
            {
                TenantId = TenantId,
                EmployeeId = me.employeeId,
                WorkflowStatus = RequestStatus.Open
            }).Entity;
        });
        LoginAsTenant("Default", me.userName);

        var added = await Attach(EmployeePersonalDataChangeAttachmentOwnerPolicy.Type, change.Id, "civil-id.jpg");

        added.ContentType.ShouldBe("image/jpeg");
    }

    // ---------------------------------------------------------------- storage

    [Fact]
    public async Task Should_Keep_The_Bytes_On_Disk_And_Only_The_Path_In_The_Database()
    {
        var docId = CreateDocument(CreateEmployee("DSK", null), RequestStatus.Open);

        var added = await Attach(DocType, docId, "scan.pdf", size: 100);

        var row = LoadRow(added.Id);
        row.StoragePath.ShouldStartWith("tenant-" + TenantId + "/attachments/EmployeeDocument/");
        row.StoragePath.ShouldNotContain("scan");
        row.Size.ShouldBe(100);
        (await Resolve<IFileStore>().ReadOrNullAsync(row.StoragePath)).Length.ShouldBe(100);
    }

    [Fact]
    public async Task Download_Should_Hand_Back_The_Same_Bytes()
    {
        var docId = CreateDocument(CreateEmployee("DWN", null), RequestStatus.Open);
        var added = await Attach(DocType, docId, "scan.pdf", size: 40);

        var file = await _attachments.GetAttachmentDownload(new EntityDto(added.Id));

        file.FileName.ShouldBe("scan.pdf");
        file.FileType.ShouldBe("application/pdf");
        _tempFiles.GetFile(file.FileToken).Length.ShouldBe(40);
    }

    [Fact]
    public async Task Delete_Should_Remove_The_File_From_Disk()
    {
        var docId = CreateDocument(CreateEmployee("DEL", null), RequestStatus.Open);
        var added = await Attach(DocType, docId);
        var path = LoadRow(added.Id).StoragePath;

        await _attachments.DeleteAttachment(new EntityDto(added.Id));

        (await List(DocType, docId)).Items.ShouldBeEmpty();
        (await Resolve<IFileStore>().ReadOrNullAsync(path)).ShouldBeNull();
    }

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("page.html")]
    [InlineData("image.svg")]
    [InlineData("noextension")]
    public async Task Should_Refuse_Types_Outside_The_Default_List(string fileName)
    {
        var docId = CreateDocument(CreateEmployee("EXT", null), RequestStatus.Open);

        await Should.ThrowAsync<UserFriendlyException>(() => Attach(DocType, docId, fileName));
    }

    [Fact]
    public async Task Should_Honour_The_Tenants_Own_Extension_List()
    {
        SetDocumentSettings(s => s.AllowedFileExtensions = "pdf");
        var docId = CreateDocument(CreateEmployee("TXL", null), RequestStatus.Open);

        await Should.ThrowAsync<UserFriendlyException>(() => Attach(DocType, docId, "photo.jpg"));
        (await Attach(DocType, docId, "scan.PDF")).Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Refuse_A_File_Over_The_Size_Limit()
    {
        SetDocumentSettings(s => s.MaxFileSizePerAttachmentMB = 1);
        var docId = CreateDocument(CreateEmployee("BIG", null), RequestStatus.Open);

        await Should.ThrowAsync<UserFriendlyException>(() => Attach(DocType, docId, "big.pdf", size: 1024 * 1024 + 1));
    }

    [Fact]
    public async Task Should_Refuse_More_Files_Than_Allowed_Per_Record()
    {
        SetDocumentSettings(s => s.MaxAttachmentsPerTransaction = 1);
        var docId = CreateDocument(CreateEmployee("CNT", null), RequestStatus.Open);
        await Attach(DocType, docId);

        await Should.ThrowAsync<UserFriendlyException>(() => Attach(DocType, docId));
    }

    [Fact]
    public async Task Should_Refuse_When_The_Tenants_Storage_Is_Full()
    {
        SetDocumentSettings(s => s.MaxStorageSizeMB = 1);
        var docId = CreateDocument(CreateEmployee("FUL", null), RequestStatus.Open);
        await Attach(DocType, docId, "a.pdf", size: 600 * 1024);

        await Should.ThrowAsync<UserFriendlyException>(() => Attach(DocType, docId, "b.pdf", size: 600 * 1024));
    }

    [Fact]
    public async Task Should_Refuse_An_Expired_Or_Unknown_Upload_Token()
    {
        var docId = CreateDocument(CreateEmployee("TOK", null), RequestStatus.Open);

        await Should.ThrowAsync<UserFriendlyException>(() => _attachments.AddAttachment(new AddAttachmentInput
        {
            EntityType = DocType,
            EntityId = docId.ToString(),
            FileToken = Guid.NewGuid().ToString("N")
        }));
    }

    [Fact]
    public async Task Should_Number_Documents_In_Sequence()
    {
        var docId = CreateDocument(CreateEmployee("SEQ", null), RequestStatus.Open);

        var first = await Attach(DocType, docId);
        var second = await Attach(DocType, docId);

        second.DocNo.ShouldBe(first.DocNo + 1);
    }

    [Fact]
    public async Task Settings_Should_Fall_Back_To_Safe_Defaults()
    {
        var settings = await _attachments.GetAttachmentSettings();

        settings.MaxFileSizeMB.ShouldBe(AppAttachmentConsts.DefaultMaxFileSizeMB);
        settings.AllowedExtensions.ShouldContain(".pdf");
        settings.AllowedExtensions.ShouldNotContain(".exe");
    }
}
