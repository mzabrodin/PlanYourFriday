import type { UserDto } from '../api'

export function UserAvatar({ user, size = 32 }: { user: Pick<UserDto, 'displayName' | 'photoUrl'>; size?: number }) {
  const style = { width: size, height: size, fontSize: size * 0.42 }
  if (user.photoUrl) {
    return <img className="user-avatar" src={user.photoUrl} alt="" style={style} referrerPolicy="no-referrer" />
  }
  return (
    <span className="user-avatar" style={style} aria-hidden="true">
      {user.displayName.trim().slice(0, 1).toUpperCase() || '?'}
    </span>
  )
}
