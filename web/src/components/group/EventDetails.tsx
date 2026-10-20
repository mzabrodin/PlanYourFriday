import {
  CalendarCheck,
  CalendarDays,
  CalendarX,
  Check,
  CircleAlert,
  Clock,
  Copy,
  Crown,
  Download,
  ExternalLink,
  MapPin,
  NotebookPen,
  Pencil,
  RefreshCw,
  Video,
} from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { api } from '../../api'
import type { EventDto, GroupDto } from '../../api'
import { useCancelEvent, useUpdateEvent } from '../../api/queries'
import { downloadIcs, mapsUrl } from '../../lib/ics'
import { formatDuration, formatRange, formatRelative } from '../../lib/time'
import { DateTile } from '../home/DateTile'
import { InlineError } from '../States'
import { UserAvatar } from '../UserAvatar'

export function EventDetails({ group, event, meId }: { group: GroupDto; event: EventDto; meId: string | undefined }) {
  const isOrganizer = group.myRole === 'ORGANIZER'
  const [editing, setEditing] = useState(false)
  const [copied, setCopied] = useState(false)
  const [now] = useState(Date.now)
  const cancel = useCancelEvent(group.id)
  const past = new Date(event.time.end).getTime() < now
  const minutes = Math.round((new Date(event.time.end).getTime() - new Date(event.time.start).getTime()) / 60_000)

  async function copyDetails() {
    const text = [event.title, formatRange(event.time), event.location, event.meetingLink, event.description]
      .filter(Boolean)
      .join('\n')
    try {
      await navigator.clipboard.writeText(text)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {}
  }

  return (
    <section className="event-details" aria-labelledby="event-title">
      <div className="event-main">
        <div className="event-head">
          <DateTile iso={event.time.start} color="var(--ok)" />
          <div>
            <p className="eyebrow">{past ? 'This happened' : `It's on · ${formatRelative(event.time.start, now)}`}</p>
            <h2 id="event-title">{event.title}</h2>
          </div>
        </div>

        {editing ? (
          <EditEventForm group={group} event={event} onDone={() => setEditing(false)} />
        ) : (
          <>
            <ul className="event-facts">
              <li>
                <Clock size={18} aria-hidden="true" />
                <span>
                  {formatRange(event.time)} <span className="muted">· {formatDuration(minutes)}</span>
                </span>
              </li>
              {event.location && (
                <li>
                  <MapPin size={18} aria-hidden="true" />
                  <span>
                    {event.location}{' '}
                    <a href={mapsUrl(event.location)} target="_blank" rel="noreferrer" className="small">
                      Open in Maps <ExternalLink size={12} aria-hidden="true" />
                    </a>
                  </span>
                </li>
              )}
              {event.meetingLink && (
                <li>
                  <Video size={18} aria-hidden="true" />
                  <a href={event.meetingLink} target="_blank" rel="noreferrer">
                    {event.meetingLink}
                  </a>
                </li>
              )}
              {event.description && (
                <li>
                  <NotebookPen size={18} aria-hidden="true" />
                  <p className="event-notes">{event.description}</p>
                </li>
              )}
            </ul>

            <CalendarSyncLine status={event.myCalendarSync} />

            <div className="event-actions">
              <button type="button" className="btn btn-small" onClick={() => downloadIcs(event)}>
                <Download size={16} aria-hidden="true" /> Download .ics
              </button>
              <button type="button" className="btn btn-small" onClick={copyDetails}>
                {copied ? <Check size={16} aria-hidden="true" /> : <Copy size={16} aria-hidden="true" />}
                {copied ? 'Copied' : 'Copy details'}
              </button>
              {isOrganizer && !past && (
                <>
                  <button type="button" className="btn btn-small" onClick={() => setEditing(true)}>
                    <Pencil size={16} aria-hidden="true" /> Edit details
                  </button>
                  <button
                    type="button"
                    className="btn btn-small btn-danger"
                    disabled={cancel.isPending}
                    onClick={() => {
                      if (window.confirm('Cancel this event for everyone? It will be removed from their calendars.')) {
                        cancel.mutate(event.id)
                      }
                    }}
                  >
                    <CalendarX size={16} aria-hidden="true" /> {cancel.isPending ? 'Cancelling…' : 'Cancel event'}
                  </button>
                </>
              )}
            </div>
            <InlineError error={cancel.error} />
          </>
        )}
      </div>

      <aside className="event-people" aria-label="Who's coming">
        <h3>Who's coming · {group.members.length}</h3>
        <ul>
          {group.members.map((m) => (
            <li key={m.userId}>
              <UserAvatar user={{ displayName: m.displayName, photoUrl: null }} size={30} />
              <span>{m.userId === meId && m.displayName !== 'You' ? `${m.displayName} (you)` : m.displayName}</span>
              {m.role === 'ORGANIZER' && (
                <span className="organizer-tag" title="Organizer">
                  <Crown size={14} aria-hidden="true" /> organizer
                </span>
              )}
            </li>
          ))}
        </ul>
      </aside>
    </section>
  )
}

function CalendarSyncLine({ status }: { status: EventDto['myCalendarSync'] }) {
  switch (status) {
    case 'SYNCED':
      return (
        <p className="sync-line sync-ok">
          <CalendarCheck size={16} aria-hidden="true" /> In your Google Calendar
        </p>
      )
    case 'PENDING':
      return (
        <p className="sync-line">
          <RefreshCw size={16} aria-hidden="true" /> Adding to your Google Calendar…
        </p>
      )
    case 'FAILED':
      return (
        <p className="sync-line sync-failed">
          <CircleAlert size={16} aria-hidden="true" /> Couldn't add it to your Google Calendar. We'll retry soon.
        </p>
      )
    case 'NOT_CONNECTED':
      return (
        <p className="sync-line">
          <CalendarDays size={16} aria-hidden="true" /> Not in your Google Calendar.{' '}
          <button type="button" className="link-button" onClick={() => api.startCalendarConnect(window.location.pathname)}>
            Connect it
          </button>{' '}
          or download the .ics file.
        </p>
      )
  }
}

function EditEventForm({ group, event, onDone }: { group: GroupDto; event: EventDto; onDone: () => void }) {
  const update = useUpdateEvent(group.id)
  const [title, setTitle] = useState(event.title)
  const [location, setLocation] = useState(event.location)
  const [meetingLink, setMeetingLink] = useState(event.meetingLink ?? '')
  const [description, setDescription] = useState(event.description ?? '')

  function submit(e: FormEvent) {
    e.preventDefault()
    update.mutate(
      {
        eventId: event.id,
        body: {
          title: title.trim(),
          location: location.trim(),
          meetingLink: meetingLink.trim() || null,
          description: description.trim() || null,
        },
      },
      { onSuccess: onDone },
    )
  }

  return (
    <form className="form" onSubmit={submit}>
      <label>
        Title
        <input required value={title} onChange={(e) => setTitle(e.target.value)} />
      </label>
      <label>
        Location
        <input required value={location} onChange={(e) => setLocation(e.target.value)} />
      </label>
      <label>
        Meeting link <span className="muted">(optional)</span>
        <input type="url" value={meetingLink} onChange={(e) => setMeetingLink(e.target.value)} placeholder="https://" />
      </label>
      <label>
        Notes <span className="muted">(optional)</span>
        <textarea rows={3} value={description} onChange={(e) => setDescription(e.target.value)} />
      </label>
      <p className="hint">Changes are sent to everyone's calendar. To change the time, cancel and pick a new one.</p>
      <InlineError error={update.error} />
      <div className="form-actions">
        <button type="button" className="btn btn-ghost" onClick={onDone}>
          Cancel
        </button>
        <button type="submit" className="btn btn-primary" disabled={update.isPending}>
          {update.isPending ? 'Saving…' : 'Save'}
        </button>
      </div>
    </form>
  )
}
