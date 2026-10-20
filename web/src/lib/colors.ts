import type { GroupStatus, TimeRange } from '../api/types'

export type Stage = 'planning' | 'voting' | 'scheduled' | 'done'

export const STAGE_COLOR: Record<Stage, string> = {
  planning: 'var(--stage-planning)',
  voting: 'var(--stage-voting)',
  scheduled: 'var(--stage-scheduled)',
  done: 'var(--stage-done)',
}

export const STAGE_LABEL: Record<Stage, string> = {
  planning: 'Collecting free time',
  voting: 'Voting on a time',
  scheduled: 'Confirmed',
  done: 'Past or cancelled',
}

export function stageOf(
  g: { status: GroupStatus; window: TimeRange; event: { time: TimeRange } | null },
  now: number,
): Stage {
  const end = new Date(g.event?.time.end ?? g.window.end).getTime()
  if (g.status === 'CANCELLED' || end < now) return 'done'
  if (g.status === 'VOTING') return 'voting'
  if (g.status === 'SCHEDULED') return 'scheduled'
  return 'planning'
}

export const WHEEL_COLORS = ['#e2572b', '#2e7dd1', '#1f8a5b', '#8e44ad', '#c2185b', '#b7791f', '#00838f', '#5d6d7e']
