import { useState } from 'react'
import { ArrowRight, Hourglass, Moon, PartyPopper, Plus, Sofa, Sun, Sunrise, Sunset, type LucideIcon } from 'lucide-react'
import { Link } from 'react-router'
import type { GroupSummaryDto } from '../api'
import { useGroups, useMe } from '../api/queries'
import { AvatarStack } from '../components/home/AvatarStack'
import { NextUpCard } from '../components/home/NextUpCard'
import { WeekStrip } from '../components/home/WeekStrip'
import { YourMove } from '../components/home/YourMove'
import { ErrorState, Loading } from '../components/States'
import { needsMyInput } from '../lib/groupFilters'
import { daysUntil } from '../lib/time'

export function HomePage() {
  const { data: groups, isPending, error, refetch } = useGroups()
  const [now] = useState(Date.now)

  const upcomingEvents = (groups ?? [])
    .filter((g) => g.event?.status === 'CONFIRMED' && new Date(g.event.time.end).getTime() >= now)
    .sort((a, b) => new Date(a.event!.time.start).getTime() - new Date(b.event!.time.start).getTime())
  const waitingOnMe = (groups ?? []).filter((g) => needsMyInput(g, now))

  return (
    <div className="home">
      <Hero groups={groups} upcoming={upcomingEvents} waiting={waitingOnMe} now={now} />

      {isPending && <Loading />}
      {error && <ErrorState error={error} onRetry={refetch} />}

      {groups && (
        <div className="home-top">
          <NextUpCard group={upcomingEvents[0]} now={now} />
          <YourMove groups={waitingOnMe} now={now} />
        </div>
      )}

      {groups && groups.length > 0 && <WeekStrip groups={groups} now={now} />}

      {groups?.length === 0 && (
        <div className="empty">
          <p>You're not in any groups yet.</p>
          <p className="muted">Create one, or open an invite link a friend sent you.</p>
        </div>
      )}

      {groups && groups.length > 0 && (
        <Link to="/groups" className="all-groups-link">
          <span>
            <strong>All groups</strong>
            <span className="muted small">
              {groups.length} {groups.length === 1 ? 'group' : 'groups'} · search, filter, past plans
            </span>
          </span>
          <ArrowRight size={20} aria-hidden="true" />
        </Link>
      )}
    </div>
  )
}

function greeting(now: number): { text: string; Icon: LucideIcon } {
  const h = new Date(now).getHours()
  if (h < 5) return { text: 'Up late', Icon: Moon }
  if (h < 12) return { text: 'Good morning', Icon: Sunrise }
  if (h < 18) return { text: 'Good afternoon', Icon: Sun }
  return { text: 'Good evening', Icon: Sunset }
}

function fridayLine(now: number): { text: string; Icon: LucideIcon } {
  const day = new Date(now).getDay()
  if (day === 5) return { text: "It's Friday!", Icon: PartyPopper }
  if (day === 6 || day === 0) return { text: 'Weekend mode', Icon: Sofa }
  const days = (5 - day + 7) % 7
  return { text: days === 1 ? 'Friday is tomorrow' : `Friday in ${days} days`, Icon: Hourglass }
}

function Hero({
  groups,
  upcoming,
  waiting,
  now,
}: {
  groups: GroupSummaryDto[] | undefined
  upcoming: GroupSummaryDto[]
  waiting: GroupSummaryDto[]
  now: number
}) {
  const { data: me } = useMe()
  const name = me?.displayName && me.displayName !== 'You' ? me.displayName.split(' ')[0] : null
  const thisWeek = upcoming.filter((g) => daysUntil(g.event!.time.start, now) < 7).length
  const friends = [...new Set((groups ?? []).flatMap((g) => g.memberNames).filter((n) => n !== 'You'))]

  const summary = [
    thisWeek > 0 ? `${thisWeek} ${thisWeek === 1 ? 'plan' : 'plans'} this week` : 'No plans this week yet',
    waiting.length > 0 && `${waiting.length} waiting for you`,
  ]
    .filter(Boolean)
    .join(' · ')

  const hello = greeting(now)
  const friday = fridayLine(now)

  return (
    <header className="hero">
      <div>
        <p className="hero-kicker">
          <friday.Icon size={16} aria-hidden="true" /> {friday.text}
        </p>
        <h1 className="hero-title">
          <span>
            {hello.text}
            {name ? `, ${name}` : ''}
          </span>
          <span className="hero-sun" aria-hidden="true">
            <hello.Icon size={30} strokeWidth={2.2} />
          </span>
        </h1>
        <p className="hero-summary">{summary}</p>
      </div>
      <div className="hero-actions">
        {friends.length > 0 && (
          <div className="hero-friends">
            <AvatarStack names={friends} max={5} />
            <span className="muted small">{friends.length} friends</span>
          </div>
        )}
        <Link to="/groups/new" className="btn btn-primary btn-big">
          <Plus size={18} aria-hidden="true" /> New plan
        </Link>
      </div>
    </header>
  )
}
