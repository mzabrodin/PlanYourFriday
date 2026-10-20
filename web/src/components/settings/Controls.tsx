import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'

export function Switch({
  checked,
  onChange,
  label,
  disabled,
}: {
  checked: boolean
  onChange: (checked: boolean) => void
  label: string
  disabled?: boolean
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      className="switch"
      disabled={disabled}
      onClick={() => onChange(!checked)}
    >
      <span className="switch-thumb" />
    </button>
  )
}

export function Segmented<T extends string | number>({
  value,
  options,
  onChange,
  label,
}: {
  value: T
  options: { value: T; label: ReactNode }[]
  onChange: (value: T) => void
  label: string
}) {
  return (
    <div className="segmented" role="radiogroup" aria-label={label}>
      {options.map((o) => (
        <button
          key={String(o.value)}
          type="button"
          role="radio"
          aria-checked={value === o.value}
          onClick={() => onChange(o.value)}
        >
          {o.label}
        </button>
      ))}
    </div>
  )
}

export function SettingRow({
  title,
  description,
  children,
  htmlFor,
}: {
  title: ReactNode
  description?: ReactNode
  children: ReactNode
  htmlFor?: string
}) {
  return (
    <div className="setting-row">
      <div className="setting-text">
        {htmlFor ? (
          <label htmlFor={htmlFor} className="setting-title">
            {title}
          </label>
        ) : (
          <span className="setting-title">{title}</span>
        )}
        {description && <p className="setting-desc">{description}</p>}
      </div>
      <div className="setting-control">{children}</div>
    </div>
  )
}

export function SettingsSection({
  id,
  icon: Icon,
  title,
  description,
  children,
}: {
  id: string
  icon: LucideIcon
  title: string
  description?: string
  children: ReactNode
}) {
  return (
    <section id={id} className="card settings-section" aria-labelledby={`${id}-title`}>
      <header className="settings-section-header">
        <span className="settings-icon" aria-hidden="true">
          <Icon size={20} />
        </span>
        <div>
          <h2 id={`${id}-title`}>{title}</h2>
          {description && <p className="muted small">{description}</p>}
        </div>
      </header>
      {children}
    </section>
  )
}
