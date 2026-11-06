export type DocumentItem = {
  id: string
  fileName: string
  mediaType: string
  fileSizeBytes: number
  status: 'processing' | 'ready' | 'failed' | 'deleting'
  failureMessage: string | null
  createdAt: string
  indexedAt: string | null
}
