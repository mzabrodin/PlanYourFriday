import { ArrowRight, CalendarClock, Hourglass, Vote } from 'lucide-react'
import type { CSSProperties } from 'react'
import { Link } from 'react-router'
import type { GroupSummaryDto } from '../../api'
import { STAGE_COLOR, stageOf } from '../../lib/colors'
import { formatRange, formatRelative } from '../../lib/time'

export function YourMove({ groups, now }: { groups: GroupSummaryDto[]; now: number }) {
  if (groups.length === 0) return null
  const deadlineOf = (g: GroupSummaryDto) => (g.status === 'VOTING' ? g.votingDeadline : g.availabilityDeadline)
  const urgency = (g: GroupSummaryDto) => new Date(deadlineOf(g) ?? g.window.start).getTime()
  const sorted = [...groups].sort((a, b) => urgency(a) - urgency(b))

  return (
    <section className="home-section">
      <header className="section-header">
        <h2>
          Your move <span className="section-count">{groups.length}</span>
        </h2>
        <p className="muted small">Friends are waiting for you</p>
      </header>
      <ul className="move-list">
        {sorted.map((g) => {
          const voting = g.status === 'VOTING'
          const deadline = deadlineOf(g)
          return (
            <li key={g.id}>
              <Link to={`/groups/${g.id}`} className="move-card" style={{ '--group': STAGE_COLOR[stageOf(g, now)] } as CSSProperties}>
                <span className="move-icon" aria-hidden="true">
                  {voting ? <Vote size={20} /> : <CalendarClock size={20} />}
                </span>
                <div className="move-body">
                  <h3>{g.title}</h3>
                  <p className="muted small">
                    {formatRange(g.window)} · by {g.organizerName}
                  </p>
                  {!voting && (
                    <div className="readiness">
                      <div className="readiness-bar">
                        <span style={{ width: `${(g.readyCount / g.memberCount) * 100}%` }} />
                      </div>
                      <span>
                        {g.readyCount}/{g.memberCount} ready
                      </span>
                    </div>
                  )}
                  {voting && <p className="small">The vote is on: pick the times that work for you.</p>}
                </div>
                <div className="move-cta">
                  {deadline && (
                    <span className="deadline">
                      <Hourglass size={12} aria-hidden="true" /> closes {formatRelative(deadline, now)}
                    </span>
                  )}
                  <span className="move-arrow">
                    {voting ? 'Vote' : 'Mark free time'} <ArrowRight size={14} aria-hidden="true" />
                  </span>
                </div>
              </Link>
            </li>
          )
        })}
      </ul>
    </section>
  )
}
