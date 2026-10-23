import { ArrowLeft } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { useCreateGroup, useFriends, useMe, usePoll, usePrefs } from '../api/queries'
import { FriendPicker } from '../components/friends/FriendPicker'
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
  const pollId = params.get('poll')
  const poll = usePoll(pollId)
  const friends = useFriends()
  const { data: me } = useMe()
  const [members, setMembers] = useState<string[]>([])
  const [preselectedFrom, setPreselectedFrom] = useState<string | null>(null)

  const friendIds = new Set(friends.data?.friends.map((f) => f.userId))
  if (poll.data && friends.data && preselectedFrom !== poll.data.id) {
    setPreselectedFrom(poll.data.id)
    setMembers(poll.data.participants.map((p) => p.userId).filter((id) => id !== me?.id && friendIds.has(id)))
  }

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
        memberIds: members,
        pollId: poll.data ? poll.data.id : null,
      },
      { onSuccess: (group) => navigate(`/groups/${group.id}`) },
    )
  }

  return (
    <div className="narrow stack">
      <Link to="/groups" className="back icon-line">
        <ArrowLeft size={16} aria-hidden="true" /> My events
      </Link>
      <h1>New event</h1>
      {poll.data && <p className="notice">From the poll "{poll.data.title}". Friends who voted are already added.</p>}

      <form className="card form" onSubmit={submit}>
        <label>
          Title
          <input
            required
            maxLength={120}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="Board games night"
          />
        </label>

        <label>
          <span>
            Description <span className="optional">(optional)</span>
          </span>
          <textarea rows={3} value={description} onChange={(e) => setDescription(e.target.value)} />
        </label>

        <fieldset>
          <legend>When could it happen?</legend>
          <p className="hint">Members mark their free time inside this window.</p>
          <div className="row">
            <label>
              From
              <input
                type="datetime-local"
                required
                value={window.start}
                onChange={(e) => setWindow({ ...window, start: e.target.value })}
              />
            </label>
            <label>
              To
              <input
                type="datetime-local"
                required
                value={window.end}
                onChange={(e) => setWindow({ ...window, end: e.target.value })}
              />
            </label>
          </div>
        </fieldset>

        <div className="row">
          <label>
            Event length
            <select value={duration} onChange={(e) => setDuration(Number(e.target.value))}>
              {[...new Set([...DURATIONS, prefs.defaultEventDurationMinutes])]
                .sort((a, b) => a - b)
                .map((m) => (
                  <option key={m} value={m}>
                    {m < 60 ? `${m} min` : `${m / 60} h`}
                  </option>
                ))}
            </select>
          </label>
          <label>
            <span>
              Availability deadline <span className="optional">(optional)</span>
            </span>
            <input type="datetime-local" value={deadline} onChange={(e) => setDeadline(e.target.value)} />
          </label>
        </div>

        <fieldset>
          <legend>
            Invite friends <span className="optional">(optional)</span>
          </legend>
          {friends.data && friends.data.friends.length > 0 ? (
            <>
              <p className="hint">They join right away. You can also share an invite link later.</p>
              <FriendPicker
                friends={friends.data.friends}
                selected={members}
                onChange={setMembers}
                label="Friends to invite"
              />
            </>
          ) : (
            <p className="hint">
              No friends yet. <Link to="/friends">Add friends</Link> or share the invite link after creating the event.
            </p>
          )}
        </fieldset>

        {formError && <p className="inline-error">{formError}</p>}
        <InlineError error={create.error} />

        <div className="form-actions">
          <Link to="/groups" className="btn btn-ghost">
            Cancel
          </Link>
          <button type="submit" className="btn btn-primary" disabled={create.isPending}>
            {create.isPending ? 'Creating…' : 'Create event'}
          </button>
        </div>
      </form>
    </div>
  )
}
