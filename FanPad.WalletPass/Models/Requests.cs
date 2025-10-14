namespace FanPad.WalletPass.Models;

/// <summary>
/// Request models for API endpoints
/// Using records for immutability and built-in validation
/// Think of these as TypeScript interfaces
/// </summary>

public record InitiatePassRequest(
    string CountryCode,    // e.g., "+1", "+44"
    string Phone,          // National number only, e.g., "5551234567"
    Guid ArtistId
);

public record VerifyPhoneRequest(
    Guid PassId,
    string Code
);

public record CompletePassRequest(
    Guid PassId,
    string FanName
);

public record SendNotificationRequest(
    string Message,
    string? ActionUrl = null,
    string Priority = "normal"
);

