import type { TimeRange } from '../api/types'

const toMs = (iso: string) => new Date(iso).getTime()

export function mergeRanges(ranges: TimeRange[]): TimeRange[] {
  const sorted = [...ranges].sort((a, b) => toMs(a.start) - toMs(b.start))
  const merged: TimeRange[] = []
  for (const r of sorted) {
    const last = merged.at(-1)
    if (last && toMs(r.start) <= toMs(last.end)) {
      if (toMs(r.end) > toMs(last.end)) last.end = r.end
    } else {
      merged.push({ ...r })
    }
  }
  return merged
}

export function intersectRanges(a: TimeRange[], b: TimeRange[]): TimeRange[] {
  const result: TimeRange[] = []
  const left = mergeRanges(a)
  const right = mergeRanges(b)
  let i = 0
  let j = 0
  while (i < left.length && j < right.length) {
    const start = Math.max(toMs(left[i].start), toMs(right[j].start))
    const end = Math.min(toMs(left[i].end), toMs(right[j].end))
    if (start < end) {
      result.push({ start: new Date(start).toISOString(), end: new Date(end).toISOString() })
    }
    if (toMs(left[i].end) < toMs(right[j].end)) i++
    else j++
  }
  return result
}

export function findCommonSlots(
  memberSlots: TimeRange[][],
  window: TimeRange,
  minMinutes: number,
): TimeRange[] {
  if (memberSlots.length === 0) return []
  const common = memberSlots.reduce((acc, slots) => intersectRanges(acc, slots), [window])
  return common.filter((r) => toMs(r.end) - toMs(r.start) >= minMinutes * 60_000)
}

const dateFmt = new Intl.DateTimeFormat(undefined, {
  weekday: 'short',
  day: 'numeric',
  month: 'short',
})
const makeTimeFmt = (hour12: boolean | undefined) =>
  new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit', hour12 })
let timeFmt = makeTimeFmt(false)

export function setHour12(hour12: boolean) {
  timeFmt = makeTimeFmt(hour12)
}

export function formatDate(iso: string): string {
  return dateFmt.format(new Date(iso))
}

export function formatDateTime(iso: string): string {
  const d = new Date(iso)
  return `${dateFmt.format(d)}, ${timeFmt.format(d)}`
}

export function formatRange(range: TimeRange): string {
  const start = new Date(range.start)
  const end = new Date(range.end)
  const sameDay = start.toDateString() === end.toDateString()
  return sameDay
    ? `${dateFmt.format(start)}, ${timeFmt.format(start)}–${timeFmt.format(end)}`
    : `${dateFmt.format(start)} ${timeFmt.format(start)} – ${dateFmt.format(end)} ${timeFmt.format(end)}`
}

export function formatDuration(minutes: number): string {
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  if (h && m) return `${h} h ${m} min`
  return h ? `${h} h` : `${m} min`
}

export function toLocalInput(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

export function fromLocalInput(value: string): string {
  return new Date(value).toISOString()
}

const DAY = 86_400_000

function startOfDayMs(ms: number): number {
  const d = new Date(ms)
  d.setHours(0, 0, 0, 0)
  return d.getTime()
}

export function daysUntil(iso: string, now: number): number {
  return Math.round((startOfDayMs(new Date(iso).getTime()) - startOfDayMs(now)) / DAY)
}

export function formatRelative(iso: string, now: number): string {
  const diff = new Date(iso).getTime() - now
  const days = daysUntil(iso, now)
  if (diff < 0) {
    const ago = -diff
    if (ago < 60_000) return 'just now'
    if (ago < 3_600_000) return `${Math.round(ago / 60_000)} min ago`
    if (ago < 86_400_000) return `${Math.round(ago / 3_600_000)} h ago`
    return days === -1 ? 'yesterday' : `${-days} days ago`
  }
  if (days === 0) {
    const hours = Math.round(diff / 3_600_000)
    return hours < 1 ? 'in less than an hour' : `in ${hours} h`
  }
  if (days === 1) return 'tomorrow'
  if (days < 7) return `in ${days} days`
  const weeks = Math.round(days / 7)
  return weeks === 1 ? 'in a week' : `in ${weeks} weeks`
}

export function formatTime(iso: string): string {
  return timeFmt.format(new Date(iso))
}

export function usualSlots(
  window: TimeRange,
  usual: { weekdays: { start: string; end: string } | null; weekends: { start: string; end: string } | null },
): TimeRange[] {
  const slots: TimeRange[] = []
  const day = new Date(window.start)
  day.setHours(0, 0, 0, 0)
  const windowEnd = new Date(window.end)
  while (day < windowEnd) {
    const weekend = day.getDay() === 0 || day.getDay() === 6
    const hours = weekend ? usual.weekends : usual.weekdays
    if (hours) {
      const at = (hhmm: string) => {
        const [h, m] = hhmm.split(':').map(Number)
        const d = new Date(day)
        d.setHours(h, m, 0, 0)
        return d
      }
      const start = at(hours.start)
      let end = at(hours.end)
      if (end <= start) end = new Date(end.getTime() + DAY)
      slots.push({ start: start.toISOString(), end: end.toISOString() })
    }
    day.setDate(day.getDate() + 1)
  }
  return intersectRanges(slots, [window])
}

export function suggestTimes(
  members: { userId: string; slots: TimeRange[] }[],
  window: TimeRange,
  durationMinutes: number,
  { stepMinutes = 30, max = 6 } = {},
): { time: TimeRange; freeUserIds: string[] }[] {
  const durationMs = durationMinutes * 60_000
  const stepMs = stepMinutes * 60_000
  const merged = members.map((m) => ({ userId: m.userId, slots: mergeRanges(m.slots) }))
  const windowStart = new Date(window.start).getTime()
  const windowEnd = new Date(window.end).getTime()

  const candidates: { start: number; free: string[] }[] = []
  for (let start = Math.ceil(windowStart / stepMs) * stepMs; start + durationMs <= windowEnd; start += stepMs) {
    const end = start + durationMs
    const free = merged
      .filter((m) => m.slots.some((s) => new Date(s.start).getTime() <= start && new Date(s.end).getTime() >= end))
      .map((m) => m.userId)
    if (free.length > 0) candidates.push({ start, free })
  }

  candidates.sort((a, b) => b.free.length - a.free.length || a.start - b.start)
  const picked: typeof candidates = []
  for (const c of candidates) {
    if (picked.length >= max) break
    if (picked.every((p) => c.start + durationMs <= p.start || p.start + durationMs <= c.start)) picked.push(c)
  }
  return picked.map((c) => ({
    time: { start: new Date(c.start).toISOString(), end: new Date(c.start + durationMs).toISOString() },
    freeUserIds: c.free,
  }))
}
