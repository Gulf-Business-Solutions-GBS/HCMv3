using Abp.Dependency;

namespace HCMv3.GoLiveSetup;

/// <summary>
/// How many employees go through an engine at once.
///
/// <para>Both engines hold everything they write for a call in one unit of work until the call ends, so the
/// work they keep in memory grows with every employee and each one is slower than the last: 186 employees
/// went from 3.5 to 9 seconds each. Calling them a batch at a time, each batch saved on its own, keeps every
/// employee as quick as the first -- and a failure costs its batch, not the whole run.</para>
/// </summary>
public class GoLiveEngineBatches : ISingletonDependency
{
    public int Size { get; set; } = 10;
}
