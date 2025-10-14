import { CheckCircle, Download, Apple, Smartphone } from "lucide-react"

interface DoneStepProps {
  onDownload: (platform?: "apple" | "google") => void
}

export default function DoneStep({ onDownload }: DoneStepProps) {
  return (
    <div className="space-y-4">
      <div className="text-center py-4">
        <div className="inline-flex items-center justify-center w-16 h-16 bg-green-500/20 rounded-full mb-4">
          <CheckCircle className="w-10 h-10 text-green-500" />
        </div>
        <h3 className="text-2xl font-bold text-white mb-2">
          Your Fan Pass is Ready!
        </h3>
        <p className="text-gray-400">
          Add it to your wallet to never miss a thing.
        </p>
      </div>

      <button
        onClick={() => onDownload()}
        className="w-full bg-white text-black py-4 px-6 rounded-full font-semibold text-base hover:bg-gray-200 transition-all cursor-pointer flex items-center justify-center gap-2"
      >
        <Download className="w-5 h-5" />
        Add to Wallet
      </button>

      <div className="grid grid-cols-2 gap-3">
        <button
          onClick={() => onDownload("apple")}
          className="bg-gray-900 text-white py-3 px-4 rounded-full text-sm font-medium hover:bg-gray-800 transition-all border border-gray-700 cursor-pointer flex items-center justify-center gap-2"
        >
          <Apple className="w-4 h-4" />
          Add to Apple Wallet
        </button>

        <button
          onClick={() => onDownload("google")}
          className="bg-gray-900 text-white py-3 px-4 rounded-full text-sm font-medium hover:bg-gray-800 transition-all border border-gray-700 cursor-pointer flex items-center justify-center gap-2"
        >
          <Smartphone className="w-4 h-4" />
          Add to Google Wallet
        </button>
      </div>
    </div>
  )
}
