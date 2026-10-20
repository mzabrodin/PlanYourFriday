import type { GroupSummaryDto } from '../api/types'

export type GroupView = 'upcoming' | 'needs-input' | 'planning' | 'scheduled' | 'past' | 'all'
export type RoleFilter = 'any' | 'organizer' | 'participant'
export type GroupSort = 'soonest' | 'latest' | 'title'

export const GROUP_VIEWS: { value: GroupView; label: string }[] = [
  { value: 'upcoming', label: 'Upcoming' },
  { value: 'needs-input', label: 'Needs your input' },
  { value: 'planning', label: 'Planning' },
  { value: 'scheduled', label: 'Scheduled' },
  { value: 'past', label: 'Past & cancelled' },
  { value: 'all', label: 'All' },
]

export interface GroupFilters {
  view: GroupView
  role: RoleFilter
  sort: GroupSort
  query: string
}

const ms = (iso: string) => new Date(iso).getTime()

export const groupStart = (g: GroupSummaryDto) => ms(g.event?.time.start ?? g.window.start)
const groupEnd = (g: GroupSummaryDto) => ms(g.event?.time.end ?? g.window.end)

export function needsMyInput(g: GroupSummaryDto, now: number): boolean {
  if (groupEnd(g) < now) return false
  if (g.status === 'VOTING') return g.myVotePending && (g.votingDeadline === null || ms(g.votingDeadline) > now)
  const deadlineOk = g.availabilityDeadline === null || ms(g.availabilityDeadline) > now
  return g.status === 'PLANNING' && !g.myAvailabilityMarked && deadlineOk
}

export function matchesView(g: GroupSummaryDto, view: GroupView, now: number): boolean {
  const over = g.status === 'CANCELLED' || groupEnd(g) < now
  switch (view) {
    case 'upcoming':
      return !over
    case 'needs-input':
      return needsMyInput(g, now)
    case 'planning':
      return !over && (g.status === 'PLANNING' || g.status === 'VOTING')
    case 'scheduled':
      return !over && g.status === 'SCHEDULED'
    case 'past':
      return over
    case 'all':
      return true
  }
}

function matchesQuery(g: GroupSummaryDto, query: string): boolean {
  const q = query.trim().toLocaleLowerCase()
  if (!q) return true
  return [g.title, g.description ?? '', g.organizerName, g.event?.location ?? ''].some((field) =>
    field.toLocaleLowerCase().includes(q),
  )
}

export function filterBase(groups: GroupSummaryDto[], f: Pick<GroupFilters, 'role' | 'query'>): GroupSummaryDto[] {
  return groups.filter(
    (g) =>
      (f.role === 'any' || (f.role === 'organizer') === (g.myRole === 'ORGANIZER')) && matchesQuery(g, f.query),
  )
}

export function applyGroupFilters(groups: GroupSummaryDto[], f: GroupFilters, now: number): GroupSummaryDto[] {
  const result = filterBase(groups, f).filter((g) => matchesView(g, f.view, now))
  const compare: Record<GroupSort, (a: GroupSummaryDto, b: GroupSummaryDto) => number> = {
    soonest: (a, b) => groupStart(a) - groupStart(b),
    latest: (a, b) => groupStart(b) - groupStart(a),
    title: (a, b) => a.title.localeCompare(b.title),
  }
  return result.sort(compare[f.sort])
}
