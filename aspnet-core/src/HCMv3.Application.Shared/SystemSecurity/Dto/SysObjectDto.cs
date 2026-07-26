using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class SysObjectDto : EntityDto<int>
{
    public string Name { get; set; }
    public string FullName { get; set; }
    public bool IsPayrollSensitive { get; set; }
    public bool RespectPermission { get; set; }
    public bool MustRetroRun { get; set; }
    public bool IsTimeSensitive { get; set; }
}
