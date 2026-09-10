using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Help.Dto;

namespace HCMv3.Help;

/// <summary>
/// Technical facts about the fields in-app help describes -- the table and column behind each one
/// (docs/help/DESIGN.md §9). Generated from the EF model on request, never authored.
/// </summary>
public interface IHelpMetadataAppService : IApplicationService
{
    /// <summary>
    /// The storage schema of one application entity, by full CLR name
    /// (<c>HCMv3.PersonnelAdministration.Employee</c>).
    /// </summary>
    Task<EntitySchemaDto> GetEntitySchema(string entityFullName);
}
