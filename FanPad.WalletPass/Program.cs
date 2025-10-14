using FanPad.WalletPass.Models;
using FanPad.WalletPass.Services;
using Scalar.AspNetCore;

// ============================================================================
// FanPad Wallet Pass API
// ============================================================================
// 
// This API demonstrates platform abstraction for Apple Wallet & Google Wallet
// Key architectural pattern: WalletPassBase abstraction handles platform differences
// 
// Functional programming principles:
// - Minimal APIs (like Express.js route handlers)
// - Result pattern for error handling (no exceptions for business logic)
// - Immutable records (like TypeScript const objects)
// - Dependency injection (like Angular services)
//
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

// Service Registration (Dependency Injection)
builder.Services.AddOpenApi();

// Core services
builder.Services.AddSingleton<PassRepository>();
builder.Services.AddSingleton<PhoneVerificationService>();
builder.Services.AddSingleton<UserAgentService>();

// Platform-specific wallet services (both registered for demonstration)
builder.Services.AddSingleton<AppleWalletService>();
builder.Services.AddSingleton<GoogleWalletService>();

// CORS (if needed for frontend)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();


// Middleware Pipeline

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Modern Swagger UI alternative
}

app.UseHttpsRedirection();
app.UseCors();


// API Endpoints

// Root endpoint - API info
app.MapGet("/", () => new
{
    name = "FanPad Wallet Pass API",
    version = "1.0.0",
    description = "Mobile wallet pass generation for Apple Wallet & Google Wallet",
    endpoints = new[]
    {
        "POST /api/pass/initiate - Start pass creation with phone verification",
        "POST /api/pass/verify - Verify phone code (step 1)",
        "POST /api/pass/complete - Complete pass with fan name (step 2)",
        "GET /api/pass/{id} - Get pass details",
        "GET /api/pass/{id}/download?platform={apple|google} - Download platform-specific pass",
        "POST /api/pass/{id}/notify - Send push notification",
        "GET /api/artists - List available artists"
    },
    documentation = "/swagger"
})
.WithName("GetApiInfo")
.WithOpenApi();


// 1. Initiate Pass Creation

app.MapPost("/api/pass/initiate", async (
    InitiatePassRequest request,
    PassRepository repo,
    PhoneVerificationService phoneService,
    ILogger<Program> logger) =>
{
    // Combine country code + phone for verification
    var fullPhoneNumber = $"{request.CountryCode}{request.Phone}";

    logger.LogInformation(
        "Initiating pass creation for phone {CountryCode}{Phone}",
        request.CountryCode,
        request.Phone
    );

    // Functional pipeline: Validate → Create → Send Code
    var codeResult = await phoneService.SendVerificationCode(fullPhoneNumber);
    if (!codeResult.IsSuccess)
    {
        return Results.BadRequest(new { error = codeResult.Error });
    }

    var passResult = await repo.CreatePass(request.CountryCode, request.Phone, request.ArtistId);
    if (!passResult.IsSuccess)
    {
        return Results.BadRequest(new { error = passResult.Error });
    }

    var response = new InitiatePassResponse(
        PassId: passResult.Value!.Id,
        Message: $"Verification code sent to {MaskPhone(fullPhoneNumber)}",
        ExpiresIn: 600 // 10 minutes
    );

    return Results.Ok(response);
})
.WithName("InitiatePass")
.WithOpenApi();


// 2. Verify Phone Pass

app.MapPost("/api/pass/verify", async (
    VerifyPhoneRequest request,
    PassRepository repo,
    PhoneVerificationService phoneService,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Verifying pass {PassId}", request.PassId);

    // Get existing pass
    var passResult = await repo.GetPass(request.PassId);
    if (!passResult.IsSuccess)
    {
        return Results.NotFound(new { error = passResult.Error });
    }

    var pass = passResult.Value!;

    // Verify code (using full phone number: country code + national)
    var verifyResult = await phoneService.VerifyCode(pass.FullPhoneNumber, request.Code);
    if (!verifyResult.IsSuccess)
    {
        return Results.BadRequest(new { error = verifyResult.Error });
    }

    // Mark pass as verified (but not complete - still needs fan name)
    var updateResult = await repo.VerifyPass(request.PassId);
    if (!updateResult.IsSuccess)
    {
        return Results.BadRequest(new { error = updateResult.Error });
    }

    // Return simple success response - no fan name yet
    return Results.Ok(new
    {
        success = true,
        passId = request.PassId,
        message = "Phone verified successfully"
    });
})
.WithName("VerifyPass")
.WithOpenApi();


// 3. Complete Pass with Fan Name

app.MapPost("/api/pass/complete", async (
    CompletePassRequest request,
    PassRepository repo,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Completing pass {PassId} with fan name", request.PassId);

    // Get existing pass
    var passResult = await repo.GetPass(request.PassId);
    if (!passResult.IsSuccess)
    {
        return Results.NotFound(new { error = passResult.Error });
    }

    var pass = passResult.Value!;

    // Check if pass is verified
    if (!pass.PhoneVerified)
    {
        return Results.BadRequest(new { error = "Pass must be verified before completing" });
    }

    // Update pass with fan name
    var updateResult = await repo.CompletePass(request.PassId, request.FanName);
    if (!updateResult.IsSuccess)
    {
        return Results.BadRequest(new { error = updateResult.Error });
    }

    var updatedPass = updateResult.Value!;

    var response = new VerifyPhoneResponse(
        Success: true,
        PassId: updatedPass.Id,
        FanName: updatedPass.FanName,
        ArtistName: updatedPass.ArtistName,
        TierName: updatedPass.TierName,

        DownloadUrls: new Dictionary<string, string>
        {
            ["apple"] = $"/api/pass/{updatedPass.Id}/download?platform=apple",
            ["google"] = $"/api/pass/{updatedPass.Id}/download?platform=google"
        }
    );

    return Results.Ok(response);
})
.WithName("CompletePass")
.WithOpenApi();

// ============================================================================
// 3. Get Pass Details
// ============================================================================
app.MapGet("/api/pass/{id:guid}", async (
    Guid id,
    PassRepository repo,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Getting pass details for {PassId}", id);

    var passResult = await repo.GetPass(id);
    if (!passResult.IsSuccess)
    {
        return Results.NotFound(new { error = passResult.Error });
    }

    var pass = passResult.Value!;
    var devices = await repo.GetDevices(id);

    // Build platform info
    var platformInfo = new Dictionary<string, PlatformInfo>();

    foreach (var (key, device) in devices)
    {
        platformInfo[device.Platform] = new PlatformInfo(
            Installed: true,
            DeviceId: device.DeviceId,
            LastUpdated: device.RegisteredAt
        );
    }

    // Add missing platforms
    if (!platformInfo.ContainsKey("apple"))
    {
        platformInfo["apple"] = new PlatformInfo(Installed: false);
    }
    if (!platformInfo.ContainsKey("google"))
    {
        platformInfo["google"] = new PlatformInfo(Installed: false);
    }

    var response = new PassDetailsResponse(
        PassId: pass.Id,
        FanName: pass.FanName,
        FanId: pass.FanId,
        ArtistName: pass.ArtistName,
        TierName: pass.TierName,
        Status: pass.Status,
        PhoneVerified: pass.PhoneVerified,
        Platforms: platformInfo,
        CreatedAt: pass.CreatedAt
    );

    return Results.Ok(response);
})
.WithName("GetPassDetails")
.WithOpenApi();

// ============================================================================
// 4. Download Platform-Specific Pass
// ============================================================================
app.MapGet("/api/pass/{id:guid}/download", async (
    HttpContext context,
    Guid id,
    string? platform,  // Optional: for explicit override
    PassRepository repo,
    UserAgentService userAgentService,
    AppleWalletService appleService,
    GoogleWalletService googleService,
    ILogger<Program> logger) =>
{
    // Validate pass exists and is verified
    var passResult = await repo.GetPass(id);
    if (!passResult.IsSuccess)
    {
        return Results.NotFound(new { error = passResult.Error });
    }

    var pass = passResult.Value!;
    if (!pass.PhoneVerified)
    {
        return Results.BadRequest(new { error = "Pass must be verified before downloading" });
    }

    // Detect platform using hybrid strategy: explicit parameter > User-Agent > default
    var detection = userAgentService.DetectPlatform(context, platform);

    logger.LogInformation(
        "Pass {PassId} download: Platform={Platform}, Method={Method}, Confidence={Confidence:P0}, Source={Source}",
        id,
        detection.Platform,
        detection.DetectionMethod,
        detection.Confidence,
        detection.Source
    );

    // Warn if confidence is low (might want to show platform choice to user)
    if (detection.Confidence < 0.70 && detection.DetectionMethod != DetectionMethod.ExplicitParameter)
    {
        logger.LogWarning(
            "Low confidence platform detection ({Confidence:P0}) for pass {PassId}. Consider prompting user.",
            detection.Confidence,
            id
        );
    }

    // Select platform service using enum (type-safe polymorphism)
    WalletPassBase walletService = detection.Platform switch
    {
        Platform.Apple => appleService,
        Platform.Google => googleService,
        Platform.Unknown => appleService, // Default fallback
        _ => appleService
    };

    // Generate platform-specific pass
    var passFile = await walletService.GeneratePass(pass);

    // Google Wallet returns a redirect URL
    if (passFile.RedirectUrl is not null)
    {
        return Results.Redirect(passFile.RedirectUrl);
    }

    // Apple Wallet returns a binary file
    return Results.File(
        passFile.Data,
        passFile.ContentType,
        passFile.FileName
    );
})
.WithName("DownloadPass")
.WithOpenApi();

// ============================================================================
// 5. Send Push Notification
// ============================================================================
app.MapPost("/api/pass/{id:guid}/notify", async (
    Guid id,
    SendNotificationRequest request,
    PassRepository repo,
    AppleWalletService appleService,
    GoogleWalletService googleService,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Sending notification to pass {PassId}", id);

    // Validate pass exists
    var passResult = await repo.GetPass(id);
    if (!passResult.IsSuccess)
    {
        return Results.NotFound(new { error = passResult.Error });
    }

    // Get registered devices
    var devices = await repo.GetDevices(id);
    if (devices.Count == 0)
    {
        return Results.BadRequest(new { error = "No devices registered for this pass" });
    }

    // Send notifications to all registered devices
    var delivered = new Dictionary<string, bool>();
    var details = new Dictionary<string, NotificationDetail>();

    foreach (var (key, device) in devices)
    {
        WalletPassBase service = device.Platform.ToLower() switch
        {
            "apple" => appleService,
            "google" => googleService,
            _ => null!
        };

        if (service is null)
        {
            logger.LogWarning("Invalid platform: {Platform}", device.Platform);
            continue;
        }

        var result = await service.SendPushNotification(device.DeviceId, id, request.Message);

        delivered[device.Platform] = result.IsSuccess;
        details[device.Platform] = result.IsSuccess
            ? result.Value!
            : new NotificationDetail(
                DeviceId: device.DeviceId,
                SentAt: null,
                Status: "failed",
                Error: result.Error
            );
    }

    var response = new NotificationResponse(
        Success: delivered.Values.Any(v => v),
        Delivered: delivered,
        Details: details
    );

    return Results.Ok(response);
})
.WithName("SendNotification")
.WithOpenApi();

// ============================================================================
// 6. List Available Artists (Helper endpoint)
// ============================================================================
app.MapGet("/api/artists", (PassRepository repo) =>
{
    var artists = repo.GetAllArtists();
    return Results.Ok(artists);
})
.WithName("ListArtists")
.WithOpenApi();

// ============================================================================
// 7. Platform Requirements (Documentation endpoint)
// ============================================================================
app.MapGet("/api/platforms", (
    AppleWalletService appleService,
    GoogleWalletService googleService) =>
{
    return Results.Ok(new
    {
        apple = appleService.GetPlatformRequirements(),
        google = googleService.GetPlatformRequirements()
    });
})
.WithName("GetPlatformRequirements")
.WithOpenApi();

// ============================================================================
// 8. Detect Platform (Debugging/Analytics endpoint)
// ============================================================================
app.MapGet("/api/platform/detect", (
    HttpContext context,
    string? platform,
    UserAgentService userAgentService) =>
{
    var detection = userAgentService.DetectPlatform(context, platform);
    var platformInfo = userAgentService.GetPlatformInfo(context);

    return Results.Ok(new
    {
        // Detection result
        platform = detection.Platform.ToLowerString(),
        confidence = detection.Confidence,
        method = detection.DetectionMethod.ToString(),
        source = detection.Source,

        // User-Agent analysis
        userAgent = platformInfo.UserAgent,
        isDesktop = platformInfo.IsDesktop,
        isMobile = platformInfo.IsMobile,
        isBot = platformInfo.IsBot,
        browser = platformInfo.BrowserFamily,
        os = platformInfo.OSFamily,

        // Recommendation
        recommendation = detection.Platform == Platform.Apple
            ? "Use Apple Wallet"
            : "Use Google Wallet",

        // Warning
        shouldPromptUser = detection.Confidence < 0.70 && detection.DetectionMethod != DetectionMethod.ExplicitParameter,
        warning = detection.Confidence < 0.70 && detection.DetectionMethod != DetectionMethod.ExplicitParameter
            ? "Low confidence - consider showing platform choice to user"
            : null
    });
})
.WithName("DetectPlatform")
.WithOpenApi();

app.Run();

// ============================================================================
// Helper Functions
// ============================================================================

static string MaskPhone(string phone)
{
    if (phone.Length < 4) return "***";
    return $"{phone[..3]}***{phone[^4..]}";
}
