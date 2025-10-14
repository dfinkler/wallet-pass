namespace FanPad.WalletPass.Models;

/// <summary>
/// Response models for API endpoints
/// </summary>

public record InitiatePassResponse(
    Guid PassId,
    string Message,
    int ExpiresIn
);

public record VerifyPhoneResponse(
    bool Success,
    Guid PassId,
    string FanName,
    string ArtistName,
    string TierName,
    Dictionary<string, string> DownloadUrls
);

public record PassDetailsResponse(
    Guid PassId,
    string FanName,
    string FanId,
    string ArtistName,
    string TierName,
    PassStatus Status,
    bool PhoneVerified,
    Dictionary<string, PlatformInfo> Platforms,
    DateTime CreatedAt
);

public record PlatformInfo(
    bool Installed,
    string? DeviceId = null,
    DateTime? LastUpdated = null
);

public record NotificationResponse(
    bool Success,
    Dictionary<string, bool> Delivered,
    Dictionary<string, NotificationDetail> Details
);

public record NotificationDetail(
    string? DeviceId,
    DateTime? SentAt,
    string Status,
    string? Error = null
);

public record PassFile(
    string FileName,
    string ContentType,
    byte[] Data,
    string? RedirectUrl = null
);

