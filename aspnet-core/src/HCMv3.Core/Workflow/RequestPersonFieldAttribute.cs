using System;

namespace HCMv3.Workflow;

/// <summary>
/// Marks a plain employee id on a transaction entity as a person a "Person on Request" step can route to.
///
/// <para>
/// Only needed where the id has no <c>Employee</c> navigation beside it -- a leave's delegates are bare ids, a
/// document withdrawal's guarantor is not. A property with an <c>Employee</c> navigation is offered without it.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RequestPersonFieldAttribute : Attribute
{
}
