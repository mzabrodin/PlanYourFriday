import { ArrowLeft, Ban, CalendarDays, Check, ChevronDown, Clock, Hourglass, Trash2 } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import type { GroupDto, TimeRange } from '../api'
import {
  useAvailability,
  useCancelGroup,
  useDeleteGroup,
  useGroup,
  useImportFromCalendar,
  useMe,
  useOptimalSlots,
  usePrefs,
  useUpdateAvailability,
} from '../api/queries'
import { api } from '../api'
import { AvailabilityCalendar } from '../components/AvailabilityCalendar'
import { EventDetails } from '../components/group/EventDetails'
import { SuggestionsPanel } from '../components/group/SuggestionsPanel'
import { VotingPanel } from '../components/group/VotingPanel'
import { MembersPanel } from '../components/MembersPanel'
import { Card, ErrorState, InlineError, Loading, StatusBadge } from '../components/States'
import { formatDateTime, formatDuration, formatRange, mergeRanges, usualSlots } from '../lib/time'

export function GroupPage() {
  const { groupId = '' } = useParams()
  const { data: me } = useMe()
  const group = useGroup(groupId)
  const availability = useAvailability(groupId)
  const optimal = useOptimalSlots(groupId)
  const update = useUpdateAvailability(groupId)
  const importCalendar = useImportFromCalendar(groupId)
  const prefs = usePrefs()
  const hasUsualHours = prefs.usualFreeTime.weekdays !== null || prefs.usualFreeTime.weekends !== null

  const [draft, setDraft] = useState<TimeRange[] | null>(null)
  const [openedAt] = useState(Date.now)

  if (group.isPending) return <Loading />
  if (group.error) return <ErrorState error={group.error} onRetry={group.refetch} />

  const g = group.data
  const savedMine = availability.data?.find((a) => a.userId === me?.id)?.slots ?? []
  const mySlots = draft ?? savedMine
  const otherSlots = availability.data?.filter((a) => a.userId !== me?.id).flatMap((a) => a.slots) ?? []
  const deadlinePassed = g.availabilityDeadline !== null && new Date(g.availabilityDeadline).getTime() < openedAt
  const canEdit = g.status === 'PLANNING' && !deadlinePassed

  function importFromCalendar() {
    if (draft && !window.confirm('Importing replaces your unsaved changes. Continue?')) return
    importCalendar.mutate(undefined, { onSuccess: () => setDraft(null) })
  }

  return (
    <div className="stack">
      <Link to="/groups" className="back icon-line">
        <ArrowLeft size={16} aria-hidden="true" /> My events
      </Link>

      <header className="group-header">
        <div>
          <div className="title-row">
            <h1>{g.title}</h1>
            <StatusBadge status={g.status} />
          </div>
          {g.description && <p>{g.description}</p>}
          <ul className="group-facts">
            <li>
              <CalendarDays size={16} aria-hidden="true" /> {formatRange(g.window)}
            </li>
            <li>
              <Clock size={16} aria-hidden="true" /> {formatDuration(g.eventDurationMinutes)} event
            </li>
            {g.availabilityDeadline && g.status === 'PLANNING' && (
              <li>
                <Hourglass size={16} aria-hidden="true" /> mark free time by {formatDateTime(g.availabilityDeadline)}
              </li>
            )}
          </ul>
        </div>
        <OrganizerActions group={g} />
      </header>

      {g.status === 'CANCELLED' && (
        <p className="notice notice-cancelled icon-line">
          <Ban size={18} aria-hidden="true" />
          {g.myRole === 'ORGANIZER'
            ? 'You cancelled this event. Everyone was notified.'
            : `${g.members.find((m) => m.role === 'ORGANIZER')?.displayName ?? 'The organizer'} cancelled this event.`}
        </p>
      )}

      <PhaseSteps status={g.status} />

      {g.event && <EventDetails group={g} event={g.event} meId={me?.id} />}
      {g.status === 'VOTING' && <VotingPanel group={g} meId={me?.id} />}

      <div className={`group-layout${g.status === 'SCHEDULED' ? ' group-layout-single' : ''}`}>
        <Collapsible
          collapsed={g.status === 'SCHEDULED' || g.status === 'CANCELLED'}
          summary="Free time everyone marked"
        >
          <Card
            title={canEdit ? 'When are you free?' : 'Availability'}
            actions={
              canEdit && (
                <>
                  {hasUsualHours && (
                    <button
                      type="button"
                      className="btn"
                      title="Adds your usual free hours from Settings to every day of the window"
                      onClick={() => setDraft(mergeRanges([...mySlots, ...usualSlots(g.window, prefs.usualFreeTime)]))}
                    >
                      Fill my usual hours
                    </button>
                  )}
                  {me?.calendarConnected ? (
                    <button
                      type="button"
                      className="btn"
                      disabled={importCalendar.isPending}
                      onClick={importFromCalendar}
                    >
                      {importCalendar.isPending ? 'Importing…' : 'Import from Google Calendar'}
                    </button>
                  ) : (
                    <button
                      type="button"
                      className="btn"
                      onClick={() => api.startCalendarConnect(window.location.pathname)}
                    >
                      Connect Google Calendar to import
                    </button>
                  )}
                </>
              )
            }
          >
            {canEdit ? (
              <p className="hint">
                Drag on the calendar to add free time. Drag or resize to adjust, click a block to remove it.
              </p>
            ) : (
              <p className="hint">
                {g.status === 'PLANNING'
                  ? 'The availability deadline has passed.'
                  : g.status === 'VOTING'
                    ? 'Free time is locked while everyone votes. Highlighted: the times being voted on.'
                    : 'Free time is closed for this event.'}
              </p>
            )}

            {g.status === 'PLANNING' && <Legend />}

            {availability.error ? (
              <ErrorState error={availability.error} onRetry={availability.refetch} />
            ) : (
              <AvailabilityCalendar
                window={g.window}
                mySlots={mySlots}
                otherSlots={otherSlots}
                optimalSlots={
                  g.status === 'PLANNING'
                    ? (optimal.data ?? [])
                    : g.status === 'VOTING'
                      ? g.options.map((o) => o.time)
                      : []
                }
                event={g.event}
                editable={canEdit}
                onChange={setDraft}
              />
            )}

            <InlineError error={update.error ?? importCalendar.error} />

            {canEdit && (
              <div className="save-bar" data-dirty={draft !== null}>
                <span className="muted small">{draft ? 'You have unsaved changes' : 'All changes saved'}</span>
                <button
                  type="button"
                  className="btn btn-ghost"
                  disabled={!draft || update.isPending}
                  onClick={() => setDraft(null)}
                >
                  Discard
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  disabled={!draft || update.isPending}
                  onClick={() => draft && update.mutate(draft, { onSuccess: () => setDraft(null) })}
                >
                  {update.isPending ? 'Saving…' : 'Save my free time'}
                </button>
              </div>
            )}
          </Card>
        </Collapsible>

        <aside className="stack">
          {g.status === 'PLANNING' && <SuggestionsPanel group={g} meId={me?.id} />}
          {g.status !== 'SCHEDULED' && <MembersPanel group={g} />}
        </aside>
      </div>
    </div>
  )
}

function OrganizerActions({ group }: { group: GroupDto }) {
  const navigate = useNavigate()
  const cancel = useCancelGroup(group.id)
  const remove = useDeleteGroup(group.id)
  const [now] = useState(Date.now)
  if (group.myRole !== 'ORGANIZER') return null

  const over = group.status === 'CANCELLED' || new Date(group.event?.time.end ?? group.window.end).getTime() < now
  // A scheduled event has its own "Cancel event" button next to the details.
  const canCancel = !over && group.status !== 'SCHEDULED'

  if (!canCancel && !over) return null
  return (
    <div className="group-header-actions">
      {canCancel && (
        <button
          type="button"
          className="btn btn-small btn-danger"
          disabled={cancel.isPending}
          onClick={() => {
            if (window.confirm('Cancel this event for everyone? Everyone gets notified.')) cancel.mutate()
          }}
        >
          <Ban size={16} aria-hidden="true" /> {cancel.isPending ? 'Cancelling…' : 'Cancel event'}
        </button>
      )}
      {over && (
        <button
          type="button"
          className="btn btn-small btn-danger"
          disabled={remove.isPending}
          onClick={() => {
            if (window.confirm("Delete this event for everyone? This can't be undone.")) {
              remove.mutate(undefined, { onSuccess: () => navigate('/groups', { replace: true }) })
            }
          }}
        >
          <Trash2 size={16} aria-hidden="true" /> {remove.isPending ? 'Deleting…' : 'Delete event'}
        </button>
      )}
      <InlineError error={cancel.error ?? remove.error} />
    </div>
  )
}

function Legend() {
  return (
    <ul className="legend">
      <li>
        <span className="swatch swatch-mine" /> You're free
      </li>
      <li>
        <span className="swatch swatch-others" /> Others free (darker = more people)
      </li>
      <li>
        <span className="swatch swatch-optimal" /> Everyone free
      </li>
    </ul>
  )
}

const PHASES = [
  { key: 'PLANNING', label: 'Mark free time' },
  { key: 'VOTING', label: 'Vote on a time' },
  { key: 'SCHEDULED', label: "It's on" },
] as const

function PhaseSteps({ status }: { status: GroupDto['status'] }) {
  if (status === 'CANCELLED') return null
  const current = PHASES.findIndex((p) => p.key === status)
  return (
    <ol className="phase-steps" aria-label="Planning progress">
      {PHASES.map((p, i) => (
        <li
          key={p.key}
          className={i < current ? 'done' : i === current ? 'current' : ''}
          aria-current={i === current ? 'step' : undefined}
        >
          <span className="phase-pill">
            <span className="phase-dot">{i < current ? <Check size={12} aria-hidden="true" /> : i + 1}</span>
            {p.label}
          </span>
          {i < PHASES.length - 1 && <span className="phase-line" aria-hidden="true" />}
        </li>
      ))}
    </ol>
  )
}

function Collapsible({ collapsed, summary, children }: { collapsed: boolean; summary: string; children: ReactNode }) {
  if (!collapsed) return <>{children}</>
  return (
    <details className="collapsible">
      <summary>
        {summary} <ChevronDown size={18} aria-hidden="true" />
      </summary>
      {children}
    </details>
  )
}
