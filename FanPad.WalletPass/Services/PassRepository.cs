using FanPad.WalletPass.Models;
using System.Collections.Concurrent;

namespace FanPad.WalletPass.Services;

/// <summary>
/// In-memory repository for pass data
/// 
/// PRODUCTION: Replace with Entity Framework Core + PostgreSQL
/// - PassHolder entity with encrypted PII fields
/// - Phone number hashing for lookups
/// - Audit logging for all operations
/// - Indexes on frequently queried fields
/// </summary>
public class PassRepository
{
    private readonly ILogger<PassRepository> _logger;
    private readonly ConcurrentDictionary<Guid, PassData> _passes = new();
    private readonly ConcurrentDictionary<Guid, Dictionary<string, DeviceRegistration>> _devices = new();

    // Sample data - VOILÀ artist
    private static readonly ArtistTemplate VoilaTemplate = new(
        Id: Guid.Parse("a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890"),
        Name: "VOILÀ",
        TierName: "Magician Pass",
        LogoUrl: "https://placehold.co/400x400/6366f1/white?text=VOILA",
        BackgroundUrl: "https://placehold.co/600x400/4338ca/white?text=Magician+Pass"
    );

    public PassRepository(ILogger<PassRepository> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create a new pass (initiated but not yet verified)
    /// </summary>
    public async Task<Result<PassData>> CreatePass(string countryCode, string nationalNumber, Guid artistId)
    {
        // Validate artist exists
        var artist = GetArtistTemplate(artistId);
        if (artist is null)
        {
            return Result<PassData>.Failure($"Artist with ID {artistId} not found");
        }

        // Generate fan ID (7-digit)
        var fanId = GenerateFanId();

        var passData = new PassData(
            Id: Guid.NewGuid(),
            FanName: "Pending", // Will be set during verification
            CountryCode: countryCode,
            NationalNumber: nationalNumber,
            FanId: fanId,
            ArtistName: artist.Name,
            TierName: artist.TierName,
            LogoUrl: artist.LogoUrl,
            BackgroundUrl: artist.BackgroundUrl,
            Status: PassStatus.Pending,
            PhoneVerified: false,
            CreatedAt: DateTime.UtcNow,
            VerifiedAt: null
        );

        _passes[passData.Id] = passData;
        _devices[passData.Id] = new Dictionary<string, DeviceRegistration>();

        _logger.LogInformation(
            "Created pass {PassId} for artist {Artist} (Country: {CountryCode})",
            passData.Id,
            artist.Name,
            countryCode
        );

        await Task.CompletedTask;
        return Result<PassData>.Success(passData);
    }

    /// <summary>
    /// Update pass after phone verification
    /// </summary>
    public async Task<Result<PassData>> VerifyPass(Guid passId)
    {
        if (!_passes.TryGetValue(passId, out var existingPass))
        {
            return Result<PassData>.Failure($"Pass {passId} not found");
        }

        if (existingPass.PhoneVerified)
        {
            return Result<PassData>.Failure("Pass already verified");
        }

        var updatedPass = existingPass with
        {
            PhoneVerified = true,
            Status = PassStatus.Active,
            VerifiedAt = DateTime.UtcNow
        };

        _passes[passId] = updatedPass;

        _logger.LogInformation(
            "Pass {PassId} verified",
            passId
        );

        await Task.CompletedTask;
        return Result<PassData>.Success(updatedPass);
    }

    /// <summary>
    /// Complete pass with fan name
    /// </summary>
    public async Task<Result<PassData>> CompletePass(Guid passId, string fanName)
    {
        if (!_passes.TryGetValue(passId, out var existingPass))
        {
            return Result<PassData>.Failure($"Pass {passId} not found");
        }

        var updatedPass = existingPass with
        {
            FanName = fanName
        };

        _passes[passId] = updatedPass;

        _logger.LogInformation(
            "Pass {PassId} completed with fan name {FanName}",
            passId,
            fanName
        );

        await Task.CompletedTask;
        return Result<PassData>.Success(updatedPass);
    }


    /// <summary>
    /// Get pass by ID
    /// </summary>
    public async Task<Result<PassData>> GetPass(Guid passId)
    {
        if (!_passes.TryGetValue(passId, out var pass))
        {
            return Result<PassData>.Failure($"Pass {passId} not found");
        }

        await Task.CompletedTask;
        return Result<PassData>.Success(pass);
    }

    /// <summary>
    /// Register a device for push notifications
    /// </summary>
    public async Task<bool> RegisterDevice(Guid passId, string platform, string deviceId, string? pushToken = null)
    {
        if (!_devices.TryGetValue(passId, out var deviceDict))
        {
            _logger.LogWarning("Cannot register device for non-existent pass {PassId}", passId);
            return false;
        }

        var registration = new DeviceRegistration(
            Platform: platform,
            DeviceId: deviceId,
            PushToken: pushToken,
            RegisteredAt: DateTime.UtcNow
        );

        var key = $"{platform}:{deviceId}";
        deviceDict[key] = registration;

        _logger.LogInformation(
            "Registered {Platform} device {DeviceId} for pass {PassId}",
            platform,
            deviceId,
            passId
        );

        await Task.CompletedTask;
        return true;
    }

    /// <summary>
    /// Get all devices registered for a pass
    /// </summary>
    public async Task<Dictionary<string, DeviceRegistration>> GetDevices(Guid passId)
    {
        if (!_devices.TryGetValue(passId, out var deviceDict))
        {
            return new Dictionary<string, DeviceRegistration>();
        }

        await Task.CompletedTask;
        return new Dictionary<string, DeviceRegistration>(deviceDict);
    }

    /// <summary>
    /// Get artist template by ID
    /// </summary>
    public ArtistTemplate? GetArtistTemplate(Guid artistId)
    {
        // In production: Query from database
        // For demo: Only VOILÀ template available
        return artistId == VoilaTemplate.Id ? VoilaTemplate : null;
    }

    /// <summary>
    /// Get all available artists (for demo)
    /// </summary>
    public List<ArtistTemplate> GetAllArtists()
    {
        return new List<ArtistTemplate> { VoilaTemplate };
    }

    // Generate random 7-digit fan ID
    private static string GenerateFanId()
    {
        return Random.Shared.Next(1000000, 9999999).ToString();
    }
}

/// <summary>
/// Artist template record
/// </summary>
public record ArtistTemplate(
    Guid Id,
    string Name,
    string TierName,
    string LogoUrl,
    string? BackgroundUrl
);

/// <summary>
/// Device registration record
/// </summary>
public record DeviceRegistration(
    string Platform,
    string DeviceId,
    string? PushToken,
    DateTime RegisteredAt
);

