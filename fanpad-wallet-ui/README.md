# FanPad Wallet Pass UI

Next.js TypeScript app for creating and downloading wallet passes.

## 🚀 Quick Start

### 1. Start the Backend API

```bash
cd ../FanPad.WalletPass
dotnet run
# Running on http://localhost:5076
```

### 2. Start the Frontend

```bash
yarn dev
# Running on http://localhost:3000
```

### 3. Open in Browser

Navigate to [http://localhost:3000](http://localhost:3000)

---

## 🏗️ Project Structure

```
fanpad-wallet-ui/
├── app/
│   ├── page.tsx           # Main wallet pass UI (3-step flow)
│   ├── layout.tsx         # Root layout
│   └── globals.css        # Global styles (Tailwind)
├── lib/
│   └── api.ts             # API client for backend
└── .env.local             # Environment variables (create this)
```

---

## ⚙️ Configuration

Create `.env.local`:

```bash
NEXT_PUBLIC_API_URL=http://localhost:5076
```

---

## 🎨 Features

- ✅ **3-Step Flow**: Phone → Verify → Download
- ✅ **Auto-Platform Detection**: Backend detects iOS/Android from User-Agent
- ✅ **Manual Platform Selection**: Choose Apple or Google Wallet
- ✅ **Beautiful UI**: Tailwind CSS with gradient background
- ✅ **TypeScript**: Fully typed API client
- ✅ **Loading States**: Spinner animations
- ✅ **Error Handling**: User-friendly error messages
- ✅ **Demo Mode**: Shows verification code in UI

---

## 🔌 API Integration

The app calls these endpoints:

1. **POST /api/pass/initiate** - Send verification code
2. **POST /api/pass/verify** - Verify code and create pass
3. **GET /api/pass/{id}/download** - Download wallet pass

See [API Documentation](http://localhost:5076/scalar/v1) for details.

---

## 🎯 Usage Flow

### Step 1: Phone Number
```
User enters phone: +12125551234
 ↓
POST /api/pass/initiate
 ↓
API sends SMS code (demo: shown in UI)
```

### Step 2: Verification
```
User enters code: 123456
User enters name: John Doe
 ↓
POST /api/pass/verify
 ↓
Pass created and verified
```

### Step 3: Download
```
User clicks download button
 ↓
GET /api/pass/{id}/download
 ↓
Backend detects platform from User-Agent
 ↓
Returns .pkpass (iOS) or JWT redirect (Android)
```

---

## 🔧 Development

```bash
# Install dependencies
yarn install

# Run dev server
yarn dev

# Build for production
yarn build

# Start production server
yarn start

# Lint
yarn lint
```

---

## 📱 Testing

### Test on Desktop
1. Open http://localhost:3000
2. Backend detects macOS → defaults to Apple Wallet

### Test on Mobile
1. Start dev server with network access:
   ```bash
   yarn dev --hostname 0.0.0.0
   ```
2. Visit from phone: `http://YOUR_IP:3000`
3. Backend auto-detects iOS/Android from User-Agent

### Test Platform Selection
- Use the "Choose Platform" buttons in Step 3
- Overrides auto-detection with explicit platform

---

## 🎨 Customization

### Change Colors
Edit `app/page.tsx`:
```tsx
// Background gradient
className="bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500"

// Button colors
className="bg-indigo-600 hover:bg-indigo-700"
```

### Change Artist
Edit `lib/api.ts`:
```typescript
const VOILA_ARTIST_ID = 'your-artist-id-here';
```

---

## 🚢 Deployment

### Vercel (Recommended)
```bash
npm install -g vercel
vercel
```

Set environment variable:
```
NEXT_PUBLIC_API_URL=https://your-api-domain.com
```

### Docker
```bash
docker build -t fanpad-wallet-ui .
docker run -p 3000:3000 fanpad-wallet-ui
```

---

## 🔗 Links

- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5076
- **API Docs**: http://localhost:5076/scalar/v1
- **Next.js Docs**: https://nextjs.org/docs
- **Tailwind Docs**: https://tailwindcss.com/docs

---

## 📝 Notes

- **Demo Mode**: Verification codes are shown in UI for testing
- **CORS**: Backend already configured to allow frontend requests
- **Platform Detection**: Uses UserAgentService on backend
- **Production**: Remove demo code display and use real SMS service
