using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Common.Services;

public interface IMediaFormatConverter
{
    /// <summary>
    /// Checks if the given file requires conversion.
    /// </summary>
    /// <param name="filePath">Input file path.</param>
    /// <param name="outputExtension">The extension it should be converted to (including dot, e.g. ".webm").</param>
    /// <returns>True if conversion is required, otherwise false.</returns>
    bool RequiresConversion(string filePath, out string outputExtension);

    /// <summary>
    /// Converts the given file to the specified output format.
    /// </summary>
    /// <param name="inputPath">The input file path.</param>
    /// <param name="outputPath">The output file path.</param>
    /// <returns>True if conversion succeeded, otherwise false.</returns>
    Task<bool> TryConvertAsync(string inputPath, string outputPath);
}
