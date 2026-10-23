import { Bell, CalendarClock, CalendarDays, Palette, ShieldCheck, User } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useBlocker } from 'react-router'
import { api } from '../api'
import type {
  CalendarConnectionDto,
  DailyHours,
  UpdateCalendarConnectionRequest,
  UserDto,
  UserSettingsDto,
} from '../api'
import {
  useCalendarConnections,
  useDeleteAccount,
  useDisconnectCalendar,
  useLogout,
  useLogoutEverywhere,
  useMe,
  useSettings,
  useUpdateCalendarConnection,
  useUpdateProfile,
  useUpdateSettings,
} from '../api/queries'
import { SettingRow, SettingsSection, Segmented, Switch } from '../components/settings/Controls'
import { ErrorState, InlineError, Loading } from '../components/States'
import { UserAvatar } from '../components/UserAvatar'
import { applyTheme } from '../lib/preferences'
import { formatRelative } from '../lib/time'

const SECTIONS = [
  { id: 'profile', label: 'Profile', icon: User },
  { id: 'calendar', label: 'Google Calendar', icon: CalendarDays },
  { id: 'planning', label: 'Planning', icon: CalendarClock },
  { id: 'notifications', label: 'Notifications', icon: Bell },
  { id: 'appearance', label: 'Appearance', icon: Palette },
  { id: 'account', label: 'Account', icon: ShieldCheck },
] as const

function useActiveSection(ids: readonly string[]): string {
  const [active, setActive] = useState(ids[0])
  useEffect(() => {
    const visible = new Map<string, number>()
    const observer = new IntersectionObserver(
      (entries) => {
        for (const e of entries) visible.set(e.target.id, e.isIntersecting ? e.intersectionRatio : 0)
        const atBottom = window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 4
        if (atBottom) return setActive(ids[ids.length - 1])
        const top = ids.find((id) => (visible.get(id) ?? 0) > 0)
        if (top) setActive(top)
      },
      { rootMargin: '-15% 0px -60% 0px', threshold: [0, 0.01, 0.5, 1] },
    )
    for (const id of ids) {
      const el = document.getElementById(id)
      if (el) observer.observe(el)
    }
    const onScroll = () => {
      if (window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 4)
        setActive(ids[ids.length - 1])
    }
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => {
      observer.disconnect()
      window.removeEventListener('scroll', onScroll)
    }
  }, [ids])
  return active
}

const SECTION_IDS = SECTIONS.map((s) => s.id)

const DURATIONS = [30, 60, 90, 120, 180, 240, 300, 480]
const REMINDER_HOURS = [1, 3, 12, 24, 48]

const durationLabel = (m: number) => (m < 60 ? `${m} min` : `${m / 60} h`)

export function SettingsPage() {
  const me = useMe()
  const settings = useSettings()
  const connections = useCalendarConnections()

  if (me.isPending || settings.isPending || connections.isPending) return <Loading />
  const error = me.error ?? settings.error ?? connections.error
  if (error || !me.data || !settings.data || !connections.data) {
    return <ErrorState error={error ?? new Error('Could not load settings')} onRetry={() => void settings.refetch()} />
  }
  return <SettingsForm user={me.data} settings={settings.data} connection={connections.data[0] ?? null} />
}

interface ProfileDraft {
  displayName: string
  username: string
  timeZone: string
}

const USERNAME_RE = /^[a-z0-9_.]{3,30}$/

const same = (a: unknown, b: unknown) => JSON.stringify(a) === JSON.stringify(b)

function SettingsForm({
  user,
  settings,
  connection,
}: {
  user: UserDto
  settings: UserSettingsDto
  connection: CalendarConnectionDto | null
}) {
  const savedProfile: ProfileDraft = { displayName: user.displayName, username: user.username, timeZone: user.timeZone }
  const savedConnection: UpdateCalendarConnectionRequest | null = connection && {
    busyCalendarIds: connection.busyCalendarIds,
    writeCalendarId: connection.writeCalendarId,
  }

  const [profile, setProfile] = useState(savedProfile)
  const [prefs, setPrefs] = useState(settings)
  const [calendars, setCalendars] = useState(savedConnection)

  const updateProfile = useUpdateProfile()
  const updateSettings = useUpdateSettings()
  const updateConnection = useUpdateCalendarConnection()

  const profileDirty = !same(profile, savedProfile)
  const prefsDirty = !same(prefs, settings)
  const calendarsDirty = calendars !== null && savedConnection !== null && !same(calendars, savedConnection)
  const dirty = profileDirty || prefsDirty || calendarsDirty
  const saving = updateProfile.isPending || updateSettings.isPending || updateConnection.isPending
  const nameError = profile.displayName.trim() === '' ? 'Name cannot be empty' : null
  const usernameError = USERNAME_RE.test(profile.username) ? null : 'Use 3–30 lowercase letters, numbers, _ or .'
  const formError = nameError ?? usernameError

  const connectionKey = connection ? `${connection.id}:${connection.status}` : 'none'
  const [lastConnectionKey, setLastConnectionKey] = useState(connectionKey)
  if (connectionKey !== lastConnectionKey) {
    setLastConnectionKey(connectionKey)
    setCalendars(savedConnection)
  }

  const savedTheme = useRef(settings.theme)
  useEffect(() => {
    savedTheme.current = settings.theme
  }, [settings.theme])
  useEffect(() => applyTheme(prefs.theme), [prefs.theme])
  useEffect(() => () => applyTheme(savedTheme.current), [])

  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) => dirty && currentLocation.pathname !== nextLocation.pathname,
  )
  useEffect(() => {
    if (blocker.state !== 'blocked') return
    if (window.confirm('You have unsaved changes. Leave without saving?')) blocker.proceed()
    else blocker.reset()
  }, [blocker])
  useEffect(() => {
    if (!dirty) return
    const onBeforeUnload = (e: BeforeUnloadEvent) => e.preventDefault()
    window.addEventListener('beforeunload', onBeforeUnload)
    return () => window.removeEventListener('beforeunload', onBeforeUnload)
  }, [dirty])

  function discard() {
    setProfile(savedProfile)
    setPrefs(settings)
    setCalendars(savedConnection)
  }

  async function save() {
    if (formError) return
    await Promise.allSettled([
      profileDirty &&
        updateProfile.mutateAsync({
          displayName: profile.displayName.trim(),
          username: profile.username,
          timeZone: profile.timeZone,
        }),
      prefsDirty && updateSettings.mutateAsync(prefs),
      calendarsDirty && connection && calendars && updateConnection.mutateAsync({ id: connection.id, body: calendars }),
    ])
  }

  const activeSection = useActiveSection(SECTION_IDS)
  useEffect(() => {
    if (!window.matchMedia('(max-width: 800px)').matches) return
    document
      .querySelector(`.settings-nav a[href="#${activeSection}"]`)
      ?.scrollIntoView({ block: 'nearest', inline: 'nearest' })
  }, [activeSection])

  const setNotif = (patch: Partial<UserSettingsDto['notifications']>) =>
    setPrefs({ ...prefs, notifications: { ...prefs.notifications, ...patch } })
  const setUsual = (kind: 'weekdays' | 'weekends', hours: DailyHours | null) =>
    setPrefs({ ...prefs, usualFreeTime: { ...prefs.usualFreeTime, [kind]: hours } })

  return (
    <div className="settings">
      <div className="page-header">
        <h1>Settings</h1>
      </div>

      <div className="settings-layout">
        <nav className="settings-nav" aria-label="Settings sections">
          {SECTIONS.map(({ id, label, icon: Icon }) => (
            <a
              key={id}
              href={`#${id}`}
              className={activeSection === id ? 'active' : undefined}
              aria-current={activeSection === id ? 'location' : undefined}
            >
              <Icon size={18} aria-hidden="true" /> {label}
            </a>
          ))}
        </nav>

        <div className="settings-sections">
          <SettingsSection id="profile" icon={User} title="Profile" description="How friends see you in events.">
            <div className="profile-head">
              <UserAvatar user={{ ...user, displayName: profile.displayName || user.displayName }} size={64} />
              <div>
                <p className="profile-name">{profile.displayName || user.displayName}</p>
                <p className="muted small">{user.email}</p>
                <p className="muted small">Your photo comes from your Google account.</p>
              </div>
            </div>

            <SettingRow
              title="Display name"
              description="Shown on event pages, invites and calendars."
              htmlFor="displayName"
            >
              <input
                id="displayName"
                maxLength={60}
                value={profile.displayName}
                onChange={(e) => setProfile({ ...profile, displayName: e.target.value })}
                aria-invalid={!!nameError}
              />
              {nameError && <p className="inline-error">{nameError}</p>}
            </SettingRow>

            <SettingRow
              title="Username"
              description="Friends can find you by it. Lowercase letters, numbers, _ and ."
              htmlFor="username"
            >
              <div className="prefixed-input">
                <span aria-hidden="true">@</span>
                <input
                  id="username"
                  maxLength={30}
                  autoComplete="off"
                  spellCheck={false}
                  value={profile.username}
                  onChange={(e) =>
                    setProfile({ ...profile, username: e.target.value.toLowerCase().replace(/\s/g, '') })
                  }
                  aria-invalid={!!usernameError}
                />
              </div>
              {usernameError && <p className="inline-error">{usernameError}</p>}
            </SettingRow>

            <SettingRow
              title="Email"
              description="You sign in with this Google account. Invitations and reminders go here."
            >
              <div className="readonly-field">
                <span>{user.email}</span>
                <span className="badge badge-ok">Google</span>
              </div>
            </SettingRow>

            <SettingRow
              title="Time zone"
              description="Used for emails and reminders. The app shows times in your device's time zone."
              htmlFor="timeZone"
            >
              <TimeZoneSelect value={profile.timeZone} onChange={(timeZone) => setProfile({ ...profile, timeZone })} />
            </SettingRow>
          </SettingsSection>

          <CalendarSection connection={connection} draft={calendars} onChange={setCalendars} />

          <SettingsSection
            id="planning"
            icon={CalendarClock}
            title="Planning"
            description="Shortcuts for creating events and marking free time."
          >
            <SettingRow
              title="Default event length"
              description="Pre-selected when you create a new event."
              htmlFor="defaultDuration"
            >
              <select
                id="defaultDuration"
                value={prefs.defaultEventDurationMinutes}
                onChange={(e) => setPrefs({ ...prefs, defaultEventDurationMinutes: Number(e.target.value) })}
              >
                {DURATIONS.map((m) => (
                  <option key={m} value={m}>
                    {durationLabel(m)}
                  </option>
                ))}
              </select>
            </SettingRow>

            <div className="setting-group">
              <p className="setting-title">My usual free time</p>
              <p className="setting-desc">
                “Fill my usual hours” on an event's calendar adds these hours to every day of the planning window. You
                can still adjust them after.
              </p>
              <UsualHoursRow
                label="Weekdays"
                hint="Mon–Fri"
                value={prefs.usualFreeTime.weekdays}
                fallback={{ start: '18:00', end: '23:00' }}
                onChange={(h) => setUsual('weekdays', h)}
              />
              <UsualHoursRow
                label="Weekends"
                hint="Sat–Sun"
                value={prefs.usualFreeTime.weekends}
                fallback={{ start: '12:00', end: '23:00' }}
                onChange={(h) => setUsual('weekends', h)}
              />
            </div>
          </SettingsSection>

          <SettingsSection
            id="notifications"
            icon={Bell}
            title="Notifications"
            description={`You always get notifications in the app (the bell). Choose what we also email to ${user.email}.`}
          >
            <SettingRow title="Invitations" description="When someone invites you to an event.">
              <Switch
                label="Invitations"
                checked={prefs.notifications.invitations}
                onChange={(v) => setNotif({ invitations: v })}
              />
            </SettingRow>
            <SettingRow
              title="Deadline reminders"
              description="A nudge a day before the availability deadline if you haven't marked free time."
            >
              <Switch
                label="Deadline reminders"
                checked={prefs.notifications.deadlineReminders}
                onChange={(v) => setNotif({ deadlineReminders: v })}
              />
            </SettingRow>
            <SettingRow title="Event confirmed" description="When the organizer picks the final time.">
              <Switch
                label="Event confirmed"
                checked={prefs.notifications.eventConfirmed}
                onChange={(v) => setNotif({ eventConfirmed: v })}
              />
            </SettingRow>
            <SettingRow title="Event cancelled" description="When a confirmed event is cancelled.">
              <Switch
                label="Event cancelled"
                checked={prefs.notifications.eventCancelled}
                onChange={(v) => setNotif({ eventCancelled: v })}
              />
            </SettingRow>
            <SettingRow title="Reminder before an event" description="So nobody forgets the plan.">
              <div className="inline-controls">
                <select
                  aria-label="Reminder time"
                  disabled={!prefs.notifications.eventReminder}
                  value={prefs.notifications.eventReminderHours}
                  onChange={(e) => setNotif({ eventReminderHours: Number(e.target.value) })}
                >
                  {REMINDER_HOURS.map((h) => (
                    <option key={h} value={h}>
                      {h < 24 ? `${h} h before` : h === 24 ? '1 day before' : `${h / 24} days before`}
                    </option>
                  ))}
                </select>
                <Switch
                  label="Reminder before an event"
                  checked={prefs.notifications.eventReminder}
                  onChange={(v) => setNotif({ eventReminder: v })}
                />
              </div>
            </SettingRow>
          </SettingsSection>

          <SettingsSection id="appearance" icon={Palette} title="Appearance">
            <div className="setting-group">
              <p className="setting-title">Theme</p>
              <div className="theme-options" role="radiogroup" aria-label="Theme">
                {(['system', 'light', 'dark'] as const).map((theme) => (
                  <button
                    key={theme}
                    type="button"
                    role="radio"
                    aria-checked={prefs.theme === theme}
                    className="theme-option"
                    onClick={() => setPrefs({ ...prefs, theme })}
                  >
                    <span className={`theme-preview theme-preview-${theme}`} aria-hidden="true">
                      <span />
                      <span />
                      <span />
                    </span>
                    {theme === 'system' ? 'Match device' : theme === 'light' ? 'Light' : 'Dark'}
                  </button>
                ))}
              </div>
            </div>
            <SettingRow title="Time format">
              <Segmented
                label="Time format"
                value={prefs.timeFormat}
                onChange={(timeFormat) => setPrefs({ ...prefs, timeFormat })}
                options={[
                  { value: '24h', label: '19:00' },
                  { value: '12h', label: '7:00 PM' },
                ]}
              />
            </SettingRow>
            <SettingRow title="Week starts on" description="In calendars.">
              <Segmented
                label="Week starts on"
                value={prefs.weekStartsOn}
                onChange={(weekStartsOn) => setPrefs({ ...prefs, weekStartsOn })}
                options={[
                  { value: 1, label: 'Monday' },
                  { value: 0, label: 'Sunday' },
                ]}
              />
            </SettingRow>
          </SettingsSection>

          <AccountSection email={user.email} />
        </div>
      </div>

      {(dirty || saving) && (
        <div className="save-dock" role="region" aria-label="Unsaved changes">
          <span>{saving ? 'Saving…' : 'You have unsaved changes'}</span>
          <InlineError error={updateProfile.error ?? updateSettings.error ?? updateConnection.error} />
          <button type="button" className="btn btn-ghost" disabled={saving} onClick={discard}>
            Discard
          </button>
          <button type="button" className="btn btn-primary" disabled={saving || !!formError} onClick={save}>
            Save changes
          </button>
        </div>
      )}
    </div>
  )
}

// The same zone can have several names (Europe/Kyiv and the older Europe/Kiev), so compare what the browser resolves them to.
function sameZone(a: string, b: string) {
  try {
    const resolve = (tz: string) => new Intl.DateTimeFormat('en', { timeZone: tz }).resolvedOptions().timeZone
    return resolve(a) === resolve(b)
  } catch {
    return a === b
  }
}

function TimeZoneSelect({ value, onChange }: { value: string; onChange: (tz: string) => void }) {
  const detected = Intl.DateTimeFormat().resolvedOptions().timeZone
  const zones = typeof Intl.supportedValuesOf === 'function' ? Intl.supportedValuesOf('timeZone') : [detected]
  const options = zones.includes(value) ? zones : [value, ...zones]
  return (
    <div className="tz-control">
      <select id="timeZone" value={value} onChange={(e) => onChange(e.target.value)}>
        {options.map((tz) => (
          <option key={tz} value={tz}>
            {tz.replaceAll('_', ' ')}
          </option>
        ))}
      </select>
      {!sameZone(value, detected) && (
        <button type="button" className="link-button small" onClick={() => onChange(detected)}>
          Use this device's: {detected.replaceAll('_', ' ')}
        </button>
      )}
    </div>
  )
}

function UsualHoursRow({
  label,
  hint,
  value,
  fallback,
  onChange,
}: {
  label: string
  hint: string
  value: DailyHours | null
  fallback: DailyHours
  onChange: (hours: DailyHours | null) => void
}) {
  return (
    <div className="usual-row">
      <Switch
        label={`Free on ${label.toLowerCase()}`}
        checked={value !== null}
        onChange={(on) => onChange(on ? fallback : null)}
      />
      <span className="usual-label">
        {label} <span className="muted small">{hint}</span>
      </span>
      {value ? (
        <span className="usual-times">
          <input
            type="time"
            aria-label={`${label} from`}
            value={value.start}
            onChange={(e) => onChange({ ...value, start: e.target.value })}
          />
          <span className="muted">–</span>
          <input
            type="time"
            aria-label={`${label} to`}
            value={value.end}
            onChange={(e) => onChange({ ...value, end: e.target.value })}
          />
        </span>
      ) : (
        <span className="muted small">Usually busy</span>
      )}
    </div>
  )
}

function CalendarSection({
  connection,
  draft,
  onChange,
}: {
  connection: CalendarConnectionDto | null
  draft: UpdateCalendarConnectionRequest | null
  onChange: (draft: UpdateCalendarConnectionRequest) => void
}) {
  const disconnect = useDisconnectCalendar()
  const [now] = useState(Date.now)
  const returnTo = '/settings#calendar'

  if (!connection || !draft) {
    return (
      <SettingsSection id="calendar" icon={CalendarDays} title="Google Calendar">
        <div className="connect-empty">
          <p>
            Connect Google Calendar to import your busy times into events and to get confirmed events added to your
            calendar automatically.
          </p>
          <p className="muted small">We only read when you're busy, never the details of your events.</p>
          <button type="button" className="btn btn-primary" onClick={() => api.startCalendarConnect(returnTo)}>
            Connect Google Calendar
          </button>
        </div>
      </SettingsSection>
    )
  }

  const toggleBusy = (id: string, on: boolean) =>
    onChange({
      ...draft,
      busyCalendarIds: on ? [...draft.busyCalendarIds, id] : draft.busyCalendarIds.filter((c) => c !== id),
    })

  return (
    <SettingsSection id="calendar" icon={CalendarDays} title="Google Calendar">
      <div className="connection-head">
        <div>
          <p className="setting-title">{connection.accountEmail}</p>
          <p className="muted small">
            {connection.status === 'ACTIVE' && <span className="badge badge-ok">Connected</span>}
            {connection.status === 'EXPIRED' && <span className="badge badge-warn">Access expired</span>}
            {connection.status === 'REVOKED' && <span className="badge badge-muted">Access revoked</span>}
            {connection.lastSyncedAt && <> · synced {formatRelative(connection.lastSyncedAt, now)}</>}
          </p>
        </div>
        <div className="inline-controls">
          {connection.status !== 'ACTIVE' && (
            <button type="button" className="btn btn-primary" onClick={() => api.startCalendarConnect(returnTo)}>
              Reconnect
            </button>
          )}
          <button
            type="button"
            className="btn btn-danger"
            disabled={disconnect.isPending}
            onClick={() => {
              if (
                window.confirm(
                  "Disconnect Google Calendar? Busy times won't be imported and new events won't be added to your calendar.",
                )
              ) {
                disconnect.mutate()
              }
            }}
          >
            {disconnect.isPending ? 'Disconnecting…' : 'Disconnect'}
          </button>
        </div>
      </div>
      <InlineError error={disconnect.error} />

      <div className="setting-group">
        <p className="setting-title">Check these calendars for busy time</p>
        <p className="setting-desc">Used by “Import from Google Calendar” on events.</p>
        <ul className="calendar-checklist">
          {connection.calendars.map((c) => (
            <li key={c.id}>
              <label>
                <input
                  type="checkbox"
                  checked={draft.busyCalendarIds.includes(c.id)}
                  onChange={(e) => toggleBusy(c.id, e.target.checked)}
                />
                <span className="cal-dot" style={{ background: c.color }} />
                {c.name}
                {c.primary && <span className="muted small"> · primary</span>}
              </label>
            </li>
          ))}
        </ul>
        {draft.busyCalendarIds.length === 0 && (
          <p className="inline-error">With no calendars selected, importing won't find any busy time.</p>
        )}
      </div>

      <SettingRow
        title="Add confirmed events to"
        description="Where events from PlanYourFriday appear."
        htmlFor="writeCalendar"
      >
        <select
          id="writeCalendar"
          value={draft.writeCalendarId}
          onChange={(e) => onChange({ ...draft, writeCalendarId: e.target.value })}
        >
          {connection.calendars.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </SettingRow>
    </SettingsSection>
  )
}

function AccountSection({ email }: { email: string }) {
  const logout = useLogout()
  const logoutAll = useLogoutEverywhere()
  const deleteAccount = useDeleteAccount()
  const [exporting, setExporting] = useState(false)
  const [exportError, setExportError] = useState<unknown>(null)
  const [confirmText, setConfirmText] = useState('')
  const [confirmOpen, setConfirmOpen] = useState(false)

  async function download() {
    setExporting(true)
    setExportError(null)
    try {
      const data = await api.exportMyData()
      const url = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' }))
      const a = document.createElement('a')
      a.href = url
      a.download = `planyourfriday-${new Date().toISOString().slice(0, 10)}.json`
      a.click()
      URL.revokeObjectURL(url)
    } catch (e) {
      setExportError(e)
    } finally {
      setExporting(false)
    }
  }

  return (
    <SettingsSection id="account" icon={ShieldCheck} title="Account">
      <SettingRow title="Download my data" description="Your profile, settings, events and free time as a JSON file.">
        <button type="button" className="btn" disabled={exporting} onClick={download}>
          {exporting ? 'Preparing…' : 'Download'}
        </button>
      </SettingRow>
      <InlineError error={exportError} />

      <SettingRow title="Sign out" description="Sign out on this device.">
        <button type="button" className="btn" disabled={logout.isPending} onClick={() => logout.mutate()}>
          Sign out
        </button>
      </SettingRow>

      <SettingRow title="Sign out everywhere" description="Ends your sessions on all devices, including this one.">
        <button
          type="button"
          className="btn"
          disabled={logoutAll.isPending}
          onClick={() => {
            if (window.confirm('Sign out on all devices?')) logoutAll.mutate()
          }}
        >
          Sign out everywhere
        </button>
      </SettingRow>
      <InlineError error={logoutAll.error} />

      <div className="danger-zone">
        <div className="setting-row">
          <div className="setting-text">
            <span className="setting-title">Delete account</span>
            <p className="setting-desc">
              Removes your profile, free time and calendar connection, and takes you out of all events. Events you
              organize are deleted for everyone. This can't be undone.
            </p>
          </div>
          {!confirmOpen && (
            <div className="setting-control">
              <button type="button" className="btn btn-danger" onClick={() => setConfirmOpen(true)}>
                Delete account…
              </button>
            </div>
          )}
        </div>
        {confirmOpen && (
          <form
            className="delete-confirm"
            onSubmit={(e) => {
              e.preventDefault()
              if (confirmText === email) deleteAccount.mutate()
            }}
          >
            <label>
              <span>
                Type your email <strong>{email}</strong> to confirm
              </span>
              <input value={confirmText} onChange={(e) => setConfirmText(e.target.value)} autoComplete="off" />
            </label>
            <InlineError error={deleteAccount.error} />
            <div className="form-actions">
              <button
                type="button"
                className="btn btn-ghost"
                onClick={() => {
                  setConfirmOpen(false)
                  setConfirmText('')
                }}
              >
                Cancel
              </button>
              <button
                type="submit"
                className="btn btn-danger-solid"
                disabled={confirmText !== email || deleteAccount.isPending}
              >
                {deleteAccount.isPending ? 'Deleting…' : 'Delete my account'}
              </button>
            </div>
          </form>
        )}
      </div>
    </SettingsSection>
  )
}
