using FanPad.WalletPass.Models;

namespace FanPad.WalletPass.Services;

/// <summary>
/// Service for analyzing HTTP request headers to determine user's platform
/// 
/// This makes the API self-contained - clients don't need to pass platform information.
/// The backend intelligently detects iOS vs Android based on User-Agent and other headers.
/// 
/// Benefits:
/// - Works with QR codes and direct links (no frontend needed)
/// - Consistent detection logic in one place
/// - Easy to update rules without changing clients
/// - Supports override for testing
/// </summary>
public class UserAgentService
{
    private readonly ILogger<UserAgentService> _logger;

    public UserAgentService(ILogger<UserAgentService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detect platform using priority-based detection strategy
    /// 
    /// Priority Order:
    /// 1. Explicit platform parameter (client knows best) - HIGHEST PRIORITY
    /// 2. User-Agent analysis (automatic fallback)
    /// 3. Default (Platform.Apple) - last resort
    /// 
    /// This hybrid approach gives clients full control while still working
    /// with QR codes and email links where no client-side detection exists.
    /// </summary>
    /// <param name="context">HTTP context containing request headers</param>
    /// <param name="explicitPlatform">Platform explicitly specified by client (preferred)</param>
    /// <returns>Detected platform with detection method and confidence</returns>
    public PlatformDetectionResult DetectPlatform(HttpContext context, string? explicitPlatform = null)
    {
        // ================================================================
        // PRIORITY 1: Explicit Platform Parameter (PREFERRED)
        // ================================================================
        // If client explicitly specifies platform, trust it completely.
        // Client-side detection is more reliable than server-side heuristics.
        if (!string.IsNullOrEmpty(explicitPlatform))
        {
            var parsed = PlatformExtensions.FromString(explicitPlatform);
            if (parsed != Platform.Unknown)
            {
                _logger.LogInformation(
                    "Platform explicitly specified by client: {Platform}",
                    parsed
                );
                return new PlatformDetectionResult
                {
                    Platform = parsed,
                    DetectionMethod = DetectionMethod.ExplicitParameter,
                    Confidence = 1.0, // 100% - client told us explicitly
                    Source = $"Query parameter: {explicitPlatform}"
                };
            }

            _logger.LogWarning(
                "Invalid platform parameter specified: {Platform}, falling back to User-Agent detection",
                explicitPlatform
            );
        }

        // ================================================================
        // PRIORITY 2: User-Agent Analysis (AUTOMATIC FALLBACK)
        // ================================================================
        // If no explicit parameter, analyze User-Agent header.
        // This enables QR codes, email links, etc. to work without frontend.
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        if (string.IsNullOrEmpty(userAgent))
        {
            _logger.LogWarning("No User-Agent header present, using default platform");
            return new PlatformDetectionResult
            {
                Platform = Platform.Apple,
                DetectionMethod = DetectionMethod.Default,
                Confidence = 0.0, // No information available
                Source = "No User-Agent header"
            };
        }

        var result = AnalyzeUserAgentWithConfidence(userAgent);

        _logger.LogInformation(
            "Platform detected from User-Agent: {Platform} (confidence: {Confidence:P0}, method: {Method})",
            result.Platform,
            result.Confidence,
            result.DetectionMethod
        );

        return result;
    }

    /// <summary>
    /// Analyze User-Agent with confidence scoring
    /// </summary>
    private PlatformDetectionResult AnalyzeUserAgentWithConfidence(string userAgent)
    {
        var ua = userAgent.ToLower();

        // High Confidence: iOS Devices (100%)
        if (ContainsAny(ua, "iphone", "ipad", "ipod"))
        {
            return new PlatformDetectionResult
            {
                Platform = Platform.Apple,
                DetectionMethod = DetectionMethod.UserAgent,
                Confidence = 1.0, // 100% - Definitive
                Source = "iOS device in User-Agent"
            };
        }

        // High Confidence: Android Devices (100%)
        if (ua.Contains("android"))
        {
            return new PlatformDetectionResult
            {
                Platform = Platform.Google,
                DetectionMethod = DetectionMethod.UserAgent,
                Confidence = 1.0, // 100% - Definitive
                Source = "Android in User-Agent"
            };
        }

        // Medium Confidence: macOS Desktop (75%)
        if (ContainsAny(ua, "macintosh", "mac os x") && !ua.Contains("android"))
        {
            return new PlatformDetectionResult
            {
                Platform = Platform.Apple,
                DetectionMethod = DetectionMethod.Heuristic,
                Confidence = 0.75, // 75% - Mac users often have iPhones
                Source = "macOS desktop (Apple ecosystem heuristic)"
            };
        }

        // Low Confidence: Windows + Chrome (55%)
        if (ua.Contains("windows") && ua.Contains("chrome") && !ua.Contains("edge"))
        {
            return new PlatformDetectionResult
            {
                Platform = Platform.Google,
                DetectionMethod = DetectionMethod.Heuristic,
                Confidence = 0.55, // 55% - Slight preference
                Source = "Windows + Chrome (Google ecosystem heuristic)"
            };
        }

        // Medium Confidence: Safari Browser (70%)
        if (ua.Contains("safari") && !ua.Contains("chrome") && !ua.Contains("android"))
        {
            return new PlatformDetectionResult
            {
                Platform = Platform.Apple,
                DetectionMethod = DetectionMethod.Heuristic,
                Confidence = 0.70, // 70% - Safari users likely in Apple ecosystem
                Source = "Safari browser (Apple ecosystem heuristic)"
            };
        }

        // Low Confidence: Mobile keyword (60%)
        if (ua.Contains("mobile") && !ua.Contains("android"))
        {
            return new PlatformDetectionResult
            {
                Platform = Platform.Apple,
                DetectionMethod = DetectionMethod.Heuristic,
                Confidence = 0.60, // 60% - Likely iOS if mobile but not Android
                Source = "Mobile device (non-Android)"
            };
        }

        // Very Low Confidence: Bot/Crawler (0%)
        if (ContainsAny(ua, "bot", "crawler", "spider", "curl", "wget", "postman"))
        {
            return new PlatformDetectionResult
            {
                Platform = Platform.Apple,
                DetectionMethod = DetectionMethod.Default,
                Confidence = 0.0, // 0% - Just a guess for testing
                Source = "Bot/Crawler (default fallback)"
            };
        }

        // No Confidence: Unknown (50%)
        return new PlatformDetectionResult
        {
            Platform = Platform.Apple,
            DetectionMethod = DetectionMethod.Default,
            Confidence = 0.50, // 50% - Pure guess based on market share
            Source = "Unknown User-Agent (market share fallback)"
        };
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    [Obsolete("Use DetectPlatform() which returns confidence scores")]
    private Platform AnalyzeUserAgent(string userAgent)
    {
        var ua = userAgent.ToLower();

        // ================================================================
        // Rule 1: iOS Devices (High Confidence)
        // ================================================================
        // iPhone, iPad, iPod clearly indicate Apple ecosystem
        if (ContainsAny(ua, "iphone", "ipad", "ipod"))
        {
            return Platform.Apple;
        }

        // ================================================================
        // Rule 2: Android Devices (High Confidence)
        // ================================================================
        // Android in User-Agent is definitive
        if (ua.Contains("android"))
        {
            return Platform.Google;
        }

        // ================================================================
        // Rule 3: Desktop/Laptop (Lower Confidence - Heuristics)
        // ================================================================

        // macOS users likely have iPhones (Apple ecosystem)
        if (ContainsAny(ua, "macintosh", "mac os x") && !ua.Contains("android"))
        {
            _logger.LogDebug("Detected macOS desktop, defaulting to Apple");
            return Platform.Apple;
        }

        // Windows with Chrome - could go either way
        // Slight preference for Google since Chrome is Google's browser
        if (ua.Contains("windows") && ua.Contains("chrome") && !ua.Contains("edge"))
        {
            _logger.LogDebug("Detected Windows + Chrome, defaulting to Google");
            return Platform.Google;
        }

        // Safari on any platform → likely Apple user
        if (ua.Contains("safari") && !ua.Contains("chrome") && !ua.Contains("android"))
        {
            _logger.LogDebug("Detected Safari browser, defaulting to Apple");
            return Platform.Apple;
        }

        // ================================================================
        // Rule 4: Mobile-specific indicators
        // ================================================================

        // "Mobile" keyword often indicates phone
        if (ua.Contains("mobile"))
        {
            // If we see mobile but not Android, likely iOS
            if (!ua.Contains("android"))
            {
                return Platform.Apple;
            }
        }

        // ================================================================
        // Rule 5: Bots and Crawlers
        // ================================================================

        if (ContainsAny(ua, "bot", "crawler", "spider", "curl", "wget", "postman"))
        {
            _logger.LogWarning("Detected bot/crawler, defaulting to Apple for testing");
            return Platform.Apple; // Return something testable
        }

        // ================================================================
        // Default: Apple
        // ================================================================
        // In US, iOS has ~60% market share, so Apple is safer default
        _logger.LogWarning(
            "Could not confidently detect platform from User-Agent, defaulting to Apple"
        );
        return Platform.Apple;
    }

    /// <summary>
    /// Check if string contains any of the given substrings
    /// </summary>
    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(v => text.Contains(v, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Mask User-Agent for logging (privacy)
    /// </summary>
    private static string MaskUserAgent(string userAgent)
    {
        if (userAgent.Length <= 50)
            return userAgent;

        return $"{userAgent[..40]}...{userAgent[^10..]}";
    }

    /// <summary>
    /// Get detailed platform information for analytics
    /// </summary>
    public UserAgentInfo GetPlatformInfo(HttpContext context)
    {
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var detectedPlatform = DetectPlatform(context);

        return new UserAgentInfo
        {
            DetectedPlatform = detectedPlatform.Platform,
            UserAgent = userAgent,
            IsDesktop = IsDesktop(userAgent),
            IsMobile = IsMobile(userAgent),
            IsBot = IsBot(userAgent),
            BrowserFamily = DetectBrowser(userAgent),
            OSFamily = DetectOS(userAgent)
        };
    }

    private static bool IsDesktop(string userAgent)
    {
        var ua = userAgent.ToLower();
        return ContainsAny(ua, "windows", "macintosh", "linux", "x11")
               && !ua.Contains("mobile");
    }

    private static bool IsMobile(string userAgent)
    {
        var ua = userAgent.ToLower();
        return ContainsAny(ua, "mobile", "android", "iphone", "ipad", "ipod");
    }

    private static bool IsBot(string userAgent)
    {
        var ua = userAgent.ToLower();
        return ContainsAny(ua, "bot", "crawler", "spider", "curl", "wget");
    }

    private static string DetectBrowser(string userAgent)
    {
        var ua = userAgent.ToLower();

        if (ua.Contains("edg/")) return "Edge";
        if (ua.Contains("chrome/")) return "Chrome";
        if (ua.Contains("safari/") && !ua.Contains("chrome")) return "Safari";
        if (ua.Contains("firefox/")) return "Firefox";

        return "Unknown";
    }

    private static string DetectOS(string userAgent)
    {
        var ua = userAgent.ToLower();

        if (ua.Contains("iphone")) return "iOS (iPhone)";
        if (ua.Contains("ipad")) return "iOS (iPad)";
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("mac os x")) return "macOS";
        if (ua.Contains("windows")) return "Windows";
        if (ua.Contains("linux")) return "Linux";

        return "Unknown";
    }
}

/// <summary>
/// Result of platform detection with confidence scoring
/// </summary>
public record PlatformDetectionResult
{
    /// <summary>
    /// Detected platform (Apple or Google)
    /// </summary>
    public Platform Platform { get; init; }

    /// <summary>
    /// How the platform was detected
    /// </summary>
    public DetectionMethod DetectionMethod { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// 1.0 = 100% certain (explicit parameter or definitive User-Agent)
    /// 0.5 = 50% guess (ambiguous or unknown)
    /// 0.0 = No information (pure default)
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Human-readable explanation of detection source
    /// </summary>
    public string Source { get; init; } = string.Empty;
}

/// <summary>
/// Method used to detect platform
/// </summary>
public enum DetectionMethod
{
    /// <summary>
    /// Client explicitly specified platform via query parameter (BEST)
    /// </summary>
    ExplicitParameter,

    /// <summary>
    /// Detected from User-Agent header (definitive - iPhone/Android)
    /// </summary>
    UserAgent,

    /// <summary>
    /// Heuristic/guess based on browser/OS (medium confidence)
    /// </summary>
    Heuristic,

    /// <summary>
    /// No information, using default (WORST)
    /// </summary>
    Default
}

/// <summary>
/// Detailed User-Agent analysis for analytics
/// </summary>
public record UserAgentInfo
{
    public Platform DetectedPlatform { get; init; } = Platform.Unknown;
    public string UserAgent { get; init; } = string.Empty;
    public bool IsDesktop { get; init; }
    public bool IsMobile { get; init; }
    public bool IsBot { get; init; }
    public string BrowserFamily { get; init; } = string.Empty;
    public string OSFamily { get; init; } = string.Empty;
}

