import { useState } from 'react'
import { formatDuration } from '../lib/time'

const PRESETS = [30, 60, 90, 120, 180, 240, 300, 480]
export const MIN_DURATION = 15

// A few common lengths, plus "Custom…" for anything else in hours and minutes.
export function DurationPicker({
  id,
  value,
  onChange,
  max,
}: {
  id?: string
  value: number
  onChange: (minutes: number) => void
  max: number
}) {
  const [custom, setCustom] = useState(!PRESETS.includes(value))
  const hours = Math.floor(value / 60)
  const minutes = value % 60
  const clamp = (m: number) => Math.min(max, Math.max(MIN_DURATION, m))

  return (
    <div className="duration-picker">
      <select
        id={id}
        value={custom ? 'custom' : value}
        onChange={(e) => {
          if (e.target.value === 'custom') return setCustom(true)
          setCustom(false)
          onChange(Number(e.target.value))
        }}
      >
        {PRESETS.filter((m) => m <= max).map((m) => (
          <option key={m} value={m}>
            {formatDuration(m)}
          </option>
        ))}
        <option value="custom">Custom…</option>
      </select>
      {custom && (
        <span className="duration-custom" onBlur={() => onChange(clamp(value))}>
          <input
            type="number"
            inputMode="numeric"
            min={0}
            max={Math.floor(max / 60)}
            value={hours}
            aria-label="Hours"
            onChange={(e) => onChange(Math.max(0, Number(e.target.value)) * 60 + minutes)}
          />
          <span className="muted">h</span>
          <input
            type="number"
            inputMode="numeric"
            min={0}
            max={59}
            step={5}
            value={minutes}
            aria-label="Minutes"
            onChange={(e) => onChange(hours * 60 + Math.min(59, Math.max(0, Number(e.target.value))))}
          />
          <span className="muted">min</span>
        </span>
      )}
    </div>
  )
}
