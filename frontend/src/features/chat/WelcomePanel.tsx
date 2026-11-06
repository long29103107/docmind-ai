type Suggestion = {
  icon: string
  title: string
  prompt: string
}

const suggestions: Suggestion[] = [
  { icon: '⌕', title: 'Find a specific detail', prompt: 'What are the main points covered by my documents?' },
  { icon: '✳', title: 'Compare information', prompt: 'Compare the key ideas in my uploaded material.' },
  { icon: '↗', title: 'Summarize a topic', prompt: 'Give me a concise summary of the relevant documents.' },
]

type Props = {
  onSelectSuggestion: (prompt: string) => void
}

export default function WelcomePanel({ onSelectSuggestion }: Props) {
  return (
    <div className="welcome">
      <div className="welcome-icon"><span className="brand-mark"><span /></span></div>
      <p className="eyebrow">YOUR DOCUMENTS, IN CONVERSATION</p>
      <h1>What would you like<br />to <span>find out?</span></h1>
      <p className="welcome-copy">Answers are grounded in the documents in your workspace.</p>
      <div className="suggestions">
        {suggestions.map((item) => (
          <button key={item.title} className="suggestion" onClick={() => onSelectSuggestion(item.prompt)}>
            <span className="suggestion-icon">{item.icon}</span>
            <span><strong>{item.title}</strong><small>{item.prompt}</small></span>
            <span className="suggestion-arrow">↗</span>
          </button>
        ))}
      </div>
    </div>
  )
}
