using Lively.Models;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;

namespace Lively.Common.Helpers
{
    public static class LivelyInfoUtil
    {
        public static LivelyInfoModel? GetLocalized(string locPath, string languageCode = "")
        {
            if (!File.Exists(locPath))
                return null;

            LivelyInfoLocalizationFile loc;
            try
            {
                loc = JsonConvert.DeserializeObject<LivelyInfoLocalizationFile>(File.ReadAllText(locPath));
            }
            catch
            {
                return null;
            }

            if (loc?.Languages is null)
                return null;

            // ApplicationLanguages.PrimaryLanguageOverride is empty when not set / use system default.
            languageCode = string.IsNullOrEmpty(languageCode) ? CultureInfo.CurrentUICulture.Name : languageCode;
            // Try exact match first, eg: zh-CN
            if (!loc.Languages.TryGetValue(languageCode, out var lang))
            {
                // Try base language fallback, eg: zh
                var baseLang = languageCode.Split('-')[0];
                if (!loc.Languages.TryGetValue(baseLang, out lang))
                    return null;
            }
            return lang;
        }
    }
}
