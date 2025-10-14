namespace FanPad.WalletPass.Models;

/// <summary>
/// Core domain model representing a fan's wallet pass
/// Using record for immutability (similar to TypeScript const objects)
/// </summary>
public record PassData(
    Guid Id,
    string FanName,
    string CountryCode,        // e.g., "+1", "+44" (NOT encrypted - for analytics)
    string NationalNumber,     // National number only (in production: encrypted)
    string FanId,
    string ArtistName,
    string TierName,
    string LogoUrl,
    string? BackgroundUrl,
    PassStatus Status,
    bool PhoneVerified,
    DateTime CreatedAt,
    DateTime? VerifiedAt
)
{
    // Computed property for backward compatibility and display
    public string FullPhoneNumber => $"{CountryCode}{NationalNumber}";
};

/// <summary>
/// Pass status enum (like TypeScript union types)
/// </summary>
public enum PassStatus
{
    Pending,
    Active,
    Deleted
}

