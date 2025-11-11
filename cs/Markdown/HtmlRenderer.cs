namespace Markdown;

using Markdown.Entities;
using System.Text;

public class HtmlRenderer
{
    public string Render(IReadOnlyList<Block> blocks)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            var inner = RenderInlines(block.Inlines);

            switch (block.Type)
            {
                case BlockType.Heading:
                    sb.Append("<h1>").Append(inner).Append("</h1>");
                    break;

                case BlockType.Paragraph:
                    sb.Append("<p>").Append(inner).Append("</p>");
                    break;
            }

            if (i < blocks.Count - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string RenderInlines(IReadOnlyList<Node> inlines)
    {
        var sb = new StringBuilder();

        foreach (var node in inlines)
        {
            switch (node.Type)
            {
                case NodeType.Text:
                    sb.Append(node.Text ?? string.Empty);
                    break;

                case NodeType.Em:
                    sb.Append("<em>")
                      .Append(node.Text ?? string.Empty)
                      .Append("</em>");
                    break;

                case NodeType.Strong:
                    sb.Append("<strong>")
                      .Append(node.Text ?? string.Empty)
                      .Append("</strong>");
                    break;
            }
        }

        return sb.ToString();
    }
}
