import type { ChatMessage } from './types'
import WelcomePanel from './WelcomePanel'

type Props = {
  messages: ChatMessage[]
  error: string
  onSelectSuggestion: (prompt: string) => void
}

export default function ChatTranscript({ messages, error, onSelectSuggestion }: Props) {
  return (
    <section className={`conversation ${messages.length ? 'has-messages' : ''}`} aria-live="polite">
      {messages.length === 0 ? (
        <WelcomePanel onSelectSuggestion={onSelectSuggestion} />
      ) : (
        <div className="message-list">
          {messages.map((message) => (
            <article className={`message message-${message.role} ${message.failed ? 'message-failed' : ''}`} key={message.id}>
              {message.role === 'assistant' && <div className="assistant-avatar"><span className="brand-mark"><span /></span></div>}
              <div className="message-body">
                <span className="message-label">{message.role === 'user' ? 'You' : 'DocMind'}</span>
                {message.content
                  ? <p>{message.content}{message.streaming && <span className="streaming-cursor" aria-hidden="true">▍</span>}</p>
                  : message.streaming && <div className="typing" aria-label="DocMind is searching and thinking"><i /><i /><i /></div>}
                {message.citations && message.citations.length > 0 && (
                  <div className="citations">
                    <span className="citation-heading">SOURCES</span>
                    {message.citations.map((citation) => (
                      <details className="citation" key={citation.id}>
                        <summary>
                          <span className="citation-id">{citation.id}</span>
                          <span>{citation.fileName}{citation.pageNumber ? ` · Page ${citation.pageNumber}` : ` · Section ${citation.chunkIndex + 1}`}</span>
                        </summary>
                        <p>{citation.excerpt}</p>
                      </details>
                    ))}
                  </div>
                )}
              </div>
            </article>
          ))}
          {error && <p className="error-note">{error}</p>}
        </div>
      )}
    </section>
  )
}
