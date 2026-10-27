using System.Collections.Generic;

namespace HCMv3.DynamicEntityProperties;

public interface IDynamicEntityPropertyDefinitionAppService
{
    List<string> GetAllAllowedInputTypeNames();

    List<string> GetAllEntities();
}

