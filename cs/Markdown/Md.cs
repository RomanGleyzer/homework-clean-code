namespace Markdown;

public class Md
{
    public string Render(string text, BlockSegmenter segmenter, InlineParser parser, HtmlRenderer renderer)
    {
        var blocks = segmenter.Segment(text);

        foreach (var block in blocks)
        {
            var inlines = parser.Parse(block.RawText);
            block.Inlines = inlines;
        }

        var html = renderer.Render(blocks);

        return html;
    }
}
