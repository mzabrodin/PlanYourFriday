import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '.'
import { applyPreferences, DEFAULT_SETTINGS } from '../lib/preferences'
import type {
  ConfirmEventRequest,
  CreateGroupRequest,
  GroupDto,
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

export const useGroup = (id: string) =>
  useQuery({ queryKey: keys.group(id), queryFn: () => api.getGroup(id) })

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
      ]),
  })
}

export function useJoinGroup() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (token: string) => api.joinGroup(token),
    onSuccess: () =>
      Promise.all([
        qc.invalidateQueries({ queryKey: keys.groups }),
        qc.invalidateQueries({ queryKey: ['calendar'] }),
      ]),
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
