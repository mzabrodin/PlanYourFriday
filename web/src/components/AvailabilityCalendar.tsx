import FullCalendar, { type DateSpanApi, type EventInput } from '@fullcalendar/react'
import interactionPlugin from '@fullcalendar/react/interaction'
import classicThemePlugin from '@fullcalendar/react/themes/classic'
import timeGridPlugin from '@fullcalendar/react/timegrid'
import '@fullcalendar/react/skeleton.css'
import '@fullcalendar/react/themes/classic/theme.css'
import '@fullcalendar/react/themes/classic/palette.css'
import type { EventDto, TimeRange } from '../api'
import { usePrefs } from '../api/queries'
import { mergeRanges } from '../lib/time'

interface Props {
  window: TimeRange
  mySlots: TimeRange[]
  otherSlots: TimeRange[]
  optimalSlots: TimeRange[]
  event: EventDto | null
  editable: boolean
  onChange: (slots: TimeRange[]) => void
}

const DAY_MS = 86_400_000

function startOfDay(iso: string) {
  const d = new Date(iso)
  d.setHours(0, 0, 0, 0)
  return d
}

export function AvailabilityCalendar({ window, mySlots, otherSlots, optimalSlots, event, editable, onChange }: Props) {
  const prefs = usePrefs()
  const hour12 = prefs.timeFormat === '12h'
  const firstDay = startOfDay(window.start)
  const lastDay = new Date(startOfDay(window.end).getTime() + DAY_MS)
  const days = Math.round((lastDay.getTime() - firstDay.getTime()) / DAY_MS)

  const windowStart = new Date(window.start)
  const windowEnd = new Date(window.end)
  const insideWindow = (span: DateSpanApi) => !span.allDay && span.start >= windowStart && span.end <= windowEnd

  const events: EventInput[] = [
    { start: firstDay, end: windowStart, display: 'background', className: 'fc-outside-window' },
    { start: windowEnd, end: lastDay, display: 'background', className: 'fc-outside-window' },
    ...otherSlots.map((s) => ({ ...s, display: 'background', className: 'fc-slot-others' })),
    ...optimalSlots.map((s) => ({ ...s, display: 'background', className: 'fc-slot-optimal' })),
    ...mySlots.map((s, i) => ({
      id: `mine-${i}`,
      title: 'Free',
      ...s,
      className: 'fc-slot-mine',
      editable,
    })),
  ]
  if (event) events.push({ title: event.title, ...event.time, className: 'fc-slot-event', editable: false })

  const indexOf = (id: string) => Number(id.replace('mine-', ''))

  return (
    <div className="calendar">
      <FullCalendar
        plugins={[timeGridPlugin, interactionPlugin, classicThemePlugin]}
        initialView="timeGrid"
        dayCount={Math.min(days, 7)}
        initialDate={firstDay}
        validRange={{ start: firstDay, end: lastDay }}
        headerToolbar={days > 7 ? { start: 'prev,next', center: 'title', end: '' } : false}
        allDaySlot={false}
        slotDuration="00:30:00"
        snapDuration="00:15:00"
        scrollTime={`${String(Math.max(0, windowStart.getHours() - 1)).padStart(2, '0')}:00:00`}
        height={640}
        slotHeaderFormat={{ hour: '2-digit', minute: '2-digit', hour12 }}
        eventTimeFormat={{ hour: '2-digit', minute: '2-digit', hour12 }}
        nowIndicator
        events={events}
        selectable={editable}
        selectMirror
        selectAllow={insideWindow}
        eventAllow={insideWindow}
        select={(info) => {
          onChange(mergeRanges([...mySlots, { start: info.start.toISOString(), end: info.end.toISOString() }]))
          info.view.calendar.unselect()
        }}
        eventChange={(info) => {
          const { event: changed } = info
          if (!changed.id.startsWith('mine-') || !changed.start || !changed.end) return info.revert()
          const next = [...mySlots]
          next[indexOf(changed.id)] = { start: changed.start.toISOString(), end: changed.end.toISOString() }
          onChange(mergeRanges(next))
        }}
        eventClick={(info) => {
          if (!editable || !info.event.id.startsWith('mine-')) return
          onChange(mySlots.filter((_, i) => i !== indexOf(info.event.id)))
        }}
      />
    </div>
  )
}
