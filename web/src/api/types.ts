export interface TimeRange {
  start: string
  end: string
}

export type GroupStatus = 'PLANNING' | 'VOTING' | 'SCHEDULED' | 'CANCELLED'
export type MemberRole = 'ORGANIZER' | 'PARTICIPANT'
export type InvitationStatus = 'PENDING' | 'SENT' | 'ACCEPTED' | 'REVOKED'
export type EventStatus = 'CONFIRMED' | 'CANCELLED'

export interface UserDto {
  id: string
  email: string
  username: string
  displayName: string
  photoUrl: string | null
  timeZone: string
  calendarConnected: boolean
}

export interface UpdateProfileRequest {
  displayName: string
  username: string
  timeZone: string
}

export interface DailyHours {
  start: string
  end: string
}

export interface UserSettingsDto {
  theme: 'system' | 'light' | 'dark'
  timeFormat: '24h' | '12h'
  weekStartsOn: 0 | 1
  defaultEventDurationMinutes: number
  usualFreeTime: {
    weekdays: DailyHours | null
    weekends: DailyHours | null
  }
  notifications: {
    invitations: boolean
    deadlineReminders: boolean
    eventConfirmed: boolean
    eventCancelled: boolean
    eventReminder: boolean
    eventReminderHours: number
  }
}

export type ConnectionStatus = 'ACTIVE' | 'EXPIRED' | 'REVOKED'

export interface ExternalCalendarDto {
  id: string
  name: string
  color: string
  primary: boolean
}

export interface CalendarConnectionDto {
  id: string
  provider: 'GOOGLE' | 'MICROSOFT' | 'APPLE'
  accountEmail: string
  status: ConnectionStatus
  calendars: ExternalCalendarDto[]
  busyCalendarIds: string[]
  writeCalendarId: string
  lastSyncedAt: string | null
}

export interface UpdateCalendarConnectionRequest {
  busyCalendarIds: string[]
  writeCalendarId: string
}

export interface MemberDto {
  userId: string
  displayName: string
  email: string
  role: MemberRole
  hasAvailability: boolean
}

export interface JoinGroupResponse {
  groupId: string
  member: MemberDto
}

export interface InvitationDto {
  id: string
  email: string
  status: InvitationStatus
}

export type CalendarSyncStatus = 'SYNCED' | 'PENDING' | 'FAILED' | 'NOT_CONNECTED'

export interface EventDto {
  id: string
  title: string
  time: TimeRange
  location: string
  meetingLink: string | null
  description: string | null
  status: EventStatus
  myCalendarSync: CalendarSyncStatus
}

export interface UpdateEventRequest {
  title: string
  location: string
  meetingLink: string | null
  description: string | null
}

export type VoteAnswer = 'YES' | 'MAYBE' | 'NO'

export interface SuggestedTimeDto {
  time: TimeRange
  freeUserIds: string[]
}

export interface TimeOptionDto {
  id: string
  time: TimeRange
  freeUserIds: string[]
  votes: { userId: string; answer: VoteAnswer }[]
}

export interface StartVotingRequest {
  options: TimeRange[]
  deadline: string | null
}

export interface GroupSummaryDto {
  id: string
  title: string
  description: string | null
  organizerName: string
  status: GroupStatus
  window: TimeRange
  availabilityDeadline: string | null
  memberCount: number
  memberNames: string[]
  readyCount: number
  myRole: MemberRole
  myAvailabilityMarked: boolean
  myVotePending: boolean
  votingDeadline: string | null
  event: EventDto | null
}

export interface GroupDto {
  id: string
  title: string
  description: string | null
  organizerId: string
  inviteToken: string | null
  window: TimeRange
  eventDurationMinutes: number
  availabilityDeadline: string | null
  status: GroupStatus
  myRole: MemberRole
  members: MemberDto[]
  invitations: InvitationDto[]
  options: TimeOptionDto[]
  votingDeadline: string | null
  event: EventDto | null
}

export interface MemberAvailabilityDto {
  userId: string
  slots: TimeRange[]
}

export interface CreateGroupRequest {
  title: string
  description: string | null
  window: TimeRange
  eventDurationMinutes: number
  availabilityDeadline: string | null
  memberIds?: string[]
  pollId?: string | null
}

export interface ConfirmEventRequest {
  title: string
  time: TimeRange
  location: string
  meetingLink: string | null
  description: string | null
}

export type NotificationKind =
  | 'INVITED'
  | 'MEMBER_JOINED'
  | 'VOTING_STARTED'
  | 'DEADLINE_SOON'
  | 'EVENT_CONFIRMED'
  | 'EVENT_UPDATED'
  | 'EVENT_CANCELLED'
  | 'FRIEND_REQUEST'
  | 'FRIEND_ACCEPTED'
  | 'POLL_STARTED'
  | 'POLL_CLOSED'

export interface NotificationDto {
  id: string
  kind: NotificationKind
  groupId: string | null
  groupTitle: string | null
  pollId: string | null
  pollTitle: string | null
  actorName: string | null
  createdAt: string
  read: boolean
}

export type CalendarItemKind = 'EVENT' | 'PLANNING' | 'MY_FREE_TIME'

export interface CalendarItemDto {
  id: string
  kind: CalendarItemKind
  groupId: string
  groupTitle: string
  title: string
  time: TimeRange
  location: string | null
  people: string[]
  organizerName: string
  groupStatus: GroupStatus
}

export type Relation = 'SELF' | 'NONE' | 'FRIEND' | 'REQUEST_SENT' | 'REQUEST_RECEIVED'

export interface PersonDto {
  userId: string
  displayName: string
  username: string
  relation: Relation
}

export interface FriendDto {
  userId: string
  displayName: string
  username: string
  email: string
  since: string
}

export interface FriendRequestDto {
  id: string
  person: PersonDto
  direction: 'INCOMING' | 'OUTGOING'
  createdAt: string
}

export interface FriendSuggestionDto {
  person: PersonDto
  sharedGroups: string[]
}

export interface FriendsOverviewDto {
  friends: FriendDto[]
  incoming: FriendRequestDto[]
  outgoing: FriendRequestDto[]
  suggestions: FriendSuggestionDto[]
  friendCode: string
}

export type PollStatus = 'OPEN' | 'CLOSED'

export interface PollPersonDto {
  userId: string
  displayName: string
}

export interface PollOptionDto {
  id: string
  title: string
  addedById: string
  voterIds: string[]
}

export interface PollDto {
  id: string
  title: string
  description: string | null
  creatorId: string
  creatorName: string
  status: PollStatus
  deadline: string | null
  multipleChoice: boolean
  allowNewOptions: boolean
  participants: PollPersonDto[]
  options: PollOptionDto[]
  winnerOptionIds: string[]
  groupId: string | null
  createdAt: string
}

export interface PollSummaryDto {
  id: string
  title: string
  creatorName: string
  status: PollStatus
  deadline: string | null
  participantCount: number
  votedCount: number
  optionCount: number
  myVotePending: boolean
  leadingOptions: string[]
  groupId: string | null
  createdAt: string
}

export interface CreatePollRequest {
  title: string
  description: string | null
  participantIds: string[]
  options: string[]
  deadline: string | null
  multipleChoice: boolean
  allowNewOptions: boolean
}
