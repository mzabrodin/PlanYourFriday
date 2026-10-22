import { Check, Copy, Link2, RefreshCw, Search, UserMinus, UserPlus, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import type { PersonDto } from '../api'
import {
  useAcceptFriendRequest,
  useFriends,
  usePeopleSearch,
  useRegenerateFriendCode,
  useRemoveFriend,
  useRemoveFriendRequest,
  useSendFriendRequest,
} from '../api/queries'
import { PersonRow } from '../components/friends/PersonRow'
import { Card, ErrorState, InlineError, Loading } from '../components/States'
import { formatDate } from '../lib/time'

function useDebounced<T>(value: T, ms: number): T {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), ms)
    return () => window.clearTimeout(timer)
  }, [value, ms])
  return debounced
}

export function FriendsPage() {
  const { data, isPending, error, refetch } = useFriends()
  const accept = useAcceptFriendRequest()
  const removeRequest = useRemoveFriendRequest()
  const removeFriend = useRemoveFriend()
  const send = useSendFriendRequest()

  if (isPending) return <Loading />
  if (error || !data) return <ErrorState error={error} onRetry={refetch} />

  return (
    <div className="stack">
      <div className="page-header">
        <h1>Friends</h1>
      </div>

      <div className="friends-layout">
        <div className="stack">
          <FindPeople />

          {data.incoming.length > 0 && (
            <Card title={`Friend requests · ${data.incoming.length}`}>
              <ul className="person-list">
                {data.incoming.map((r) => (
                  <PersonRow
                    key={r.id}
                    displayName={r.person.displayName}
                    username={r.person.username}
                    detail="wants to be friends"
                  >
                    <button
                      type="button"
                      className="btn btn-primary btn-small"
                      disabled={accept.isPending}
                      onClick={() => accept.mutate(r.id)}
                    >
                      <Check size={14} aria-hidden="true" /> Accept
                    </button>
                    <button
                      type="button"
                      className="btn btn-ghost btn-small"
                      disabled={removeRequest.isPending}
                      onClick={() => removeRequest.mutate(r.id)}
                    >
                      Decline
                    </button>
                  </PersonRow>
                ))}
              </ul>
              <InlineError error={accept.error ?? removeRequest.error} />
            </Card>
          )}

          <Card title={`Your friends · ${data.friends.length}`}>
            {data.friends.length === 0 ? (
              <p className="muted">No friends yet. Share your link or find people by username.</p>
            ) : (
              <ul className="person-list">
                {data.friends.map((f) => (
                  <PersonRow
                    key={f.userId}
                    displayName={f.displayName}
                    username={f.username}
                    detail={`since ${formatDate(f.since)}`}
                  >
                    <button
                      type="button"
                      className="icon-button"
                      aria-label={`Remove ${f.displayName} from friends`}
                      title="Remove from friends"
                      disabled={removeFriend.isPending}
                      onClick={() => {
                        if (window.confirm(`Remove ${f.displayName} from your friends?`)) removeFriend.mutate(f.userId)
                      }}
                    >
                      <UserMinus size={16} />
                    </button>
                  </PersonRow>
                ))}
              </ul>
            )}
            <InlineError error={removeFriend.error} />
          </Card>

          {data.outgoing.length > 0 && (
            <Card title="Waiting for an answer">
              <ul className="person-list">
                {data.outgoing.map((r) => (
                  <PersonRow
                    key={r.id}
                    displayName={r.person.displayName}
                    username={r.person.username}
                    detail="request sent"
                  >
                    <button
                      type="button"
                      className="btn btn-ghost btn-small"
                      disabled={removeRequest.isPending}
                      onClick={() => removeRequest.mutate(r.id)}
                    >
                      <X size={14} aria-hidden="true" /> Cancel
                    </button>
                  </PersonRow>
                ))}
              </ul>
            </Card>
          )}
        </div>

        <aside className="stack">
          <FriendLink code={data.friendCode} />

          {data.suggestions.length > 0 && (
            <Card title="People you planned with">
              <ul className="person-list">
                {data.suggestions.map((s) => (
                  <PersonRow
                    key={s.person.userId}
                    displayName={s.person.displayName}
                    username={s.person.username}
                    detail={s.sharedGroups.join(', ')}
                  >
                    <button
                      type="button"
                      className="btn btn-small"
                      disabled={send.isPending}
                      onClick={() => send.mutate(s.person.userId)}
                    >
                      <UserPlus size={14} aria-hidden="true" /> Add
                    </button>
                  </PersonRow>
                ))}
              </ul>
              <InlineError error={send.error} />
            </Card>
          )}
        </aside>
      </div>
    </div>
  )
}

function FriendLink({ code }: { code: string }) {
  const regenerate = useRegenerateFriendCode()
  const [copied, setCopied] = useState(false)
  const link = `${window.location.origin}/add/${code}`

  async function copy() {
    try {
      await navigator.clipboard.writeText(link)
      setCopied(true)
      window.setTimeout(() => setCopied(false), 2000)
    } catch {}
  }

  return (
    <section className="card friend-link">
      <h2 className="icon-line">
        <Link2 size={20} aria-hidden="true" /> Your friend link
      </h2>
      <p className="muted small">Anyone who opens it becomes your friend right away. Send it in a chat you trust.</p>
      <div className="copy-row">
        <input readOnly value={link} onFocus={(e) => e.target.select()} aria-label="Your friend link" />
        <button type="button" className="btn" onClick={copy}>
          {copied ? <Check size={16} aria-hidden="true" /> : <Copy size={16} aria-hidden="true" />}
          {copied ? 'Copied' : 'Copy'}
        </button>
      </div>
      <button
        type="button"
        className="link-button small icon-line"
        disabled={regenerate.isPending}
        onClick={() => {
          if (window.confirm('Make a new link? The old one will stop working.')) regenerate.mutate()
        }}
      >
        <RefreshCw size={13} aria-hidden="true" /> New link
      </button>
      <InlineError error={regenerate.error} />
    </section>
  )
}

function FindPeople() {
  const [query, setQuery] = useState('')
  const debounced = useDebounced(query.trim(), 300)
  const search = usePeopleSearch(debounced)
  const tooShort = debounced.replace(/^@/, '').length < 2

  return (
    <Card title="Find people">
      <label className="search-field">
        <Search size={18} aria-hidden="true" />
        <input
          type="search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search by @username or name"
          aria-label="Search people"
          autoComplete="off"
          spellCheck={false}
        />
      </label>
      {!tooShort &&
        search.data &&
        (search.data.length === 0 ? (
          <p className="muted small search-empty">Nobody found for "{debounced}".</p>
        ) : (
          <ul className="person-list">
            {search.data.map((p) => (
              <PersonRow key={p.userId} displayName={p.displayName} username={p.username}>
                <RelationAction person={p} />
              </PersonRow>
            ))}
          </ul>
        ))}
      <InlineError error={search.error} />
    </Card>
  )
}

function RelationAction({ person }: { person: PersonDto }) {
  const send = useSendFriendRequest()
  switch (person.relation) {
    case 'SELF':
      return <span className="muted small">That's you</span>
    case 'FRIEND':
      return (
        <span className="badge badge-ok icon-line">
          <Check size={12} aria-hidden="true" /> Friends
        </span>
      )
    case 'REQUEST_SENT':
      return <span className="badge badge-muted">Requested</span>
    case 'REQUEST_RECEIVED':
      return (
        <button
          type="button"
          className="btn btn-primary btn-small"
          disabled={send.isPending}
          onClick={() => send.mutate(person.userId)}
        >
          <Check size={14} aria-hidden="true" /> Accept
        </button>
      )
    default:
      return (
        <button
          type="button"
          className="btn btn-small"
          disabled={send.isPending}
          onClick={() => send.mutate(person.userId)}
        >
          <UserPlus size={14} aria-hidden="true" /> Add
        </button>
      )
  }
}
