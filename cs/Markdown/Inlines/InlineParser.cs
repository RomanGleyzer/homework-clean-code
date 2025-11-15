using Markdown.Entities;
using System.Text;
using static Markdown.Inlines.InlineSyntax;

namespace Markdown.Inlines;

public class InlineParser
{
    public IReadOnlyList<Node> Parse(string text)
    {
        var input = PreprocessEscapes(text);

        var nodes = new List<Node>();
        var buffer = new StringBuilder();

        var processor = new InlineMarkerProcessor(input, nodes, buffer);

        for (int i = 0; i < input.Length;)
        {
            if (input[i] == Underscore)
            {
                var underscoresAmount = CountSequentialUnderscores(input, i);
                if (underscoresAmount >= 2)
                {
                    if (processor.TryHandleStrong(ref i)) continue;
                }
                else
                {
                    if (processor.TryHandleEm(ref i)) continue;
                }
            }

            var ch = input[i];
            if (char.IsWhiteSpace(ch))
                processor.OnWhitespace();

            buffer.Append(ch);
            i += 1;
        }

        processor.FinalizeUnclosedMarkers();

        buffer.CommitText(nodes);
        TranslatePlaceholdersBack(nodes);
        nodes.MergeTextNodes();

        return nodes;
    }

    private static string PreprocessEscapes(string text)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == Escape)
            {
                if (i + 1 < text.Length)
                {
                    var nextChar = text[i + 1];

                    if (nextChar == Underscore)
                    {
                        sb.Append(PlaceholderUnderscore);
                    }
                    else if (nextChar == Escape)
                    {
                        var afterPair = i + 2 < text.Length ? text[i + 2] : '\0';
                        if (afterPair == Underscore || afterPair == '#')
                            sb.Append(PlaceholderBackslash);
                        else
                            sb.Append(Escape).Append(Escape);
                    }
                    else if (nextChar == '#')
                    {
                        sb.Append(PlaceholderHash);
                    }
                    else
                    {
                        sb.Append(Escape).Append(nextChar);
                    }
                    
                    i++;
                }
                else
                {
                    sb.Append(Escape);
                }
            }
            else
            {
                sb.Append(text[i]);
            }
        }
        return sb.ToString();
    }

    private static void TranslatePlaceholdersBack(List<Node> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var restoredText = RestoreTextPlaceholders(nodes[i].Text) ?? string.Empty;
            nodes[i] = new Node(restoredText, nodes[i].Type);
        }
    }

    private static string? RestoreTextPlaceholders(string? text)
    {
        return text?
            .Replace(PlaceholderUnderscore, Underscore)
            .Replace(PlaceholderBackslash, Escape)
            .Replace(PlaceholderHash, Hash);
    }

    private static int CountSequentialUnderscores(string text, int position)
    {
        var length = 0;
        while (position + length < text.Length && text[position + length] == Underscore)
            length++;
        return length;
    }
}
