import type {
  CalendarConnectionDto,
  CalendarItemDto,
  ConfirmEventRequest,
  CreateGroupRequest,
  EventDto,
  GroupDto,
  GroupSummaryDto,
  JoinGroupResponse,
  MemberAvailabilityDto,
  NotificationDto,
  StartVotingRequest,
  SuggestedTimeDto,
  UpdateEventRequest,
  VoteAnswer,
  TimeRange,
  UpdateCalendarConnectionRequest,
  UpdateProfileRequest,
  UserDto,
  UserSettingsDto,
} from './types'

export interface Api {
  getMe(): Promise<UserDto | null>
  startLogin(returnTo: string): void
  logout(): Promise<void>
  logoutEverywhere(): Promise<void>

  updateProfile(body: UpdateProfileRequest): Promise<UserDto>
  getSettings(): Promise<UserSettingsDto>
  updateSettings(body: UserSettingsDto): Promise<UserSettingsDto>
  exportMyData(): Promise<unknown>
  deleteAccount(): Promise<void>

  startCalendarConnect(returnTo: string): void
  disconnectCalendar(): Promise<void>
  getCalendarConnections(): Promise<CalendarConnectionDto[]>
  updateCalendarConnection(id: string, body: UpdateCalendarConnectionRequest): Promise<CalendarConnectionDto>

  listGroups(): Promise<GroupSummaryDto[]>
  getGroup(groupId: string): Promise<GroupDto>
  createGroup(body: CreateGroupRequest): Promise<GroupDto>
  joinGroup(inviteToken: string): Promise<JoinGroupResponse>
  invite(groupId: string, emails: string[]): Promise<void>
  revokeInvitation(invitationId: string): Promise<void>

  getAvailability(groupId: string): Promise<MemberAvailabilityDto[]>
  updateAvailability(groupId: string, slots: TimeRange[]): Promise<void>
  importFromCalendar(groupId: string): Promise<void>
  getOptimalSlots(groupId: string): Promise<TimeRange[]>
  getSuggestions(groupId: string): Promise<SuggestedTimeDto[]>

  startVoting(groupId: string, body: StartVotingRequest): Promise<void>
  vote(groupId: string, optionId: string, answer: VoteAnswer): Promise<void>
  cancelVoting(groupId: string): Promise<void>

  getCalendar(range: TimeRange): Promise<CalendarItemDto[]>

  confirmEvent(groupId: string, body: ConfirmEventRequest): Promise<EventDto>
  cancelEvent(eventId: string): Promise<void>
  updateEvent(eventId: string, body: UpdateEventRequest): Promise<EventDto>

  getNotifications(): Promise<NotificationDto[]>
  markNotificationsRead(ids?: string[]): Promise<void>
}
