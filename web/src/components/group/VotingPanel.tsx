import { Check, CircleHelp, Crown, Hourglass, Undo2, Vote, X } from 'lucide-react'
import { useState, type ComponentType } from 'react'
import type { GroupDto, TimeOptionDto, TimeRange, VoteAnswer } from '../../api'
import { useCancelVoting, useVote } from '../../api/queries'
import { formatRange, formatRelative } from '../../lib/time'
import { DateTile } from '../home/DateTile'
import { InlineError } from '../States'
import { ConfirmEventForm } from './ConfirmEventForm'
import { joinNames, memberNameMap } from './memberNames'

const ANSWERS: { value: VoteAnswer; label: string; Icon: ComponentType<{ size?: number }> }[] = [
  { value: 'YES', label: 'Works', Icon: Check },
  { value: 'MAYBE', label: 'Maybe', Icon: CircleHelp },
  { value: 'NO', label: "Can't", Icon: X },
]

function optionScore(o: TimeOptionDto): number {
  return o.votes.reduce((sum, v) => sum + (v.answer === 'YES' ? 2 : v.answer === 'MAYBE' ? 1 : 0), 0)
}

export function VotingPanel({ group, meId }: { group: GroupDto; meId: string | undefined }) {
  const vote = useVote(group.id, meId)
  const cancelVoting = useCancelVoting(group.id)
  const [confirming, setConfirming] = useState<TimeRange | null>(null)
  const [now] = useState(Date.now)

  const isOrganizer = group.myRole === 'ORGANIZER'
  const names = memberNameMap(group, meId)
  const closed = group.votingDeadline !== null && new Date(group.votingDeadline).getTime() < now
  const options = [...group.options].sort((a, b) => a.time.start.localeCompare(b.time.start))
  const best = Math.max(0, ...options.map(optionScore))
  const answered = (userId: string) => options.filter((o) => o.votes.some((v) => v.userId === userId)).length
  const voted = group.members.filter((m) => answered(m.userId) === options.length)
  const notStarted = group.members.filter((m) => answered(m.userId) === 0)
  const partly = group.members.filter((m) => answered(m.userId) > 0 && answered(m.userId) < options.length)
  const nameList = (members: typeof group.members) => joinNames(members.map((m) => names.get(m.userId)!))
  const partlyNames = nameList(partly)
  const waiting = [
    notStarted.length > 0 && `Waiting for ${nameList(notStarted)}`,
    partly.length > 0 &&
      `${partlyNames[0].toUpperCase()}${partlyNames.slice(1)} ${partly.length === 1 && partlyNames !== 'you' ? "hasn't" : "haven't"} answered every time yet`,
  ].filter(Boolean)

  if (confirming) {
    return (
      <section className="card voting">
        <ConfirmEventForm group={group} time={confirming} onCancel={() => setConfirming(null)} />
      </section>
    )
  }

  return (
    <section className="card voting" aria-labelledby="voting-title">
      <header className="voting-header">
        <div>
          <h2 id="voting-title" className="icon-line">
            <Vote size={22} aria-hidden="true" /> Vote for a time
          </h2>
          <p className="muted small">
            Mark every time that works for you. Works = 2 points, maybe = 1. The most points wins.
          </p>
        </div>
        <div className="voting-meta">
          {group.votingDeadline && (
            <span className="deadline">
              <Hourglass size={14} aria-hidden="true" />
              {closed ? 'Voting closed' : `closes ${formatRelative(group.votingDeadline, now)}`}
            </span>
          )}
          <span className="muted small">
            {voted.length}/{group.members.length} voted
          </span>
        </div>
      </header>

      <ul className="option-list">
        {options.map((o) => {
          const score = optionScore(o)
          const leading = best > 0 && score === best
          const mine = o.votes.find((v) => v.userId === meId)?.answer
          const count = (a: VoteAnswer) => o.votes.filter((v) => v.answer === a).length
          const total = group.members.length
          return (
            <li key={o.id} className={`option-card${leading ? ' option-leading' : ''}`}>
              {leading && (
                <span className="leading-badge">
                  <Crown size={14} aria-hidden="true" /> Leading
                </span>
              )}
              <div className="option-top">
                <DateTile iso={o.time.start} color="var(--primary)" />
                <div>
                  <p className="option-time">{formatRange(o.time)}</p>
                  <p className="muted small">
                    {o.freeUserIds.length === total
                      ? 'Everyone marked this as free'
                      : `${o.freeUserIds.length}/${total} marked this as free`}
                  </p>
                </div>
              </div>

              <div className="vote-buttons" role="group" aria-label={`Your vote for ${formatRange(o.time)}`}>
                {ANSWERS.map(({ value, label, Icon }) => (
                  <button
                    key={value}
                    type="button"
                    className={`vote-btn vote-${value.toLowerCase()}`}
                    aria-pressed={mine === value}
                    disabled={closed}
                    onClick={() => vote.mutate({ optionId: o.id, answer: value })}
                  >
                    <Icon size={16} /> {label}
                  </button>
                ))}
              </div>

              <div
                className="tally"
                aria-label={`${count('YES')} works, ${count('MAYBE')} if needed, ${count('NO')} can't`}
              >
                <span className="tally-yes" style={{ width: `${(count('YES') / total) * 100}%` }} />
                <span className="tally-maybe" style={{ width: `${(count('MAYBE') / total) * 100}%` }} />
                <span className="tally-no" style={{ width: `${(count('NO') / total) * 100}%` }} />
              </div>
              <ul className="voters">
                {o.votes.map((v) => (
                  <li
                    key={v.userId}
                    className={`voter voter-${v.answer.toLowerCase()}`}
                    title={ANSWERS.find((a) => a.value === v.answer)!.label}
                  >
                    {v.answer === 'YES' ? (
                      <Check size={12} />
                    ) : v.answer === 'MAYBE' ? (
                      <CircleHelp size={12} />
                    ) : (
                      <X size={12} />
                    )}
                    {names.get(v.userId)}
                  </li>
                ))}
              </ul>

              {isOrganizer && (
                <button type="button" className="btn btn-small option-pick" onClick={() => setConfirming(o.time)}>
                  Pick this time
                </button>
              )}
            </li>
          )
        })}
      </ul>

      <InlineError error={vote.error ?? cancelVoting.error} />

      <footer className="voting-footer">
        <span className="muted small">{waiting.length > 0 ? waiting.join(' · ') : 'Everyone has voted'}</span>
        {isOrganizer && (
          <button
            type="button"
            className="btn btn-ghost btn-small"
            disabled={cancelVoting.isPending}
            onClick={() => {
              if (window.confirm('Stop the vote and let everyone change their free time again? Votes will be lost.')) {
                cancelVoting.mutate()
              }
            }}
          >
            <Undo2 size={16} aria-hidden="true" /> Reopen free time
          </button>
        )}
      </footer>
    </section>
  )
}
