namespace BoardAgentService.Services;

public class GuardrailsService
{
    public string MaskPii(string query) 
        => System.Text.RegularExpressions.Regex.Replace(query, @"[\w\.-]+@[\w\.-]+\.\w+", "[MASKIRAN EMAIL]");
    
    public string EnforceGrounding(string answer)
    {
        if (!answer.Contains("[Izvor:") && !answer.ToLower().Contains("ne znam"))
            return answer + "\n\n⚠️ [SISTEMSKI BLOCK]: Odgovor nije referencirao izvor.";
        return answer;
    }
}
