import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'

type Citation = {
  id: string
  documentId: string
  fileName: string
  pageNumber: number | null
  chunkIndex: number
  excerpt: string
}

type Message = {
  id: number
  role: 'user' | 'assistant'
  content: string
  citations?: Citation[]
  failed?: boolean
}

type DocumentItem = {
  id: string
  fileName: string
  mediaType: string
  fileSizeBytes: number
  status: 'processing' | 'ready' | 'failed' | 'deleting'
  failureMessage: string | null
  createdAt: string
  indexedAt: string | null
}

const suggestions = [
  { icon: '⌕', title: 'Find a specific detail', prompt: 'What are the main points covered by my documents?' },
  { icon: '✳', title: 'Compare information', prompt: 'Compare the key ideas in my uploaded material.' },
  { icon: '↗', title: 'Summarize a topic', prompt: 'Give me a concise summary of the relevant documents.' },
]

export default function App() {
  const [view, setView] = useState<'chat' | 'documents'>('chat')
  const [messages, setMessages] = useState<Message[]>([])
  const [documents, setDocuments] = useState<DocumentItem[]>([])
  const [draft, setDraft] = useState('')
  const [busy, setBusy] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState('')
  const [documentError, setDocumentError] = useState('')
  const nextId = useRef(1)
  const inputRef = useRef<HTMLTextAreaElement>(null)
  const fileRef = useRef<HTMLInputElement>(null)

  async function refreshDocuments() {
    try {
      const response = await fetch('/api/documents')
      if (!response.ok) throw new Error('Could not load the document library.')
      setDocuments(await response.json() as DocumentItem[])
      setDocumentError('')
    } catch (cause) {
      setDocumentError(cause instanceof Error ? cause.message : 'Could not load the document library.')
    }
  }

  useEffect(() => { void refreshDocuments() }, [])

  async function sendMessage(value: string) {
    const message = value.trim()
    if (!message || busy) return

    const history = messages.filter((item) => !item.failed).slice(-10).map(({ role, content }) => ({ role, content }))
    setMessages((current) => [...current, { id: nextId.current++, role: 'user', content: message }])
    setDraft('')
    setError('')
    setBusy(true)

    try {
      const response = await fetch('/api/knowledge/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message, history }),
      })

      if (!response.ok) {
        const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
        throw new Error(problem?.detail || problem?.title || `The request failed (${response.status}).`)
      }

      const result = await response.json() as { answer: string; citations: Citation[] }
      setMessages((current) => [...current, { id: nextId.current++, role: 'assistant', content: result.answer, citations: result.citations }])
    } catch (cause) {
      const detail = cause instanceof Error ? cause.message : 'Something went wrong while contacting DocMind.'
      setError(detail)
      setMessages((current) => [...current, { id: nextId.current++, role: 'assistant', content: detail, failed: true }])
    } finally {
      setBusy(false)
      inputRef.current?.focus()
    }
  }

  async function uploadDocument(event: FormEvent<HTMLInputElement>) {
    const file = event.currentTarget.files?.[0]
    event.currentTarget.value = ''
    if (!file) return

    setUploading(true)
    setDocumentError('')
    const form = new FormData()
    form.append('file', file)
    try {
      const response = await fetch('/api/documents', { method: 'POST', body: form })
      if (!response.ok) {
        const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
        throw new Error(problem?.detail || problem?.title || `Upload failed (${response.status}).`)
      }
      const document = await response.json() as DocumentItem
      setDocuments((current) => [document, ...current.filter((item) => item.id !== document.id)])
    } catch (cause) {
      setDocumentError(cause instanceof Error ? cause.message : 'The document could not be uploaded.')
    } finally {
      setUploading(false)
    }
  }

  async function deleteDocument(document: DocumentItem) {
    if (!window.confirm(`Delete “${document.fileName}” and its indexed content?`)) return
    setDocumentError('')
    try {
      const response = await fetch(`/api/documents/${document.id}`, { method: 'DELETE' })
      if (!response.ok) throw new Error('The document could not be deleted. Try again.')
      setDocuments((current) => current.filter((item) => item.id !== document.id))
    } catch (cause) {
      setDocumentError(cause instanceof Error ? cause.message : 'The document could not be deleted.')
      await refreshDocuments()
    }
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    void sendMessage(draft)
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      void sendMessage(draft)
    }
  }

  function startNewChat() {
    setMessages([])
    setDraft('')
    setError('')
    setView('chat')
    inputRef.current?.focus()
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="#home" aria-label="DocMind home" onClick={startNewChat}>
          <span className="brand-mark"><span /></span>
          <span>docmind<span className="brand-ai">.ai</span></span>
        </a>

        <button className="new-chat" onClick={startNewChat}>
          <span className="plus">＋</span> New chat <span className="shortcut">⌘ K</span>
        </button>

        <div className="sidebar-section">
          <p className="section-label">WORKSPACE</p>
          <div className="workspace-card">
            <span className="workspace-icon">N</span>
            <span className="workspace-name">Personal workspace</span>
            <span className="chevron">⌄</span>
          </div>
          <button className={`nav-item ${view === 'chat' ? 'nav-active' : ''}`} onClick={() => setView('chat')}><span>◌</span> Ask documents</button>
          <button className={`nav-item ${view === 'documents' ? 'nav-active' : ''}`} onClick={() => { setView('documents'); void refreshDocuments() }}><span>▤</span> Documents <span className="nav-count">{documents.length}</span></button>
        </div>

        <div className="sidebar-section recent-section">
          <p className="section-label">THIS SESSION</p>
          {messages.length === 0 ? (
            <p className="empty-history">Your questions will appear here.</p>
          ) : (
            <div className="recent-item"><span>◌</span>{messages.find((item) => item.role === 'user')?.content}</div>
          )}
        </div>

        <div className="sidebar-footer">
          <div className="avatar">DM</div>
          <div className="profile-copy"><strong>Local workspace</strong><span>Private development</span></div>
          <span className="more">•••</span>
        </div>
      </aside>

      <main className="main-panel">
        <header className="topbar">
          <div className="mobile-brand"><span className="brand-mark"><span /></span> docmind<span className="brand-ai">.ai</span></div>
          <div className="mode-pill"><span className="status-dot" /> {view === 'chat' ? 'Knowledge chat' : 'Document library'} <span className="pill-chevron">⌄</span></div>
          <button className="share-button" title="Sharing is not available in this MVP" aria-label="Sharing unavailable">↗</button>
        </header>

        {view === 'documents' ? (
          <section className="documents-page">
            <div className="documents-heading">
              <div><p className="eyebrow align-left">YOUR PRIVATE WORKSPACE</p><h1>Documents</h1><p className="welcome-copy align-left">Upload files to make their contents searchable in chat.</p></div>
              <button className="upload-button" disabled={uploading} onClick={() => fileRef.current?.click()}>{uploading ? 'Indexing…' : '＋ Add documents'}</button>
              <input ref={fileRef} className="visually-hidden" type="file" accept=".pdf,.docx,.txt,.md" onChange={uploadDocument} />
            </div>
            <div className="format-note">PDF, DOCX, TXT, or Markdown · Up to 20 MB · Scanned PDFs are not supported yet</div>
            {documentError && <div className="inline-error" role="alert">{documentError}</div>}
            {documents.length === 0 ? (
              <div className="documents-empty"><div className="empty-file-icon">▤</div><strong>No documents yet</strong><p>Add a document to start asking questions about your material.</p><button className="upload-button secondary-upload" disabled={uploading} onClick={() => fileRef.current?.click()}>Choose a file</button></div>
            ) : (
              <div className="document-table">
                <div className="document-table-head"><span>DOCUMENT</span><span>STATUS</span><span>SIZE</span><span /></div>
                {documents.map((document) => (
                  <article className="document-row" key={document.id}>
                    <div className="file-cell"><span className="file-icon">{document.fileName.toLowerCase().endsWith('.pdf') ? 'PDF' : 'DOC'}</span><span className="file-meta"><strong>{document.fileName}</strong><small>Added {new Date(document.createdAt).toLocaleDateString()}</small></span></div>
                    <span className={`status-badge status-${document.status}`}><i />{document.status}</span>
                    <span className="file-size">{formatBytes(document.fileSizeBytes)}</span>
                    <button className="delete-document" title={`Delete ${document.fileName}`} aria-label={`Delete ${document.fileName}`} onClick={() => void deleteDocument(document)}>×</button>
                    {document.failureMessage && <p className="document-failure">{document.failureMessage}</p>}
                  </article>
                ))}
              </div>
            )}
          </section>
        ) : (
          <>
            <section className={`conversation ${messages.length ? 'has-messages' : ''}`} aria-live="polite">
              {messages.length === 0 ? (
                <div className="welcome">
                  <div className="welcome-icon"><span className="brand-mark"><span /></span></div>
                  <p className="eyebrow">YOUR DOCUMENTS, IN CONVERSATION</p>
                  <h1>What would you like<br />to <span>find out?</span></h1>
                  <p className="welcome-copy">Answers are grounded in the documents in your workspace.</p>
                  <div className="suggestions">
                    {suggestions.map((item) => (
                      <button key={item.title} className="suggestion" onClick={() => void sendMessage(item.prompt)}>
                        <span className="suggestion-icon">{item.icon}</span>
                        <span><strong>{item.title}</strong><small>{item.prompt}</small></span>
                        <span className="suggestion-arrow">↗</span>
                      </button>
                    ))}
                  </div>
                </div>
              ) : (
                <div className="message-list">
                  {messages.map((message) => (
                    <article className={`message message-${message.role} ${message.failed ? 'message-failed' : ''}`} key={message.id}>
                      {message.role === 'assistant' && <div className="assistant-avatar"><span className="brand-mark"><span /></span></div>}
                      <div className="message-body">
                        <span className="message-label">{message.role === 'user' ? 'You' : 'DocMind'}</span>
                        <p>{message.content}</p>
                        {message.citations && message.citations.length > 0 && <div className="citations"><span className="citation-heading">SOURCES</span>{message.citations.map((citation) => <details className="citation" key={citation.id}><summary><span className="citation-id">{citation.id}</span><span>{citation.fileName}{citation.pageNumber ? ` · Page ${citation.pageNumber}` : ` · Section ${citation.chunkIndex + 1}`}</span></summary><p>{citation.excerpt}</p></details>)}</div>}
                      </div>
                    </article>
                  ))}
                  {busy && <article className="message message-assistant"><div className="assistant-avatar"><span className="brand-mark"><span /></span></div><div className="typing" aria-label="DocMind is searching and thinking"><i /><i /><i /></div></article>}
                  {error && <p className="error-note">Could not complete that request. Check the API, database, and provider configuration.</p>}
                </div>
              )}
            </section>

            <div className="composer-area">
              <form className="composer" onSubmit={submit}>
                <textarea ref={inputRef} value={draft} onChange={(event) => setDraft(event.target.value)} onKeyDown={handleKeyDown} placeholder="Ask your documents..." rows={1} maxLength={8000} aria-label="Ask your documents" disabled={busy} />
                <div className="composer-bottom"><span className="composer-hint"><kbd>Shift</kbd> + <kbd>Enter</kbd> for a new line</span><button className="send-button" type="submit" disabled={!draft.trim() || busy} aria-label="Send question">↑</button></div>
              </form>
              <p className="disclaimer">Answers use retrieved passages and show their sources. Documents stay in this private local workspace.</p>
            </div>
          </>
        )}
      </main>
    </div>
  )
}

function formatBytes(bytes: number) {
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
