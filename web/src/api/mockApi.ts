import type { Api } from './api'
import { ApiError } from './client'
import type {
  CalendarConnectionDto,
  NotificationDto,
  VoteAnswer,
  CalendarItemDto,
  EventDto,
  GroupDto,
  InvitationDto,
  MemberDto,
  MemberRole,
  TimeRange,
  UserDto,
  UserSettingsDto,
} from './types'
import { DEFAULT_SETTINGS } from '../lib/preferences'
import { findCommonSlots, suggestTimes } from '../lib/time'


interface StoredGroup {
  id: string
  title: string
  description: string | null
  organizerId: string
  inviteToken: string
  window: TimeRange
  eventDurationMinutes: number
  availabilityDeadline: string | null
  status: GroupDto['status']
  members: { userId: string; role: MemberRole }[]
  invitations: InvitationDto[]
  event: StoredEvent | null
  options: StoredOption[]
  votingDeadline: string | null
}

type StoredEvent = Omit<EventDto, 'myCalendarSync'>

interface StoredOption {
  id: string
  time: TimeRange
  freeUserIds: string[]
  votes: Record<string, VoteAnswer>
}

interface MockDb {
  loggedIn: boolean
  meId: string
  users: UserDto[]
  groups: StoredGroup[]
  availability: Record<string, Record<string, TimeRange[]>>
  settings: UserSettingsDto
  connection: CalendarConnectionDto | null
  notifications: NotificationDto[]
}

function newConnection(email: string): CalendarConnectionDto {
  return {
    id: 'cc-google',
    provider: 'GOOGLE',
    accountEmail: email,
    status: 'ACTIVE',
    calendars: [
      { id: 'primary', name: email, color: '#4285f4', primary: true },
      { id: 'uni', name: 'University schedule', color: '#0b8043', primary: false },
      { id: 'work', name: 'Work', color: '#d50000', primary: false },
      { id: 'holidays', name: 'Holidays in Ukraine', color: '#7986cb', primary: false },
    ],
    busyCalendarIds: ['primary', 'uni', 'work'],
    writeCalendarId: 'primary',
    lastSyncedAt: new Date().toISOString(),
  }
}

const STORAGE_KEY = 'pyf-mock-db-v6'

const at = (base: Date, dayOffset: number, hours: number) => {
  const d = new Date(base)
  d.setDate(d.getDate() + dayOffset)
  d.setHours(hours, 0, 0, 0)
  return d.toISOString()
}

function nextFriday(): Date {
  const d = new Date()
  d.setDate(d.getDate() + ((5 - d.getDay() + 7) % 7 || 7))
  d.setHours(0, 0, 0, 0)
  return d
}

function seed(): MockDb {
  const fri = nextFriday()
  const me: UserDto = {
    id: 'u-me',
    email: 'you@example.com',
    displayName: 'You',
    photoUrl: null,
    timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    calendarConnected: false,
  }
  const max: UserDto = { ...me, id: 'u-max', email: 'max@example.com', displayName: 'Max' }
  const tania: UserDto = { ...me, id: 'u-tania', email: 'tania@example.com', displayName: 'Tania' }
  const olena: UserDto = { ...me, id: 'u-olena', email: 'olena@example.com', displayName: 'Olena' }

  const raw = {
    loggedIn: false,
    meId: me.id,
    settings: DEFAULT_SETTINGS,
    connection: null,
    users: [me, max, tania, olena],
    groups: [
      {
        id: 'g-board-games',
        title: 'Board games night',
        description: 'Catan + pizza. Bring snacks!',
        organizerId: me.id,
        inviteToken: 'demo-board-games',
        window: { start: at(fri, 0, 16), end: at(fri, 2, 23) },
        eventDurationMinutes: 180,
        availabilityDeadline: at(fri, -1, 20),
        status: 'PLANNING',
        members: [
          { userId: me.id, role: 'ORGANIZER' },
          { userId: max.id, role: 'PARTICIPANT' },
          { userId: tania.id, role: 'PARTICIPANT' },
        ],
        invitations: [
          { id: 'i-1', email: 'max@example.com', status: 'ACCEPTED' },
          { id: 'i-2', email: 'tania@example.com', status: 'ACCEPTED' },
          { id: 'i-3', email: 'olena@example.com', status: 'SENT' },
        ],
        event: null,
      },
      {
        id: 'g-hiking',
        title: 'Hiking trip',
        description: null,
        organizerId: max.id,
        inviteToken: 'demo-hiking',
        window: { start: at(fri, 1, 8), end: at(fri, 2, 20) },
        eventDurationMinutes: 300,
        availabilityDeadline: null,
        status: 'SCHEDULED',
        members: [
          { userId: max.id, role: 'ORGANIZER' },
          { userId: me.id, role: 'PARTICIPANT' },
        ],
        invitations: [],
        event: {
          id: 'e-hiking',
          title: 'Hiking trip',
          time: { start: at(fri, 1, 9), end: at(fri, 1, 14) },
          location: 'Holosiivskyi park, main entrance',
          meetingLink: null,
          description: 'Bring water and comfy shoes. Coffee at the entrance kiosk after.',
          status: 'CONFIRMED',
        },
      },
      {
        id: 'g-movie',
        title: 'Movie night',
        description: 'Dune: Part Three in IMAX',
        organizerId: tania.id,
        inviteToken: 'demo-movie',
        window: { start: at(fri, 5, 18), end: at(fri, 7, 23) },
        eventDurationMinutes: 180,
        availabilityDeadline: null,
        status: 'SCHEDULED',
        members: [
          { userId: tania.id, role: 'ORGANIZER' },
          { userId: me.id, role: 'PARTICIPANT' },
          { userId: olena.id, role: 'PARTICIPANT' },
        ],
        invitations: [],
        event: {
          id: 'e-movie',
          title: 'Movie night',
          time: { start: at(fri, 6, 19), end: at(fri, 6, 22) },
          location: 'Multiplex, Lavina Mall',
          meetingLink: null,
          description: 'Tickets are on Tania, send her 250 UAH.',
          status: 'CONFIRMED',
        },
      },
      {
        id: 'g-sushi',
        title: 'Sushi dinner',
        description: null,
        organizerId: olena.id,
        inviteToken: 'demo-sushi',
        window: { start: at(fri, -7, 17), end: at(fri, -6, 23) },
        eventDurationMinutes: 120,
        availabilityDeadline: null,
        status: 'SCHEDULED',
        members: [
          { userId: olena.id, role: 'ORGANIZER' },
          { userId: me.id, role: 'PARTICIPANT' },
          { userId: max.id, role: 'PARTICIPANT' },
        ],
        invitations: [],
        event: {
          id: 'e-sushi',
          title: 'Sushi dinner',
          time: { start: at(fri, -7, 19), end: at(fri, -7, 21) },
          location: 'Sushi Master, Podil',
          meetingLink: null,
          description: null,
          status: 'CONFIRMED',
        },
      },
      {
        id: 'g-study',
        title: 'Exam prep: ООАД',
        description: 'Go through the C4 + UML diagrams together',
        organizerId: max.id,
        inviteToken: 'demo-study',
        window: { start: at(fri, 3, 9), end: at(fri, 4, 21) },
        eventDurationMinutes: 120,
        availabilityDeadline: null,
        status: 'PLANNING',
        members: [
          { userId: max.id, role: 'ORGANIZER' },
          { userId: me.id, role: 'PARTICIPANT' },
          { userId: tania.id, role: 'PARTICIPANT' },
        ],
        invitations: [],
        event: null,
      },
      {
        id: 'g-bowling',
        title: 'Bowling',
        description: 'Cancelled: the place is closed for renovation',
        organizerId: tania.id,
        inviteToken: 'demo-bowling',
        window: { start: at(fri, 1, 12), end: at(fri, 2, 22) },
        eventDurationMinutes: 120,
        availabilityDeadline: null,
        status: 'CANCELLED',
        members: [
          { userId: tania.id, role: 'ORGANIZER' },
          { userId: me.id, role: 'PARTICIPANT' },
        ],
        invitations: [],
        event: null,
      },
      {
        id: 'g-birthday',
        title: "Olena's birthday",
        description: 'Surprise party, keep it secret',
        organizerId: max.id,
        inviteToken: 'demo-birthday',
        window: { start: at(fri, 14, 17), end: at(fri, 16, 23) },
        eventDurationMinutes: 240,
        availabilityDeadline: at(fri, 10, 20),
        status: 'PLANNING',
        members: [
          { userId: max.id, role: 'ORGANIZER' },
          { userId: me.id, role: 'PARTICIPANT' },
          { userId: tania.id, role: 'PARTICIPANT' },
        ],
        invitations: [],
        event: null,
      },
    ],
    availability: {
      'g-bowling': {},
      'g-birthday': { [max.id]: [{ start: at(fri, 15, 18), end: at(fri, 15, 23) }] },
      'g-board-games': {
        [max.id]: [
          { start: at(fri, 0, 18), end: at(fri, 0, 23) },
          { start: at(fri, 1, 12), end: at(fri, 1, 18) },
        ],
        [tania.id]: [
          { start: at(fri, 0, 19), end: at(fri, 0, 23) },
          { start: at(fri, 1, 14), end: at(fri, 1, 20) },
          { start: at(fri, 2, 16), end: at(fri, 2, 21) },
        ],
      },
      'g-hiking': {},
      'g-movie': {},
      'g-sushi': {},
      'g-study': {
        [me.id]: [{ start: at(fri, 3, 14), end: at(fri, 3, 18) }],
        [max.id]: [{ start: at(fri, 3, 10), end: at(fri, 3, 17) }],
        [tania.id]: [
          { start: at(fri, 3, 15), end: at(fri, 3, 19) },
          { start: at(fri, 4, 10), end: at(fri, 4, 13) },
        ],
      },
    },
  }

  const hoursAgo = (h: number) => new Date(Date.now() - h * 3_600_000).toISOString()
  const data: MockDb = {
    ...raw,
    groups: raw.groups.map((g) => ({ ...g, options: [], votingDeadline: null }) as StoredGroup),
    notifications: [
      { id: 'n-1', kind: 'VOTING_STARTED', groupId: 'g-study', groupTitle: 'Exam prep: ООАД', actorName: 'Max', createdAt: hoursAgo(2), read: false },
      { id: 'n-2', kind: 'DEADLINE_SOON', groupId: 'g-board-games', groupTitle: 'Board games night', actorName: null, createdAt: hoursAgo(3), read: false },
      { id: 'n-3', kind: 'EVENT_UPDATED', groupId: 'g-hiking', groupTitle: 'Hiking trip', actorName: 'Max', createdAt: hoursAgo(5), read: false },
      { id: 'n-4', kind: 'INVITED', groupId: 'g-birthday', groupTitle: "Olena's birthday", actorName: 'Max', createdAt: hoursAgo(26), read: false },
      { id: 'n-5', kind: 'EVENT_CONFIRMED', groupId: 'g-movie', groupTitle: 'Movie night', actorName: 'Tania', createdAt: hoursAgo(50), read: true },
      { id: 'n-6', kind: 'MEMBER_JOINED', groupId: 'g-board-games', groupTitle: 'Board games night', actorName: 'Tania', createdAt: hoursAgo(74), read: true },
      { id: 'n-7', kind: 'EVENT_CANCELLED', groupId: 'g-bowling', groupTitle: 'Bowling', actorName: 'Tania', createdAt: hoursAgo(98), read: true },
    ],
  }

  const study = data.groups.find((g) => g.id === 'g-study')!
  study.status = 'VOTING'
  study.votingDeadline = at(fri, 2, 20)
  study.options = [
    { id: 'o-1', time: { start: at(fri, 3, 15), end: at(fri, 3, 17) }, freeUserIds: [me.id, max.id, tania.id], votes: { [max.id]: 'YES', [tania.id]: 'YES' } },
    { id: 'o-2', time: { start: at(fri, 3, 14), end: at(fri, 3, 16) }, freeUserIds: [me.id, max.id], votes: { [max.id]: 'YES', [tania.id]: 'NO' } },
    { id: 'o-3', time: { start: at(fri, 4, 10), end: at(fri, 4, 12) }, freeUserIds: [tania.id], votes: { [max.id]: 'MAYBE', [tania.id]: 'YES' } },
  ]
  return data
}

function normalize(data: Partial<MockDb>): MockDb {
  const fresh = seed()
  return {
    ...fresh,
    ...data,
    settings: { ...DEFAULT_SETTINGS, ...data.settings },
    connection: data.connection ?? null,
    notifications: data.notifications ?? [],
    users: (data.users ?? fresh.users).map((u) => ({ ...u, photoUrl: u.photoUrl ?? null })),
    groups: (data.groups ?? fresh.groups).map((g) => ({
      ...g,
      options: g.options ?? [],
      votingDeadline: g.votingDeadline ?? null,
      event: g.event && { ...g.event, description: g.event.description ?? null },
    })),
    availability: data.availability ?? {},
  }
}

function load(): MockDb {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw) return normalize(JSON.parse(raw) as Partial<MockDb>)
  } catch {}
  return seed()
}

let db = load()

function save() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(db))
  } catch {}
}

function redirectBack(returnTo: string) {
  const target = new URL(returnTo, window.location.href)
  if (target.pathname === window.location.pathname) {
    window.location.hash = target.hash
    window.location.reload()
  } else {
    window.location.assign(returnTo)
  }
}

export function resetMockDb() {
  db = seed()
  db.loggedIn = true
  save()
}

const delay = <T>(value: T, ms = 250) =>
  new Promise<T>((resolve) => setTimeout(() => resolve(value === undefined ? value : structuredClone(value)), ms))
const uid = (prefix: string) => `${prefix}-${crypto.randomUUID().slice(0, 8)}`

function me(): UserDto {
  if (!db.loggedIn) throw new ApiError(401, 'Not signed in')
  return db.users.find((u) => u.id === db.meId)!
}

function findGroup(groupId: string): StoredGroup {
  const group = db.groups.find((g) => g.id === groupId)
  if (!group || !group.members.some((m) => m.userId === me().id)) {
    throw new ApiError(404, 'Group not found')
  }
  return group
}

function requireOrganizer(group: StoredGroup) {
  if (group.organizerId !== me().id) throw new ApiError(403, 'Only the organizer can do this')
}

function toMemberDto(group: StoredGroup, userId: string, role: MemberRole): MemberDto {
  const user = db.users.find((u) => u.id === userId)!
  return {
    userId,
    displayName: user.displayName,
    email: user.email,
    role,
    hasAvailability: (db.availability[group.id]?.[userId]?.length ?? 0) > 0,
  }
}

function toEventDto(event: StoredEvent | null): EventDto | null {
  if (!event) return null
  return { ...event, myCalendarSync: me().calendarConnected ? 'SYNCED' : 'NOT_CONNECTED' }
}

function toGroupDto(group: StoredGroup): GroupDto {
  const isOrganizer = group.organizerId === me().id
  return {
    id: group.id,
    title: group.title,
    description: group.description,
    organizerId: group.organizerId,
    inviteToken: isOrganizer ? group.inviteToken : null,
    window: group.window,
    eventDurationMinutes: group.eventDurationMinutes,
    availabilityDeadline: group.availabilityDeadline,
    status: group.status,
    myRole: isOrganizer ? 'ORGANIZER' : 'PARTICIPANT',
    members: group.members.map((m) => toMemberDto(group, m.userId, m.role)),
    invitations: isOrganizer ? group.invitations : [],
    options: group.options.map((o) => ({
      id: o.id,
      time: o.time,
      freeUserIds: o.freeUserIds,
      votes: Object.entries(o.votes).map(([userId, answer]) => ({ userId, answer })),
    })),
    votingDeadline: group.votingDeadline,
    event: toEventDto(group.event),
  }
}

function memberSlots(group: StoredGroup) {
  const byUser = db.availability[group.id] ?? {}
  return group.members.map((m) => ({ userId: m.userId, slots: byUser[m.userId] ?? [] }))
}

export const mockApi: Api = {
  getMe: () => delay(db.loggedIn ? me() : null),

  startLogin(returnTo) {
    db.loggedIn = true
    save()
    redirectBack(returnTo)
  },

  async logout() {
    db.loggedIn = false
    save()
    await delay(undefined)
  },

  startCalendarConnect(returnTo) {
    me().calendarConnected = true
    db.connection = newConnection(me().email)
    save()
    redirectBack(returnTo)
  },

  async disconnectCalendar() {
    me().calendarConnected = false
    db.connection = null
    save()
    await delay(undefined)
  },

  async logoutEverywhere() {
    db.loggedIn = false
    save()
    await delay(undefined)
  },

  async updateProfile(body) {
    const user = me()
    const name = body.displayName.trim()
    if (!name) throw new ApiError(400, 'Name cannot be empty')
    user.displayName = name
    user.timeZone = body.timeZone
    save()
    return delay({ ...user })
  },

  async getSettings() {
    me()
    return delay(structuredClone(db.settings))
  },

  async updateSettings(body) {
    me()
    db.settings = structuredClone(body)
    save()
    return delay(structuredClone(db.settings))
  },

  async getCalendarConnections() {
    me()
    return delay(db.connection ? [structuredClone(db.connection)] : [])
  },

  async updateCalendarConnection(id, body) {
    me()
    if (!db.connection || db.connection.id !== id) throw new ApiError(404, 'Calendar connection not found')
    if (!db.connection.calendars.some((c) => c.id === body.writeCalendarId)) {
      throw new ApiError(400, 'Unknown calendar')
    }
    db.connection.busyCalendarIds = body.busyCalendarIds
    db.connection.writeCalendarId = body.writeCalendarId
    save()
    return delay(structuredClone(db.connection))
  },

  async exportMyData() {
    const user = me()
    const groups = db.groups.filter((g) => g.members.some((m) => m.userId === user.id))
    return delay({
      exportedAt: new Date().toISOString(),
      profile: user,
      settings: db.settings,
      calendarConnection: db.connection && { ...db.connection, accessToken: '[not exported]' },
      groups: groups.map((g) => ({
        id: g.id,
        title: g.title,
        role: g.organizerId === user.id ? 'ORGANIZER' : 'PARTICIPANT',
        window: g.window,
        event: g.event,
        myAvailability: db.availability[g.id]?.[user.id] ?? [],
      })),
    })
  },

  async deleteAccount() {
    me()
    db = seed()
    save()
    await delay(undefined, 600)
  },

  async listGroups() {
    const userId = me().id
    return delay(
      db.groups
        .filter((g) => g.members.some((m) => m.userId === userId))
        .map((g) => ({
          id: g.id,
          title: g.title,
          description: g.description,
          organizerName: g.organizerId === userId ? 'You' : db.users.find((u) => u.id === g.organizerId)!.displayName,
          status: g.status,
          window: g.window,
          availabilityDeadline: g.availabilityDeadline,
          memberCount: g.members.length,
          memberNames: [...g.members]
            .sort((a, b) => Number(b.role === 'ORGANIZER') - Number(a.role === 'ORGANIZER'))
            .map((m) => (m.userId === userId ? 'You' : db.users.find((u) => u.id === m.userId)!.displayName)),
          readyCount: g.members.filter((m) => (db.availability[g.id]?.[m.userId]?.length ?? 0) > 0).length,
          myRole: g.organizerId === userId ? ('ORGANIZER' as const) : ('PARTICIPANT' as const),
          myAvailabilityMarked: (db.availability[g.id]?.[userId]?.length ?? 0) > 0,
          myVotePending: g.status === 'VOTING' && g.options.some((o) => !o.votes[userId]),
          votingDeadline: g.votingDeadline,
          event: toEventDto(g.event),
        })),
    )
  },

  getGroup: async (groupId) => delay(toGroupDto(findGroup(groupId))),

  async createGroup(body) {
    const group: StoredGroup = {
      id: uid('g'),
      ...body,
      organizerId: me().id,
      inviteToken: uid('t'),
      status: 'PLANNING',
      members: [{ userId: me().id, role: 'ORGANIZER' }],
      invitations: [],
      event: null,
      options: [],
      votingDeadline: null,
    }
    db.groups.push(group)
    db.availability[group.id] = {}
    save()
    return delay(toGroupDto(group))
  },

  async joinGroup(inviteToken) {
    const group = db.groups.find((g) => g.inviteToken === inviteToken)
    if (!group) throw new ApiError(404, 'This invite link is invalid or has expired')
    if (group.status === 'CANCELLED') throw new ApiError(409, 'This group was cancelled')
    const user = me()
    let member = group.members.find((m) => m.userId === user.id)
    if (!member) {
      member = { userId: user.id, role: 'PARTICIPANT' }
      group.members.push(member)
      const invitation = group.invitations.find((i) => i.email === user.email)
      if (invitation) invitation.status = 'ACCEPTED'
      save()
    }
    return delay({ groupId: group.id, member: toMemberDto(group, user.id, member.role) })
  },

  async invite(groupId, emails) {
    const group = findGroup(groupId)
    requireOrganizer(group)
    for (const email of emails) {
      const existing = group.invitations.find((i) => i.email === email)
      if (existing) existing.status = 'SENT'
      else group.invitations.push({ id: uid('i'), email, status: 'SENT' })
    }
    save()
    await delay(undefined)
  },

  async revokeInvitation(invitationId) {
    const group = db.groups.find((g) => g.invitations.some((i) => i.id === invitationId))
    if (!group) throw new ApiError(404, 'Invitation not found')
    requireOrganizer(group)
    group.invitations.find((i) => i.id === invitationId)!.status = 'REVOKED'
    save()
    await delay(undefined)
  },

  async getAvailability(groupId) {
    const group = findGroup(groupId)
    const byUser = db.availability[group.id] ?? {}
    return delay(group.members.map((m) => ({ userId: m.userId, slots: byUser[m.userId] ?? [] })))
  },

  async updateAvailability(groupId, slots) {
    const group = findGroup(groupId)
    if (group.status !== 'PLANNING') throw new ApiError(409, 'Free time is closed for this group')
    if (group.availabilityDeadline && new Date(group.availabilityDeadline) < new Date()) {
      throw new ApiError(409, 'The availability deadline has passed')
    }
    db.availability[group.id] = { ...db.availability[group.id], [me().id]: slots }
    save()
    await delay(undefined)
  },

  async importFromCalendar(groupId) {
    const group = findGroup(groupId)
    if (!me().calendarConnected) throw new ApiError(409, 'Connect Google Calendar first')
    const slots: TimeRange[] = []
    const day = new Date(group.window.start)
    day.setHours(0, 0, 0, 0)
    while (day < new Date(group.window.end)) {
      const start = new Date(day)
      start.setHours(18)
      const end = new Date(day)
      end.setHours(23)
      slots.push({ start: start.toISOString(), end: end.toISOString() })
      day.setDate(day.getDate() + 1)
    }
    db.availability[group.id] = {
      ...db.availability[group.id],
      [me().id]: findCommonSlots([slots], group.window, 0),
    }
    save()
    await delay(undefined, 600)
  },

  async getOptimalSlots(groupId) {
    const group = findGroup(groupId)
    const byUser = db.availability[group.id] ?? {}
    const memberSlots = group.members.map((m) => byUser[m.userId] ?? [])
    return delay(findCommonSlots(memberSlots, group.window, group.eventDurationMinutes))
  },

  async getCalendar(range) {
    const user = me()
    const overlaps = (r: TimeRange) => r.start < range.end && r.end > range.start
    const items: CalendarItemDto[] = []

    for (const group of db.groups) {
      if (!group.members.some((m) => m.userId === user.id)) continue
      const organizer = db.users.find((u) => u.id === group.organizerId)!
      const people = [...group.members]
        .sort((a, b) => Number(b.role === 'ORGANIZER') - Number(a.role === 'ORGANIZER'))
        .map((m) => (m.userId === user.id ? 'You' : db.users.find((u) => u.id === m.userId)!.displayName))
      const base = {
        groupId: group.id,
        groupTitle: group.title,
        people,
        organizerName: organizer.displayName,
        groupStatus: group.status,
      }

      if (group.event && group.event.status === 'CONFIRMED') {
        items.push({ ...base, id: group.event.id, kind: 'EVENT', title: group.event.title, time: group.event.time, location: group.event.location })
      } else if (group.status === 'PLANNING' || group.status === 'VOTING') {
        items.push({ ...base, id: `${group.id}-window`, kind: 'PLANNING', title: group.title, time: group.window, location: null })
        const mine = db.availability[group.id]?.[user.id] ?? []
        mine.forEach((slot, i) =>
          items.push({ ...base, id: `${group.id}-free-${i}`, kind: 'MY_FREE_TIME', title: 'Free', time: slot, location: null }),
        )
      }
    }
    return delay(items.filter((i) => overlaps(i.time)))
  },

  async getSuggestions(groupId) {
    const group = findGroup(groupId)
    return delay(suggestTimes(memberSlots(group), group.window, group.eventDurationMinutes))
  },

  async startVoting(groupId, body) {
    const group = findGroup(groupId)
    requireOrganizer(group)
    if (group.status !== 'PLANNING') throw new ApiError(409, 'Voting can only start while planning')
    if (body.options.length < 1 || body.options.length > 6) throw new ApiError(400, 'Pick between 1 and 6 times')
    const slots = memberSlots(group)
    const user = me()
    group.options = body.options.map((time) => {
      const freeUserIds = slots
        .filter((m) => m.slots.some((s) => s.start <= time.start && s.end >= time.end))
        .map((m) => m.userId)
      return { id: uid('o'), time, freeUserIds, votes: {} }
    })
    const others = group.members.filter((m) => m.userId !== user.id).slice(0, -1)
    for (const o of group.options) {
      for (const m of others) o.votes[m.userId] = o.freeUserIds.includes(m.userId) ? 'YES' : 'NO'
    }
    group.status = 'VOTING'
    group.votingDeadline = body.deadline
    save()
    await delay(undefined)
  },

  async vote(groupId, optionId, answer) {
    const group = findGroup(groupId)
    if (group.status !== 'VOTING') throw new ApiError(409, 'Voting is closed')
    if (group.votingDeadline && new Date(group.votingDeadline) < new Date()) throw new ApiError(409, 'The voting deadline has passed')
    const option = group.options.find((o) => o.id === optionId)
    if (!option) throw new ApiError(404, 'Option not found')
    option.votes[me().id] = answer
    save()
    await delay(undefined, 150)
  },

  async cancelVoting(groupId) {
    const group = findGroup(groupId)
    requireOrganizer(group)
    if (group.status !== 'VOTING') throw new ApiError(409, 'There is no vote to cancel')
    group.status = 'PLANNING'
    group.options = []
    group.votingDeadline = null
    save()
    await delay(undefined)
  },

  async confirmEvent(groupId, body) {
    const group = findGroup(groupId)
    requireOrganizer(group)
    if (group.status !== 'PLANNING' && group.status !== 'VOTING') throw new ApiError(409, 'The event is already scheduled')
    group.event = { id: uid('e'), ...body, status: 'CONFIRMED' }
    group.status = 'SCHEDULED'
    group.options = []
    group.votingDeadline = null
    save()
    return delay(toEventDto(group.event)!)
  },

  async updateEvent(eventId, body) {
    const group = db.groups.find((g) => g.event?.id === eventId)
    if (!group || !group.event) throw new ApiError(404, 'Event not found')
    requireOrganizer(group)
    if (!body.title.trim()) throw new ApiError(400, 'Title cannot be empty')
    group.event = { ...group.event, ...body }
    save()
    return delay(toEventDto(group.event)!)
  },

  async getNotifications() {
    me()
    return delay([...db.notifications].sort((a, b) => b.createdAt.localeCompare(a.createdAt)), 150)
  },

  async markNotificationsRead(ids) {
    me()
    for (const n of db.notifications) if (!ids || ids.includes(n.id)) n.read = true
    save()
    await delay(undefined, 100)
  },

  async cancelEvent(eventId) {
    const group = db.groups.find((g) => g.event?.id === eventId)
    if (!group) throw new ApiError(404, 'Event not found')
    requireOrganizer(group)
    group.event = null
    group.status = 'PLANNING'
    group.options = []
    save()
    await delay(undefined)
  },
}
