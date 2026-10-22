import { Check, UserPlus } from 'lucide-react'
import { Link, useParams } from 'react-router'
import { useAddFriendByCode, usePersonByCode } from '../api/queries'
import { ErrorState, InlineError, Loading } from '../components/States'
import { UserAvatar } from '../components/UserAvatar'

export function AddFriendPage() {
  const { code = '' } = useParams()
  const { data: person, isPending, error } = usePersonByCode(code)
  const add = useAddFriendByCode()

  if (isPending) return <Loading />
  if (error || !person) {
    return (
      <div className="narrow stack">
        <ErrorState error={error} />
        <Link to="/friends" className="btn">
          Go to friends
        </Link>
      </div>
    )
  }

  const relation = add.data?.relation ?? person.relation

  return (
    <div className="narrow">
      <section className="card add-friend">
        <UserAvatar user={{ displayName: person.displayName, photoUrl: null }} size={72} />
        <div>
          <h1>{person.displayName}</h1>
          <p className="muted">@{person.username}</p>
        </div>

        {relation === 'SELF' ? (
          <p className="notice">This is your own friend link. Send it to someone you want to plan with.</p>
        ) : relation === 'FRIEND' ? (
          <p className="add-friend-done icon-line">
            <Check size={18} aria-hidden="true" /> {add.data ? "You're friends now" : "You're already friends"}
          </p>
        ) : (
          <>
            <p className="muted">wants to plan Fridays with you.</p>
            <button
              type="button"
              className="btn btn-primary btn-big"
              disabled={add.isPending}
              onClick={() => add.mutate(code)}
            >
              <UserPlus size={18} aria-hidden="true" /> {add.isPending ? 'Adding…' : 'Add friend'}
            </button>
          </>
        )}
        <InlineError error={add.error} />
        <Link to="/friends" className="link-button small">
          All friends
        </Link>
      </section>
    </div>
  )
}
