import { ArrowRight, Plus, RotateCcw, Shuffle, X } from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { WHEEL_COLORS } from '../lib/colors'

const STORAGE_KEY = 'pyf-wheel-ideas'
const DEFAULT_IDEAS = [
  'Board games',
  'Karaoke',
  'Movie night',
  'Picnic',
  'Bowling',
  'Escape room',
  'Cook dinner together',
  'Pub quiz',
  'Ice skating',
  'Sunset walk',
]
const MIN_IDEAS = 2
const MAX_IDEAS = 16
const SPIN_MS = 4200

function loadIdeas(): string[] {
  try {
    const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? 'null')
    if (Array.isArray(saved) && saved.length >= MIN_IDEAS && saved.every((s) => typeof s === 'string')) return saved
  } catch {}
  return DEFAULT_IDEAS
}

function saveIdeas(ideas: string[]) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(ideas))
  } catch {}
}

const prefersReducedMotion = () => window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false

export function WheelPage() {
  const [ideas, setIdeas] = useState(loadIdeas)
  const [rotation, setRotation] = useState(0)
  const [spinning, setSpinning] = useState(false)
  const [result, setResult] = useState<string | null>(null)
  const [draft, setDraft] = useState('')

  useEffect(() => saveIdeas(ideas), [ideas])

  const slice = 360 / ideas.length

  function spin() {
    if (spinning) return
    const winner = Math.floor(Math.random() * ideas.length)
    const jitter = (Math.random() - 0.5) * slice * 0.6
    const target = 360 - (winner * slice + slice / 2) + jitter
    const turns = prefersReducedMotion() ? 0 : 6
    const next = rotation - (rotation % 360) + turns * 360 + target
    setResult(null)
    setSpinning(true)
    setRotation(next)
    window.setTimeout(
      () => {
        setSpinning(false)
        setResult(ideas[winner])
      },
      prefersReducedMotion() ? 0 : SPIN_MS,
    )
  }

  function addIdea(e: FormEvent) {
    e.preventDefault()
    const idea = draft.trim()
    if (!idea || ideas.length >= MAX_IDEAS || ideas.some((i) => i.toLowerCase() === idea.toLowerCase())) return
    setIdeas([...ideas, idea])
    setDraft('')
    setResult(null)
  }

  return (
    <div className="wheel-page">
      <header className="wheel-intro">
        <h1>No idea what to do?</h1>
        <p className="lead">Spin the Friday wheel and let fate pick the plan.</p>
      </header>

      <div className="wheel-layout">
        <div className="wheel-stage">
          <div className="wheel-pointer" aria-hidden="true" />
          <svg
            className="wheel"
            viewBox="-110 -110 220 220"
            role="img"
            aria-label={`Wheel with ${ideas.length} ideas`}
            style={{ transform: `rotate(${rotation}deg)`, transitionDuration: spinning ? `${SPIN_MS}ms` : '0ms' }}
          >
            {ideas.map((idea, i) => {
              const a0 = ((i * slice - 90) * Math.PI) / 180
              const a1 = (((i + 1) * slice - 90) * Math.PI) / 180
              const large = slice > 180 ? 1 : 0
              const mid = i * slice + slice / 2
              const label = idea.length > 16 ? `${idea.slice(0, 15)}…` : idea
              return (
                <g key={idea}>
                  <path
                    d={`M0 0 L${100 * Math.cos(a0)} ${100 * Math.sin(a0)} A100 100 0 ${large} 1 ${100 * Math.cos(a1)} ${100 * Math.sin(a1)} Z`}
                    fill={WHEEL_COLORS[i % WHEEL_COLORS.length]}
                    stroke="var(--surface)"
                    strokeWidth="1.5"
                  />
                  <text
                    transform={mid > 180 ? `rotate(${mid + 90}) translate(-92 0)` : `rotate(${mid - 90}) translate(92 0)`}
                    textAnchor={mid > 180 ? 'start' : 'end'}
                    dominantBaseline="middle"
                    className="wheel-label"
                    fontSize={ideas.length > 12 ? 6.5 : 8}
                  >
                    {label}
                  </text>
                </g>
              )
            })}
            <circle r="16" className="wheel-hub" />
          </svg>
          <button type="button" className="wheel-spin" onClick={spin} disabled={spinning}>
            <Shuffle size={18} aria-hidden="true" />
            {spinning ? 'Spinning…' : 'Spin'}
          </button>
        </div>

        <aside className="wheel-side">
          <div className={`wheel-result${result ? ' has-result' : ''}`} aria-live="polite">
            {result ? (
              <>
                <p className="eyebrow">This Friday:</p>
                <p className="wheel-result-title">{result}</p>
                <Link to={`/groups/new?title=${encodeURIComponent(result)}`} className="btn btn-primary">
                  Plan it with friends <ArrowRight size={16} aria-hidden="true" />
                </Link>
                <button type="button" className="link-button small" onClick={spin} disabled={spinning}>
                  Nah, spin again
                </button>
              </>
            ) : (
              <p className="muted">{spinning ? 'And the plan is…' : 'Hit Spin to get an idea.'}</p>
            )}
          </div>

          <section className="card wheel-ideas">
            <header className="card-header">
              <h2>Ideas on the wheel</h2>
              <button
                type="button"
                className="link-button small icon-line"
                onClick={() => {
                  setIdeas(DEFAULT_IDEAS)
                  setResult(null)
                }}
              >
                <RotateCcw size={14} aria-hidden="true" /> Reset
              </button>
            </header>
            <ul>
              {ideas.map((idea, i) => (
                <li key={idea}>
                  <span className="cal-dot" style={{ background: WHEEL_COLORS[i % WHEEL_COLORS.length] }} />
                  <span className="wheel-idea">{idea}</span>
                  <button
                    type="button"
                    className="icon-button"
                    aria-label={`Remove ${idea}`}
                    disabled={spinning || ideas.length <= MIN_IDEAS}
                    onClick={() => {
                      setIdeas(ideas.filter((x) => x !== idea))
                      setResult(null)
                    }}
                  >
                    <X size={14} />
                  </button>
                </li>
              ))}
            </ul>
            <form className="copy-row" onSubmit={addIdea}>
              <input
                value={draft}
                onChange={(e) => setDraft(e.target.value)}
                placeholder="Add your own idea"
                maxLength={40}
                aria-label="New idea"
                disabled={spinning || ideas.length >= MAX_IDEAS}
              />
              <button type="submit" className="btn" disabled={spinning || !draft.trim() || ideas.length >= MAX_IDEAS}>
                <Plus size={16} aria-hidden="true" /> Add
              </button>
            </form>
            <p className="hint">Saved in this browser.</p>
          </section>
        </aside>
      </div>
    </div>
  )
}
