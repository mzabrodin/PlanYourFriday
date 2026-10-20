import { ArrowRight, MapPin } from 'lucide-react'
import { Link } from 'react-router'
import type { GroupSummaryDto } from '../../api'
import { formatRange, formatRelative } from '../../lib/time'
import { AvatarStack } from './AvatarStack'
import { DateTile } from './DateTile'

export function NextUpCard({ group, now }: { group: GroupSummaryDto | undefined; now: number }) {
  if (!group?.event) {
    return (
      <div className="next-up next-up-empty">
        <p className="next-up-label">Next up</p>
        <h2>Nothing on the calendar yet</h2>
        <p>Round up your friends and find a time that works for everyone.</p>
        <Link to="/groups/new" className="btn btn-on-dark">
          Start a plan <ArrowRight size={16} aria-hidden="true" />
        </Link>
      </div>
    )
  }

  const { event } = group
  return (
    <Link to={`/groups/${group.id}`} className="next-up">
      <div className="next-up-main">
        <p className="next-up-label">
          Next up · <strong>{formatRelative(event.time.start, now)}</strong>
        </p>
        <h2>{event.title}</h2>
        <p className="next-up-when">{formatRange(event.time)}</p>
        {event.location && (
          <p className="next-up-where icon-line">
            <MapPin size={16} aria-hidden="true" /> {event.location}
          </p>
        )}
        <div className="next-up-people">
          <AvatarStack names={group.memberNames} max={5} />
          <span>{group.memberCount === 2 ? 'You and 1 friend' : `You and ${group.memberCount - 1} friends`}</span>
        </div>
      </div>
      <div className="next-up-stub" aria-hidden="true">
        <DateTile iso={event.time.start} color="#fff" size="lg" />
      </div>
    </Link>
  )
}
