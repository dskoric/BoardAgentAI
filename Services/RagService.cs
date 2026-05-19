using System.Linq;
using Microsoft.Extensions.AI;
using UglyToad.PdfPig;

namespace BoardAgentService.Services;

public class RagService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingService;
    private List<DocumentChunk> _chunks = new();

    public RagService(IEmbeddingGenerator<string, Embedding<float>> embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public async Task LoadDocumentsAsync(string folderPath)
    {
        var pdfFiles = Directory.GetFiles(folderPath, "*.pdf");
        foreach (var file in pdfFiles)
        {
            using var pdf = PdfDocument.Open(file);
            string fullText = string.Join(" ", pdf.GetPages().Select(p => p.Text));
            
            Console.WriteLine($"-> Učitano karaktera iz PDF-a: {fullText.Length}");

            for (int i = 0; i < fullText.Length; i += 500)
            {
                string chunkText = fullText.Substring(i, Math.Min(500, fullText.Length - i));
                if (string.IsNullOrWhiteSpace(chunkText)) continue;

                var embeddings = await _embeddingService.GenerateAsync([chunkText]);
                _chunks.Add(new DocumentChunk { Text = chunkText, Embedding = embeddings[0].Vector.ToArray() });
            }
        }
    }

    public async Task<string> SearchAsync(string query)
    {
        var queryEmbeddings = await _embeddingService.GenerateAsync([query]);
        var queryVector = queryEmbeddings[0].Vector.ToArray();

        var bestChunk = _chunks.OrderByDescending(c => CosineSimilarity(c.Embedding, queryVector)).FirstOrDefault();

        if (bestChunk == null) return "Nema podataka u bazi. Ukupno učitano delova: " + _chunks.Count;
        
        string result = $"IZVOR: Dokument\nSADRŽAJ: {bestChunk.Text}";
        Console.WriteLine($"RAG VRATIO: {result}");
        return result;
    }

    private float CosineSimilarity(float[] v1, float[] v2)
    {
        float dot = 0, mag1 = 0, mag2 = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            dot += v1[i] * v2[i];
            mag1 += v1[i] * v1[i];
            mag2 += v2[i] * v2[i];
        }
        return dot / ((float)Math.Sqrt(mag1) * (float)Math.Sqrt(mag2));
    }

    private class DocumentChunk 
    { 
        public string Text { get; set; } = ""; 
        public float[] Embedding { get; set; } = Array.Empty<float>(); 
    }
}