import type { Api } from './api'
import { ApiError, request } from './client'
import type { UserDto } from './types'

const json = (body: unknown) => JSON.stringify(body)
const returnParam = (returnTo: string) => `returnUrl=${encodeURIComponent(returnTo)}`

export const httpApi: Api = {
  async getMe() {
    try {
      return await request<UserDto>('/auth/me')
    } catch (e) {
      if (e instanceof ApiError && e.status === 401) return null
      throw e
    }
  },
  startLogin: (returnTo) =>
    window.location.assign(
      `/api/auth/login/google?${returnParam(returnTo)}&tz=${encodeURIComponent(Intl.DateTimeFormat().resolvedOptions().timeZone)}`,
    ),
  logout: () => request('/auth/logout', { method: 'POST' }),
  logoutEverywhere: () => request('/auth/logout-all', { method: 'POST' }),

  updateProfile: (body) => request('/me', { method: 'PATCH', body: json(body) }),
  getSettings: () => request('/me/settings'),
  updateSettings: (body) => request('/me/settings', { method: 'PUT', body: json(body) }),
  exportMyData: () => request('/me/export'),
  deleteAccount: () => request('/me', { method: 'DELETE' }),

  startCalendarConnect: (returnTo) =>
    window.location.assign(`/api/calendar-connections/google/connect?${returnParam(returnTo)}`),
  disconnectCalendar: () => request('/calendar-connections/primary', { method: 'DELETE' }),
  getCalendarConnections: () => request('/calendar-connections'),
  updateCalendarConnection: (id, body) => request(`/calendar-connections/${id}`, { method: 'PATCH', body: json(body) }),

  listGroups: () => request('/groups'),
  getGroup: (groupId) => request(`/groups/${groupId}`),
  createGroup: (body) => request('/groups', { method: 'POST', body: json(body) }),
  joinGroup: (inviteToken) => request(`/groups/join/${encodeURIComponent(inviteToken)}`, { method: 'POST' }),
  invite: (groupId, emails) => request(`/groups/${groupId}/invitations`, { method: 'POST', body: json({ emails }) }),
  revokeInvitation: (invitationId) => request(`/invitations/${invitationId}`, { method: 'DELETE' }),

  getAvailability: (groupId) => request(`/groups/${groupId}/availability`),
  updateAvailability: (groupId, slots) =>
    request(`/groups/${groupId}/availability`, { method: 'PUT', body: json({ slots }) }),
  importFromCalendar: (groupId) => request(`/groups/${groupId}/availability/import`, { method: 'POST' }),
  getOptimalSlots: (groupId) => request(`/groups/${groupId}/optimal-slots`),
  getSuggestions: (groupId) => request(`/groups/${groupId}/suggestions`),

  startVoting: (groupId, body) => request(`/groups/${groupId}/voting`, { method: 'POST', body: json(body) }),
  vote: (groupId, optionId, answer) =>
    request(`/groups/${groupId}/options/${optionId}/vote`, { method: 'PUT', body: json({ answer }) }),
  cancelVoting: (groupId) => request(`/groups/${groupId}/voting`, { method: 'DELETE' }),
  cancelGroup: (groupId) => request(`/groups/${groupId}/cancel`, { method: 'POST' }),
  deleteGroup: (groupId) => request(`/groups/${groupId}`, { method: 'DELETE' }),

  getCalendar: ({ start, end }) => request(`/calendar?from=${encodeURIComponent(start)}&to=${encodeURIComponent(end)}`),

  confirmEvent: (groupId, body) => request(`/groups/${groupId}/event`, { method: 'POST', body: json(body) }),
  cancelEvent: (eventId) => request(`/events/${eventId}`, { method: 'DELETE' }),
  updateEvent: (eventId, body) => request(`/events/${eventId}`, { method: 'PATCH', body: json(body) }),

  getNotifications: () => request('/notifications'),
  markNotificationsRead: (ids) => request('/notifications/read', { method: 'POST', body: json({ ids: ids ?? null }) }),

  getFriends: () => request('/friends'),
  searchPeople: (query) => request(`/people?q=${encodeURIComponent(query)}`),
  getPersonByCode: (code) => request(`/people/by-code/${encodeURIComponent(code)}`),
  addFriendByCode: (code) => request(`/friends/by-code/${encodeURIComponent(code)}`, { method: 'POST' }),
  sendFriendRequest: (userId) => request('/friends/requests', { method: 'POST', body: json({ userId }) }),
  acceptFriendRequest: (requestId) => request(`/friends/requests/${requestId}/accept`, { method: 'POST' }),
  removeFriendRequest: (requestId) => request(`/friends/requests/${requestId}`, { method: 'DELETE' }),
  removeFriend: (userId) => request(`/friends/${userId}`, { method: 'DELETE' }),
  regenerateFriendCode: () => request('/me/friend-code', { method: 'POST' }),
  addMembers: (groupId, userIds) => request(`/groups/${groupId}/members`, { method: 'POST', body: json({ userIds }) }),

  listPolls: () => request('/polls'),
  getPoll: (pollId) => request(`/polls/${pollId}`),
  createPoll: (body) => request('/polls', { method: 'POST', body: json(body) }),
  addPollOption: (pollId, title) => request(`/polls/${pollId}/options`, { method: 'POST', body: json({ title }) }),
  votePoll: (pollId, optionIds) => request(`/polls/${pollId}/votes`, { method: 'PUT', body: json({ optionIds }) }),
  closePoll: (pollId) => request(`/polls/${pollId}/close`, { method: 'POST' }),
  deletePoll: (pollId) => request(`/polls/${pollId}`, { method: 'DELETE' }),
}
