namespace HCMv3.Integrations;

/// <summary>
/// How the Integration Center's dynamic configuration form renders one field.
///
/// The form is driven entirely by field metadata returned from the server, so adding a new integration
/// is a backend-only change -- no new Angular component, route or modal.
/// </summary>
public enum IntegrationFieldType
{
    Text = 0,
    Password = 1,
    Url = 2,
    Number = 3,
    Boolean = 4,
    Email = 5
}
