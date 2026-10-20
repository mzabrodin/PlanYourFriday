import { Plus } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { useGroups } from '../api/queries'
import { GroupsBrowser } from '../components/GroupsBrowser'
import { ErrorState, Loading } from '../components/States'

export function GroupsPage() {
  const { data: groups, isPending, error, refetch } = useGroups()
  const [now] = useState(Date.now)

  return (
    <div className="stack">
      <div className="page-header">
        <h1>My groups</h1>
        <Link to="/groups/new" className="btn btn-primary">
          <Plus size={18} aria-hidden="true" /> New group
        </Link>
      </div>

      {isPending && <Loading />}
      {error && <ErrorState error={error} onRetry={refetch} />}

      {groups?.length === 0 && (
        <div className="empty">
          <p>You're not in any groups yet.</p>
          <p className="muted">Create one, or open an invite link a friend sent you.</p>
        </div>
      )}

      {groups && groups.length > 0 && <GroupsBrowser groups={groups} now={now} />}
    </div>
  )
}
