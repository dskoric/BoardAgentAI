
Board Agent Service: Enterprise RAG & Agentic Workflow in .NET
A secure, cloud-ready AI microservice built with .NET 8 and Semantic Kernel. Designed to simulate the backend for a secure board management platform, demonstrating autonomous agent behavior, Retrieval-Augmented Generation (RAG), input/output guardrails, and automated quality evaluation.

Architecture Overview
The system follows an enterprise microservices pattern, isolating the AI workflow from external exposure:
Client -> API Gateway (Rate Limiting) -> Input Guardrails (PII Masking) -> Semantic Kernel Agent -> RAG Plugin -> LLM -> Output Guardrails (Grounding Check) -> Response

Key Features & Implementation Details
Agentic Orchestration (Semantic Kernel):
Instead of hardcoded logic, the system uses FunctionChoiceBehavior.Auto(). The LLM autonomously decides when to invoke the RAG search tool based on the user's intent, demonstrating true agentic behavior.
RAG Pipeline (In-Memory Vector DB):
Demonstrates the core mechanics of RAG: PDF parsing (PdfPig), text chunking (500 chars), embedding generation via OpenAI, and context retrieval using Cosine Similarity. (Note: In production, this List<DocumentChunk> maps directly to Azure AI Search with Hybrid Search & Semantic Ranking).
Enterprise Guardrails:
Input: Regex-based PII (email) masking before the prompt reaches the LLM.
Output: Grounding validation ensuring the LLM doesn't hallucinate by forcing source citations. If citations are missing, a system warning is appended.
Quality Gates (Evaluation):
An automated /api/Evaluation/run endpoint tests the agent against a "Gold Dataset" to calculate Precision metrics, ensuring prompt changes don't degrade performance.
Observability (OpenTelemetry):
Configured AddSource("Microsoft.SemanticKernel") to trace LLM calls, tool executions, and latencies. The setup is structured to export traces to Langfuse for prompt engineering and debugging.
API Gateway & Resilience:
Implemented FixedWindowRateLimiter to protect the backend from LLM spam and control API costs.
Tech Stack
Framework: .NET 8, ASP.NET Core Minimal APIs / Controllers
AI Orchestrator: Microsoft Semantic Kernel
LLM: OpenAI API (gpt-4o-mini)
Document Processing: UglyToad.PdfPig
Observability: OpenTelemetry, Langfuse
Architecture: Clean Architecture principles, Dependency Injection
Getting Started
Prerequisites
.NET 8 SDK
OpenAI API Key
Langfuse Account (Optional, for tracing)
Configuration
The appsettings.Development.json is excluded from Git for security. Create it in the root with the following structure:

{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "OpenAI": {
    "ApiKey": "sk-your-openai-key",
    "ModelId": "gpt-4o-mini"
  },
  "LangFuse": {
    "PublicKey": "pk-lf-...",
    "SecretKey": "sk-lf-..."
  }
}

Run the Service

Place a sample .pdf document in the Data/ folder.
Run the application

dotnet run

3. The console will log -> RAG: Documents successfully loaded and vectorized!.

4. Open http://localhost:5143/swagger to interact with the API endpoints (/api/Chat, /api/Evaluation).

Project Structure

Controllers/ - API endpoints (Chat, Evaluation)
Plugins/ - Semantic Kernel Tools (SearchPlugin)
Services/ - Core business logic (RagService, GuardrailsService)
Data/ - Storage for PDF documents to be indexed
