namespace FanPad.WalletPass.Models;

/// <summary>
/// Supported mobile wallet platforms
/// </summary>
public enum Platform
{
    /// <summary>
    /// Apple Wallet (iOS, watchOS, macOS)
    /// </summary>
    Apple,

    /// <summary>
    /// Google Wallet (Android)
    /// </summary>
    Google,

    /// <summary>
    /// Unknown or unsupported platform
    /// </summary>
    Unknown
}

/// <summary>
/// Extension methods for Platform enum
/// </summary>
public static class PlatformExtensions
{
    /// <summary>
    /// Convert Platform enum to lowercase string (for API compatibility)
    /// </summary>
    public static string ToLowerString(this Platform platform)
    {
        return platform.ToString().ToLower();
    }

    /// <summary>
    /// Parse string to Platform enum
    /// </summary>
    public static Platform FromString(string platformString)
    {
        return platformString.ToLower() switch
        {
            "apple" => Platform.Apple,
            "google" => Platform.Google,
            "ios" => Platform.Apple,      // Alias
            "android" => Platform.Google, // Alias
            _ => Platform.Unknown
        };
    }
}

