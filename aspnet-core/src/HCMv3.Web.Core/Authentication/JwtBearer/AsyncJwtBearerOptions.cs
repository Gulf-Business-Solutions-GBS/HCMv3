using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace HCMv3.Web.Authentication.JwtBearer;

public class AsyncJwtBearerOptions : JwtBearerOptions
{
    public readonly List<IAsyncSecurityTokenValidator> AsyncSecurityTokenValidators;

    private readonly HCMv3AsyncJwtSecurityTokenHandler _defaultAsyncHandler = new HCMv3AsyncJwtSecurityTokenHandler();

    public AsyncJwtBearerOptions()
    {
        AsyncSecurityTokenValidators = new List<IAsyncSecurityTokenValidator>() { _defaultAsyncHandler };
    }
}


