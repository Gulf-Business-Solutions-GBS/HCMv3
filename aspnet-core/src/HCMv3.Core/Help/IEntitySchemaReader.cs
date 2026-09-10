using System.Threading.Tasks;

namespace HCMv3.Help;

/// <summary>
/// Reads how an entity is stored: its table, and the column behind each property.
///
/// <para>
/// Declared here and implemented in the EntityFrameworkCore project because the answer lives in the EF
/// model, and nothing outside that project may touch the DbContext. Help's technical tab
/// (docs/help/DESIGN.md §9) is the consumer; it must never be hand-written, since a table name typed into
/// documentation is wrong the first time someone renames a column.
/// </para>
/// </summary>
public interface IEntitySchemaReader
{
    /// <summary>
    /// The storage schema for an entity, by full CLR name
    /// (<c>HCMv3.PersonnelAdministration.Employee</c>), or null when no such type is mapped.
    /// </summary>
    Task<EntitySchema> FindAsync(string entityFullName);
}
