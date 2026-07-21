using Abp.Dependency;

namespace HCMv3.Web.Xss;

public interface IHtmlSanitizer : ITransientDependency
{
    string Sanitize(string html);
}

