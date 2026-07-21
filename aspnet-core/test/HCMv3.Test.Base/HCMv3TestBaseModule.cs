using System;
using System.IO;
using Abp;
using Abp.AspNetZeroCore;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Modules;
using Abp.Net.Mail;
using Abp.TestBase;
using Abp.Zero.Configuration;
using Castle.MicroKernel.Registration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using HCMv3.Authorization.Users;
using HCMv3.Configuration;
using HCMv3.EntityFrameworkCore;
using HCMv3.MultiTenancy;
using HCMv3.Security.Recaptcha;
using HCMv3.Test.Base.DependencyInjection;
using HCMv3.Test.Base.UiCustomization;
using HCMv3.Test.Base.Url;
using HCMv3.Test.Base.Web;
using HCMv3.UiCustomization;
using HCMv3.Url;
using NSubstitute;

namespace HCMv3.Test.Base;

[DependsOn(
    typeof(HCMv3ApplicationModule),
    typeof(HCMv3EntityFrameworkCoreModule),
    typeof(AbpTestBaseModule))]
public class HCMv3TestBaseModule : AbpModule
{
    public HCMv3TestBaseModule(HCMv3EntityFrameworkCoreModule abpZeroTemplateEntityFrameworkCoreModule)
    {
        abpZeroTemplateEntityFrameworkCoreModule.SkipDbContextRegistration = true;
    }

    public override void PreInitialize()
    {
        var configuration = GetConfiguration();

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;

        Configuration.UnitOfWork.Timeout = TimeSpan.FromMinutes(30);
        Configuration.UnitOfWork.IsTransactional = false;

        //Use database for language management
        Configuration.Modules.Zero().LanguageManagement.EnableDbLocalization();

        RegisterFakeService<AbpZeroDbMigrator>();
        RegisterFakeService<IHttpContextAccessor>();

        IocManager.Register<IAppUrlService, FakeAppUrlService>();
        IocManager.Register<IWebUrlService, FakeWebUrlService>();
        IocManager.Register<IRecaptchaValidator, FakeRecaptchaValidator>();

        Configuration.ReplaceService<IAppConfigurationAccessor, TestAppConfigurationAccessor>();
        Configuration.ReplaceService<IEmailSender, NullEmailSender>(DependencyLifeStyle.Transient);
        Configuration.ReplaceService<IUiThemeCustomizerFactory, NullUiThemeCustomizerFactory>();

        Configuration.Modules.AspNetZero().LicenseCode = configuration["AbpZeroLicenseCode"];

        //Uncomment below line to write change logs for the entities below:
        Configuration.EntityHistory.IsEnabled = true;
        Configuration.EntityHistory.Selectors.Add("HCMv3Entities", typeof(User), typeof(Tenant));
    }

    public override void Initialize()
    {
        ServiceCollectionRegistrar.Register(IocManager);
    }

    private void RegisterFakeService<TService>()
        where TService : class
    {
        IocManager.IocContainer.Register(
            Component.For<TService>()
                .UsingFactoryMethod(() => Substitute.For<TService>())
                .LifestyleSingleton()
        );
    }

    private static IConfigurationRoot GetConfiguration()
    {
        return AppConfigurations.Get(Directory.GetCurrentDirectory(), addUserSecrets: true);
    }
}
