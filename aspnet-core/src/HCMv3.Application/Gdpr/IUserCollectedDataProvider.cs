using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using HCMv3.Dto;

namespace HCMv3.Gdpr;

public interface IUserCollectedDataProvider
{
    Task<List<FileDto>> GetFiles(UserIdentifier user);
}
