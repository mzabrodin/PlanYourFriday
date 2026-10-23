import type { GroupDto } from '../../api'

export function memberNameMap(group: GroupDto, meId: string | undefined): Map<string, string> {
  return new Map(group.members.map((m) => [m.userId, m.userId === meId ? 'You' : m.displayName]))
}

// Used mid-sentence ("Waiting for you, Petro and Iryna"), so "You" goes first and lowercase.
export function joinNames(names: string[]): string {
  const list = [...names.filter((n) => n === 'You').map(() => 'you'), ...names.filter((n) => n !== 'You')]
  if (list.length <= 1) return list.join('')
  return `${list.slice(0, -1).join(', ')} and ${list.at(-1)}`
}
