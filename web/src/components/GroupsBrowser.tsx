import { useState } from 'react'
import { useSearchParams } from 'react-router'
import type { GroupSummaryDto } from '../api'
import {
  applyGroupFilters,
  filterBase,
  GROUP_VIEWS,
  matchesView,
  type GroupFilters,
  type GroupSort,
  type GroupView,
  type RoleFilter,
} from '../lib/groupFilters'
import { GroupCard } from './home/GroupCard'

const DEFAULTS: GroupFilters = { view: 'upcoming', role: 'any', sort: 'soonest', query: '' }

function useGroupFilters() {
  const [params, setParams] = useSearchParams()
  const pick = <T extends string>(key: string, allowed: readonly T[], fallback: T): T => {
    const value = params.get(key) as T | null
    return value && allowed.includes(value) ? value : fallback
  }
  const filters: GroupFilters = {
    view: pick<GroupView>(
      'view',
      GROUP_VIEWS.map((v) => v.value),
      DEFAULTS.view,
    ),
    role: pick<RoleFilter>('role', ['any', 'organizer', 'participant'], DEFAULTS.role),
    sort: pick<GroupSort>('sort', ['soonest', 'latest', 'title'], DEFAULTS.sort),
    query: params.get('q') ?? '',
  }

  function update(patch: Partial<GroupFilters>) {
    setParams(
      (prev) => {
        const out = new URLSearchParams(prev)
        const keys = { view: 'view', role: 'role', sort: 'sort', query: 'q' } as const
        for (const [field, value] of Object.entries(patch) as [keyof GroupFilters, string][]) {
          if (value && value !== DEFAULTS[field]) out.set(keys[field], value)
          else out.delete(keys[field])
        }
        return out
      },
      { replace: true },
    )
  }

  const isDefault =
    filters.view === DEFAULTS.view && filters.role === DEFAULTS.role && filters.sort === DEFAULTS.sort && !filters.query

  return { filters, update, reset: () => setParams({}, { replace: true }), isDefault }
}

export function GroupsBrowser({ groups, now }: { groups: GroupSummaryDto[]; now: number }) {
  const { filters, update, reset, isDefault } = useGroupFilters()
  const [search, setSearch] = useState(filters.query)

  const base = filterBase(groups, filters)
  const visible = applyGroupFilters(groups, filters, now)

  return (
    <>
      <div className="toolbar">
        <input
          type="search"
          className="search"
          placeholder="Search by name, organizer or place…"
          aria-label="Search events"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            update({ query: e.target.value })
          }}
        />
        <select aria-label="Role" value={filters.role} onChange={(e) => update({ role: e.target.value as RoleFilter })}>
          <option value="any">Any role</option>
          <option value="organizer">I organize</option>
          <option value="participant">I'm invited</option>
        </select>
        <select aria-label="Sort" value={filters.sort} onChange={(e) => update({ sort: e.target.value as GroupSort })}>
          <option value="soonest">Soonest first</option>
          <option value="latest">Latest first</option>
          <option value="title">A–Z</option>
        </select>
      </div>

      <div className="view-tabs" role="tablist" aria-label="Show events">
        {GROUP_VIEWS.map(({ value, label }) => {
          const count = base.filter((g) => matchesView(g, value, now)).length
          return (
            <button
              key={value}
              type="button"
              role="tab"
              aria-selected={filters.view === value}
              // On phones the tab row scrolls; keep the selected tab in view.
              ref={
                filters.view === value ? (el) => el?.scrollIntoView({ block: 'nearest', inline: 'nearest' }) : undefined
              }
              className={`view-tab${value === 'needs-input' && count > 0 ? ' view-tab-alert' : ''}`}
              onClick={() => update({ view: value })}
            >
              {label} <span className="count">{count}</span>
            </button>
          )
        })}
      </div>

      {visible.length === 0 ? (
        <div className="empty">
          <p>No events match these filters.</p>
          {!isDefault && (
            <button
              type="button"
              className="btn"
              onClick={() => {
                setSearch('')
                reset()
              }}
            >
              Clear filters
            </button>
          )}
        </div>
      ) : (
        <ul className="plan-grid">
          {visible.map((g) => (
            <li key={g.id}>
              <GroupCard group={g} now={now} />
            </li>
          ))}
        </ul>
      )}
    </>
  )
}
