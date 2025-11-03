using Markdown.Blocks;
using Markdown.Parsing;
using Markdown.Rendering;

namespace Markdown;

public class Pipeline(BlockSegmenter blockSegmenter, InlineParser parser, HtmlRenderer htmlRenderer)
{
    public string Run(string text)
    {
        var blocks = blockSegmenter.Segment(text);

        foreach (var block in blocks)
        {
            var inlines = parser.Parse(block.RawText);
            block.SetInlines(inlines);
        }

        var html = htmlRenderer.Render(blocks);

        return html;
    }
}
