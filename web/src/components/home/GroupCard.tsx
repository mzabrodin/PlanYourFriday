import { MapPin } from 'lucide-react'
import type { CSSProperties } from 'react'
import { Link } from 'react-router'
import type { GroupSummaryDto } from '../../api'
import { STAGE_COLOR, stageOf } from '../../lib/colors'
import { needsMyInput } from '../../lib/groupFilters'
import { formatDate, formatRange } from '../../lib/time'
import { StatusBadge } from '../States'
import { AvatarStack } from './AvatarStack'
import { DateTile } from './DateTile'

export function GroupCard({ group: g, now }: { group: GroupSummaryDto; now: number }) {
  const color = STAGE_COLOR[stageOf(g, now)]
  const needsInput = needsMyInput(g, now)
  const planning = g.status === 'PLANNING'
  const voting = g.status === 'VOTING'
  const faded = g.status === 'CANCELLED' || new Date(g.event?.time.end ?? g.window.end).getTime() < now

  return (
    <Link
      to={`/groups/${g.id}`}
      className={`plan-card${needsInput ? ' plan-card-alert' : ''}${faded ? ' plan-card-faded' : ''}`}
      style={{ '--group': color } as CSSProperties}
    >
      <DateTile iso={g.event?.time.start ?? g.window.start} color={color} />
      <div className="plan-card-body">
        <div className="plan-card-top">
          <h3>{g.title}</h3>
          <StatusBadge status={g.status} />
        </div>
        <p className="plan-card-when">
          {g.event ? (
            formatRange(g.event.time)
          ) : (
            <span className="muted">
              {g.status === 'CANCELLED' ? 'Was planned for' : 'Picking a time'} · {formatDate(g.window.start)}
              {formatDate(g.window.start) !== formatDate(g.window.end) && <> – {formatDate(g.window.end)}</>}
            </span>
          )}
        </p>
        {g.event?.location && (
          <p className="plan-card-where icon-line">
            <MapPin size={14} aria-hidden="true" /> {g.event.location}
          </p>
        )}

        {voting && <p className="vote-hint">{g.myVotePending ? 'Vote on a time' : 'Voting in progress · you voted'}</p>}
        {planning && (
          <div className="readiness" aria-label={`${g.readyCount} of ${g.memberCount} marked free time`}>
            <div className="readiness-bar">
              <span style={{ width: `${(g.readyCount / g.memberCount) * 100}%` }} />
            </div>
            <span>
              {g.readyCount}/{g.memberCount} ready
            </span>
          </div>
        )}

        <div className="plan-card-footer">
          <AvatarStack names={g.memberNames} />
          <span className="muted small">{g.myRole === 'ORGANIZER' ? 'You organize' : `by ${g.organizerName}`}</span>
        </div>
      </div>
    </Link>
  )
}
