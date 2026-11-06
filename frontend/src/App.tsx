import { useEffect, useState } from 'react'
import ChatView from './features/chat/ChatView'
import DocumentsView from './features/documents/DocumentsView'
import { getDocuments } from './features/documents/api'

type View = 'chat' | 'documents'

export default function App() {
  const [view, setView] = useState<View>('chat')
  const [documentCount, setDocumentCount] = useState(0)
  const [chatSession, setChatSession] = useState(0)
  const [recentQuestion, setRecentQuestion] = useState('')

  useEffect(() => {
    void getDocuments().then((documents) => setDocumentCount(documents.length)).catch(() => undefined)
  }, [])

  function startNewChat() {
    setChatSession((current) => current + 1)
    setRecentQuestion('')
    setView('chat')
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
          <button className={`nav-item ${view === 'documents' ? 'nav-active' : ''}`} onClick={() => setView('documents')}><span>▤</span> Documents <span className="nav-count">{documentCount}</span></button>
        </div>

        <div className="sidebar-section recent-section">
          <p className="section-label">THIS SESSION</p>
          {recentQuestion
            ? <div className="recent-item"><span>◌</span>{recentQuestion}</div>
            : <p className="empty-history">Your questions will appear here.</p>}
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

        {view === 'documents'
          ? <DocumentsView onCountChange={setDocumentCount} />
          : <ChatView key={chatSession} onFirstQuestion={setRecentQuestion} />}
      </main>
    </div>
  )
}
