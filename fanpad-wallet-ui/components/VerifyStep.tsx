import { Loader2 } from "lucide-react"

interface VerifyStepProps {
  verificationCode: string
  setVerificationCode: (value: string) => void
  loading: boolean
  onSubmit: () => void
}

export default function VerifyStep({
  verificationCode,
  setVerificationCode,
  loading,
  onSubmit,
}: VerifyStepProps) {
  return (
    <div className="space-y-4">
      <input
        type="text"
        value={verificationCode}
        onChange={(e) =>
          setVerificationCode(e.target.value.replace(/\D/g, "").slice(0, 6))
        }
        placeholder="Enter 6-digit code"
        maxLength={6}
        className="w-full bg-gray-900 text-white placeholder-gray-600 px-6 py-4 rounded-full text-center text-2xl font-mono tracking-widest border border-gray-700 focus:outline-none focus:border-gray-500"
      />

      <button
        onClick={onSubmit}
        disabled={loading || verificationCode.length !== 6}
        className="w-full bg-white text-black py-4 px-6 rounded-full font-semibold text-base hover:bg-gray-200 transition-all disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer flex items-center justify-center gap-2"
      >
        {loading && <Loader2 className="w-5 h-5 animate-spin" />}
        {loading ? "Verifying..." : "Verify Code"}
      </button>
    </div>
  )
}
