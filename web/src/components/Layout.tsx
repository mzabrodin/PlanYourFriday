import { CalendarCheck } from 'lucide-react'
import { useEffect } from 'react'
import { Link, NavLink, Outlet, useLocation } from 'react-router'
import { api, usingMocks } from '../api'
import { resetMockDb } from '../api/mockApi'
import { useDisconnectCalendar, useFriends, useLogout, useMe, usePolls } from '../api/queries'
import { NotificationBell } from './NotificationBell'
import { UserAvatar } from './UserAvatar'

export function Layout() {
  const { data: me } = useMe()
  const logout = useLogout()
  const disconnect = useDisconnectCalendar()
  const signedIn = !!me
  const friends = useFriends(signedIn)
  const polls = usePolls(signedIn)
  const incoming = friends.data?.incoming.length ?? 0
  const pendingVotes = polls.data?.filter((p) => p.myVotePending).length ?? 0
  const { pathname } = useLocation()

  useEffect(() => {
    document.querySelector('.tabs a.active')?.scrollIntoView({ block: 'nearest', inline: 'nearest' })
  }, [pathname, signedIn])

  return (
    <div className="app">
      {usingMocks && (
        <div className="mock-banner">
          Mock API: data lives in this browser only.{' '}
          <button
            type="button"
            className="link-button"
            onClick={() => {
              resetMockDb()
              window.location.assign('/')
            }}
          >
            Reset demo data
          </button>
        </div>
      )}

      <header className="topbar">
        <div className="topbar-left">
          <Link to="/" className="brand" aria-label="PlanYourFriday – home">
            Plan<span>Your</span>Friday
          </Link>
          {me && (
            <nav className="tabs" aria-label="Main">
              <NavLink to="/groups" end>
                Groups
              </NavLink>
              <NavLink to="/ideas">
                Ideas
                <NavCount count={pendingVotes} label="polls waiting for your vote" />
              </NavLink>
              <NavLink to="/calendar">Calendar</NavLink>
              <NavLink to="/friends">
                Friends
                <NavCount count={incoming} label="friend requests" />
              </NavLink>
              <NavLink to="/wheel">Wheel</NavLink>
            </nav>
          )}
        </div>

        {me && (
          <div className="topbar-user">
            {me.calendarConnected ? (
              <button
                type="button"
                className="chip chip-ok"
                title="Disconnect Google Calendar"
                disabled={disconnect.isPending}
                onClick={() => disconnect.mutate()}
              >
                <CalendarCheck size={14} aria-hidden="true" /> Google Calendar connected
              </button>
            ) : (
              <button
                type="button"
                className="chip"
                onClick={() => api.startCalendarConnect(window.location.pathname)}
              >
                Connect Google Calendar
              </button>
            )}
            <NotificationBell />
            <NavLink to="/settings" className="topbar-profile" title={`${me.email} · Settings`}>
              <UserAvatar user={me} />
              <span className="topbar-name">{me.displayName}</span>
            </NavLink>
            <button type="button" className="btn btn-ghost" onClick={() => logout.mutate()}>
              Sign out
            </button>
          </div>
        )}
      </header>

      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}

function NavCount({ count, label }: { count: number; label: string }) {
  if (count === 0) return null
  return (
    <span className="nav-count" aria-label={`${count} ${label}`}>
      {count > 9 ? '9+' : count}
    </span>
  )
}
