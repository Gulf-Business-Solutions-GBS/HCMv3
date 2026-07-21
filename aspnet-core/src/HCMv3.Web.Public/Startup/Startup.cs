using System;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.Mvc.Extensions;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.Castle.Logging.Log4Net;
using Castle.Facilities.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HCMv3.Configuration;
using HCMv3.Identity;
using HCMv3.Web.HealthCheck;

namespace HCMv3.Web.Public.Startup;

public class Startup
{
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IConfigurationRoot _appConfiguration;

    public Startup(IWebHostEnvironment env)
    {
        _hostingEnvironment = env;
        _appConfiguration = env.GetAppConfiguration();
    }

    public IServiceProvider ConfigureServices(IServiceCollection services)
    {
        //MVC
        var mvcBuilder = services.AddControllersWithViews(options =>
        {
            // Anti-forgery validation can be disabled via appsettings.json for easier API testing
            // In production, set DisableAntiforgeryTokenValidation to false for security
            var disableAntiforgery = _appConfiguration.GetValue<bool>("App:DisableAntiforgeryTokenValidation");
            if (!disableAntiforgery)
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            }
        });

#if DEBUG
        mvcBuilder.AddRazorRuntimeCompilation();
#endif

        if (bool.Parse(_appConfiguration["KestrelServer:IsEnabled"]))
        {
            ConfigureKestrel(services);
        }

        IdentityRegistrar.Register(services);
        services.AddSignalR();

        ConfigureHealthChecks(services);

        //Configure Abp and Dependency Injection
        return services.AddAbp<HCMv3WebFrontEndModule>(options =>
        {
            //Configure Log4Net logging
            options.IocManager.IocContainer.AddFacility<LoggingFacility>(
                f => f.UseAbpLog4Net().WithConfig(_hostingEnvironment.IsDevelopment()
                    ? "log4net.config"
                    : "log4net.Production.config")
            );
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        app.UseAbp(); //Initializes ABP framework.

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseStatusCodePagesWithRedirects("~/Error?statusCode={0}");
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<AbpCommonHub>("/signalr");

            endpoints.MapControllerRoute("defaultWithArea", "{area}/{controller=Home}/{action=Index}/{id?}");
            endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

            app.ApplicationServices.GetRequiredService<IAbpAspNetCoreConfiguration>().EndpointConfiguration.ConfigureAllEndpoints(endpoints);
        });

        app.UseHealthChecks("/health", new HealthCheckOptions()
        {
            Predicate = _ => true
        });
    }

    private void ConfigureKestrel(IServiceCollection services)
    {
        services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.Listen(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 443),
                listenOptions =>
                {
                    var certPassword = _appConfiguration.GetValue<string>("Kestrel:Certificates:Default:Password");
                    var certPath = _appConfiguration.GetValue<string>("Kestrel:Certificates:Default:Path");
                    var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, certPassword);
                    listenOptions.UseHttps(new HttpsConnectionAdapterOptions()
                    {
                        ServerCertificate = cert
                    });
                });
        });
    }

    private void ConfigureHealthChecks(IServiceCollection services)
    {
        services.AddAbpZeroHealthCheck();
    }
}

