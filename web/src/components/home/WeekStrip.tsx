import { ArrowRight, PartyPopper, Vote } from 'lucide-react'
import type { CSSProperties } from 'react'
import { Link } from 'react-router'
import type { GroupSummaryDto } from '../../api'
import { STAGE_COLOR, stageOf } from '../../lib/colors'
import { formatTime } from '../../lib/time'

const weekday = new Intl.DateTimeFormat(undefined, { weekday: 'short' })

interface DayItem {
  group: GroupSummaryDto
  time: string | null
}

export function WeekStrip({ groups, now }: { groups: GroupSummaryDto[]; now: number }) {
  const today = new Date(now)
  today.setHours(0, 0, 0, 0)
  const days = Array.from({ length: 7 }, (_, i) => {
    const start = new Date(today)
    start.setDate(today.getDate() + i)
    const end = new Date(start)
    end.setDate(start.getDate() + 1)
    return { start, end }
  })

  const itemsFor = ({ start, end }: { start: Date; end: Date }): DayItem[] => {
    const overlaps = (s: string, e: string) => new Date(s) < end && new Date(e) > start
    const items: DayItem[] = []
    for (const g of groups) {
      if (g.status === 'CANCELLED') continue
      if (g.event && overlaps(g.event.time.start, g.event.time.end)) items.push({ group: g, time: g.event.time.start })
      else if ((g.status === 'PLANNING' || g.status === 'VOTING') && overlaps(g.window.start, g.window.end))
        items.push({ group: g, time: null })
    }
    return items.sort((a, b) => Number(a.time === null) - Number(b.time === null))
  }

  return (
    <section className="home-section">
      <header className="section-header">
        <h2>Next 7 days</h2>
        <Link to="/calendar" className="muted small">
          Open calendar <ArrowRight size={14} aria-hidden="true" />
        </Link>
      </header>
      <ol className="week-strip">
        {days.map((day, i) => {
          const items = itemsFor(day)
          const isFriday = day.start.getDay() === 5
          return (
            <li
              key={day.start.toISOString()}
              className={`day${i === 0 ? ' day-today' : ''}${isFriday ? ' day-friday' : ''}`}
            >
              <div className="day-head">
                <span className="day-name">{i === 0 ? 'Today' : weekday.format(day.start)}</span>
                <span className="day-num">{day.start.getDate()}</span>
                {isFriday && <PartyPopper className="day-party" size={16} aria-label="Friday!" />}
              </div>
              <div className="day-items">
                {items.slice(0, 3).map(({ group, time }) => (
                  <Link
                    key={group.id}
                    to={`/groups/${group.id}`}
                    className={`day-chip${time ? '' : ' day-chip-planning'}`}
                    style={{ '--group': STAGE_COLOR[stageOf(group, now)] } as CSSProperties}
                    title={group.title}
                  >
                    {time ? (
                      <strong>{formatTime(time)}</strong>
                    ) : (
                      <Vote size={12} aria-hidden="true" className="chip-icon" />
                    )}{' '}
                    {group.title}
                  </Link>
                ))}
                {items.length > 3 && <span className="muted small">+{items.length - 3} more</span>}
                {items.length === 0 && <span className="day-free">free</span>}
              </div>
            </li>
          )
        })}
      </ol>
    </section>
  )
}
