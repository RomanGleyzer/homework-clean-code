namespace Markdown;

public class Md(Pipeline pipeline)
{
    public string Render(string markdownText)
    {
        return pipeline.Run(markdownText);
    }
}
