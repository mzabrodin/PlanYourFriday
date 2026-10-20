import { Sparkles, Vote } from 'lucide-react'
import { useState } from 'react'
import type { GroupDto, SuggestedTimeDto, TimeRange } from '../../api'
import { useStartVoting, useSuggestions } from '../../api/queries'
import { formatDuration, formatRange, fromLocalInput } from '../../lib/time'
import { Card, InlineError } from '../States'
import { ConfirmEventForm } from './ConfirmEventForm'
import { joinNames, memberNameMap } from './memberNames'

const MAX_OPTIONS = 6

export function SuggestionsPanel({ group, meId }: { group: GroupDto; meId: string | undefined }) {
  const suggestions = useSuggestions(group.id)
  const isOrganizer = group.myRole === 'ORGANIZER'
  const ready = group.members.filter((m) => m.hasAvailability)
  const waitingFor = group.members.filter((m) => !m.hasAvailability)
  const names = memberNameMap(group, meId)
  const organizerName = group.members.find((m) => m.role === 'ORGANIZER')?.displayName ?? 'The organizer'

  const [selected, setSelected] = useState<Set<string> | null>(null)
  const [confirming, setConfirming] = useState<TimeRange | null>(null)

  const list = suggestions.data ?? []
  const chosen = selected ?? new Set(list.slice(0, 3).map((s) => s.time.start))

  if (confirming) {
    return (
      <Card>
        <ConfirmEventForm group={group} time={confirming} onCancel={() => setConfirming(null)} />
      </Card>
    )
  }

  function toggle(s: SuggestedTimeDto) {
    const next = new Set(chosen)
    if (next.has(s.time.start)) next.delete(s.time.start)
    else if (next.size < MAX_OPTIONS) next.add(s.time.start)
    setSelected(next)
  }

  return (
    <Card title="Best times">
      <div className="readiness readiness-lg" aria-label={`${ready.length} of ${group.members.length} marked free time`}>
        <div className="readiness-bar">
          <span style={{ width: `${(ready.length / group.members.length) * 100}%` }} />
        </div>
        <span>
          {ready.length}/{group.members.length} marked free time
        </span>
      </div>

      {isOrganizer && waitingFor.length === 0 && list.length > 0 && (
        <p className="callout">
          <Sparkles size={18} aria-hidden="true" />
          Everyone's in. Pick the best times and start a vote.
        </p>
      )}
      {waitingFor.length > 0 && (
        <p className="hint">Waiting for {joinNames(waitingFor.map((m) => names.get(m.userId)!))}.</p>
      )}

      {suggestions.isPending && <p className="muted small">Looking for good times…</p>}
      {suggestions.data && list.length === 0 && (
        <p className="muted small">No suggestions yet. They appear as soon as someone marks free time.</p>
      )}

      {list.length > 0 && (
        <>
          <p className="hint">
            {formatDuration(group.eventDurationMinutes)} slots where the most people are free
            {isOrganizer ? '. Select the ones to vote on.' : ':'}
          </p>
          <ul className="suggestion-list">
            {list.map((s) => {
              const missing = group.members.filter((m) => !s.freeUserIds.includes(m.userId))
              const cant = missing.filter((m) => m.hasAvailability).map((m) => names.get(m.userId)!)
              const notMarked = missing.filter((m) => !m.hasAvailability).map((m) => names.get(m.userId)!)
              const all = missing.length === 0
              const checked = chosen.has(s.time.start)
              const content = (
                <>
                  <span className="suggestion-main">
                    <span className="suggestion-time">{formatRange(s.time)}</span>
                    <span className="muted small">
                      {all
                        ? 'Everyone can make it'
                        : [cant.length > 0 && `Can't: ${joinNames(cant)}`, notMarked.length > 0 && `Not marked yet: ${joinNames(notMarked)}`]
                            .filter(Boolean)
                            .join(' · ')}
                    </span>
                  </span>
                  <span className={`free-count${all ? ' free-count-all' : ''}`}>
                    {s.freeUserIds.length}/{group.members.length}
                  </span>
                </>
              )
              return (
                <li key={s.time.start} className={checked && isOrganizer ? 'is-selected' : ''}>
                  {isOrganizer ? (
                    <>
                      <label className="suggestion-row">
                        <input type="checkbox" checked={checked} onChange={() => toggle(s)} />
                        {content}
                      </label>
                      <button type="button" className="link-button small" onClick={() => setConfirming(s.time)}>
                        Confirm this time
                      </button>
                    </>
                  ) : (
                    <div className="suggestion-row">{content}</div>
                  )}
                </li>
              )
            })}
          </ul>
        </>
      )}

      {isOrganizer && list.length > 0 && (
        <StartVote group={group} options={list.filter((s) => chosen.has(s.time.start)).map((s) => s.time)} />
      )}
      {!isOrganizer && (
        <p className="hint">When everyone's in, {organizerName} starts a vote on the best times.</p>
      )}
    </Card>
  )
}

function StartVote({ group, options }: { group: GroupDto; options: TimeRange[] }) {
  const start = useStartVoting(group.id)
  const [deadline, setDeadline] = useState('')

  return (
    <div className="start-vote">
      <label>
        <span>
          Voting closes <span className="optional">(optional)</span>
        </span>
        <input type="datetime-local" value={deadline} onChange={(e) => setDeadline(e.target.value)} />
      </label>
      <InlineError error={start.error} />
      <button
        type="button"
        className="btn btn-primary"
        disabled={options.length === 0 || start.isPending}
        onClick={() => start.mutate({ options, deadline: deadline ? fromLocalInput(deadline) : null })}
      >
        <Vote size={18} aria-hidden="true" />
        {start.isPending ? 'Starting…' : `Start vote with ${options.length} ${options.length === 1 ? 'time' : 'times'}`}
      </button>
      <p className="hint">Friends can't change their free time once the vote starts. You can reopen it later.</p>
    </div>
  )
}
