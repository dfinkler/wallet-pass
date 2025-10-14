using FanPad.WalletPass.Models;
using FanPad.WalletPass.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace FanPad.WalletPass.Tests.Services;

/// <summary>
/// Tests for UserAgentService platform detection logic
/// 
/// Test Scenarios:
/// 1. Explicit platform parameter short-circuits User-Agent analysis
/// 2. iOS User-Agent detection
/// 3. Android User-Agent detection
/// 4. Unknown User-Agent defaults appropriately
/// </summary>
public class UserAgentServiceTests
{
    private readonly UserAgentService _service;
    private readonly Mock<ILogger<UserAgentService>> _mockLogger;

    public UserAgentServiceTests()
    {
        _mockLogger = new Mock<ILogger<UserAgentService>>();
        _service = new UserAgentService(_mockLogger.Object);
    }

    // Test 1: Explicit Parameter Short-Circuits

    [Theory]
    [InlineData("apple", Platform.Apple)]
    [InlineData("google", Platform.Google)]
    [InlineData("ios", Platform.Apple)]      // Alias
    [InlineData("android", Platform.Google)] // Alias
    [InlineData("APPLE", Platform.Apple)]    // Case-insensitive
    [InlineData("Google", Platform.Google)]  // Case-insensitive
    public void DetectPlatform_WithExplicitParameter_ShortCircuitsEvaluation(
        string explicitPlatform,
        Platform expectedPlatform)
    {
        // Arrange - Create context with Android User-Agent (should be ignored)
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36"
        );

        // Act - Pass explicit parameter (should override User-Agent)
        var result = _service.DetectPlatform(context, explicitPlatform);

        // Assert
        result.Platform.Should().Be(expectedPlatform);
        result.DetectionMethod.Should().Be(DetectionMethod.ExplicitParameter);
        result.Confidence.Should().Be(1.0); // 100% confidence
        result.Source.Should().Contain("Query parameter");
    }

    [Fact]
    public void DetectPlatform_WithExplicitParameter_IgnoresUserAgent()
    {
        // Arrange - Android User-Agent but explicit "apple" parameter
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (Linux; Android 13) AppleWebKit/537.36"
        );

        // Act - Explicit apple should win
        var result = _service.DetectPlatform(context, "apple");

        // Assert - Should detect Apple despite Android User-Agent
        result.Platform.Should().Be(Platform.Apple);
        result.DetectionMethod.Should().Be(DetectionMethod.ExplicitParameter);
    }

    // Test 2: iOS User-Agent Detection

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 16_5 like Mac OS X) AppleWebKit/605.1.15")]
    [InlineData("Mozilla/5.0 (iPod touch; CPU iPhone OS 15_0 like Mac OS X) WebKit/605.1.15")]
    public void DetectPlatform_WithiOSUserAgent_DetectsApple(string userAgent)
    {
        // Arrange
        var context = CreateHttpContextWithUserAgent(userAgent);

        // Act - No explicit parameter, should analyze User-Agent
        var result = _service.DetectPlatform(context);

        // Assert
        result.Platform.Should().Be(Platform.Apple);
        result.DetectionMethod.Should().Be(DetectionMethod.UserAgent);
        result.Confidence.Should().Be(1.0); // 100% confidence for iOS devices
        result.Source.Should().Contain("iOS device");
    }

    [Fact]
    public void DetectPlatform_WithiPhone_HasHighConfidence()
    {
        // Arrange
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)"
        );

        // Act
        var result = _service.DetectPlatform(context);

        // Assert
        result.Platform.Should().Be(Platform.Apple);
        result.Confidence.Should().Be(1.0, "iPhone is definitively iOS");
        result.DetectionMethod.Should().Be(DetectionMethod.UserAgent);
    }

    // Test 3: Android User-Agent Detection

    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36")]
    [InlineData("Mozilla/5.0 (Linux; Android 12; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0")]
    [InlineData("Mozilla/5.0 (Android 11; Mobile; rv:68.0) Gecko/68.0 Firefox/68.0")]
    public void DetectPlatform_WithAndroidUserAgent_DetectsGoogle(string userAgent)
    {
        // Arrange
        var context = CreateHttpContextWithUserAgent(userAgent);

        // Act
        var result = _service.DetectPlatform(context);

        // Assert
        result.Platform.Should().Be(Platform.Google);
        result.DetectionMethod.Should().Be(DetectionMethod.UserAgent);
        result.Confidence.Should().Be(1.0); // 100% confidence for Android devices
        result.Source.Should().Contain("Android");
    }

    [Fact]
    public void DetectPlatform_WithAndroid_HasHighConfidence()
    {
        // Arrange
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (Linux; Android 13; Pixel 7)"
        );

        // Act
        var result = _service.DetectPlatform(context);

        // Assert
        result.Platform.Should().Be(Platform.Google);
        result.Confidence.Should().Be(1.0, "Android is definitively Google");
        result.DetectionMethod.Should().Be(DetectionMethod.UserAgent);
    }

    // Test 4: Unknown/Ambiguous User-Agent Handling

    [Fact]
    public void DetectPlatform_WithUnknownUserAgent_UsesDefault()
    {
        // Arrange - Generic/unknown User-Agent
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (Unknown Device)"
        );

        // Act
        var result = _service.DetectPlatform(context);

        // Assert
        result.Platform.Should().Be(Platform.Apple); // Default to Apple
        result.DetectionMethod.Should().Be(DetectionMethod.Default);
        result.Confidence.Should().Be(0.5); // 50% - pure guess
        result.Source.Should().Contain("Unknown User-Agent");
    }

    [Fact]
    public void DetectPlatform_WithNoUserAgent_UsesDefault()
    {
        // Arrange - No User-Agent header at all
        var context = CreateHttpContextWithUserAgent(string.Empty);

        // Act
        var result = _service.DetectPlatform(context);

        // Assert
        result.Platform.Should().Be(Platform.Apple);
        result.DetectionMethod.Should().Be(DetectionMethod.Default);
        result.Confidence.Should().Be(0.0); // 0% - no information
        result.Source.Should().Contain("No User-Agent");
    }

    [Fact]
    public void DetectPlatform_WithBot_UsesDefault()
    {
        // Arrange - Bot User-Agent
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)"
        );

        // Act
        var result = _service.DetectPlatform(context);

        // Assert
        result.Platform.Should().Be(Platform.Apple);
        result.DetectionMethod.Should().Be(DetectionMethod.Default);
        result.Confidence.Should().Be(0.0); // No real information
        result.Source.Should().Contain("Bot/Crawler");
    }

    // Test 5: Heuristic Detection (Desktop)

    [Fact]
    public void DetectPlatform_WithMacOS_UsesHeuristic()
    {
        // Arrange - macOS desktop
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36"
        );

        // Act
        var result = _service.DetectPlatform(context);

        // Assert
        result.Platform.Should().Be(Platform.Apple);
        result.DetectionMethod.Should().Be(DetectionMethod.Heuristic);
        result.Confidence.Should().Be(0.75); // 75% - Mac users often have iPhones
        result.Source.Should().Contain("macOS desktop");
    }

    [Fact]
    public void DetectPlatform_WithWindowsChrome_UsesHeuristic()
    {
        // Arrange - Windows + Chrome
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0"
        );

        // Act
        var result = _service.DetectPlatform(context);

        // Assert
        result.Platform.Should().Be(Platform.Google);
        result.DetectionMethod.Should().Be(DetectionMethod.Heuristic);
        result.Confidence.Should().Be(0.55); // 55% - slight preference
        result.Source.Should().Contain("Windows + Chrome");
    }

    [Fact]
    public void DetectPlatform_WithSafari_UsesHeuristic()
    {
        // Arrange - Safari browser on macOS (macOS rule takes precedence)
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15"
        );

        // Act
        var result = _service.DetectPlatform(context);

        // Assert
        result.Platform.Should().Be(Platform.Apple);
        result.DetectionMethod.Should().Be(DetectionMethod.Heuristic);
        result.Confidence.Should().Be(0.75); // 75% - macOS desktop (detected before Safari-specific rule)
        result.Source.Should().Contain("macOS desktop");
    }

    // Test 6: Priority Order

    [Fact]
    public void DetectPlatform_PrioritizesExplicitOverUserAgent()
    {
        // Arrange - Clear iOS User-Agent but explicit google parameter
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0)"
        );

        // Act - Explicit parameter should win
        var result = _service.DetectPlatform(context, "google");

        // Assert
        result.Platform.Should().Be(Platform.Google, "Explicit parameter takes precedence");
        result.DetectionMethod.Should().Be(DetectionMethod.ExplicitParameter);
        result.Confidence.Should().Be(1.0);
    }

    [Fact]
    public void DetectPlatform_WithInvalidExplicit_FallsBackToUserAgent()
    {
        // Arrange
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0)"
        );

        // Act - Invalid explicit parameter should fall back to User-Agent
        var result = _service.DetectPlatform(context, "invalid-platform");

        // Assert - Should fall back to iPhone detection
        result.Platform.Should().Be(Platform.Apple);
        result.DetectionMethod.Should().Be(DetectionMethod.UserAgent);
    }

    // Test 7: GetPlatformInfo (Analytics)

    [Fact]
    public void GetPlatformInfo_ReturnsFullAnalysis()
    {
        // Arrange - More complete iPhone User-Agent with Safari version
        var context = CreateHttpContextWithUserAgent(
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1"
        );

        // Act
        var info = _service.GetPlatformInfo(context);

        // Assert
        info.DetectedPlatform.Should().Be(Platform.Apple);
        info.IsMobile.Should().BeTrue();
        info.IsDesktop.Should().BeFalse();
        info.IsBot.Should().BeFalse();
        info.OSFamily.Should().Contain("iOS");
        info.BrowserFamily.Should().Be("Safari"); // Now includes "safari/" in User-Agent
    }

    // Helper Methods

    /// <summary>
    /// Create a mock HttpContext with specified User-Agent header
    /// </summary>
    private static HttpContext CreateHttpContextWithUserAgent(string userAgent)
    {
        var context = new DefaultHttpContext();

        if (!string.IsNullOrEmpty(userAgent))
        {
            context.Request.Headers["User-Agent"] = userAgent;
        }

        return context;
    }
}

