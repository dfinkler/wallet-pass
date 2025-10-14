import { Send, Loader2 } from "lucide-react"

interface PhoneStepProps {
  phoneNumber: string
  setPhoneNumber: (value: string) => void
  countryCode: string
  setCountryCode: (value: string) => void
  loading: boolean
  onSubmit: () => void
}

export default function PhoneStep({
  phoneNumber,
  setPhoneNumber,
  countryCode,
  setCountryCode,
  loading,
  onSubmit,
}: PhoneStepProps) {
  const formatPhoneNumber = (value: string) => {
    // Remove all non-digits
    const digits = value.replace(/\D/g, "")

    // Format as XXX-XXX-XXXX (10 digits)
    if (digits.length <= 3) {
      return digits
    } else if (digits.length <= 6) {
      return `${digits.slice(0, 3)}-${digits.slice(3)}`
    } else if (digits.length <= 10) {
      return `${digits.slice(0, 3)}-${digits.slice(3, 6)}-${digits.slice(6)}`
    } else {
      return `${digits.slice(0, 3)}-${digits.slice(3, 6)}-${digits.slice(6, 10)}`
    }
  }

  const handlePhoneChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const digits = e.target.value.replace(/\D/g, "")
    setPhoneNumber(digits.slice(0, 10)) // Max 10 digits
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 bg-gray-900 rounded-full px-5 py-4 border border-gray-700">
        <select
          value={countryCode}
          onChange={(e) => setCountryCode(e.target.value)}
          className="bg-transparent text-white border-none focus:outline-none text-base cursor-pointer"
        >
          <option value="+1">+1</option>
          <option value="+44">+44</option>
          <option value="+33">+33</option>
          <option value="+49">+49</option>
        </select>
        <span className="text-gray-600">|</span>
        <input
          type="tel"
          value={formatPhoneNumber(phoneNumber)}
          onChange={handlePhoneChange}
          placeholder="555-123-4567"
          className="bg-transparent text-white placeholder-gray-600 flex-1 border-none focus:outline-none text-base"
        />
        <button
          onClick={onSubmit}
          disabled={loading || phoneNumber.length < 10}
          className="text-gray-400 font-medium text-sm hover:text-white transition-colors disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer flex items-center gap-1"
        >
          {loading ? (
            <Loader2 className="w-4 h-4 animate-spin" />
          ) : (
            <>
              Send Code
              <Send className="w-3.5 h-3.5" />
            </>
          )}
        </button>
      </div>

      <p className="text-gray-500 text-xs leading-relaxed px-2">
        By submitting, you agree to receive recurrent messages to the contact
        information provided from VOILÀ and to FanPad's{" "}
        <a
          href="#"
          className="underline text-gray-400 hover:text-white cursor-pointer"
        >
          terms & conditions
        </a>{" "}
        and{" "}
        <a
          href="#"
          className="underline text-gray-400 hover:text-white cursor-pointer"
        >
          privacy policy
        </a>
        .
      </p>
    </div>
  )
}
