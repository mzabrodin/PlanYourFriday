import {
  Bell,
  CalendarCheck,
  CalendarX,
  CheckCheck,
  Hourglass,
  Mail,
  Pencil,
  UserPlus,
  Vote,
  type LucideIcon,
} from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import type { NotificationDto, NotificationKind } from '../api'
import { useMarkNotificationsRead, useNotifications } from '../api/queries'
import { formatRelative } from '../lib/time'

const KINDS: Record<NotificationKind, { Icon: LucideIcon; tone: string; text: (n: NotificationDto) => string }> = {
  INVITED: { Icon: Mail, tone: 'info', text: (n) => `${n.actorName} invited you to ${n.groupTitle}` },
  MEMBER_JOINED: { Icon: UserPlus, tone: 'info', text: (n) => `${n.actorName} joined ${n.groupTitle}` },
  VOTING_STARTED: { Icon: Vote, tone: 'vote', text: (n) => `${n.actorName} started a vote in ${n.groupTitle}` },
  DEADLINE_SOON: { Icon: Hourglass, tone: 'warn', text: (n) => `Mark your free time for ${n.groupTitle} before the deadline` },
  EVENT_CONFIRMED: { Icon: CalendarCheck, tone: 'ok', text: (n) => `${n.groupTitle} is on! ${n.actorName} confirmed the time` },
  EVENT_UPDATED: { Icon: Pencil, tone: 'info', text: (n) => `${n.actorName} updated the details of ${n.groupTitle}` },
  EVENT_CANCELLED: { Icon: CalendarX, tone: 'danger', text: (n) => `${n.actorName} cancelled ${n.groupTitle}` },
}

export function NotificationBell() {
  const { data: notifications = [] } = useNotifications()
  const markRead = useMarkNotificationsRead()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [now, setNow] = useState(Date.now)
  const root = useRef<HTMLDivElement>(null)
  const unread = notifications.filter((n) => !n.read).length

  useEffect(() => {
    if (!open) return
    const onPointer = (e: PointerEvent) => {
      if (!root.current?.contains(e.target as Node)) setOpen(false)
    }
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(false)
    document.addEventListener('pointerdown', onPointer)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('pointerdown', onPointer)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  function openNotification(n: NotificationDto) {
    if (!n.read) markRead.mutate([n.id])
    setOpen(false)
    navigate(`/groups/${n.groupId}`)
  }

  return (
    <div className="bell" ref={root}>
      <button
        type="button"
        className="bell-button"
        aria-label={unread > 0 ? `Notifications, ${unread} unread` : 'Notifications'}
        aria-expanded={open}
        aria-haspopup="dialog"
        onClick={() => {
          setNow(Date.now())
          setOpen(!open)
        }}
      >
        <Bell size={20} aria-hidden="true" />
        {unread > 0 && <span className="bell-badge">{unread > 9 ? '9+' : unread}</span>}
      </button>

      {open && (
        <div className="bell-panel" role="dialog" aria-label="Notifications">
          <header>
            <h2>Notifications</h2>
            {unread > 0 && (
              <button type="button" className="link-button small icon-line" onClick={() => markRead.mutate(undefined)}>
                <CheckCheck size={14} aria-hidden="true" /> Mark all as read
              </button>
            )}
          </header>
          {notifications.length === 0 ? (
            <p className="bell-empty">
              <Bell size={28} aria-hidden="true" />
              You're all caught up.
            </p>
          ) : (
            <ul>
              {notifications.map((n) => {
                const { Icon, tone, text } = KINDS[n.kind]
                return (
                  <li key={n.id}>
                    <button type="button" className={`bell-item${n.read ? '' : ' unread'}`} onClick={() => openNotification(n)}>
                      <span className={`bell-icon tone-${tone}`} aria-hidden="true">
                        <Icon size={16} />
                      </span>
                      <span className="bell-text">
                        <span>{text(n)}</span>
                        <span className="muted small">{formatRelative(n.createdAt, now)}</span>
                      </span>
                      {!n.read && <span className="unread-dot" aria-label="unread" />}
                    </button>
                  </li>
                )
              })}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}
