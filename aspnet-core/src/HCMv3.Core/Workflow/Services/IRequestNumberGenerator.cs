using System.Threading.Tasks;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Issues the number a request is known by, drawn from one counter shared by every request type in the
/// tenant.
///
/// <para>
/// Replaces the per-type "read the highest number and add one" that each caller used to do for itself. That
/// gave every type its own sequence -- exit permits counted 1, 2, 3 and leave started again at 1 -- and it
/// was not safe under concurrent submission either: two employees pressing Submit in the same moment both
/// read the same highest number and both got it, with nothing in the schema to refuse the second.
/// </para>
///
/// <para>
/// Declared in Core because the domain services that raise requests live here; implemented in the
/// EntityFrameworkCore project, because claiming a number atomically is a statement the database has to
/// run, and reaching a <c>DbContext</c> is that project's job alone.
/// </para>
/// </summary>
public interface IRequestNumberGenerator
{
    /// <summary>
    /// Claims the next number and formats it, e.g. <c>EXP-000042</c>.
    /// </summary>
    /// <param name="prefix">
    /// A short tag naming the type, kept purely so a number can be read at a glance. It takes no part in
    /// making the number unique -- the digits after it are unique on their own, across every type -- so two
    /// types may not share a number even if somebody gives them the same prefix. Blank falls back to
    /// <c>REQ</c>.
    /// </param>
    Task<string> NextAsync(string prefix);
}
