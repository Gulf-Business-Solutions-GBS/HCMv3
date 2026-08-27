using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.Mvc.Antiforgery;
using Abp.AspNetCore.Mvc.Extensions;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.AspNetZeroCore.Web.Authentication.JwtBearer;
using Abp.Castle.Logging.Log4Net;
using Abp.Extensions;
using Abp.Hangfire;
using Abp.PlugIns;
using Castle.Facilities.Logging;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HCMv3.Authorization;
using HCMv3.Configuration;
using HCMv3.EntityFrameworkCore;
using HCMv3.Identity;
using HCMv3.Web.Chat.SignalR;
using HCMv3.Web.Common;
using HCMv3.Web.Swagger;
using Stripe;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using GraphQL.Server.Ui.Playground;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using HCMv3.Configure;
using HCMv3.Schemas;
using HCMv3.Web.HealthCheck;
using Owl.reCAPTCHA;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using HCMv3.Web.MultiTenancy;
using Abp.HtmlSanitizer;
using Microsoft.AspNetCore.Authentication.Cookies;
using HCMv3.Web.Authentication.PasswordlessLogin;
using HCMv3.Web.OpenIddict;
using Abp.AspNetCore.OpenIddict;
using Microsoft.OpenApi;
using HCMv3.Authorization.QrLogin;
using DevExpress.AspNetCore;
using DevExpress.AspNetCore.Reporting;
using DevExpress.DataAccess.Web;
using DevExpress.DataAccess.Wizard.Services;
using DevExpress.XtraReports.Web.Extensions;
using HCMv3.Web.Reporting;

namespace HCMv3.Web.Startup;

public class Startup
{
    private const string DefaultCorsPolicyName = "localhost";

    private readonly IConfigurationRoot _appConfiguration;
    private readonly IWebHostEnvironment _hostingEnvironment;

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
                options.Filters.Add(new AbpAutoValidateAntiforgeryTokenAttribute());
            }
            options.AddAbpHtmlSanitizer();
        });
#if DEBUG
        mvcBuilder.AddRazorRuntimeCompilation();
#endif

        services.AddSignalR();

        ConfigureDevExpressReporting(services);

        // Enables IHttpClientFactory (used by HCMv3.TimeManagement.Services.TimeAttendanceIntegrationService
        // to call the vendor time-attendance device API) throughout the app, per the standard
        // ASP.NET Core pattern (never `new HttpClient()` directly).
        services.AddHttpClient();

        //Configure CORS for angular2 UI
        services.AddCors(options =>
        {
            options.AddPolicy(DefaultCorsPolicyName, builder =>
            {
                //App:CorsOrigins in appsettings.json can contain more than one address with splitted by comma.
                builder
                    .WithOrigins(
                        // App:CorsOrigins in appsettings.json can contain more than one address separated by comma.
                        _appConfiguration["App:CorsOrigins"]
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                    )
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        if (bool.Parse(_appConfiguration["KestrelServer:IsEnabled"]))
        {
            ConfigureKestrel(services);
        }

        IdentityRegistrar.Register(services);
        AuthConfigurer.Configure(services, _appConfiguration);

        if (bool.Parse(_appConfiguration["OpenIddict:IsEnabled"]))
        {
            OpenIddictRegistrar.Register(services, _appConfiguration);
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme,
                options => { options.LoginPath = "/Ui/Login"; });
        }

        services.Configure<SecurityStampValidatorOptions>(opts =>
        {
            opts.OnRefreshingPrincipal = SecurityStampValidatorCallback.UpdatePrincipal;
            opts.ValidationInterval = TimeSpan.Zero;
        });

        if (WebConsts.SwaggerUiEnabled)
        {
            //Swagger - Enable this line and the related lines in Configure method to enable swagger UI
            ConfigureSwagger(services);
        }

        services.AddPasswordlessLoginRateLimit();

        //Recaptcha
        services.AddreCAPTCHAV3(x =>
        {
            x.SiteKey = _appConfiguration["Recaptcha:SiteKey"];
            x.SiteSecret = _appConfiguration["Recaptcha:SecretKey"];
        });

        if (WebConsts.HangfireDashboardEnabled)
        {
            //Hangfire(Enable to use Hangfire instead of default job manager)
            services.AddHangfire(config =>
            {
                config.UseSqlServerStorage(_appConfiguration.GetConnectionString("Default"));
            });

            services.AddHangfireServer();
        }

        if (WebConsts.GraphQL.Enabled)
        {
            services.AddAndConfigureGraphQL();
        }

        ConfigureHealthChecks(services);

        //Configure Abp and Dependency Injection
        return services.AddAbp<HCMv3WebHostModule>(options =>
        {
            //Configure Log4Net logging
            options.IocManager.IocContainer.AddFacility<LoggingFacility>(
                f => f.UseAbpLog4Net().WithConfig(_hostingEnvironment.IsDevelopment()
                    ? "log4net.config"
                    : "log4net.Production.config")
            );

            options.PlugInSources.AddFolder(Path.Combine(_hostingEnvironment.WebRootPath, "Plugins"),
                SearchOption.AllDirectories);
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        //Initializes ABP framework.
        app.UseAbp(options =>
        {
            options.UseAbpRequestLocalization = false; //used below: UseAbpRequestLocalization
        });

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseHsts();
        }
        else
        {
            app.UseStatusCodePagesWithRedirects("~/Error?statusCode={0}");
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();

        // DevExpress reporting: serves the client scripts/styles and the report backends. Must come
        // before UseRouting so the DevExpress resource handlers are in the pipeline.
        app.UseDevExpressControls();
        ReportStorageWebExtension.RegisterExtensionGlobal(
            app.ApplicationServices.GetRequiredService<ReportStorageWebExtension>());

#pragma warning disable CS0162
        if (HCMv3Consts.PreventNotExistingTenantSubdomains)
        {
            app.UseMiddleware<DomainTenantCheckMiddleware>();
        }

        app.UseRouting();

        app.UseCors(DefaultCorsPolicyName); //Enable CORS!
        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseJwtTokenMiddleware();

        if (bool.Parse(_appConfiguration["OpenIddict:IsEnabled"]))
        {
            app.UseAbpOpenIddictValidation();
        }

        app.UseAuthorization();

        using (var scope = app.ApplicationServices.CreateScope())
        {
            if (scope.ServiceProvider.GetService<DatabaseCheckHelper>()
                .Exist(_appConfiguration["ConnectionStrings:Default"]))
            {
                app.UseAbpRequestLocalization();
            }
        }

        if (WebConsts.HangfireDashboardEnabled)
        {
            //Hangfire dashboard &server(Enable to use Hangfire instead of default job manager)
            app.UseHangfireDashboard(WebConsts.HangfireDashboardEndPoint, new DashboardOptions
            {
                Authorization = new[]
                    {new AbpHangfireAuthorizationFilter(AppPermissions.Pages_Administration_HangfireDashboard)}
            });
        }

        if (bool.Parse(_appConfiguration["Payment:Stripe:IsActive"]))
        {
            StripeConfiguration.ApiKey = _appConfiguration["Payment:Stripe:SecretKey"];
        }

        if (WebConsts.GraphQL.Enabled)
        {
            app.UseGraphQL<MainSchema>(WebConsts.GraphQL.EndPoint);
            if (WebConsts.GraphQL.PlaygroundEnabled)
            {
                // to explorer API navigate https://*DOMAIN*/ui/playground
                app.UseGraphQLPlayground(
                    WebConsts.GraphQL.PlaygroundEndPoint,
                    new PlaygroundOptions()
                );
            }
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<AbpCommonHub>("/signalr");
            endpoints.MapHub<ChatHub>("/signalr-chat");
            endpoints.MapHub<QrLoginHub>("signalr-qr-login");

            endpoints.MapControllerRoute("defaultWithArea", "{area}/{controller=Home}/{action=Index}/{id?}");
            endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

            app.ApplicationServices.GetRequiredService<IAbpAspNetCoreConfiguration>().EndpointConfiguration
                .ConfigureAllEndpoints(endpoints);
        });

        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        if (WebConsts.SwaggerUiEnabled)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger();
            // Enable middleware to serve swagger-ui assets (HTML, JS, CSS etc.)

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(_appConfiguration["App:SwaggerEndPoint"], "HCMv3 API V1");
                options.IndexStream = () => Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("HCMv3.Web.wwwroot.swagger.ui.index.html");
                options.InjectBaseUrl(_appConfiguration["App:ServerRootAddress"]);
            }); //URL: /swagger
        }
    }

    /// <summary>
    /// Registers the DevExpress Web Report Designer / Web Document Viewer / Query Builder backends
    /// that serve the report classes in HCMv3.Reports. See DevExpressReportingControllers.cs for
    /// the URL contract (/DXXRD, /DXXRDV, /DXXQB).
    /// </summary>
    private void ConfigureDevExpressReporting(IServiceCollection services)
    {
        services.AddDevExpressControls();

        services.ConfigureReportingServices(configurator =>
        {
            configurator.ConfigureWebDocumentViewer(viewer =>
            {
                viewer.UseCachedReportSourceBuilder();
            });

            configurator.ConfigureReportDesigner(designer =>
            {
            });
        });

        // Report layouts are served from source-controlled report classes, not from a file store.
        services.AddSingleton<ReportStorageWebExtension, HcmReportStorageWebExtension>();

        // Publish the ported report NAMES to Core.Shared so HCMv3.Application can compute
        // PrintReport.isAvailable without referencing HCMv3.Reports. See PortedReportCatalog.
        HcmReportFactoryRegistry.PublishToPortedReportCatalog();

        // Resolves the connection NAME carried by report layouts into a real connection, server
        // side, so the connection string is never serialised into the designer's client model.
        //
        // BOTH registrations are required. DevExpress's viewer/designer pipeline resolves the
        // FACTORY (DevExpress.DataAccess.Web.IConnectionProviderFactory) and calls Create(); it
        // never resolves IConnectionProviderService (DevExpress.DataAccess.Wizard.Services)
        // directly. Registering only the service leaves the report's connection name unresolved,
        // and SqlDataSource.CreateConnection(name) NREs as soon as the viewer touches the data
        // source. See HcmReportConnectionProviderFactory.
        services.AddScoped<IConnectionProviderService, HcmReportConnectionProviderService>();
        services.AddSingleton<IConnectionProviderFactory, HcmReportConnectionProviderFactory>();
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
                    var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath,
                        certPassword);
                    listenOptions.UseHttps(new HttpsConnectionAdapterOptions()
                    {
                        ServerCertificate = cert
                    });
                });
        });
    }

    private void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo() { Title = "HCMv3 API", Version = "v1" });
            options.DocInclusionPredicate((docName, description) => true);
            options.ParameterFilter<SwaggerEnumParameterFilter>();
            options.ParameterFilter<SwaggerNullableParameterFilter>();
            options.SchemaFilter<SwaggerEnumSchemaFilter>();
            options.OperationFilter<SwaggerOperationIdFilter>();
            options.OperationFilter<SwaggerOperationFilter>();
            options.CustomDefaultSchemaIdSelector();

            //add summaries to swagger
            bool canShowSummaries = _appConfiguration.GetValue<bool>("Swagger:ShowSummaries");
            if (canShowSummaries)
            {
                var hostXmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var hostXmlPath = Path.Combine(AppContext.BaseDirectory, hostXmlFile);
                options.IncludeXmlComments(hostXmlPath);

                var applicationXml = $"HCMv3.Application.xml";
                var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, applicationXml);
                options.IncludeXmlComments(applicationXmlPath);

                var webCoreXmlFile = $"HCMv3.Web.Core.xml";
                var webCoreXmlPath = Path.Combine(AppContext.BaseDirectory, webCoreXmlFile);
                options.IncludeXmlComments(webCoreXmlPath);
            }
        });
    }

    private void ConfigureHealthChecks(IServiceCollection services)
    {
        services.AddAbpZeroHealthCheck();
    }
}

