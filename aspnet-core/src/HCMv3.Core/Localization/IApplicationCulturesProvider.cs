using System.Globalization;

namespace HCMv3.Localization;

public interface IApplicationCulturesProvider
{
    CultureInfo[] GetAllCultures();
}

