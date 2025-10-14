using FanPad.WalletPass.Models;

namespace FanPad.WalletPass.Services;

/// <summary>
/// Abstract base class for wallet pass generation and management.
/// This is the KEY ARCHITECTURAL PATTERN for handling platform differences.
/// 
/// Design principle: Each platform (Apple/Google) has fundamentally different
/// approaches to wallet passes. This abstraction provides:
/// 1. Consistent API interface for clients
/// 2. Isolation of platform-specific complexity
/// 3. Extensibility for future platforms (Samsung Wallet, etc.)
/// </summary>
public abstract class WalletPassBase
{
    /// <summary>
    /// Platform identifier (apple, google, samsung, etc.)
    /// </summary>
    public abstract string PlatformName { get; }

    /// <summary>
    /// Generate platform-specific pass file or URL
    /// 
    /// Apple Wallet: Returns .pkpass binary file (signed ZIP archive)
    /// Google Wallet: Returns save-to-wallet URL with JWT token
    /// 
    /// This abstraction allows the API layer to remain platform-agnostic
    /// while each implementation handles its own complexities.
    /// </summary>
    /// <param name="data">Pass data to include in the wallet pass</param>
    /// <returns>PassFile with platform-specific content</returns>
    public abstract Task<PassFile> GeneratePass(PassData data);

    /// <summary>
    /// Send push notification to update pass content
    /// 
    /// Apple Wallet: Uses APNs (Apple Push Notification service)
    ///   - Sends empty push to device
    ///   - Device fetches updated pass from your API
    ///   - Requires pass type identifier and device push token
    /// 
    /// Google Wallet: Uses REST API to directly update pass object
    ///   - PATCH request to walletobjects.googleapis.com
    ///   - Changes propagate to all devices automatically
    ///   - No device-specific tokens needed
    /// </summary>
    /// <param name="deviceId">Platform-specific device identifier</param>
    /// <param name="passId">Pass identifier</param>
    /// <param name="message">Notification message</param>
    /// <returns>Result indicating success/failure</returns>
    public abstract Task<Result<NotificationDetail>> SendPushNotification(
        string deviceId,
        Guid passId,
        string message
    );

    /// <summary>
    /// Get platform-specific requirements for production deployment
    /// 
    /// This method returns metadata about what's needed to actually implement
    /// this platform in production. Useful for planning and documentation.
    /// </summary>
    /// <returns>Dictionary of requirement keys and descriptions</returns>
    public abstract Dictionary<string, object> GetPlatformRequirements();

    /// <summary>
    /// Handle device registration callback from wallet provider
    /// 
    /// Apple Wallet: Device POSTs to your API with push token
    /// Google Wallet: Handled automatically by Google
    /// </summary>
    /// <param name="passId">Pass identifier</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="pushToken">Platform-specific push token (optional)</param>
    /// <returns>Success indicator</returns>
    public abstract Task<bool> RegisterDevice(
        Guid passId,
        string deviceId,
        string? pushToken = null
    );
}

