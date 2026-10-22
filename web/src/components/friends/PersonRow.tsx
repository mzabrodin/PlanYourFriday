import type { ReactNode } from 'react'
import { UserAvatar } from '../UserAvatar'

export function PersonRow({
  displayName,
  username,
  detail,
  children,
}: {
  displayName: string
  username: string
  detail?: ReactNode
  children?: ReactNode
}) {
  return (
    <li className="person-row">
      <UserAvatar user={{ displayName, photoUrl: null }} size={36} />
      <span className="person-text">
        <span className="person-name">{displayName}</span>
        <span className="muted small">
          @{username}
          {detail && <> · {detail}</>}
        </span>
      </span>
      {children && <span className="person-actions">{children}</span>}
    </li>
  )
}
