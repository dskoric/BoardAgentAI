using System.ComponentModel;
using Microsoft.SemanticKernel;
using BoardAgentService.Services;

namespace BoardAgentService.Plugins;

public class SearchPlugin
{
    private readonly RagService _ragService;

    public SearchPlugin(RagService ragService)
    {
        _ragService = ragService;
    }

    [KernelFunction("search_documents")]
    [Description("Pretražuje dokumente Odbora direktora. Koristi OVU funkciju uvek.")]
    public async Task<string> SearchAsync([Description("Upit za pretragu")] string query)
    {
        return await _ragService.SearchAsync(query);
    }
}