using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.SemanticKernel;
using BoardAgentService.Services;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddSingleton<GuardrailsService>();
builder.Services.AddSingleton<RagService>();
// 1. RATE LIMITING
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AiChatLimiter", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

// 2. LANGFUSE (OTLP — Headers is a comma-separated string, not a dictionary)
var lfPub = builder.Configuration["LangFuse:PublicKey"];
var lfSec = builder.Configuration["LangFuse:SecretKey"];
var lfBaseUrl = (builder.Configuration["LangFuse:BaseUrl"] ?? "https://cloud.langfuse.com").TrimEnd('/');
var lfAuth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{lfPub}:{lfSec}"));

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Microsoft.SemanticKernel*")
        .AddOtlpExporter(options =>
        {
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
            options.Endpoint = new Uri($"{lfBaseUrl}/api/public/otel/v1/traces");
            options.Headers = $"Authorization=Basic {lfAuth},x-langfuse-ingestion-version=4";
        }));

// 3. SEMANTIC KERNEL
builder.Services.AddKernel()
    .AddOpenAIChatCompletion(
        modelId: builder.Configuration["OpenAI:ModelId"]!,
        apiKey: builder.Configuration["OpenAI:ApiKey"]!)
    .AddOpenAIEmbeddingGenerator(
        modelId: builder.Configuration["OpenAI:EmbeddingModelId"] ?? "text-embedding-3-small",
        apiKey: builder.Configuration["OpenAI:ApiKey"]!);

var app = builder.Build();
// Učitavanje PDF-ova u memoriju pre nego što aplikacija počne da prima zahteve
using (var scope = app.Services.CreateScope())
{
    var rag = scope.ServiceProvider.GetRequiredService<RagService>();
    await rag.LoadDocumentsAsync("Data");
    Console.WriteLine("-> RAG: Dokumenti uspešno učitani i vektorizovani!");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.MapControllers();

app.Run();
