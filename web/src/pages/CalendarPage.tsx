import { ArrowRight, MapPin, Plus, X } from 'lucide-react'
import FullCalendar, { type EventInput } from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/react/daygrid'
import listPlugin from '@fullcalendar/react/list'
import classicThemePlugin from '@fullcalendar/react/themes/classic'
import timeGridPlugin from '@fullcalendar/react/timegrid'
import '@fullcalendar/react/skeleton.css'
import '@fullcalendar/react/themes/classic/theme.css'
import '@fullcalendar/react/themes/classic/palette.css'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import type { CalendarItemDto, CalendarItemKind, TimeRange } from '../api'
import { useCalendar, usePrefs } from '../api/queries'
import { ErrorState } from '../components/States'
import { STAGE_COLOR, STAGE_LABEL, type Stage } from '../lib/colors'
import { formatRange } from '../lib/time'

const KIND_LABELS: Record<CalendarItemKind, string> = {
  EVENT: 'Confirmed events',
  PLANNING: 'Still planning',
  MY_FREE_TIME: 'My free time',
}

const KINDS = Object.keys(KIND_LABELS) as CalendarItemKind[]

function localDate(iso: string, addDays = 0): string {
  const d = new Date(iso)
  d.setDate(d.getDate() + addDays)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

type ItemTone = Stage | 'mine'

function toneOf(item: CalendarItemDto, now: number): ItemTone {
  if (item.kind === 'MY_FREE_TIME') return 'mine'
  if (item.kind === 'EVENT') return new Date(item.time.end).getTime() < now ? 'done' : 'scheduled'
  return item.groupStatus === 'VOTING' ? 'voting' : 'planning'
}

const TONE_LABEL: Record<ItemTone, string> = { ...STAGE_LABEL, mine: 'Your free time' }
const TONE_COLOR: Record<ItemTone, string> = { ...STAGE_COLOR, mine: 'var(--mine)' }

function toEventInput(item: CalendarItemDto, now: number): EventInput {
  const tone = toneOf(item, now)
  const common = {
    id: item.id,
    start: item.time.start,
    end: item.time.end,
    extendedProps: { item },
    className: `pc-tone pc-${tone}`,
  }
  switch (item.kind) {
    case 'EVENT':
      return { ...common, title: item.title }
    case 'PLANNING':
      return {
        ...common,
        start: localDate(item.time.start),
        end: localDate(item.time.end, 1),
        allDay: true,
        title: `${item.title} · ${tone === 'voting' ? 'voting' : 'picking a time'}`,
        className: `${common.className} pc-tentative`,
      }
    case 'MY_FREE_TIME':
      return { ...common, title: `Free · ${item.groupTitle}` }
  }
}

export function CalendarPage() {
  const navigate = useNavigate()
  const [range, setRange] = useState<TimeRange | null>(null)
  const [visible, setVisible] = useState<Set<CalendarItemKind>>(() => new Set(KINDS))
  const [selected, setSelected] = useState<CalendarItemDto | null>(null)
  const calendar = useCalendar(range)
  const [now] = useState(Date.now)
  const prefs = usePrefs()
  const hour12 = prefs.timeFormat === '12h'

  const items = calendar.data ?? []
  const events = items.filter((i) => visible.has(i.kind)).map((i) => toEventInput(i, now))

  function toggle(kind: CalendarItemKind) {
    setVisible((prev) => {
      const next = new Set(prev)
      if (next.has(kind)) next.delete(kind)
      else next.add(kind)
      return next
    })
  }

  return (
    <div className="stack">
      <div className="page-header">
        <h1>My calendar</h1>
        <Link to="/groups/new" className="btn btn-primary">
          <Plus size={18} aria-hidden="true" /> New event
        </Link>
      </div>

      <div className="filters" role="group" aria-label="Show">
        {KINDS.map((kind) => (
          <label key={kind} className={`filter filter-${kind.toLowerCase()}`}>
            <input type="checkbox" checked={visible.has(kind)} onChange={() => toggle(kind)} />
            {KIND_LABELS[kind]}
          </label>
        ))}
        {calendar.isFetching && <span className="muted small">Loading…</span>}
      </div>

      {calendar.error && <ErrorState error={calendar.error} onRetry={calendar.refetch} />}

      <div className="calendar-page">
        <section className="card calendar personal-calendar">
          <FullCalendar
            plugins={[dayGridPlugin, timeGridPlugin, listPlugin, classicThemePlugin]}
            initialView="dayGridMonth"
            headerToolbar={{ start: 'prev,next today', center: 'title', end: 'dayGridMonth,timeGridWeek,listMonth' }}
            buttons={{
              today: { text: 'Today' },
              dayGridMonth: { text: 'Month' },
              timeGridWeek: { text: 'Week' },
              listMonth: { text: 'List' },
            }}
            firstDay={prefs.weekStartsOn}
            height={760}
            dayMaxEvents={3}
            eventDisplay="block"
            nowIndicator
            scrollTime="08:00:00"
            eventTimeFormat={{ hour: '2-digit', minute: '2-digit', hour12 }}
            slotHeaderFormat={{ hour: '2-digit', minute: '2-digit', hour12 }}
            noEventsText="Nothing planned in this period"
            events={events}
            datesSet={(info) => {
              const next = { start: info.start.toISOString(), end: info.end.toISOString() }
              setRange((prev) => (prev?.start === next.start && prev.end === next.end ? prev : next))
            }}
            eventClick={(info) => {
              info.jsEvent.preventDefault()
              setSelected(info.event.extendedProps.item as CalendarItemDto)
            }}
          />
        </section>

        <aside className="stack">
          {selected ? (
            <section className="card item-details" style={{ borderTopColor: TONE_COLOR[toneOf(selected, now)] }}>
              <header className="card-header">
                <p className="eyebrow" style={{ color: TONE_COLOR[toneOf(selected, now)] }}>
                  {TONE_LABEL[toneOf(selected, now)]}
                </p>
                <button
                  type="button"
                  className="link-button muted"
                  onClick={() => setSelected(null)}
                  aria-label="Close"
                >
                  <X size={18} />
                </button>
              </header>
              <h2>{selected.kind === 'MY_FREE_TIME' ? `Free for ${selected.groupTitle}` : selected.title}</h2>
              <p className="event-when">{formatRange(selected.time)}</p>
              {selected.kind === 'PLANNING' && <p className="muted small">Window friends are choosing a time in</p>}
              {selected.location && (
                <p className="icon-line">
                  <MapPin size={16} aria-hidden="true" /> {selected.location}
                </p>
              )}
              <p className="small">
                <strong>Organizer:</strong> {selected.organizerName}
              </p>
              <p className="small">
                <strong>Who:</strong> {selected.people.join(', ')}
              </p>
              <button type="button" className="btn" onClick={() => navigate(`/groups/${selected.groupId}`)}>
                Open event <ArrowRight size={16} aria-hidden="true" />
              </button>
            </section>
          ) : (
            <section className="card">
              <p className="muted small">Click anything on the calendar to see who's going and where.</p>
            </section>
          )}

          <section className="card">
            <h3>What the colors mean</h3>
            <ul className="group-legend">
              {(Object.keys(TONE_LABEL) as ItemTone[])
                .filter((tone) => tone !== 'cancelled')
                .map((tone) => (
                  <li key={tone}>
                    <span className="swatch" style={{ background: TONE_COLOR[tone] }} />
                    {TONE_LABEL[tone]}
                  </li>
                ))}
            </ul>
          </section>
        </aside>
      </div>
    </div>
  )
}
