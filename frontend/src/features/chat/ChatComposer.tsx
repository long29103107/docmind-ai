import { useEffect, useRef, type FormEvent, type KeyboardEvent } from 'react'

type Props = {
  busy: boolean
  value: string
  onChange: (value: string) => void
  onSubmit: () => void
}

export default function ChatComposer({ busy, value, onChange, onSubmit }: Props) {
  const inputRef = useRef<HTMLTextAreaElement>(null)
  const wasBusy = useRef(false)

  useEffect(() => {
    if (wasBusy.current && !busy) inputRef.current?.focus()
    wasBusy.current = busy
  }, [busy])

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onSubmit()
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      onSubmit()
    }
  }

  return (
    <div className="composer-area">
      <form className="composer" onSubmit={submit}>
        <textarea
          ref={inputRef}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Ask your documents..."
          rows={1}
          maxLength={8000}
          aria-label="Ask your documents"
          disabled={busy}
        />
        <div className="composer-bottom">
          <span className="composer-hint"><kbd>Shift</kbd> + <kbd>Enter</kbd> for a new line</span>
          <button className="send-button" type="submit" disabled={!value.trim() || busy} aria-label="Send question">↑</button>
        </div>
      </form>
      <p className="disclaimer">Answers use retrieved passages and show their sources. Documents stay in this private local workspace.</p>
    </div>
  )
}
