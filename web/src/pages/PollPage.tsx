import { ArrowLeft, ArrowRight, Check, Hourglass, Lock, Plus, Shuffle, Trash2, Trophy } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import type { PollDto } from '../api'
import { useAddPollOption, useClosePoll, useDeletePoll, useMe, usePoll, useVotePoll } from '../api/queries'
import { ErrorState, InlineError, Loading } from '../components/States'
import { UserAvatar } from '../components/UserAvatar'
import { formatDateTime, formatRelative } from '../lib/time'

const MAX_OPTIONS = 12

export function PollPage() {
  const { pollId = '' } = useParams()
  const { data: poll, isPending, error, refetch } = usePoll(pollId)
  const { data: me } = useMe()

  if (isPending) return <Loading />
  if (error || !poll) {
    return (
      <div className="narrow stack">
        <ErrorState error={error} onRetry={refetch} />
        <Link to="/ideas" className="btn">
          Back to ideas
        </Link>
      </div>
    )
  }
  return <Poll poll={poll} meId={me?.id} />
}

function Poll({ poll, meId }: { poll: PollDto; meId: string | undefined }) {
  const navigate = useNavigate()
  const vote = useVotePoll(poll.id, meId)
  const close = useClosePoll(poll.id)
  const remove = useDeletePoll(poll.id)
  const [now] = useState(Date.now)

  const isCreator = poll.creatorId === meId
  const closed = poll.status === 'CLOSED'
  const names = new Map(poll.participants.map((p) => [p.userId, p.userId === meId ? 'You' : p.displayName]))
  const myVotes = poll.options.filter((o) => meId && o.voterIds.includes(meId)).map((o) => o.id)
  const voters = new Set(poll.options.flatMap((o) => o.voterIds))
  const maxVotes = Math.max(1, ...poll.options.map((o) => o.voterIds.length))
  const winners = poll.options.filter((o) => poll.winnerOptionIds.includes(o.id))
  const options = closed ? [...poll.options].sort((a, b) => b.voterIds.length - a.voterIds.length) : poll.options

  function toggle(optionId: string) {
    if (closed || vote.isPending) return
    const on = myVotes.includes(optionId)
    const next = poll.multipleChoice
      ? on
        ? myVotes.filter((id) => id !== optionId)
        : [...myVotes, optionId]
      : on
        ? []
        : [optionId]
    vote.mutate(next)
  }

  return (
    <div className="narrow stack poll-page">
      <Link to="/ideas" className="back icon-line">
        <ArrowLeft size={16} aria-hidden="true" /> Ideas
      </Link>

      <header className="poll-header">
        <div>
          <p className="eyebrow">
            {closed ? 'Decided' : 'Poll'} · by {poll.creatorId === meId ? 'you' : poll.creatorName}
          </p>
          <h1>{poll.title}</h1>
          {poll.description && <p className="muted">{poll.description}</p>}
        </div>
        <div className="poll-meta">
          {poll.deadline && !closed && (
            <span className="deadline">
              <Hourglass size={14} aria-hidden="true" /> closes {formatRelative(poll.deadline, now)}
            </span>
          )}
          {closed && (
            <span className="deadline">
              <Lock size={14} aria-hidden="true" /> voting closed
            </span>
          )}
          <span className="muted small">
            {poll.participants.filter((p) => voters.has(p.userId)).length}/{poll.participants.length} voted
          </span>
        </div>
      </header>

      {closed && <Result poll={poll} winners={winners.map((w) => w.title)} />}

      <section className="card">
        {!closed && (
          <p className="muted small poll-instructions">
            {poll.multipleChoice ? 'Tap every option you like.' : 'Pick one option.'} You can change your vote until it
            closes.
          </p>
        )}
        <ul className="poll-options" role={poll.multipleChoice ? 'group' : 'radiogroup'} aria-label="Options">
          {options.map((o) => {
            const mine = myVotes.includes(o.id)
            const won = poll.winnerOptionIds.includes(o.id)
            return (
              <li key={o.id}>
                <button
                  type="button"
                  role={poll.multipleChoice ? 'checkbox' : 'radio'}
                  aria-checked={mine}
                  className={`poll-option${mine ? ' is-mine' : ''}${won ? ' is-winner' : ''}`}
                  disabled={closed}
                  onClick={() => toggle(o.id)}
                >
                  <span className={`poll-mark${poll.multipleChoice ? '' : ' poll-mark-radio'}`} aria-hidden="true">
                    {mine && <Check size={14} />}
                  </span>
                  <span className="poll-option-body">
                    <span className="poll-option-top">
                      <span className="poll-option-title">
                        {won && <Trophy size={15} aria-hidden="true" />} {o.title}
                      </span>
                      <span className="poll-count">{o.voterIds.length}</span>
                    </span>
                    <span className="poll-bar" aria-hidden="true">
                      <span style={{ width: `${(o.voterIds.length / maxVotes) * 100}%` }} />
                    </span>
                    {o.voterIds.length > 0 && (
                      <span className="poll-voters">
                        {o.voterIds.map((id) => names.get(id) ?? 'Someone').join(', ')}
                      </span>
                    )}
                  </span>
                </button>
              </li>
            )
          })}
        </ul>
        <InlineError error={vote.error} />

        {!closed && (poll.allowNewOptions || isCreator) && poll.options.length < MAX_OPTIONS && (
          <AddOption pollId={poll.id} />
        )}
      </section>

      <section className="card">
        <h2 className="card-subtitle">Who's voting</h2>
        <ul className="poll-people">
          {poll.participants.map((p) => (
            <li key={p.userId}>
              <UserAvatar user={p} size={28} />
              <span>{names.get(p.userId)}</span>
              {voters.has(p.userId) ? (
                <span className="tick">
                  <Check size={14} aria-hidden="true" /> voted
                </span>
              ) : (
                <span className="muted small">{closed ? "didn't vote" : 'not yet'}</span>
              )}
            </li>
          ))}
        </ul>
        <p className="hint">Started {formatDateTime(poll.createdAt)}</p>
      </section>

      {isCreator && (
        <div className="poll-actions">
          {!closed && (
            <button
              type="button"
              className="btn"
              disabled={close.isPending}
              onClick={() => {
                if (window.confirm('Close the poll now? Nobody will be able to vote after that.')) close.mutate()
              }}
            >
              <Lock size={16} aria-hidden="true" /> Close voting
            </button>
          )}
          <button
            type="button"
            className="btn btn-danger"
            disabled={remove.isPending}
            onClick={() => {
              if (window.confirm('Delete this poll for everyone?')) {
                remove.mutate(undefined, { onSuccess: () => navigate('/ideas', { replace: true }) })
              }
            }}
          >
            <Trash2 size={16} aria-hidden="true" /> Delete poll
          </button>
          <InlineError error={close.error ?? remove.error} />
        </div>
      )}
    </div>
  )
}

function Result({ poll, winners }: { poll: PollDto; winners: string[] }) {
  if (winners.length === 0) {
    return (
      <section className="poll-result poll-result-empty">
        <p className="eyebrow">Result</p>
        <p className="poll-result-title">Nobody voted</p>
        <p className="muted small">Start a new poll or spin the wheel instead.</p>
        <Link to="/wheel" className="btn">
          <Shuffle size={16} aria-hidden="true" /> Open the wheel
        </Link>
      </section>
    )
  }

  const groupLink = (title: string) =>
    `/groups/new?title=${encodeURIComponent(title)}&poll=${encodeURIComponent(poll.id)}`
  const tie = winners.length > 1

  return (
    <section className="poll-result">
      <p className="eyebrow">{tie ? "It's a tie" : 'The winner is'}</p>
      <p className="poll-result-title">
        <Trophy size={26} aria-hidden="true" /> {winners.join(' / ')}
      </p>

      {poll.groupId ? (
        <Link to={`/groups/${poll.groupId}`} className="btn btn-on-dark">
          Go to group <ArrowRight size={16} aria-hidden="true" />
        </Link>
      ) : tie ? (
        <div className="poll-result-actions">
          <Link
            to={`/wheel?${[...winners.map((w) => `option=${encodeURIComponent(w)}`), `poll=${encodeURIComponent(poll.id)}`].join('&')}`}
            className="btn btn-on-dark"
          >
            <Shuffle size={16} aria-hidden="true" /> Break the tie on the wheel
          </Link>
          {winners.map((w) => (
            <Link key={w} to={groupLink(w)} className="btn btn-ghost btn-small poll-result-pick">
              Plan "{w}"
            </Link>
          ))}
        </div>
      ) : (
        <Link to={groupLink(winners[0])} className="btn btn-on-dark">
          Create a group with this <ArrowRight size={16} aria-hidden="true" />
        </Link>
      )}
    </section>
  )
}

function AddOption({ pollId }: { pollId: string }) {
  const add = useAddPollOption(pollId)
  const [draft, setDraft] = useState('')

  function submit(e: FormEvent) {
    e.preventDefault()
    const title = draft.trim()
    if (!title) return
    add.mutate(title, { onSuccess: () => setDraft('') })
  }

  return (
    <form className="subsection" onSubmit={submit}>
      <div className="copy-row">
        <input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          maxLength={80}
          placeholder="Suggest another idea"
          aria-label="New option"
        />
        <button type="submit" className="btn" disabled={add.isPending || !draft.trim()}>
          <Plus size={16} aria-hidden="true" /> Add
        </button>
      </div>
      <InlineError error={add.error} />
    </form>
  )
}
