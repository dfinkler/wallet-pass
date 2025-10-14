"use client"

import { useState } from "react"
import { api } from "@/lib/api"
import PhoneStep from "@/components/PhoneStep"
import VerifyStep from "@/components/VerifyStep"
import CompleteStep from "@/components/CompleteStep"
import DoneStep from "@/components/DoneStep"
import PassCard from "@/components/PassCard"
import { XCircle, KeyRound } from "lucide-react"

type Step = "phone" | "verify" | "complete" | "done"

export default function Home() {
  const [phoneNumber, setPhoneNumber] = useState("")
  const [verificationCode, setVerificationCode] = useState("")
  const [fanName, setFanName] = useState("")
  const [passId, setPassId] = useState<string | null>(null)
  const [step, setStep] = useState<Step>("phone")
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [demoCode, setDemoCode] = useState<string | null>(null)
  const [countryCode, setCountryCode] = useState("+1")

  const handleInitiate = async () => {
    setLoading(true)
    setError(null)

    try {
      // Send country code and phone separately for analytics
      const data = await api.initiatePass(countryCode, phoneNumber)
      setPassId(data.passId)
      setDemoCode(data.verificationCode || null)
      setStep("verify")
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unknown error")
    } finally {
      setLoading(false)
    }
  }

  const handleVerifyCode = async () => {
    if (!passId) return

    setLoading(true)
    setError(null)

    try {
      await api.verifyCode(passId, verificationCode)
      setStep("complete")
    } catch (err) {
      setError(err instanceof Error ? err.message : "Invalid verification code")
    } finally {
      setLoading(false)
    }
  }

  const handleComplete = async () => {
    if (!passId) return

    setLoading(true)
    setError(null)

    try {
      await api.completePass(passId, fanName)
      setStep("done")
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to complete pass")
    } finally {
      setLoading(false)
    }
  }

  const handleDownload = (platform?: "apple" | "google") => {
    if (!passId) return
    window.location.href = api.getDownloadUrl(passId, platform)
  }

  const handleReset = () => {
    setPhoneNumber("")
    setVerificationCode("")
    setFanName("")
    setPassId(null)
    setDemoCode(null)
    setError(null)
    setStep("phone")
  }

  // NORMAL FLOW: Phone, Verify, Done
  return (
    <div className="min-h-screen max-w-md mx-auto bg-black text-white flex flex-col my-10 bg-gradient-to-b from-gray-800 to-black">
      {step === "complete" && (
        <CompleteStep
          fanName={fanName}
          setFanName={setFanName}
          loading={loading}
          error={error}
          onBack={handleReset}
          onSubmit={handleComplete}
        />
      )}
      {/* Hero Section with Background Image */}
      <div className="relative h-64 overflow-hidden">
        <div
          className="absolute inset-0 bg-no-repeat bg-center opacity-40"
          style={{
            backgroundImage: "url('/voila.png')",
            backgroundSize: "cover",
            backgroundPosition: "center",
          }}
        ></div>
      </div>

      {/* Main Heading */}
      <div className="mb-6 p-4">
        <h1 className="text-3xl font-bold text-white mb-2">
          Your official fan pass
        </h1>
        <p className="text-gray-400 text-base">
          Get exclusive updates and perks before anyone else.
        </p>
      </div>

      {/* Error Alert */}
      {error && (
        <div className="mb-4 p-4 bg-red-900/50 border border-red-700 rounded-xl flex items-center gap-3">
          <XCircle className="w-5 h-5 text-red-400 flex-shrink-0" />
          <p className="text-red-200 text-sm">{error}</p>
        </div>
      )}

      {/* Demo Code Alert */}
      {demoCode && step === "verify" && (
        <div className="mb-4 p-4 bg-amber-900/50 border border-amber-700 rounded-xl flex items-center gap-3">
          <KeyRound className="w-5 h-5 text-amber-400 flex-shrink-0" />
          <p className="text-amber-200 text-sm font-medium">
            Demo Code: <span className="font-mono text-lg">{demoCode}</span>
          </p>
        </div>
      )}

      {/* Fan Pass Card */}
      <PassCard fanName={fanName} passId={passId} />

      {/* Phone Input Step */}
      {step === "phone" && (
        <PhoneStep
          phoneNumber={phoneNumber}
          setPhoneNumber={setPhoneNumber}
          countryCode={countryCode}
          setCountryCode={setCountryCode}
          loading={loading}
          onSubmit={handleInitiate}
        />
      )}

      {/* Verification Step */}
      {step === "verify" && (
        <VerifyStep
          verificationCode={verificationCode}
          setVerificationCode={setVerificationCode}
          loading={loading}
          onSubmit={handleVerifyCode}
        />
      )}

      {/* Download Step */}
      {step === "done" && <DoneStep onDownload={handleDownload} />}

      {/* Footer */}
      <div className="text-center py-6 text-gray-600 text-xs">
        <p>fanpad.xyz</p>
      </div>
    </div>
  )
}
