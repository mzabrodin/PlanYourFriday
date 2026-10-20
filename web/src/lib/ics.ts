import type { EventDto } from '../api/types'

const stamp = (iso: string) => iso.replace(/[-:]/g, '').replace(/\.\d{3}/, '')
const escape = (text: string) => text.replace(/\\/g, '\\\\').replace(/\n/g, '\\n').replace(/[,;]/g, (c) => `\\${c}`)

export function eventToIcs(event: EventDto): string {
  const description = [event.description, event.meetingLink].filter(Boolean).join('\n\n')
  return [
    'BEGIN:VCALENDAR',
    'VERSION:2.0',
    'PRODID:-//PlanYourFriday//EN',
    'BEGIN:VEVENT',
    `UID:${event.id}@planyourfriday`,
    `DTSTAMP:${stamp(new Date().toISOString())}`,
    `DTSTART:${stamp(event.time.start)}`,
    `DTEND:${stamp(event.time.end)}`,
    `SUMMARY:${escape(event.title)}`,
    event.location && `LOCATION:${escape(event.location)}`,
    description && `DESCRIPTION:${escape(description)}`,
    event.meetingLink && `URL:${event.meetingLink}`,
    'END:VEVENT',
    'END:VCALENDAR',
  ]
    .filter(Boolean)
    .join('\r\n')
}

export function downloadIcs(event: EventDto) {
  const url = URL.createObjectURL(new Blob([eventToIcs(event)], { type: 'text/calendar' }))
  const a = document.createElement('a')
  a.href = url
  a.download = `${event.title.replace(/[^\p{L}\p{N}]+/gu, '-').toLowerCase() || 'event'}.ics`
  a.click()
  URL.revokeObjectURL(url)
}

export const mapsUrl = (place: string) => `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(place)}`
