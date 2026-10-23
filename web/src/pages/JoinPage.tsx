import { useEffect, useRef } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { useJoinGroup } from '../api/queries'
import { ErrorState, Loading } from '../components/States'

export function JoinPage() {
  const { token = '' } = useParams()
  const navigate = useNavigate()
  const { mutate, error } = useJoinGroup()
  const started = useRef(false)

  useEffect(() => {
    if (started.current) return
    started.current = true
    mutate(token, { onSuccess: ({ groupId }) => navigate(`/groups/${groupId}`, { replace: true }) })
  }, [token, mutate, navigate])

  if (error) {
    return (
      <div className="narrow stack">
        <ErrorState error={error} />
        <Link to="/" className="btn">
          Go to my events
        </Link>
      </div>
    )
  }
  return <Loading label="Joining the event…" />
}
