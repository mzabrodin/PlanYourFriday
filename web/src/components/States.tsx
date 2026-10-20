import type { ReactNode } from 'react'
import type { EventStatus, GroupStatus, InvitationStatus } from '../api'
import { errorMessage } from '../lib/errors'

export function Loading({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="state" role="status">
      <span className="spinner" aria-hidden="true" />
      {label}
    </div>
  )
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <div className="state state-error" role="alert">
      <p>{errorMessage(error)}</p>
      {onRetry && (
        <button type="button" className="btn" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  )
}

export function InlineError({ error }: { error: unknown }) {
  if (!error) return null
  return (
    <p className="inline-error" role="alert">
      {errorMessage(error)}
    </p>
  )
}

const badgeTone: Record<GroupStatus | InvitationStatus | EventStatus, string> = {
  PLANNING: 'info',
  VOTING: 'vote',
  SCHEDULED: 'ok',
  CONFIRMED: 'ok',
  ACCEPTED: 'ok',
  CANCELLED: 'muted',
  REVOKED: 'muted',
  PENDING: 'warn',
  SENT: 'warn',
}

export function StatusBadge({ status }: { status: GroupStatus | InvitationStatus | EventStatus }) {
  return <span className={`badge badge-${badgeTone[status]}`}>{status.toLowerCase()}</span>
}

export function Card({ title, actions, children }: { title?: ReactNode; actions?: ReactNode; children: ReactNode }) {
  return (
    <section className="card">
      {(title || actions) && (
        <header className="card-header">
          {title && <h2>{title}</h2>}
          {actions && <div className="card-actions">{actions}</div>}
        </header>
      )}
      {children}
    </section>
  )
}
