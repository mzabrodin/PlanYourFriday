import { ArrowLeft, Plus, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { useCreatePoll, useFriends } from '../api/queries'
import { FriendPicker } from '../components/friends/FriendPicker'
import { Switch } from '../components/settings/Controls'
import { InlineError, Loading } from '../components/States'
import { fromLocalInput } from '../lib/time'

const MIN_OPTIONS = 2
const MAX_OPTIONS = 12

export function CreatePollPage() {
  const navigate = useNavigate()
  const friends = useFriends()
  const create = useCreatePoll()
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [participants, setParticipants] = useState<string[]>([])
  const [options, setOptions] = useState(['', ''])
  const [deadline, setDeadline] = useState('')
  const [multipleChoice, setMultipleChoice] = useState(true)
  const [allowNewOptions, setAllowNewOptions] = useState(true)
  const [formError, setFormError] = useState<string | null>(null)

  function submit(e: FormEvent) {
    e.preventDefault()
    const clean = options.map((o) => o.trim()).filter(Boolean)
    if (new Set(clean.map((o) => o.toLowerCase())).size < MIN_OPTIONS) {
      return setFormError('Add at least two different options.')
    }
    if (participants.length === 0) return setFormError('Pick at least one friend to vote.')
    if (deadline && new Date(deadline) <= new Date()) return setFormError('The deadline should be in the future.')
    setFormError(null)
    create.mutate(
      {
        title: title.trim(),
        description: description.trim() || null,
        participantIds: participants,
        options: clean,
        deadline: deadline ? fromLocalInput(deadline) : null,
        multipleChoice,
        allowNewOptions,
      },
      { onSuccess: (poll) => navigate(`/ideas/${poll.id}`) },
    )
  }

  return (
    <div className="narrow stack">
      <Link to="/ideas" className="back icon-line">
        <ArrowLeft size={16} aria-hidden="true" /> Ideas
      </Link>
      <h1>New poll</h1>

      <form className="card form" onSubmit={submit}>
        <label>
          Question
          <input
            required
            maxLength={120}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="What do we do on Saturday?"
          />
        </label>

        <label>
          <span>
            Details <span className="optional">(optional)</span>
          </span>
          <textarea rows={2} value={description} onChange={(e) => setDescription(e.target.value)} />
        </label>

        <fieldset>
          <legend>Options</legend>
          <ol className="option-inputs">
            {options.map((value, i) => (
              <li key={i}>
                <span className="option-index" aria-hidden="true">
                  {i + 1}
                </span>
                <input
                  value={value}
                  maxLength={80}
                  onChange={(e) => setOptions(options.map((o, j) => (j === i ? e.target.value : o)))}
                  placeholder={['Bowling', 'Board games', 'Karaoke'][i] ?? 'Another idea'}
                  aria-label={`Option ${i + 1}`}
                />
                <button
                  type="button"
                  className="icon-button"
                  aria-label={`Remove option ${i + 1}`}
                  disabled={options.length <= MIN_OPTIONS}
                  onClick={() => setOptions(options.filter((_, j) => j !== i))}
                >
                  <X size={14} />
                </button>
              </li>
            ))}
          </ol>
          <button
            type="button"
            className="btn btn-small add-option"
            disabled={options.length >= MAX_OPTIONS}
            onClick={() => setOptions([...options, ''])}
          >
            <Plus size={14} aria-hidden="true" /> Add option
          </button>
        </fieldset>

        <fieldset>
          <legend>Who votes?</legend>
          {friends.isPending ? (
            <Loading />
          ) : friends.data && friends.data.friends.length > 0 ? (
            <FriendPicker
              friends={friends.data.friends}
              selected={participants}
              onChange={setParticipants}
              label="Friends who vote"
            />
          ) : (
            <p className="hint">
              You need friends to start a poll. <Link to="/friends">Add some first</Link>.
            </p>
          )}
          <InlineError error={friends.error} />
        </fieldset>

        <label>
          <span>
            Voting closes <span className="optional">(optional)</span>
          </span>
          <input type="datetime-local" value={deadline} onChange={(e) => setDeadline(e.target.value)} />
        </label>

        <div className="toggle-list">
          <div className="toggle-row">
            <span>
              <span className="toggle-title">Allow several choices</span>
              <span className="muted small">Everyone can vote for more than one option.</span>
            </span>
            <Switch checked={multipleChoice} onChange={setMultipleChoice} label="Allow several choices" />
          </div>
          <div className="toggle-row">
            <span>
              <span className="toggle-title">Friends can add options</span>
              <span className="muted small">Not only you can suggest new ideas.</span>
            </span>
            <Switch checked={allowNewOptions} onChange={setAllowNewOptions} label="Friends can add options" />
          </div>
        </div>

        {formError && <p className="inline-error">{formError}</p>}
        <InlineError error={create.error} />

        <div className="form-actions">
          <Link to="/ideas" className="btn btn-ghost">
            Cancel
          </Link>
          <button type="submit" className="btn btn-primary" disabled={create.isPending}>
            {create.isPending ? 'Creating…' : 'Start poll'}
          </button>
        </div>
      </form>
    </div>
  )
}
