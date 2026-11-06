import type { ChatStreamEvent, KnowledgeChatRequest, KnowledgeChatResponse, TokenHandler } from './types'

export async function streamKnowledgeChat(
  requestBody: KnowledgeChatRequest,
  onToken: TokenHandler,
): Promise<KnowledgeChatResponse> {
  const response = await fetch('/api/knowledge/chat', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(requestBody),
  })

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
    throw new Error(problem?.detail || problem?.title || `The request failed (${response.status}).`)
  }
  if (!response.body) throw new Error('The chat response did not include a readable stream.')

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  let result: KnowledgeChatResponse | undefined

  const consumeFrame = (frame: string) => {
    let event = 'message'
    const data: string[] = []
    for (const line of frame.split('\n')) {
      if (line.startsWith('event:')) event = line.slice(6).trim()
      else if (line.startsWith('data:')) data.push(line.slice(5).trimStart())
    }
    if (data.length === 0) return

    const payload = JSON.parse(data.join('\n')) as ChatStreamEvent
    if (event === 'token' && 'text' in payload) {
      onToken(payload.text)
    } else if (event === 'complete' && 'answer' in payload) {
      result = payload
    } else if (event === 'error' && 'message' in payload) {
      throw new Error(payload.message)
    }
  }

  while (true) {
    const { value: chunk, done } = await reader.read()
    buffer += decoder.decode(chunk, { stream: !done }).replace(/\r\n/g, '\n')

    let frameEnd = buffer.indexOf('\n\n')
    while (frameEnd !== -1) {
      consumeFrame(buffer.slice(0, frameEnd))
      buffer = buffer.slice(frameEnd + 2)
      frameEnd = buffer.indexOf('\n\n')
    }

    if (done) break
  }

  if (buffer.trim()) consumeFrame(buffer)
  if (!result) throw new Error('The chat stream ended before the answer was complete.')
  return result
}
