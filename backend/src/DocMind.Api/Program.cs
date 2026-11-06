using DocMind.Api.Configuration;
using DocMind.Api.Data;
using DocMind.Api.Documents;
using DocMind.Api.Rag;
using DocMind.Api.Services;
using Npgsql;
using Pgvector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
builder.Services.Configure<DocumentStorageOptions>(builder.Configuration.GetSection("DocumentStorage"));
builder.Services.Configure<RagOptions>(builder.Configuration.GetSection("Rag"));
var connectionString = builder.Configuration.GetConnectionString("KnowledgeBase")
    ?? throw new InvalidOperationException("ConnectionStrings:KnowledgeBase is required.");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();
builder.Services.AddSingleton(dataSourceBuilder.Build());
builder.Services.AddHttpClient<OpenAiChatService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient<OpenAiEmbeddingService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<DocumentRepository>();
builder.Services.AddSingleton<DocumentStorage>();
builder.Services.AddSingleton<DocumentExtractor>();
builder.Services.AddSingleton<TextChunker>();
builder.Services.AddScoped<DocumentIngestionService>();
builder.Services.AddScoped<KnowledgeChatService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
