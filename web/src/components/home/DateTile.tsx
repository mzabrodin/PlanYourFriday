import type { CSSProperties } from 'react'

const month = new Intl.DateTimeFormat(undefined, { month: 'short' })
const weekday = new Intl.DateTimeFormat(undefined, { weekday: 'short' })

export function DateTile({ iso, color, size = 'md' }: { iso: string; color: string; size?: 'md' | 'lg' }) {
  const d = new Date(iso)
  return (
    <div className={`date-tile date-tile-${size}`} style={{ '--tile': color } as CSSProperties}>
      <span className="date-tile-month">{month.format(d)}</span>
      <span className="date-tile-day">{d.getDate()}</span>
      <span className="date-tile-weekday">{weekday.format(d)}</span>
    </div>
  )
}
