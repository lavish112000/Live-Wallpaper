namespace Lively.Models.Enums;

// Ref: https://mpv.io/manual/master/#options-target-colorspace-hint-mode
public enum TargetColorspaceHintMode
{
    /// <summary>
    /// Uses metadata based on the target display's capabilities.
    /// </summary>
    target,

    /// <summary>
    /// Uses the source content's metadata.
    /// Lets Windows/compositor handle any color space mapping ("HDR passthrough" mode).
    /// </summary>
    source,

    /// <summary>
    /// Like Source, but uses dynamic per-scene metadata (experimental).
    /// Requires a display that can react to metadata changes.
    /// </summary>
    sourceDynamic
}
