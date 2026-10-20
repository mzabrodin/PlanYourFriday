import { ArrowLeft } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { useCreateGroup, usePrefs } from '../api/queries'
import { InlineError } from '../components/States'
import { fromLocalInput, toLocalInput } from '../lib/time'

const DURATIONS = [30, 60, 90, 120, 180, 240, 300, 480]

function defaultWindow() {
  const start = new Date()
  start.setDate(start.getDate() + ((5 - start.getDay() + 7) % 7 || 7))
  start.setHours(16, 0, 0, 0)
  const end = new Date(start)
  end.setDate(end.getDate() + 2)
  end.setHours(23, 0, 0, 0)
  return { start: toLocalInput(start), end: toLocalInput(end) }
}

export function CreateGroupPage() {
  const navigate = useNavigate()
  const create = useCreateGroup()
  const prefs = usePrefs()
  const [params] = useSearchParams()
  const [title, setTitle] = useState(() => params.get('title')?.slice(0, 120) ?? '')
  const [description, setDescription] = useState('')
  const [window, setWindow] = useState(defaultWindow)
  const [duration, setDuration] = useState(prefs.defaultEventDurationMinutes)
  const [deadline, setDeadline] = useState('')
  const [formError, setFormError] = useState<string | null>(null)

  function submit(e: FormEvent) {
    e.preventDefault()
    const start = new Date(window.start)
    const end = new Date(window.end)
    if (end <= start) return setFormError('The window must end after it starts.')
    if (end.getTime() - start.getTime() < duration * 60_000) {
      return setFormError('The window is shorter than the event duration.')
    }
    if (deadline && new Date(deadline) > start) {
      return setFormError('The availability deadline should be before the window starts.')
    }
    setFormError(null)
    create.mutate(
      {
        title: title.trim(),
        description: description.trim() || null,
        window: { start: fromLocalInput(window.start), end: fromLocalInput(window.end) },
        eventDurationMinutes: duration,
        availabilityDeadline: deadline ? fromLocalInput(deadline) : null,
      },
      { onSuccess: (group) => navigate(`/groups/${group.id}`) },
    )
  }

  return (
    <div className="narrow stack">
      <Link to="/groups" className="back icon-line">
        <ArrowLeft size={16} aria-hidden="true" /> My groups
      </Link>
      <h1>New group</h1>

      <form className="card form" onSubmit={submit}>
        <label>
          Title
          <input required maxLength={120} value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Board games night" />
        </label>

        <label>
          Description <span className="muted">(optional)</span>
          <textarea rows={3} value={description} onChange={(e) => setDescription(e.target.value)} />
        </label>

        <fieldset>
          <legend>When could it happen?</legend>
          <p className="hint">Members mark their free time inside this window.</p>
          <div className="row">
            <label>
              From
              <input type="datetime-local" required value={window.start} onChange={(e) => setWindow({ ...window, start: e.target.value })} />
            </label>
            <label>
              To
              <input type="datetime-local" required value={window.end} onChange={(e) => setWindow({ ...window, end: e.target.value })} />
            </label>
          </div>
        </fieldset>

        <div className="row">
          <label>
            Event length
            <select value={duration} onChange={(e) => setDuration(Number(e.target.value))}>
              {[...new Set([...DURATIONS, prefs.defaultEventDurationMinutes])].sort((a, b) => a - b).map((m) => (
                <option key={m} value={m}>
                  {m < 60 ? `${m} min` : `${m / 60} h`}
                </option>
              ))}
            </select>
          </label>
          <label>
            Availability deadline <span className="muted">(optional)</span>
            <input type="datetime-local" value={deadline} onChange={(e) => setDeadline(e.target.value)} />
          </label>
        </div>

        {formError && <p className="inline-error">{formError}</p>}
        <InlineError error={create.error} />

        <div className="form-actions">
          <Link to="/groups" className="btn btn-ghost">
            Cancel
          </Link>
          <button type="submit" className="btn btn-primary" disabled={create.isPending}>
            {create.isPending ? 'Creating…' : 'Create group'}
          </button>
        </div>
      </form>
    </div>
  )
}
