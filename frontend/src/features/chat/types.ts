export type Citation = {
  id: string
  documentId: string
  fileName: string
  pageNumber: number | null
  chunkIndex: number
  excerpt: string
}

export type ChatTurn = {
  role: 'user' | 'assistant'
  content: string
}

export type ChatMessage = ChatTurn & {
  id: number
  citations?: Citation[]
  failed?: boolean
  streaming?: boolean
}

export type KnowledgeChatResponse = {
  answer: string
  model: string
  citations: Citation[]
}

type ChatStreamEvent =
  | { text: string }
  | KnowledgeChatResponse
  | { message: string }

export type TokenHandler = (text: string) => void

export type KnowledgeChatRequest = {
  message: string
  history: ChatTurn[]
}

export type { ChatStreamEvent }
