using Markdown.Inlines;

namespace Markdown;

public class Md(BlockSegmenter segmenter, InlineParser parser, HtmlRenderer renderer)
{
    public string Render(string text)
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
