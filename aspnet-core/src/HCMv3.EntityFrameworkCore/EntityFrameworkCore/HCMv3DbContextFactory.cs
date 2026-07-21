using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using HCMv3.Configuration;
using HCMv3.Web;

namespace HCMv3.EntityFrameworkCore;

/* This class is needed to run "dotnet ef ..." commands from command line on development. Not used anywhere else */
public class HCMv3DbContextFactory : IDesignTimeDbContextFactory<HCMv3DbContext>
{
    public HCMv3DbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<HCMv3DbContext>();

        /*
         You can provide an environmentName parameter to the AppConfigurations.Get method. 
         In this case, AppConfigurations will try to read appsettings.{environmentName}.json.
         Use Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") method or from string[] args to get environment if necessary.
         https://docs.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli#args
         */
        var configuration = AppConfigurations.Get(
            WebContentDirectoryFinder.CalculateContentRootFolder(),
            addUserSecrets: true
        );

        HCMv3DbContextConfigurer.Configure(builder, configuration.GetConnectionString(HCMv3Consts.ConnectionStringName));

        return new HCMv3DbContext(builder.Options);
    }
}

