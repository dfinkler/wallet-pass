interface PassCardProps {
  fanName: string
  passId: string | null
}

export default function PassCard({ fanName, passId }: PassCardProps) {
  return (
    <div className="bg-gradient-to-br from-gray-800 to-gray-900 rounded-lg p-6 mb-6 border border-gray-700">
      <div className="flex items-center gap-4 mb-6">
        <div className="w-14 h-14 rounded-full bg-white flex items-center justify-center flex-shrink-0">
          <span className="text-black font-bold text-lg">V</span>
        </div>
        <div>
          <h2 className="text-white font-bold text-xl">VOILÀ</h2>
          <p className="text-gray-400 text-sm">Magician Pass</p>
        </div>
      </div>

      <div className="space-y-4">
        <div>
          <div className="text-gray-500 text-xs uppercase tracking-wider mb-1">
            FAN
          </div>
          <div className="text-white text-lg">{fanName || "Your name"}</div>
        </div>

        <div>
          <div className="text-gray-500 text-xs uppercase tracking-wider mb-1">
            ID
          </div>
          <div className="text-white text-lg font-mono">
            {passId
              ? passId.slice(0, 8).replace(/-/g, "").toUpperCase()
              : "0123456"}
          </div>
        </div>
      </div>
    </div>
  )
}
