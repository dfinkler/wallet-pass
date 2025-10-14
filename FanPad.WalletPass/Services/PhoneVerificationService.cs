using FanPad.WalletPass.Models;
using System.Text.RegularExpressions;

namespace FanPad.WalletPass.Services;

/// <summary>
/// Service for phone number verification via SMS
/// 
/// PRODUCTION IMPLEMENTATION:
/// - AWS SNS (Simple Notification Service) for SMS
/// - Twilio SMS API
/// - Rate limiting (5 requests/hour per phone)
/// - Code expiration (10 minutes)
/// - Attempt tracking (max 3 attempts)
/// </summary>
public partial class PhoneVerificationService
{
    private readonly ILogger<PhoneVerificationService> _logger;
    private readonly Dictionary<string, VerificationCode> _codes = new(); // In-memory for demo

    public PhoneVerificationService(ILogger<PhoneVerificationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initiate phone verification by sending SMS code
    /// </summary>
    public async Task<Result<string>> SendVerificationCode(string phoneNumber)
    {
        // Validate phone number format (E.164)
        if (!IsValidPhoneNumber(phoneNumber))
        {
            return Result<string>.Failure("Invalid phone number format. Use E.164 format (e.g., +12125551234)");
        }

        // Generate 6-digit code
        var code = GenerateCode();
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        // Store code (in production: Redis with TTL)
        _codes[phoneNumber] = new VerificationCode(code, expiresAt, 0);

        // PRODUCTION: Send SMS via AWS SNS or Twilio
        // 
        // AWS SNS Example:
        // var snsClient = new AmazonSimpleNotificationServiceClient();
        // await snsClient.PublishAsync(new PublishRequest
        // {
        //     PhoneNumber = phoneNumber,
        //     Message = $"Your FanPad verification code is: {code}",
        //     MessageAttributes = new Dictionary<string, MessageAttributeValue>
        //     {
        //         ["AWS.SNS.SMS.SenderID"] = new() { StringValue = "FanPad", DataType = "String" },
        //         ["AWS.SNS.SMS.SMSType"] = new() { StringValue = "Transactional", DataType = "String" }
        //     }
        // });
        //
        // Twilio Example:
        // var twilioClient = new TwilioRestClient(accountSid, authToken);
        // await MessageResource.CreateAsync(
        //     to: new PhoneNumber(phoneNumber),
        //     from: new PhoneNumber(twilioPhoneNumber),
        //     body: $"Your FanPad verification code is: {code}"
        // );

        _logger.LogInformation(
            "Verification code sent to {Phone} (Code: {Code})",
            MaskPhoneNumber(phoneNumber),
            code
        );

        // Stub: Simulate async SMS sending
        await Task.Delay(100);

        return Result<string>.Success(code); // In production: return success without the code
    }

    /// <summary>
    /// Verify the code entered by user
    /// </summary>
    public async Task<Result<bool>> VerifyCode(string phoneNumber, string code)
    {
        if (!_codes.TryGetValue(phoneNumber, out var storedCode))
        {
            return Result<bool>.Failure("No verification code found for this phone number");
        }

        // Check expiration
        if (DateTime.UtcNow > storedCode.ExpiresAt)
        {
            _codes.Remove(phoneNumber);
            return Result<bool>.Failure("Verification code has expired");
        }

        // Check attempts (max 3)
        if (storedCode.Attempts >= 3)
        {
            _codes.Remove(phoneNumber);
            return Result<bool>.Failure("Maximum verification attempts exceeded");
        }

        // Verify code
        if (storedCode.Code != code)
        {
            // Increment attempts
            _codes[phoneNumber] = storedCode with { Attempts = storedCode.Attempts + 1 };

            var remainingAttempts = 3 - (storedCode.Attempts + 1);
            return Result<bool>.Failure($"Invalid code. {remainingAttempts} attempts remaining");
        }

        // Success - remove code
        _codes.Remove(phoneNumber);

        _logger.LogInformation("Phone number {Phone} verified successfully", MaskPhoneNumber(phoneNumber));

        await Task.CompletedTask;
        return Result<bool>.Success(true);
    }

    // Validation: E.164 phone number format
    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        return PhoneRegex().IsMatch(phoneNumber);
    }

    // Generate random 6-digit code
    private static string GenerateCode()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }

    // Mask phone number for logging (security)
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (phoneNumber.Length < 4)
            return "***";

        return $"{phoneNumber[..3]}***{phoneNumber[^4..]}";
    }

    // Regex for E.164 phone validation
    [GeneratedRegex(@"^\+[1-9]\d{1,14}$")]
    private static partial Regex PhoneRegex();
}

/// <summary>
/// Verification code storage record
/// </summary>
internal record VerificationCode(string Code, DateTime ExpiresAt, int Attempts);

