# UserAgentService Design - Self-Contained Platform Detection

## Design Philosophy

**Design Decision:** Build backend platform detection rather than requiring frontend to explicitly pass platform information.

**Rationale:** In a production wallet pass system, users access download links from multiple contexts:
- **QR codes** - Scanned by camera apps (no frontend)
- **Email links** - Clicked from email clients (no frontend)
- **SMS messages** - Direct links from verification texts (no frontend)
- **Social media shares** - Links posted/shared by other users (no frontend)
- **Direct API access** - Third-party integrations (no frontend control)

Relying on a frontend to detect and pass the platform would fail in all these scenarios. The backend must be self-sufficient.

**Solution:** Backend analyzes HTTP User-Agent headers to intelligently detect whether to serve Apple Wallet (.pkpass) or Google Wallet (JWT URL).

**Key Benefits:**
- **Universal compatibility** - Works with or without a frontend
- **QR code friendly** - Users scan code, get correct pass automatically
- **Email integration** - Transactional emails with pass links just work
- **SMS verification flow** - "Click here to download" works on any device
- **Third-party integrations** - Partners can link directly to our API
- **Consistent logic** - One place for detection rules, easier to maintain and update
- **Testable** - Override parameter allows testing both platforms from any device

---

## Architecture

```
HTTP Request
    ↓
User-Agent: Mozilla/5.0 (iPhone; CPU iPhone OS...
    ↓
┌─────────────────────────────────────────┐
│      UserAgentService                   │
│                                         │
│  DetectPlatform(HttpContext)            │
│      ↓                                  │
│  AnalyzeUserAgent(userAgent)            │
│      ↓                                  │
│  Platform enum { Apple, Google }        │
└─────────────────────────────────────────┘
    ↓
┌──────────────────────────────────────────┐
│  Switch on Platform enum                 │
│                                          │
│  Platform.Apple  → AppleWalletService    │
│  Platform.Google → GoogleWalletService   │
└──────────────────────────────────────────┘
    ↓
Generate platform-specific pass
```

---

## Implementation Files

### 1. **`Models/Platform.cs`**

**Purpose:** Type-safe enum for platforms

```csharp
public enum Platform
{
    Apple,    // iOS, watchOS, macOS
    Google,   // Android
    Unknown   // Fallback
}

// Extension methods
public static class PlatformExtensions
{
    public static string ToLowerString(this Platform platform);
    public static Platform FromString(string platformString);
}
```

**Why enum vs string:**
- Type-safe (compile-time checking)
- Better IDE support (autocomplete)
- Easier to add platforms (add one enum value)
- Clear API (no magic strings)

---

### 2. **`Services/UserAgentService.cs`**

**Purpose:** Analyze HTTP headers to determine platform

**Key Methods:**

```csharp
public class UserAgentService
{
    // Main detection method
    public Platform DetectPlatform(
        HttpContext context, 
        string? overridePlatform = null
    );
    
    // Detailed analytics
    public UserAgentInfo GetPlatformInfo(HttpContext context);
    
    // Internal rules engine
    private Platform AnalyzeUserAgent(string userAgent);
}
```

**Detection Rules (Priority Order):**

1. **Override** - Explicit platform parameter (for testing)
2. **iOS Devices** - iPhone, iPad, iPod → Apple
3. **Android Devices** - Android in UA → Google
4. **Desktop macOS** - Likely iPhone user → Apple
5. **Desktop Chrome** - Slight preference → Google
6. **Safari Browser** - Likely Apple user → Apple
7. **Bots/Crawlers** - Testing fallback → Apple
8. **Default** - Unknown → Apple (60% US market share)

---

### 3. **Service Integration in `Program.cs`**

**Service Registration:**

The UserAgentService is registered as a singleton in the DI container:

```csharp
builder.Services.AddSingleton<UserAgentService>();
```

**Download Endpoint Integration:**

The download endpoint uses UserAgentService to detect the platform from HTTP headers:

```csharp
app.MapGet("/api/pass/{id:guid}/download", async (
    HttpContext context,              // HTTP context for headers
    Guid id,
    string? platform,                 // Optional override for testing
    UserAgentService userAgentService, // Injected service
    // ...
) => {
    // Detect platform from User-Agent header
    var detectedPlatform = userAgentService.DetectPlatform(context, platform);
    
    // Select appropriate wallet service
    WalletPassBase service = detectedPlatform switch
    {
        Platform.Apple => appleService,
        Platform.Google => googleService,
        Platform.Unknown => appleService,  // Default to Apple
        _ => appleService
    };
    
    // Generate pass using selected service
    var passFile = await service.GeneratePass(pass);
    // ...
});
```

**Debug Endpoint:**

A debug endpoint provides detailed platform detection information:

```csharp
app.MapGet("/api/platform/detect", (
    HttpContext context,
    UserAgentService userAgentService
) => {
    var info = userAgentService.GetPlatformInfo(context);
    return Results.Ok(new {
        detected = info.DetectedPlatform,
        userAgent = info.UserAgent,
        isDesktop = info.IsDesktop,
        isMobile = info.IsMobile,
        browser = info.BrowserFamily,
        os = info.OperatingSystem
    });
});
```

---

## Usage Examples

### Automatic Detection (Most Common)

```bash
# iPhone user clicks link
curl -H "User-Agent: Mozilla/5.0 (iPhone; CPU iPhone OS 17_0...)" \
  http://localhost:5076/api/pass/123/download

# Backend detects: Platform.Apple
# Returns: .pkpass file
```

```bash
# Android user clicks link
curl -H "User-Agent: Mozilla/5.0 (Linux; Android 13; Pixel 7...)" \
  http://localhost:5076/api/pass/123/download

# Backend detects: Platform.Google
# Returns: Redirect to pay.google.com
```

---

### Override for Testing

```bash
# Force Google Wallet (even from iPhone)
curl -H "User-Agent: Mozilla/5.0 (iPhone...)" \
  http://localhost:5076/api/pass/123/download?platform=google

# Override parameter takes precedence
# Returns: Google Wallet URL
```

---

### Debug Detection

```bash
# See what platform would be detected
curl -H "User-Agent: Mozilla/5.0 (Macintosh; Intel Mac OS X...)" \
  http://localhost:5076/api/platform/detect

# Response:
{
  "detected": "apple",
  "userAgent": "Mozilla/5.0 (Macintosh...",
  "isDesktop": true,
  "isMobile": false,
  "isBot": false,
  "browser": "Chrome",
  "os": "macOS",
  "recommendation": "Use Apple Wallet"
}
```

---

## Detection Rules Deep Dive

### Rule 1: iOS Devices (High Confidence)

```csharp
if (ContainsAny(ua, "iphone", "ipad", "ipod"))
{
    return Platform.Apple;
}
```

**Examples:**
```
Mozilla/5.0 (iPhone; CPU iPhone OS 17_0...) → Apple
Mozilla/5.0 (iPad; CPU OS 16_5...) → Apple
```

**Confidence:** 100% - iOS devices ONLY support Apple Wallet

---

### Rule 2: Android Devices (High Confidence)

```csharp
if (ua.Contains("android"))
{
    return Platform.Google;
}
```

**Examples:**
```
Mozilla/5.0 (Linux; Android 13; Pixel 7...) → Google
```

**Confidence:** 100% - Android devices ONLY support Google Wallet

---

### Rule 3: Desktop macOS (Medium Confidence)

```csharp
if (ContainsAny(ua, "macintosh", "mac os x") && !ua.Contains("android"))
{
    return Platform.Apple;
}
```

**Reasoning:** macOS users likely have iPhones (Apple ecosystem)

**Examples:**
```
Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7...) → Apple
```

**Confidence:** ~80% - Most Mac users have iPhones

---

### Rule 4: Windows + Chrome (Low Confidence)

```csharp
if (ua.Contains("windows") && ua.Contains("chrome") && !ua.Contains("edge"))
{
    return Platform.Google;
}
```

**Reasoning:** Chrome users might prefer Google services

**Examples:**
```
Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36... Chrome/119.0... → Google
```

**Confidence:** ~55% - Slight preference, could go either way

---

### Rule 5: Safari Browser (Medium Confidence)

```csharp
if (ua.Contains("safari") && !ua.Contains("chrome"))
{
    return Platform.Apple;
}
```

**Reasoning:** Safari users likely in Apple ecosystem

**Examples:**
```
Mozilla/5.0 (Macintosh...) Version/17.0 Safari/605.1.15 → Apple
```

**Confidence:** ~75% - Safari is Apple's browser

---

## Testing Strategy

### Unit Tests

```csharp
[Theory]
[InlineData("iPhone", Platform.Apple)]
[InlineData("Android", Platform.Google)]
[InlineData("Macintosh", Platform.Apple)]
public void DetectPlatform_WithUserAgent_ReturnsCorrectPlatform(
    string userAgent, 
    Platform expected)
{
    // Arrange
    var service = new UserAgentService(Mock.Of<ILogger>());
    var context = CreateContextWithUserAgent(userAgent);
    
    // Act
    var result = service.DetectPlatform(context);
    
    // Assert
    result.Should().Be(expected);
}
```

### Integration Tests

```csharp
[Fact]
public async Task DownloadPass_WithiPhoneUserAgent_ReturnsApplePass()
{
    // Arrange
    var client = _factory.CreateClient();
    var passId = await CreateVerifiedPass();
    
    // Set User-Agent header
    client.DefaultRequestHeaders.Add(
        "User-Agent", 
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0...)"
    );
    
    // Act
    var response = await client.GetAsync($"/api/pass/{passId}/download");
    
    // Assert
    response.Content.Headers.ContentType?.MediaType
        .Should().Be("application/vnd.apple.pkpass");
}
```

---

## Architecture Benefits

| Capability | Implementation | Benefit |
|------------|---------------|---------|
| **QR codes** | Direct API link | User scans → gets correct pass (no redirect needed) |
| **Email links** | Direct API link | Transactional emails work on any device |
| **SMS links** | Direct API link | Verification texts include working download links |
| **Frontend usage** | Simple `<a>` tag | No JavaScript detection logic needed |
| **Testing** | Override parameter | Can test both platforms from any device |
| **API integrations** | Self-contained | Partners don't need to implement detection |
| **Updates** | Backend only | Change detection rules without client updates |
| **Consistency** | Single source of truth | All consumers use same detection logic |

---

## Implementation Simplicity

### Frontend Code (When Used)

```typescript
// Simple link - no platform detection needed
function downloadPass(passId: string) {
  window.location.href = `/api/pass/${passId}/download`;
  // Backend automatically detects platform from User-Agent
}
```

**Or just a plain HTML link:**
```html
<a href="/api/pass/123/download">
  Add to Wallet
</a>
```

### Email Template Example

```html
<!-- Works perfectly in email clients -->
<p>Your VOILÀ Magician Pass is ready!</p>
<a href="https://api.fanpad.com/api/pass/{{passId}}/download" 
   style="display: inline-block; padding: 12px 24px; background: #000; color: #fff;">
  Add to Wallet
</a>
```

When user clicks from iPhone → receives Apple Wallet pass  
When user clicks from Android → redirects to Google Wallet

### QR Code Example

```
QR Code Content: https://api.fanpad.com/api/pass/123/download
```

User scans with:
- iPhone Camera → Opens Safari → Detects iOS → Downloads .pkpass → "Add to Apple Wallet?"
- Android Camera → Opens Chrome → Detects Android → Redirects → "Save to Google Wallet?"

---

## User Flows

### Flow 1: QR Code (No Frontend)

```
User scans QR code
    ↓
QR contains: https://api.fanpad.com/api/pass/123/download
    ↓
iOS Camera app opens Safari
    ↓
Safari sends request with User-Agent: ...iPhone...
    ↓
UserAgentService detects Platform.Apple
    ↓
Returns .pkpass file
    ↓
iOS prompts: "Add to Apple Wallet?"
```

---

### Flow 2: Email Link

```
User receives email with link
    ↓
<a href="https://api.fanpad.com/api/pass/123/download">
  Add to Wallet
</a>
    ↓
User taps on Android phone
    ↓
Chrome sends request with User-Agent: ...Android...
    ↓
UserAgentService detects Platform.Google
    ↓
Redirects to pay.google.com/gp/v/save/...
    ↓
Google Wallet opens automatically
```

---

### Flow 3: Web App with Override

```
User visits web app on desktop
    ↓
Desktop: Hard to detect user's phone platform
    ↓
Show two buttons:
  [ Add to Apple Wallet ]  [ Add to Google Wallet ]
    ↓
User clicks "Add to Apple Wallet"
    ↓
Frontend: /api/pass/123/download?platform=apple
    ↓
Override parameter forces Apple
    ↓
Downloads .pkpass file
```


---

## Summary

**Implementation Highlights:**
- Self-contained platform detection service
- Rule-based User-Agent analysis
- Type-safe Platform enum
- Optional override parameter for testing
- Debug endpoint for platform information

**Key Benefits:**
- Works with QR codes and direct links
- Simpler frontend (just a link!)
- Consistent detection logic
- Easy to update rules
- Comprehensive logging

**Best Practices Demonstrated:**
- Single Responsibility (service does one thing)
- Dependency Injection (testable)
- Type safety (enum vs strings)
- Logging for observability
- Override for flexibility

---

## Additional Context

For complete implementation details, see:
- **FanPad.WalletPass/Services/UserAgentService.cs** - Service implementation
- **FanPad.WalletPass.Tests/UserAgentServiceTests.cs** - 24 comprehensive unit tests
- **FanPad.WalletPass/Models/Platform.cs** - Platform enum and extensions
- **README.md** - Full architecture documentation
