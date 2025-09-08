using ImageMagick;
using Lively.Common;
using Lively.Common.Services;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Services
{
    public class MediaFormatConverter : IMediaFormatConverter
    {
        // Ref: https://github.com/mpv-player/mpv/issues/7390
        private Dictionary<string, string> ConversionMap { get; } = new() 
        {
            { ".webp", ".webm" }
        };

        public bool RequiresConversion(string filePath, out string outputExtension)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (ConversionMap.TryGetValue(ext, out outputExtension))
            {
                if (ext == ".webp")
                {
                    try
                    {
                        // Check if it's animated
                        using var images = new MagickImageCollection(filePath);
                        if (images.Count > 1)
                            return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }

            outputExtension = string.Empty;
            return false;
        }

        public async Task<bool> TryConvertAsync(string inputPath, string outputPath)
        {
            try
            {
                var fileType = FileTypes.GetFileType(inputPath);
                switch (fileType)
                {
                    case Models.Enums.WallpaperType.picture:
                        await Task.Run(() =>
                        {
                            using var images = new MagickImageCollection(inputPath);
                            images.Coalesce();
                            images.Write(outputPath);
                        });
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
