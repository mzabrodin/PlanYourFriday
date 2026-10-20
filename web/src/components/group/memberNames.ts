import type { GroupDto } from '../../api'

export function memberNameMap(group: GroupDto, meId: string | undefined): Map<string, string> {
  return new Map(group.members.map((m) => [m.userId, m.userId === meId ? 'You' : m.displayName]))
}

export function joinNames(names: string[]): string {
  if (names.length <= 1) return names.join('')
  return `${names.slice(0, -1).join(', ')} and ${names.at(-1)}`
}
