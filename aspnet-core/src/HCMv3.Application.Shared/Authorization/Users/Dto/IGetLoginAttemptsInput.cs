using Abp.Application.Services.Dto;

namespace HCMv3.Authorization.Users.Dto;

public interface IGetLoginAttemptsInput : ISortedResultRequest
{
    string Filter { get; set; }
}

