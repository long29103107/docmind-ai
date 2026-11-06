import type { DocumentItem } from './types'

export async function getDocuments(): Promise<DocumentItem[]> {
  const response = await fetch('/api/documents')
  if (!response.ok) throw await responseError(response, 'Could not load the document library.')
  return await response.json() as DocumentItem[]
}

export async function uploadDocument(file: File): Promise<DocumentItem> {
  const form = new FormData()
  form.append('file', file)
  const response = await fetch('/api/documents', { method: 'POST', body: form })
  if (!response.ok) throw await responseError(response, 'The document could not be uploaded.')
  return await response.json() as DocumentItem
}

export async function deleteDocument(documentId: string): Promise<void> {
  const response = await fetch(`/api/documents/${documentId}`, { method: 'DELETE' })
  if (!response.ok) throw await responseError(response, 'The document could not be deleted. Try again.')
}

async function responseError(response: Response, fallback: string): Promise<Error> {
  const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
  return new Error(problem?.detail || problem?.title || `${fallback} (request failed: ${response.status}).`)
}
