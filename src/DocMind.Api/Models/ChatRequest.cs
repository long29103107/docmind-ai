using System.ComponentModel.DataAnnotations;

namespace DocMind.Api.Models;

public sealed class ChatRequest
{
    [Required, StringLength(8000, MinimumLength = 1)]
    public string Message { get; init; } = string.Empty;
}
