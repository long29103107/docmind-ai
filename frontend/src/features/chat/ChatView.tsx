import { useRef, useState } from 'react'
import { streamKnowledgeChat } from './api'
import ChatComposer from './ChatComposer'
import ChatTranscript from './ChatTranscript'
import type { ChatMessage } from './types'

type Props = {
  onFirstQuestion: (question: string) => void
}

export default function ChatView({ onFirstQuestion }: Props) {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [draft, setDraft] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const nextId = useRef(1)

  async function sendMessage(value: string) {
    const message = value.trim()
    if (!message || busy) return

    const history = messages.filter((item) => !item.failed).slice(-10).map(({ role, content }) => ({ role, content }))
    if (messages.length === 0) onFirstQuestion(message)
    const userId = nextId.current++
    const assistantId = nextId.current++
    setMessages((current) => [
      ...current,
      { id: userId, role: 'user', content: message },
      { id: assistantId, role: 'assistant', content: '', streaming: true },
    ])
    setDraft('')
    setError('')
    setBusy(true)

    try {
      const result = await streamKnowledgeChat({ message, history }, (text) => {
        setMessages((current) => current.map((item) => item.id === assistantId
          ? { ...item, content: item.content + text }
          : item))
      })
      setMessages((current) => current.map((item) => item.id === assistantId
        ? { ...item, content: result.answer, citations: result.citations, streaming: false }
        : item))
    } catch (cause) {
      const detail = cause instanceof Error ? cause.message : 'Something went wrong while contacting DocMind.'
      setError(detail)
      setMessages((current) => current.map((item) => item.id === assistantId
        ? { ...item, streaming: false, failed: true, content: item.content || detail }
        : item))
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <ChatTranscript messages={messages} error={error} onSelectSuggestion={(prompt) => void sendMessage(prompt)} />
      <ChatComposer busy={busy} value={draft} onChange={setDraft} onSubmit={() => void sendMessage(draft)} />
    </>
  )
}
