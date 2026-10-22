import { Check } from 'lucide-react'
import type { FriendDto } from '../../api'
import { UserAvatar } from '../UserAvatar'

export function FriendPicker({
  friends,
  selected,
  onChange,
  label,
}: {
  friends: Pick<FriendDto, 'userId' | 'displayName'>[]
  selected: string[]
  onChange: (selected: string[]) => void
  label: string
}) {
  return (
    <div className="friend-picker" role="group" aria-label={label}>
      {friends.map((f) => {
        const on = selected.includes(f.userId)
        return (
          <label key={f.userId} className={`pick-chip${on ? ' is-on' : ''}`}>
            <input
              type="checkbox"
              checked={on}
              onChange={() => onChange(on ? selected.filter((id) => id !== f.userId) : [...selected, f.userId])}
            />
            {on ? (
              <span className="pick-check" aria-hidden="true">
                <Check size={13} />
              </span>
            ) : (
              <UserAvatar user={{ displayName: f.displayName, photoUrl: null }} size={22} />
            )}
            {f.displayName}
          </label>
        )
      })}
    </div>
  )
}
