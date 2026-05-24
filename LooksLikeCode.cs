public class LooksLikeCode
{
    public bool IsCode { get; }

    public LooksLikeCode(string c)
    {
        IsCode = CheckCode(c);
    }

    private bool CheckCode(string text)
    {
        return
            text.Contains("{") ||
            text.Contains("}") ||
            text.Contains(";") ||
            text.Contains("class ") ||
            text.Contains("public ") ||
            text.Contains("void ") ||
            text.Contains("function ") ||
            text.Contains("=>");
    }
}