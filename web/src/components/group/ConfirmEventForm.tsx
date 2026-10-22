import { Clock } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import type { GroupDto, TimeRange } from '../../api'
import { useConfirmEvent } from '../../api/queries'
import { formatRange } from '../../lib/time'
import { InlineError } from '../States'

export function ConfirmEventForm({
  group,
  time,
  onCancel,
}: {
  group: GroupDto
  time: TimeRange
  onCancel: () => void
}) {
  const confirm = useConfirmEvent(group.id)
  const [title, setTitle] = useState(group.title)
  const [location, setLocation] = useState('')
  const [meetingLink, setMeetingLink] = useState('')
  const [description, setDescription] = useState('')

  function submit(e: FormEvent) {
    e.preventDefault()
    confirm.mutate({
      title: title.trim(),
      time,
      location: location.trim(),
      meetingLink: meetingLink.trim() || null,
      description: description.trim() || null,
    })
  }

  return (
    <form className="form confirm-form" onSubmit={submit}>
      <div className="confirm-form-head">
        <h3>Confirm the event</h3>
        <p className="icon-line">
          <Clock size={16} aria-hidden="true" /> {formatRange(time)}
        </p>
      </div>
      <label>
        Title
        <input required value={title} onChange={(e) => setTitle(e.target.value)} />
      </label>
      <label>
        Location
        <input required value={location} onChange={(e) => setLocation(e.target.value)} placeholder="Address or place" />
      </label>
      <label>
        <span>
          Meeting link <span className="optional">(optional)</span>
        </span>
        <input type="url" value={meetingLink} onChange={(e) => setMeetingLink(e.target.value)} placeholder="https://" />
      </label>
      <label>
        <span>
          Notes <span className="optional">(optional)</span>
        </span>
        <textarea
          rows={3}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="What to bring, how to find the place…"
        />
      </label>
      <InlineError error={confirm.error} />
      <p className="hint">Everyone gets this event in their Google Calendar, with you as the organizer.</p>
      <div className="form-actions">
        <button type="button" className="btn btn-ghost" onClick={onCancel}>
          Back
        </button>
        <button type="submit" className="btn btn-primary" disabled={confirm.isPending}>
          {confirm.isPending ? 'Confirming…' : 'Confirm event'}
        </button>
      </div>
    </form>
  )
}
