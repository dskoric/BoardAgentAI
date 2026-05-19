using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using BoardAgentService.Plugins;
using BoardAgentService.Services;

namespace BoardAgentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly GuardrailsService _guardrails;

        public ChatController(Kernel kernel, GuardrailsService guardrails)
        {
            _kernel = kernel;
            _guardrails = guardrails;
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            string safeQuery = _guardrails.MaskPii(request.Question);
            _kernel.ImportPluginFromType<SearchPlugin>("BoardSearch");

        var prompt = @"You are an assistant. THE TOOL WILL RETURN TEXT FROM THE DOCUMENT. 
Your task is to ALWAYS take the returned text and use it to answer. 
Never say 'I don't know' if the tool did not return 'No data'.
Question: {$input}";

            var settings = new PromptExecutionSettings 
            { 
                FunctionChoiceBehavior = FunctionChoiceBehavior.Required()
            };

            var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(settings) { { "input", safeQuery } });
            
            string finalAnswer = _guardrails.EnforceGrounding(result.ToString());
            return Ok(new { Answer = finalAnswer });
        }
    }

    public class ChatRequest { public string Question { get; set; } = ""; }
}