using DocMind.Api.Configuration;
using DocMind.Api.Data;
using DocMind.Api.Documents;
using DocMind.Api.Rag;
using DocMind.Api.Services;
using Npgsql;
using Pgvector;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DocMind AI API",
        Version = "v1",
        Description = "Document management and grounded knowledge chat API."
    });
});
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

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services
        .GetRequiredService<DatabaseInitializer>()
        .InitializeAsync(app.Lifetime.ApplicationStopping);
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "DocMind AI API v1"));
}
app.MapControllers();

app.Run();

public partial class Program { }
