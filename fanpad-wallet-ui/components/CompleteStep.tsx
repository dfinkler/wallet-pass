import { XCircle, ArrowLeft, ArrowRight } from "lucide-react"

interface CompleteStepProps {
  fanName: string
  setFanName: (value: string) => void
  loading: boolean
  error: string | null
  onBack: () => void
  onSubmit: () => void
}

export default function CompleteStep({
  fanName,
  setFanName,
  loading,
  error,
  onBack,
  onSubmit,
}: CompleteStepProps) {
  return (
    <div className="min-h-screen bg-[#2C2C2E] text-white flex flex-col justify-center items-center p-6">
      <div className="max-w-md w-full space-y-8">
        {/* Top Section - Form */}
        <div>
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-white mb-3">
              Complete your account
            </h1>
            <p className="text-gray-400 text-base">
              Enter the following information to continue.
            </p>
          </div>

          {/* Error Alert */}
          {error && (
            <div className="mb-4 p-4 bg-red-900/50 border border-red-700 rounded-xl flex items-center gap-3">
              <XCircle className="w-5 h-5 text-red-400 flex-shrink-0" />
              <p className="text-red-200 text-sm">{error}</p>
            </div>
          )}

          {/* First Name Input */}
          <div className="space-y-2">
            <label className="text-gray-400 text-sm">First name</label>
            <input
              type="text"
              value={fanName}
              onChange={(e) => setFanName(e.target.value)}
              placeholder="First name"
              autoFocus
              className="w-full bg-[#1C1C1E] text-white placeholder-gray-600 px-5 py-4 rounded-xl text-lg border-2 border-indigo-500 focus:outline-none focus:border-indigo-400 transition-colors"
            />
          </div>
        </div>

        {/* Bottom Section - Buttons */}
        <div className="flex gap-3 w-full">
          <button
            onClick={onBack}
            disabled={loading}
            className="px-8 py-4 rounded-full font-semibold text-base bg-gray-700 text-white hover:bg-gray-600 transition-all disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer flex items-center gap-2"
          >
            <ArrowLeft className="w-4 h-4" />
            Back
          </button>
          <button
            onClick={onSubmit}
            disabled={loading || !fanName.trim()}
            className="flex-1 bg-indigo-600 text-white py-4 px-6 rounded-full font-semibold text-base hover:bg-indigo-700 transition-all disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer flex items-center justify-center gap-2"
          >
            {loading ? (
              "Completing..."
            ) : (
              <>
                Continue
                <ArrowRight className="w-4 h-4" />
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  )
}
