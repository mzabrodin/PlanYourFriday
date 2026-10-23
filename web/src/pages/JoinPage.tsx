import { useEffect, useRef } from 'react'
import { useNavigate, useParams } from 'react-router'
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
    return <ErrorState error={error} back={{ to: '/groups', label: 'Go to my events' }} />
  }
  return <Loading label="Joining the event…" />
}
