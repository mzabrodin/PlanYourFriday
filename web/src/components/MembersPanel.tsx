import { Check, Copy, Crown, Send, UserPlus } from 'lucide-react'
import { Link } from 'react-router'
import { useState, type FormEvent } from 'react'
import type { GroupDto } from '../api'
import { useAddMembers, useFriends, useInvite, useRevokeInvitation } from '../api/queries'
import { FriendPicker } from './friends/FriendPicker'
import { Card, InlineError, StatusBadge } from './States'
import { UserAvatar } from './UserAvatar'

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function MembersPanel({ group }: { group: GroupDto }) {
  const isOrganizer = group.myRole === 'ORGANIZER'
  const canInvite = isOrganizer && group.status === 'PLANNING'
  const canAddFriends = isOrganizer && (group.status === 'PLANNING' || group.status === 'VOTING')
  const ready = group.members.filter((m) => m.hasAvailability).length
  const showReadiness = group.status === 'PLANNING' || group.status === 'VOTING'

  return (
    <Card
      title={showReadiness ? `Members · ${ready}/${group.members.length} ready` : `Members · ${group.members.length}`}
    >
      <ul className="member-list">
        {group.members.map((m) => (
          <li key={m.userId}>
            <UserAvatar user={m} size={30} />
            <span className="member-name">
              {m.displayName}
              {m.role === 'ORGANIZER' && (
                <span className="organizer-inline" title="Organizer">
                  <Crown size={13} aria-hidden="true" /> organizer
                </span>
              )}
            </span>
            {showReadiness && (
              <span className={m.hasAvailability ? 'tick' : 'muted small'}>
                {m.hasAvailability ? (
                  <>
                    <Check size={14} aria-hidden="true" /> marked free time
                  </>
                ) : (
                  'not yet'
                )}
              </span>
            )}
          </li>
        ))}
      </ul>

      {canAddFriends && <AddFriends group={group} />}
      {canInvite && group.inviteToken && <InviteLink token={group.inviteToken} />}
      {canInvite && <InviteByEmail groupId={group.id} />}
      {isOrganizer && group.invitations.length > 0 && <Invitations group={group} />}
    </Card>
  )
}

function AddFriends({ group }: { group: GroupDto }) {
  const { data } = useFriends()
  const add = useAddMembers(group.id)
  const [selected, setSelected] = useState<string[]>([])
  if (!data) return null
  const available = data.friends.filter((f) => !group.members.some((m) => m.userId === f.userId))

  return (
    <div className="subsection">
      <h3>Add friends</h3>
      {available.length === 0 ? (
        <p className="hint">
          {data.friends.length === 0 ? 'No friends yet. ' : 'All your friends are here already. '}
          <Link to="/friends">Find more friends</Link>
        </p>
      ) : (
        <>
          <FriendPicker friends={available} selected={selected} onChange={setSelected} label="Friends to add" />
          <button
            type="button"
            className="btn add-friends-btn"
            disabled={selected.length === 0 || add.isPending}
            onClick={() => add.mutate(selected, { onSuccess: () => setSelected([]) })}
          >
            <UserPlus size={16} aria-hidden="true" />
            {add.isPending ? 'Adding…' : selected.length > 1 ? `Add ${selected.length} friends` : 'Add to event'}
          </button>
        </>
      )}
      <InlineError error={add.error} />
    </div>
  )
}

function InviteLink({ token }: { token: string }) {
  const link = `${window.location.origin}/join/${token}`
  const [copied, setCopied] = useState(false)

  async function copy() {
    try {
      await navigator.clipboard.writeText(link)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {}
  }

  return (
    <div className="subsection">
      <h3>Invite link</h3>
      <div className="copy-row">
        <input readOnly value={link} onFocus={(e) => e.target.select()} aria-label="Invite link" />
        <button type="button" className="btn" onClick={copy}>
          {copied ? <Check size={16} aria-hidden="true" /> : <Copy size={16} aria-hidden="true" />}
          {copied ? 'Copied' : 'Copy'}
        </button>
      </div>
    </div>
  )
}

function InviteByEmail({ groupId }: { groupId: string }) {
  const invite = useInvite(groupId)
  const [value, setValue] = useState('')
  const [invalid, setInvalid] = useState<string[]>([])

  function submit(e: FormEvent) {
    e.preventDefault()
    const emails = [
      ...new Set(
        value
          .split(/[\s,;]+/)
          .map((s) => s.trim().toLowerCase())
          .filter(Boolean),
      ),
    ]
    const bad = emails.filter((s) => !EMAIL_RE.test(s))
    setInvalid(bad)
    if (bad.length || emails.length === 0) return
    invite.mutate(emails, { onSuccess: () => setValue('') })
  }

  return (
    <form className="subsection" onSubmit={submit}>
      <h3>Invite by email</h3>
      <div className="copy-row">
        <input
          value={value}
          onChange={(e) => setValue(e.target.value)}
          placeholder="friend@gmail.com, another@gmail.com"
          aria-label="Emails to invite"
        />
        <button type="submit" className="btn" disabled={invite.isPending}>
          <Send size={16} aria-hidden="true" />
          {invite.isPending ? 'Sending…' : 'Send'}
        </button>
      </div>
      {invalid.length > 0 && <p className="inline-error">Not valid: {invalid.join(', ')}</p>}
      <InlineError error={invite.error} />
    </form>
  )
}

function Invitations({ group }: { group: GroupDto }) {
  const revoke = useRevokeInvitation(group.id)
  return (
    <div className="subsection">
      <h3>Invitations</h3>
      <ul className="invite-list">
        {group.invitations.map((inv) => (
          <li key={inv.id}>
            <span className="invite-email">{inv.email}</span>
            <StatusBadge status={inv.status} />
            {(inv.status === 'PENDING' || inv.status === 'SENT') && group.status === 'PLANNING' && (
              <button
                type="button"
                className="link-button"
                disabled={revoke.isPending}
                onClick={() => revoke.mutate(inv.id)}
              >
                Revoke
              </button>
            )}
          </li>
        ))}
      </ul>
      <InlineError error={revoke.error} />
    </div>
  )
}
