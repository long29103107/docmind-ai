using System.ComponentModel.DataAnnotations;

namespace DocMind.Api.Models;

public sealed class ChatRequest : IValidatableObject
{
    [Required, StringLength(8000, MinimumLength = 1)]
    public string Message { get; init; } = string.Empty;

    [MaxLength(10)]
    public IReadOnlyList<ChatTurn>? History { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (History is null)
        {
            yield return new ValidationResult("Chat history must be a list.", [nameof(History)]);
            yield break;
        }

        var totalCharacters = 0;
        foreach (var turn in History)
        {
            if (turn is null)
            {
                yield return new ValidationResult("Chat history cannot contain null entries.", [nameof(History)]);
                continue;
            }

            if (turn.Role is not ("user" or "assistant"))
            {
                yield return new ValidationResult("Chat history roles must be 'user' or 'assistant'.", [nameof(History)]);
            }

            if (string.IsNullOrWhiteSpace(turn.Content) || turn.Content.Length > 4000)
            {
                yield return new ValidationResult("Each chat history message must contain 1–4000 characters.", [nameof(History)]);
            }

            totalCharacters += turn.Content?.Length ?? 0;
        }

        if (totalCharacters > 16000)
        {
            yield return new ValidationResult("Chat history cannot exceed 16000 characters.", [nameof(History)]);
        }
    }
}

public sealed record ChatTurn(string Role, string Content);
