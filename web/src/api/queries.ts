import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '.'
import { applyPreferences, DEFAULT_SETTINGS } from '../lib/preferences'
import type {
  ConfirmEventRequest,
  CreateGroupRequest,
  CreatePollRequest,
  FriendsOverviewDto,
  GroupDto,
  PollDto,
  NotificationDto,
  StartVotingRequest,
  TimeRange,
  UpdateEventRequest,
  VoteAnswer,
  UpdateCalendarConnectionRequest,
  UpdateProfileRequest,
  UserSettingsDto,
} from './types'

export const keys = {
  me: ['me'] as const,
  settings: ['me', 'settings'] as const,
  connections: ['calendar-connections'] as const,
  groups: ['groups'] as const,
  group: (id: string) => ['groups', id] as const,
  availability: (id: string) => ['groups', id, 'availability'] as const,
  optimalSlots: (id: string) => ['groups', id, 'optimal-slots'] as const,
  calendar: (range: TimeRange) => ['calendar', range.start, range.end] as const,
  suggestions: (id: string) => ['groups', id, 'suggestions'] as const,
  notifications: ['notifications'] as const,
  friends: ['friends'] as const,
  people: (query: string) => ['people', query] as const,
  personByCode: (code: string) => ['people', 'code', code] as const,
  polls: ['polls'] as const,
  poll: (id: string) => ['polls', id] as const,
}

export const useMe = () => useQuery({ queryKey: keys.me, queryFn: api.getMe, staleTime: 60_000 })

export const useSettings = (enabled = true) =>
  useQuery({
    queryKey: keys.settings,
    queryFn: async () => {
      const settings = await api.getSettings()
      applyPreferences(settings)
      return settings
    },
    enabled,
    staleTime: Infinity,
  })

export function usePrefs(): UserSettingsDto {
  return useSettings().data ?? DEFAULT_SETTINGS
}

export const useCalendarConnections = () =>
  useQuery({ queryKey: keys.connections, queryFn: api.getCalendarConnections })

export function useUpdateProfile() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateProfileRequest) => api.updateProfile(body),
    onSuccess: (user) => {
      qc.setQueryData(keys.me, user)
      return Promise.all([
        qc.invalidateQueries({ queryKey: keys.groups }),
        qc.invalidateQueries({ queryKey: ['calendar'] }),
      ])
    },
  })
}

export function useUpdateSettings() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UserSettingsDto) => api.updateSettings(body),
    onSuccess: (settings) => {
      applyPreferences(settings)
      qc.setQueryData(keys.settings, settings)
    },
  })
}

export function useUpdateCalendarConnection() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateCalendarConnectionRequest }) =>
      api.updateCalendarConnection(id, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.connections }),
  })
}

function useSignedOut() {
  const qc = useQueryClient()
  return () => {
    qc.setQueryData(keys.me, null)
    qc.removeQueries({ predicate: (q) => !(q.queryKey.length === 1 && q.queryKey[0] === 'me') })
  }
}

export function useLogoutEverywhere() {
  const signedOut = useSignedOut()
  return useMutation({ mutationFn: api.logoutEverywhere, onSuccess: signedOut })
}

export function useDeleteAccount() {
  const signedOut = useSignedOut()
  return useMutation({ mutationFn: api.deleteAccount, onSuccess: signedOut })
}

export const useGroups = () => useQuery({ queryKey: keys.groups, queryFn: api.listGroups })

export const useGroup = (id: string) => useQuery({ queryKey: keys.group(id), queryFn: () => api.getGroup(id) })

export const useAvailability = (id: string) =>
  useQuery({ queryKey: keys.availability(id), queryFn: () => api.getAvailability(id) })

export const useOptimalSlots = (id: string) =>
  useQuery({ queryKey: keys.optimalSlots(id), queryFn: () => api.getOptimalSlots(id) })

export const useSuggestions = (id: string, enabled = true) =>
  useQuery({ queryKey: keys.suggestions(id), queryFn: () => api.getSuggestions(id), enabled })

export const useNotifications = () =>
  useQuery({
    queryKey: keys.notifications,
    queryFn: api.getNotifications,
    refetchInterval: 60_000,
  })

export function useMarkNotificationsRead() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (ids?: string[]) => api.markNotificationsRead(ids),
    onMutate: (ids) => {
      qc.setQueryData<NotificationDto[]>(keys.notifications, (list) =>
        list?.map((n) => (!ids || ids.includes(n.id) ? { ...n, read: true } : n)),
      )
    },
    onSettled: () => qc.invalidateQueries({ queryKey: keys.notifications }),
  })
}

export const useCalendar = (range: TimeRange | null) =>
  useQuery({
    queryKey: keys.calendar(range ?? { start: '', end: '' }),
    queryFn: () => api.getCalendar(range!),
    enabled: range !== null,
    placeholderData: (previous) => previous,
  })

function useInvalidateGroup() {
  const qc = useQueryClient()
  return (groupId: string) =>
    Promise.all([
      qc.invalidateQueries({ queryKey: keys.group(groupId) }),
      qc.invalidateQueries({ queryKey: keys.groups, exact: true }),
      qc.invalidateQueries({ queryKey: ['calendar'] }),
    ])
}

export function useLogout() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.logout,
    onSuccess: () => {
      qc.setQueryData(keys.me, null)
      qc.removeQueries({ queryKey: keys.groups })
    },
  })
}

export function useDisconnectCalendar() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.disconnectCalendar,
    onSuccess: () =>
      Promise.all([
        qc.invalidateQueries({ queryKey: keys.me, exact: true }),
        qc.invalidateQueries({ queryKey: keys.connections }),
      ]),
  })
}

export function useCreateGroup() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateGroupRequest) => api.createGroup(body),
    onSuccess: () =>
      Promise.all([
        qc.invalidateQueries({ queryKey: keys.groups, exact: true }),
        qc.invalidateQueries({ queryKey: ['calendar'] }),
        qc.invalidateQueries({ queryKey: keys.polls }),
      ]),
  })
}

export function useJoinGroup() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (token: string) => api.joinGroup(token),
    onSuccess: () =>
      Promise.all([qc.invalidateQueries({ queryKey: keys.groups }), qc.invalidateQueries({ queryKey: ['calendar'] })]),
  })
}

export function useInvite(groupId: string) {
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: (emails: string[]) => api.invite(groupId, emails),
    onSuccess: () => invalidate(groupId),
  })
}

export function useRevokeInvitation(groupId: string) {
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: (invitationId: string) => api.revokeInvitation(invitationId),
    onSuccess: () => invalidate(groupId),
  })
}

export function useUpdateAvailability(groupId: string) {
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: (slots: TimeRange[]) => api.updateAvailability(groupId, slots),
    onSuccess: () => invalidate(groupId),
  })
}

export function useImportFromCalendar(groupId: string) {
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: () => api.importFromCalendar(groupId),
    onSuccess: () => invalidate(groupId),
  })
}

export function useConfirmEvent(groupId: string) {
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: (body: ConfirmEventRequest) => api.confirmEvent(groupId, body),
    onSuccess: () => invalidate(groupId),
  })
}

export function useCancelEvent(groupId: string) {
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: (eventId: string) => api.cancelEvent(eventId),
    onSuccess: () => invalidate(groupId),
  })
}

export function useCancelGroup(groupId: string) {
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: () => api.cancelGroup(groupId),
    onSuccess: () => invalidate(groupId),
  })
}

export function useDeleteGroup(groupId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.deleteGroup(groupId),
    onSuccess: () => {
      qc.removeQueries({ queryKey: keys.group(groupId) })
      return Promise.all([
        qc.invalidateQueries({ queryKey: keys.groups, exact: true }),
        qc.invalidateQueries({ queryKey: ['calendar'] }),
        qc.invalidateQueries({ queryKey: keys.notifications }),
      ])
    },
  })
}

export function useStartVoting(groupId: string) {
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: (body: StartVotingRequest) => api.startVoting(groupId, body),
    onSuccess: () => invalidate(groupId),
  })
}

export function useCancelVoting(groupId: string) {
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: () => api.cancelVoting(groupId),
    onSuccess: () => invalidate(groupId),
  })
}

export function useVote(groupId: string, userId: string | undefined) {
  const qc = useQueryClient()
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: ({ optionId, answer }: { optionId: string; answer: VoteAnswer }) => api.vote(groupId, optionId, answer),
    onMutate: async ({ optionId, answer }) => {
      await qc.cancelQueries({ queryKey: keys.group(groupId), exact: true })
      const previous = qc.getQueryData<GroupDto>(keys.group(groupId))
      if (previous && userId) {
        qc.setQueryData<GroupDto>(keys.group(groupId), {
          ...previous,
          options: previous.options.map((o) =>
            o.id !== optionId
              ? o
              : { ...o, votes: [...o.votes.filter((v) => v.userId !== userId), { userId, answer }] },
          ),
        })
      }
      return { previous }
    },
    onError: (_e, _v, context) => {
      if (context?.previous) qc.setQueryData(keys.group(groupId), context.previous)
    },
    onSettled: () => invalidate(groupId),
  })
}

export function useUpdateEvent(groupId: string) {
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: ({ eventId, body }: { eventId: string; body: UpdateEventRequest }) => api.updateEvent(eventId, body),
    onSuccess: () => invalidate(groupId),
  })
}

export const useFriends = (enabled = true) => useQuery({ queryKey: keys.friends, queryFn: api.getFriends, enabled })

export const usePeopleSearch = (query: string) =>
  useQuery({
    queryKey: keys.people(query),
    queryFn: () => api.searchPeople(query),
    enabled: query.trim().replace(/^@/, '').length >= 2,
    placeholderData: (previous) => previous,
  })

export const usePersonByCode = (code: string) =>
  useQuery({ queryKey: keys.personByCode(code), queryFn: () => api.getPersonByCode(code) })

function useInvalidateFriends() {
  const qc = useQueryClient()
  return () =>
    Promise.all([
      qc.invalidateQueries({ queryKey: keys.friends }),
      qc.invalidateQueries({ queryKey: ['people'] }),
      qc.invalidateQueries({ queryKey: keys.notifications }),
    ])
}

export function useAddFriendByCode() {
  const invalidate = useInvalidateFriends()
  return useMutation({ mutationFn: (code: string) => api.addFriendByCode(code), onSuccess: invalidate })
}

export function useSendFriendRequest() {
  const invalidate = useInvalidateFriends()
  return useMutation({ mutationFn: (userId: string) => api.sendFriendRequest(userId), onSuccess: invalidate })
}

export function useAcceptFriendRequest() {
  const invalidate = useInvalidateFriends()
  return useMutation({ mutationFn: (requestId: string) => api.acceptFriendRequest(requestId), onSuccess: invalidate })
}

export function useRemoveFriendRequest() {
  const invalidate = useInvalidateFriends()
  return useMutation({ mutationFn: (requestId: string) => api.removeFriendRequest(requestId), onSuccess: invalidate })
}

export function useRemoveFriend() {
  const invalidate = useInvalidateFriends()
  return useMutation({ mutationFn: (userId: string) => api.removeFriend(userId), onSuccess: invalidate })
}

export function useRegenerateFriendCode() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.regenerateFriendCode,
    onSuccess: ({ friendCode }) =>
      qc.setQueryData<FriendsOverviewDto>(keys.friends, (data) => data && { ...data, friendCode }),
  })
}

export function useAddMembers(groupId: string) {
  const invalidate = useInvalidateGroup()
  return useMutation({
    mutationFn: (userIds: string[]) => api.addMembers(groupId, userIds),
    onSuccess: () => invalidate(groupId),
  })
}

export const usePolls = (enabled = true) => useQuery({ queryKey: keys.polls, queryFn: api.listPolls, enabled })

export const usePoll = (id: string | null) =>
  useQuery({ queryKey: keys.poll(id ?? ''), queryFn: () => api.getPoll(id!), enabled: !!id })

function usePollUpdated() {
  const qc = useQueryClient()
  return (poll: PollDto) => {
    qc.setQueryData(keys.poll(poll.id), poll)
    return qc.invalidateQueries({ queryKey: keys.polls, exact: true })
  }
}

export function useCreatePoll() {
  const updated = usePollUpdated()
  return useMutation({ mutationFn: (body: CreatePollRequest) => api.createPoll(body), onSuccess: updated })
}

export function useAddPollOption(pollId: string) {
  const updated = usePollUpdated()
  return useMutation({ mutationFn: (title: string) => api.addPollOption(pollId, title), onSuccess: updated })
}

export function useVotePoll(pollId: string, userId: string | undefined) {
  const qc = useQueryClient()
  const updated = usePollUpdated()
  return useMutation({
    mutationFn: (optionIds: string[]) => api.votePoll(pollId, optionIds),
    onMutate: async (optionIds) => {
      await qc.cancelQueries({ queryKey: keys.poll(pollId) })
      const previous = qc.getQueryData<PollDto>(keys.poll(pollId))
      if (previous && userId) {
        qc.setQueryData<PollDto>(keys.poll(pollId), {
          ...previous,
          options: previous.options.map((o) => ({
            ...o,
            voterIds: [...o.voterIds.filter((id) => id !== userId), ...(optionIds.includes(o.id) ? [userId] : [])],
          })),
        })
      }
      return { previous }
    },
    onError: (_e, _v, context) => {
      if (context?.previous) qc.setQueryData(keys.poll(pollId), context.previous)
    },
    onSuccess: updated,
  })
}

export function useClosePoll(pollId: string) {
  const updated = usePollUpdated()
  return useMutation({ mutationFn: () => api.closePoll(pollId), onSuccess: updated })
}

export function useDeletePoll(pollId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.deletePoll(pollId),
    onSuccess: () => {
      qc.removeQueries({ queryKey: keys.poll(pollId) })
      return qc.invalidateQueries({ queryKey: keys.polls, exact: true })
    },
  })
}
