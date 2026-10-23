import { ArrowRight, Hourglass, Lightbulb, Plus, Trophy } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import type { PollSummaryDto } from '../api'
import { usePolls } from '../api/queries'
import { ErrorState, Loading } from '../components/States'
import { formatRelative } from '../lib/time'

export function IdeasPage() {
  const { data: polls, isPending, error, refetch } = usePolls()
  const [now] = useState(Date.now)

  const open = (polls ?? [])
    .filter((p) => p.status === 'OPEN')
    .sort((a, b) => Number(b.myVotePending) - Number(a.myVotePending) || b.createdAt.localeCompare(a.createdAt))
  const closed = (polls ?? []).filter((p) => p.status === 'CLOSED')

  return (
    <div className="stack">
      <div className="page-header">
        <div>
          <h1>Ideas</h1>
          <p className="muted">Can't agree on what to do? Ask your friends and let them vote.</p>
        </div>
        <Link to="/ideas/new" className="btn btn-primary">
          <Plus size={18} aria-hidden="true" /> New poll
        </Link>
      </div>

      {isPending && <Loading />}
      {error && <ErrorState error={error} onRetry={refetch} />}

      {polls?.length === 0 && (
        <div className="empty">
          <Lightbulb size={32} aria-hidden="true" className="empty-icon" />
          <p>No polls yet.</p>
          <p className="muted">Start one: add a few ideas, pick friends, and see what wins.</p>
        </div>
      )}

      {open.length > 0 && (
        <section className="stack">
          <h2 className="section-title">Open · {open.length}</h2>
          <ul className="poll-grid">
            {open.map((p) => (
              <PollCard key={p.id} poll={p} now={now} />
            ))}
          </ul>
        </section>
      )}

      {closed.length > 0 && (
        <section className="stack">
          <h2 className="section-title">Decided · {closed.length}</h2>
          <ul className="poll-grid">
            {closed.map((p) => (
              <PollCard key={p.id} poll={p} now={now} />
            ))}
          </ul>
        </section>
      )}
    </div>
  )
}

function PollCard({ poll, now }: { poll: PollSummaryDto; now: number }) {
  const closed = poll.status === 'CLOSED'
  const progress = poll.participantCount ? (poll.votedCount / poll.participantCount) * 100 : 0
  return (
    <li>
      <Link
        to={`/ideas/${poll.id}`}
        className={`poll-card${poll.myVotePending ? ' poll-card-alert' : ''}${closed ? ' poll-card-closed' : ''}`}
      >
        <div className="poll-card-top">
          <h3>{poll.title}</h3>
          {poll.myVotePending ? (
            <span className="badge badge-vote badge-sentence">Vote needed</span>
          ) : (
            <span className={`badge ${closed ? 'badge-ok' : 'badge-info'}`}>{closed ? 'decided' : 'open'}</span>
          )}
        </div>
        <p className="muted small">
          by {poll.creatorName} · {poll.optionCount} options
        </p>

        {poll.leadingOptions.length > 0 && (
          <p className="poll-leading icon-line">
            <Trophy size={15} aria-hidden="true" />
            <span>
              {closed ? (poll.leadingOptions.length > 1 ? 'Tie: ' : 'Winner: ') : 'Leading: '}
              <strong>{poll.leadingOptions.join(', ')}</strong>
            </span>
          </p>
        )}

        <div className="poll-card-footer">
          <div className="readiness">
            <div className="readiness-bar" aria-hidden="true">
              <span style={{ width: `${progress}%` }} />
            </div>
            <span className="muted small">
              {poll.votedCount}/{poll.participantCount} voted
            </span>
          </div>
          {!closed && poll.deadline && (
            <span className="deadline muted small">
              <Hourglass size={13} aria-hidden="true" /> closes {formatRelative(poll.deadline, now)}
            </span>
          )}
          {closed && poll.groupId && <span className="badge badge-ok">planned</span>}
          <ArrowRight size={16} aria-hidden="true" className="poll-card-arrow" />
        </div>
      </Link>
    </li>
  )
}
