import { useEffect, useRef, useState, type FormEvent } from 'react'
import { deleteDocument, getDocuments, uploadDocument } from './api'
import type { DocumentItem } from './types'

type Props = {
  onCountChange: (count: number) => void
}

export default function DocumentsView({ onCountChange }: Props) {
  const [documents, setDocuments] = useState<DocumentItem[]>([])
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState('')
  const [loaded, setLoaded] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  async function refreshDocuments() {
    try {
      const result = await getDocuments()
      setDocuments(result)
      onCountChange(result.length)
      setError('')
      setLoaded(true)
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Could not load the document library.')
    }
  }

  useEffect(() => { void refreshDocuments() }, [])

  async function handleUpload(event: FormEvent<HTMLInputElement>) {
    const file = event.currentTarget.files?.[0]
    event.currentTarget.value = ''
    if (!file) return

    setUploading(true)
    setError('')
    try {
      const uploaded = await uploadDocument(file)
      const updated = [uploaded, ...documents.filter((item) => item.id !== uploaded.id)]
      setDocuments(updated)
      onCountChange(updated.length)
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'The document could not be uploaded.')
    } finally {
      setUploading(false)
    }
  }

  async function handleDelete(document: DocumentItem) {
    if (!window.confirm(`Delete “${document.fileName}” and its indexed content?`)) return
    setError('')
    try {
      await deleteDocument(document.id)
      const updated = documents.filter((item) => item.id !== document.id)
      setDocuments(updated)
      onCountChange(updated.length)
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'The document could not be deleted.')
      await refreshDocuments()
    }
  }

  return (
    <section className="documents-page">
      <div className="documents-heading">
        <div><p className="eyebrow align-left">YOUR PRIVATE WORKSPACE</p><h1>Documents</h1><p className="welcome-copy align-left">Upload files to make their contents searchable in chat.</p></div>
        <button className="upload-button" disabled={uploading} onClick={() => fileRef.current?.click()}>{uploading ? 'Indexing…' : '＋ Add documents'}</button>
        <input ref={fileRef} className="visually-hidden" type="file" accept=".pdf,.docx,.txt,.md" onChange={handleUpload} />
      </div>
      <div className="format-note">PDF, DOCX, TXT, or Markdown · Up to 20 MB · Scanned PDFs are not supported yet</div>
      {error && <div className="inline-error" role="alert">{error}</div>}
      {loaded && documents.length === 0 ? (
        <div className="documents-empty"><div className="empty-file-icon">▤</div><strong>No documents yet</strong><p>Add a document to start asking questions about your material.</p><button className="upload-button secondary-upload" disabled={uploading} onClick={() => fileRef.current?.click()}>Choose a file</button></div>
      ) : documents.length > 0 ? (
        <div className="document-table">
          <div className="document-table-head"><span>DOCUMENT</span><span>STATUS</span><span>SIZE</span><span /></div>
          {documents.map((document) => (
            <article className="document-row" key={document.id}>
              <div className="file-cell"><span className="file-icon">{document.fileName.toLowerCase().endsWith('.pdf') ? 'PDF' : 'DOC'}</span><span className="file-meta"><strong>{document.fileName}</strong><small>Added {new Date(document.createdAt).toLocaleDateString()}</small></span></div>
              <span className={`status-badge status-${document.status}`}><i />{document.status}</span>
              <span className="file-size">{formatBytes(document.fileSizeBytes)}</span>
              <button className="delete-document" title={`Delete ${document.fileName}`} aria-label={`Delete ${document.fileName}`} onClick={() => void handleDelete(document)}>×</button>
              {document.failureMessage && <p className="document-failure">{document.failureMessage}</p>}
            </article>
          ))}
        </div>
      ) : null}
    </section>
  )
}

function formatBytes(bytes: number) {
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
