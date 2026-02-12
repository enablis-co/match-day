import { useState, useEffect, useRef, useCallback } from 'react'

const EVENTS_URL = window.__ENV__?.EVENTS_URL || '/api/events'

const SCENARIOS = [
  { label: 'Morning Calm', time: '08:00', description: 'Before any events' },
  { label: 'Pre-Match 1', time: '13:30', description: 'Arsenal vs Chelsea build-up' },
  { label: 'Match 1 Live', time: '15:00', description: 'Arsenal vs Chelsea in play' },
  { label: 'Between Games', time: '16:30', description: 'Transition window' },
  { label: 'Pre-Match 2', time: '16:45', description: 'England vs France build-up' },
  { label: 'Match 2 Live', time: '18:00', description: 'England vs France in play' },
  { label: 'Post-Match', time: '19:30', description: 'After all events' },
  { label: 'Late Night', time: '22:00', description: 'Wind-down' },
]

function timeToMinutes(timeStr) {
  const [h, m] = timeStr.split(':').map(Number)
  return h * 60 + m
}

function minutesToTime(minutes) {
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}

// Fixture date must match the date in fixtures.json so the Events service
// can find active events when the simulated clock is set.
const FIXTURE_DATE = '2026-02-12'

function buildISOTime(timeStr) {
  const [year, month, day] = FIXTURE_DATE.split('-').map(Number)
  const [h, m] = timeStr.split(':').map(Number)
  const d = new Date(Date.UTC(year, month - 1, day, h, m, 0))
  return d.toISOString()
}

export default function ScenarioPlayer({ currentClock, onClockSet }) {
  const [activeTime, setActiveTime] = useState(null)
  const [sliderValue, setSliderValue] = useState(480) // 08:00 default
  const [clockError, setClockError] = useState(null)
  const debounceRef = useRef(null)

  // Sync active highlight with current clock
  useEffect(() => {
    if (!currentClock) return
    const d = new Date(currentClock)
    const clockMinutes = d.getUTCHours() * 60 + d.getUTCMinutes()
    setSliderValue(clockMinutes)

    const match = SCENARIOS.find(s => timeToMinutes(s.time) === clockMinutes)
    setActiveTime(match ? match.time : null)
  }, [currentClock])

  const setClock = useCallback(async (timeStr) => {
    try {
      setClockError(null)
      const res = await fetch(`${EVENTS_URL}/clock`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ time: buildISOTime(timeStr) }),
      })
      if (!res.ok) throw new Error(`Clock API returned ${res.status}`)
      setActiveTime(timeStr)
      if (onClockSet) onClockSet()
    } catch {
      setClockError('Failed to set clock')
    }
  }, [onClockSet])

  const handleScenarioClick = (timeStr) => {
    setSliderValue(timeToMinutes(timeStr))
    setClock(timeStr)
  }

  const handleSliderChange = (e) => {
    const minutes = Number(e.target.value)
    setSliderValue(minutes)

    if (debounceRef.current) clearTimeout(debounceRef.current)
    debounceRef.current = setTimeout(() => {
      const timeStr = minutesToTime(minutes)
      setClock(timeStr)
    }, 500)
  }

  // Clean up debounce on unmount
  useEffect(() => {
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current)
    }
  }, [])

  const sliderTimeLabel = minutesToTime(sliderValue)

  return (
    <div className="fixed bottom-0 left-0 right-0 bg-gray-900 border-t border-gray-700 px-6 py-4 z-50">
      {clockError && (
        <div className="text-red-400 text-sm mb-2 text-center">{clockError}</div>
      )}

      {/* Scenario Buttons */}
      <div className="flex items-center gap-2 mb-3 overflow-x-auto">
        <span className="text-gray-400 text-sm font-semibold whitespace-nowrap mr-2">
          Scenarios:
        </span>
        {SCENARIOS.map((scenario) => {
          const isActive = activeTime === scenario.time
          return (
            <button
              key={scenario.time}
              onClick={() => handleScenarioClick(scenario.time)}
              title={scenario.description}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium whitespace-nowrap transition-colors ${
                isActive
                  ? 'bg-blue-600 text-white ring-2 ring-blue-400'
                  : 'bg-gray-700 text-gray-300 hover:bg-gray-600'
              }`}
            >
              {scenario.label}
              <span className="ml-1.5 text-xs opacity-70">{scenario.time}</span>
            </button>
          )
        })}
      </div>

      {/* Time Slider */}
      <div className="flex items-center gap-4">
        <span className="text-gray-400 text-sm font-mono w-12">06:00</span>
        <input
          type="range"
          min={360}
          max={1380}
          step={5}
          value={sliderValue}
          onChange={handleSliderChange}
          className="flex-1 h-2 bg-gray-700 rounded-lg appearance-none cursor-pointer accent-blue-500"
        />
        <span className="text-gray-400 text-sm font-mono w-12">23:00</span>
        <span className="text-white text-sm font-mono bg-gray-700 px-3 py-1 rounded-lg min-w-[4rem] text-center">
          {sliderTimeLabel}
        </span>
      </div>
    </div>
  )
}
