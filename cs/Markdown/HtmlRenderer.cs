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
            var inner = RenderInlines(block.Inlines, block.Type);

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

    private static string RenderInlines(IReadOnlyList<Node> inlines, BlockType context)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < inlines.Count; i++)
        {
            var node = inlines[i];
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
                    if (context == BlockType.Heading)
                    {
                        sb.Append("<strong>")
                          .Append(node.Text ?? string.Empty);

                        var j = i + 1;
                        while (j < inlines.Count)
                        {
                            var n = inlines[j];
                            if (n.Type == NodeType.Em)
                            {
                                sb.Append("<em>")
                                  .Append(n.Text ?? string.Empty)
                                  .Append("</em>");
                                j++;
                                continue;
                            }
                            if (n.Type == NodeType.Strong)
                            {
                                sb.Append(n.Text ?? string.Empty);
                                j++;
                                continue;
                            }
                            break;
                        }

                        sb.Append("</strong>");
                        i = j - 1;
                    }
                    else
                    {
                        sb.Append("<strong>")
                          .Append(node.Text ?? string.Empty)
                          .Append("</strong>");
                    }
                    break;
            }
        }

        return sb.ToString();
    }
}
