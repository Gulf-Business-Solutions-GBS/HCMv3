using Abp.DynamicEntityProperties;
using Abp.UI.Inputs;
using HCMv3.Authorization.Users;
using HCMv3.CustomInputTypes;

namespace HCMv3.DynamicEntityProperties;

public class AppDynamicEntityPropertyDefinitionProvider : DynamicEntityPropertyDefinitionProvider
{
    public override void SetDynamicEntityProperties(IDynamicEntityPropertyDefinitionContext context)
    {
        context.Manager.AddAllowedInputType<SingleLineStringInputType>();
        context.Manager.AddAllowedInputType<ComboboxInputType>();
        context.Manager.AddAllowedInputType<CheckboxInputType>();
        context.Manager.AddAllowedInputType<MultiSelectComboboxInputType>();

        //Add entities here 
        context.Manager.AddEntity<User, long>();
    }
}

