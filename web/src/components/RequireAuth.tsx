import { Navigate, Outlet, useLocation } from 'react-router'
import { useMe, useSettings } from '../api/queries'
import { ErrorState, Loading } from './States'

export function RequireAuth() {
  const { data: me, isPending, error, refetch } = useMe()
  const settings = useSettings(!!me)
  const location = useLocation()

  if (isPending || (me && settings.isPending)) return <Loading />
  if (error) return <ErrorState error={error} onRetry={refetch} />
  if (!me) {
    const returnTo = location.pathname + location.search
    return <Navigate to={`/login?returnTo=${encodeURIComponent(returnTo)}`} replace />
  }
  return <Outlet />
}
