import type { ReactNode } from 'react'
import { UserAvatar } from '../UserAvatar'

export function PersonRow({
  displayName,
  username,
  photoUrl,
  detail,
  children,
}: {
  displayName: string
  username: string
  photoUrl: string | null
  detail?: ReactNode
  children?: ReactNode
}) {
  return (
    <li className="person-row">
      <UserAvatar user={{ displayName, photoUrl }} size={36} />
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
