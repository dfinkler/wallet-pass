// API client for FanPad Wallet Pass backend

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5076"
const VOILA_ARTIST_ID = "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890"

export interface InitiatePassResponse {
  passId: string
  status: string
  message: string
  verificationCode?: string // Demo only
}

export interface VerifyCodeResponse {
  success: boolean
  passId: string
  message: string
}

export interface CompletePassResponse {
  success: boolean
  passId: string
  fanName: string
  artistName: string
  tierName: string
  downloadUrls: {
    apple: string
    google: string
  }
}

export const api = {
  async initiatePass(countryCode: string, phone: string) {
    const response = await fetch(`${API_BASE}/api/pass/initiate`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        countryCode,
        phone,
        artistId: VOILA_ARTIST_ID,
      }),
    })

    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.message || "Failed to initiate pass")
    }

    return response.json() as Promise<InitiatePassResponse>
  },

  async verifyCode(passId: string, code: string) {
    const response = await fetch(`${API_BASE}/api/pass/verify`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        passId,
        code,
      }),
    })

    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.error || "Invalid verification code")
    }

    return response.json() as Promise<VerifyCodeResponse>
  },

  async completePass(passId: string, fanName: string) {
    const response = await fetch(`${API_BASE}/api/pass/complete`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        passId,
        fanName,
      }),
    })

    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.error || "Failed to complete pass")
    }

    return response.json() as Promise<CompletePassResponse>
  },

  getDownloadUrl(passId: string, platform?: "apple" | "google") {
    const url = new URL(`${API_BASE}/api/pass/${passId}/download`)
    if (platform) {
      url.searchParams.set("platform", platform)
    }
    return url.toString()
  },
}
