import type { UserSettingsDto } from '../api/types'
import { setHour12 } from './time'

export const DEFAULT_SETTINGS: UserSettingsDto = {
  theme: 'system',
  timeFormat: '24h',
  weekStartsOn: 1,
  defaultEventDurationMinutes: 120,
  usualFreeTime: {
    weekdays: { start: '18:00', end: '23:00' },
    weekends: { start: '12:00', end: '23:00' },
  },
  notifications: {
    invitations: true,
    deadlineReminders: true,
    eventConfirmed: true,
    eventCancelled: true,
    eventReminder: true,
    eventReminderHours: 24,
  },
}

export const THEME_STORAGE_KEY = 'pyf-theme'

export function applyTheme(theme: UserSettingsDto['theme']) {
  const root = document.documentElement
  if (theme === 'system') delete root.dataset.theme
  else root.dataset.theme = theme
  try {
    localStorage.setItem(THEME_STORAGE_KEY, theme)
  } catch {}
}

export function applyPreferences(settings: UserSettingsDto) {
  applyTheme(settings.theme)
  setHour12(settings.timeFormat === '12h')
}
