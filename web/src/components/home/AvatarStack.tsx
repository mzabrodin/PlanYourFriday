export function AvatarStack({ names, max = 4 }: { names: string[]; max?: number }) {
  const shown = names.slice(0, max)
  const rest = names.length - shown.length
  return (
    <div className="avatar-stack" aria-label={names.join(', ')} title={names.join(', ')}>
      {shown.map((name, i) => (
        <span
          key={`${name}-${i}`}
          className={`avatar-chip${name === 'You' ? ' avatar-you' : ''}`}
          style={{ zIndex: max - i }}
        >
          {name.slice(0, 1).toUpperCase()}
        </span>
      ))}
      {rest > 0 && <span className="avatar-chip avatar-more">+{rest}</span>}
    </div>
  )
}
