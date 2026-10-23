import type {
  CalendarConnectionDto,
  CalendarItemDto,
  ConfirmEventRequest,
  CreateGroupRequest,
  CreatePollRequest,
  EventDto,
  FriendsOverviewDto,
  PersonDto,
  PollDto,
  PollSummaryDto,
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
  cancelGroup(groupId: string): Promise<void>
  deleteGroup(groupId: string): Promise<void>

  getCalendar(range: TimeRange): Promise<CalendarItemDto[]>

  confirmEvent(groupId: string, body: ConfirmEventRequest): Promise<EventDto>
  cancelEvent(eventId: string): Promise<void>
  updateEvent(eventId: string, body: UpdateEventRequest): Promise<EventDto>

  getNotifications(): Promise<NotificationDto[]>
  markNotificationsRead(ids?: string[]): Promise<void>

  getFriends(): Promise<FriendsOverviewDto>
  searchPeople(query: string): Promise<PersonDto[]>
  getPersonByCode(code: string): Promise<PersonDto>
  addFriendByCode(code: string): Promise<PersonDto>
  sendFriendRequest(userId: string): Promise<PersonDto>
  acceptFriendRequest(requestId: string): Promise<void>
  removeFriendRequest(requestId: string): Promise<void>
  removeFriend(userId: string): Promise<void>
  regenerateFriendCode(): Promise<{ friendCode: string }>
  addMembers(groupId: string, userIds: string[]): Promise<void>

  listPolls(): Promise<PollSummaryDto[]>
  getPoll(pollId: string): Promise<PollDto>
  createPoll(body: CreatePollRequest): Promise<PollDto>
  addPollOption(pollId: string, title: string): Promise<PollDto>
  votePoll(pollId: string, optionIds: string[]): Promise<PollDto>
  closePoll(pollId: string): Promise<PollDto>
  deletePoll(pollId: string): Promise<void>
}
