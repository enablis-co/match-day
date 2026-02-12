import { useState, useEffect, useRef } from 'react'

const AGGREGATOR_URL = window.__ENV__?.AGGREGATOR_URL || '/api/aggregator'
const EVENTS_URL = window.__ENV__?.EVENTS_URL || '/api/events'
const POLL_INTERVAL = 5000

function formatTime(iso) {
  if (!iso) return '--:--'
  const d = new Date(iso)
  return d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })
}

function formatDeadline(iso) {
  if (!iso) return ''
  return formatTime(iso)
}

const riskColors = {
  LOW: 'bg-green-600',
  MEDIUM: 'bg-amber-500',
  HIGH: 'bg-red-600',
  CRITICAL: 'bg-red-700 animate-pulse-red',
}

const riskDots = {
  LOW: 'bg-green-400',
  MEDIUM: 'bg-amber-400',
  HIGH: 'bg-red-500',
  CRITICAL: 'bg-red-500 animate-pulse-red',
}

const intensityColors = {
  QUIET: 'bg-green-600',
  MODERATE: 'bg-lime-500',
  BUSY: 'bg-amber-500',
  HIGH: 'bg-orange-500',
  CRITICAL: 'bg-red-600',
}

const intensityBg = {
  QUIET: 'bg-green-600/20 text-green-400',
  MODERATE: 'bg-lime-500/20 text-lime-400',
  BUSY: 'bg-amber-500/20 text-amber-400',
  HIGH: 'bg-orange-500/20 text-orange-400',
  CRITICAL: 'bg-red-600/20 text-red-400',
}

const severityIcon = {
  HIGH: '\u{1F534}',
  MEDIUM: '\u{1F7E1}',
  LOW: '\u{1F7E2}',
  OK: '\u{1F7E2}',
}

export default function App() {
  const [status, setStatus] = useState(null)
  const [clock, setClock] = useState(null)
  const [error, setError] = useState(null)
  const [lastUpdate, setLastUpdate] = useState(null)
  const [forecast, setForecast] = useState(null)
  const prevRisk = useRef(null)

  useEffect(() => {
    async function poll() {
      try {
        const [statusRes, clockRes] = await Promise.all([
          fetch(`${AGGREGATOR_URL}/status/PUB-001`),
          fetch(`${EVENTS_URL}/clock`),
        ])

        const statusData = await statusRes.json()
        const clockData = await clockRes.json()

        if (statusData.surgeForecast) {
          setForecast(statusData.surgeForecast)
        }

        // Check if aggregator is still a stub
        if (statusData.message && statusData.message.includes('Not implemented')) {
          setStatus(null)
        } else {
          setStatus(statusData)
        }

        setClock(clockData.currentTime)
        setLastUpdate(new Date())
        setError(null)
      } catch (e) {
        setError('Unable to reach services')
      }
    }

    poll()
    const interval = setInterval(poll, POLL_INTERVAL)
    return () => clearInterval(interval)
  }, [])

  const riskLevel = status?.status?.riskLevel || 'LOW'
  const overall = status?.status?.overall || 'NORMAL'
  const matchDay = status?.status?.matchDay || false

  return (
    <div className="min-h-screen bg-gray-950 text-white p-6 font-sans">
      {/* Header */}
      <header className="flex items-center justify-between mb-8">
        <h1 className="text-4xl font-bold tracking-tight">
          MATCH DAY MODE
        </h1>
        <div className="text-right">
          <div className="text-3xl font-mono">
            {clock ? formatTime(clock) : '--:--'}
          </div>
          <div className="text-sm text-gray-400">simulated time</div>
        </div>
      </header>

      {/* Warning banner */}
      {error && (
        <div className="bg-yellow-900/50 border border-yellow-600 text-yellow-200 px-4 py-2 rounded mb-6 text-lg">
          {error}
        </div>
      )}

      {/* Degraded service banner */}
      {status?.serviceHealth && Object.values(status.serviceHealth).some(s =>
        (typeof s === 'string' ? s : s?.status) !== 'OK'
      ) && (
        <div className="bg-yellow-900/50 border border-yellow-600 text-yellow-200 px-4 py-2 rounded mb-6 text-lg">
          Some services unavailable — data may be incomplete.
        </div>
      )}

      {!status && !error && (
        <div className="bg-gray-800/50 border border-gray-600 text-gray-300 px-4 py-3 rounded mb-6 text-lg text-center">
          Waiting for Aggregator data — build the Aggregator service to light up this dashboard!
        </div>
      )}

      {/* Top cards row */}
      <div className="grid grid-cols-2 gap-6 mb-6">
        {/* Event Card */}
        <div className="bg-gray-800 rounded-xl p-6 border border-gray-700">
          <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-4">
            Event
          </h2>
          {status?.events?.active ? (
            <>
              <p className="text-3xl font-bold mb-1">{status.events.current}</p>
              <p className="text-gray-400 text-lg mb-3">{status.events.competition || ''}</p>
              <div className="flex items-center gap-3">
                <span className="inline-flex items-center gap-1 bg-red-600/20 text-red-400 px-3 py-1 rounded-full text-sm font-semibold">
                  <span className="w-2 h-2 bg-red-400 rounded-full animate-pulse"></span>
                  LIVE
                </span>
                <span className="text-amber-400 font-semibold text-lg">
                  {status.events.demandMultiplier}x demand
                </span>
              </div>
              {status.events.endsAt && (
                <p className="text-gray-500 text-sm mt-2">
                  Ends at {formatTime(status.events.endsAt)}
                </p>
              )}
            </>
          ) : (
            <div className="text-gray-500 text-2xl">
              {status ? 'No active event' : 'Awaiting data'}
            </div>
          )}
        </div>

        {/* Risk Level Card */}
        <div className={`rounded-xl p-6 border border-gray-700 flex flex-col items-center justify-center ${
          status ? riskColors[riskLevel] || 'bg-gray-800' : 'bg-gray-800'
        }`}>
          <h2 className="text-sm font-semibold text-white/70 uppercase tracking-wider mb-4">
            Risk Level
          </h2>
          <div className="text-5xl font-black mb-2">
            {status ? riskLevel : '—'}
          </div>
          <div className="text-lg text-white/80">
            {status ? overall : 'Awaiting data'}
          </div>
          {status && (
            <div className={`w-4 h-4 rounded-full mt-3 ${riskDots[riskLevel] || 'bg-gray-500'}`}></div>
          )}
        </div>
      </div>

      {/* Stock Alerts */}
      <div className="bg-gray-800 rounded-xl p-6 border border-gray-700 mb-6">
        <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-4">
          Stock Alerts
        </h2>
        {status?.stock?.criticalItems?.length > 0 || status?.stock?.alerts?.length > 0 ? (
          <div className="space-y-3">
            {(status.stock.alerts || status.stock.criticalItems || []).map((item, i) => {
              // Handle both array of objects and array of strings
              const name = typeof item === 'string' ? item : item.productName || item.productId
              const severity = typeof item === 'string' ? 'HIGH' : item.severity || 'HIGH'
              const depletion = typeof item === 'string' ? null : item.estimatedDepletionTime
              return (
                <div key={i} className="flex items-center gap-3 text-xl">
                  <span>{severityIcon[severity] || severityIcon.OK}</span>
                  <span className="font-semibold">{name}</span>
                  {depletion && (
                    <span className="text-gray-400">— depletes {formatTime(depletion)}</span>
                  )}
                </div>
              )
            })}
          </div>
        ) : (
          <p className="text-gray-500 text-xl">
            {status ? 'All stock levels OK' : 'Awaiting data'}
          </p>
        )}
      </div>

      {/* Predicted Demand (Surge Forecast) */}
      <div className="bg-gray-800 rounded-xl p-6 border border-gray-700 mb-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wider">
            Predicted Demand
          </h2>
          {status?.surge && (
            <span className={`px-3 py-1 rounded-full text-sm font-semibold ${intensityBg[status.surge.intensity] || 'bg-gray-600/20 text-gray-400'}`}>
              Peak at {status.surge.peakHour} — {status.surge.intensity}
            </span>
          )}
        </div>

        {forecast?.forecast?.length > 0 ? (
          <div className="space-y-2">
            {forecast.forecast.map((hour, i) => {
              const widthPct = Math.max((hour.surgeScore / 10) * 100, 2)
              const barColor = intensityColors[hour.intensity] || 'bg-gray-600'
              const isCurrentHour = i === 0
              return (
                <div key={i} className={`flex items-center gap-3 ${isCurrentHour ? 'bg-gray-700/50 -mx-2 px-2 py-1 rounded' : ''}`}>
                  <span className="text-gray-400 font-mono text-sm w-14 shrink-0">
                    {hour.hour}
                    {isCurrentHour && <span className="text-xs text-amber-400 ml-1">now</span>}
                  </span>
                  <div className="flex-1 h-6 bg-gray-700 rounded overflow-hidden">
                    <div
                      className={`h-full ${barColor} rounded transition-all duration-500`}
                      style={{ width: `${widthPct}%` }}
                    />
                  </div>
                  <span className="text-gray-300 font-mono text-sm w-10 text-right shrink-0">
                    {hour.surgeScore.toFixed(1)}
                  </span>
                </div>
              )
            })}
          </div>
        ) : (
          <p className="text-gray-500 text-xl">
            {status ? 'No forecast data available' : 'Awaiting data'}
          </p>
        )}

        {forecast?.confidence && (
          <div className="mt-4 flex items-center gap-4 text-sm text-gray-500">
            <span>Confidence: <span className="text-gray-300 font-semibold">{forecast.confidence}</span></span>
            {forecast.signals?.temperature != null && (
              <span>{forecast.signals.temperature.toFixed(1)}°C</span>
            )}
            {forecast.signals?.rainProbability != null && (
              <span>{(forecast.signals.rainProbability * 100).toFixed(0)}% rain</span>
            )}
          </div>
        )}
      </div>

      {/* Actions */}
      <div className="bg-gray-800 rounded-xl p-6 border border-gray-700">
        <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-4">
          Actions
        </h2>
        {status?.actions?.length > 0 ? (
          <div className="space-y-3">
            {status.actions.map((action, i) => (
              <div key={i} className="flex items-start gap-3 text-xl">
                <span className="text-amber-400 font-bold min-w-[2rem]">
                  {action.priority || i + 1}.
                </span>
                <div>
                  <span className="font-semibold">
                    {action.action?.replace(/_/g, ' ') || action.description}
                  </span>
                  {action.reason && (
                    <span className="text-gray-400 ml-2">— {action.reason}</span>
                  )}
                  {action.deadline && (
                    <span className="text-red-400 ml-2">by {formatDeadline(action.deadline)}</span>
                  )}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-gray-500 text-xl">
            {status ? 'No actions required' : 'Awaiting data'}
          </p>
        )}
      </div>

      {/* Footer */}
      <div className="mt-6 flex items-center justify-between text-sm text-gray-600">
        <span>
          Polling Aggregator every {POLL_INTERVAL / 1000}s
        </span>
        {lastUpdate && (
          <span>
            Last update: {lastUpdate.toLocaleTimeString('en-GB')}
          </span>
        )}
      </div>
    </div>
  )
}
