using System.Text;

namespace Markdown;

public class InlineParser
{
    public IReadOnlyList<Node> Parse(string text)
    {
        var preprocessedText = PreprocessEscapes(text);

        var nodes = new List<Node>();
        var textBuffer = new StringBuilder();

        var strongOpen = false;
        var emOpen = false;

        var strongTextCheckpoint = 0;
        var emTextCheckpoint = 0;

        var strongOpenedInsideWord = false;
        var emOpenedInsideWord = false;
        var strongSawWhitespace = false;
        var emSawWhitespace = false;

        for (int i = 0; i < preprocessedText.Length;)
        {
            if (preprocessedText[i] == '_')
            {
                var underlineLength = GetUnderlineLength(preprocessedText, i);

                if (underlineLength >= 2)
                {
                    if (emOpen)
                    {
                        textBuffer.Append("__");
                        i += 2;
                        continue;
                    }
                    else if (strongOpen && CanOpenOrClose(preprocessedText, i, 2, open: false)
                             && !CrossesWords(strongOpenedInsideWord, IsWordChar(NextChar(preprocessedText, i, 2)), strongSawWhitespace)
                             && textBuffer.Length > strongTextCheckpoint)
                    {
                        var content = textBuffer.ToString(strongTextCheckpoint, textBuffer.Length - strongTextCheckpoint);
                        textBuffer.Length = strongTextCheckpoint;

                        AddTextIfBufferNotEmpty(nodes, textBuffer);
                        nodes.Add(new Node(content, NodeType.Strong));

                        strongOpen = false;
                        strongSawWhitespace = false;
                        i += 2;
                        continue;
                    }
                    else if (!strongOpen && CanOpenOrClose(preprocessedText, i, 2, open: true))
                    {
                        strongOpen = true;
                        strongTextCheckpoint = textBuffer.Length;
                        strongOpenedInsideWord = IsWordChar(PrevChar(preprocessedText, i));
                        strongSawWhitespace = false;
                        i += 2;
                        continue;
                    }
                    else
                    {
                        textBuffer.Append("__");
                        i += 2;
                        continue;
                    }
                }
                else
                {
                    if (emOpen && CanOpenOrClose(preprocessedText, i, 1, open: false)
                        && !CrossesWords(emOpenedInsideWord, IsWordChar(NextChar(preprocessedText, i, 1)), emSawWhitespace)
                        && textBuffer.Length > emTextCheckpoint)
                    {
                        var emContent = textBuffer.ToString(emTextCheckpoint, textBuffer.Length - emTextCheckpoint);

                        if (strongOpen && strongTextCheckpoint < emTextCheckpoint)
                        {
                            var strongBeforeEm = textBuffer.ToString(strongTextCheckpoint, emTextCheckpoint - strongTextCheckpoint);
                            textBuffer.Length = strongTextCheckpoint;
                            AddTextIfBufferNotEmpty(nodes, textBuffer);
                            nodes.Add(new Node(strongBeforeEm, NodeType.Strong));
                            strongTextCheckpoint = 0;
                        }
                        else
                        {
                            textBuffer.Length = emTextCheckpoint;
                            AddTextIfBufferNotEmpty(nodes, textBuffer);
                        }

                        nodes.Add(new Node(emContent, NodeType.Em));

                        emOpen = false;
                        emSawWhitespace = false;
                        i += 1;
                        continue;
                    }

                    else if (!emOpen && CanOpenOrClose(preprocessedText, i, 1, open: true))
                    {
                        emOpen = true;
                        emTextCheckpoint = textBuffer.Length;
                        emOpenedInsideWord = IsWordChar(PrevChar(preprocessedText, i));
                        emSawWhitespace = false;
                        i += 1;
                        continue;
                    }
                    else
                    {
                        textBuffer.Append('_');
                        i += 1;
                        continue;
                    }
                }
            }
            else
            {
                var ch = preprocessedText[i];

                if (char.IsWhiteSpace(ch))
                {
                    if (strongOpen) strongSawWhitespace = true;
                    if (emOpen) emSawWhitespace = true;
                }

                textBuffer.Append(ch);
                i++;
            }
        }

        if (strongOpen || emOpen)
        {
            var inserts = new List<(int index, string text)>(2);
            if (strongOpen) inserts.Add((strongTextCheckpoint, "__"));
            if (emOpen) inserts.Add((emTextCheckpoint, "_"));
            inserts.Sort((a, b) => b.index.CompareTo(a.index));
            foreach (var insert in inserts)
                textBuffer.Insert(insert.index, insert.text);

            strongOpen = false;
            emOpen = false;
            strongSawWhitespace = false;
            emSawWhitespace = false;
        }

        AddTextIfBufferNotEmpty(nodes, textBuffer);
        RestorePlaceholders(nodes);
        CombineNeighborTextNodes(nodes);

        return nodes;
    }

    private static string PreprocessEscapes(string text)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\')
            {
                if (i + 1 < text.Length)
                {
                    var nextChar = text[i + 1];
                    if (nextChar == '_') { sb.Append('\uE000'); i++; }
                    else if (nextChar == '\\') { sb.Append('\uE001'); i++; }
                    else if (nextChar == '#') { sb.Append('\uE002'); i++; }
                    else { sb.Append('\\').Append(nextChar); i++; }
                }
                else sb.Append('\\');
            }
            else sb.Append(text[i]);
        }
        return sb.ToString();
    }

    private static int GetUnderlineLength(string text, int position)
    {
        var length = 0;
        while (position + length < text.Length && text[position + length] == '_') length++;
        return length;
    }

    private static bool CanOpenOrClose(string text, int position, int length, bool open)
    {
        var prevChar = PrevChar(text, position);
        var nextChar = NextChar(text, position, length);

        if (open)
        {
            if (char.IsWhiteSpace(nextChar)) return false;
        }
        else
        {
            if (char.IsWhiteSpace(prevChar)) return false;
        }

        if (char.IsDigit(prevChar) && char.IsDigit(nextChar)) return false;

        return true;
    }

    private static char PrevChar(string text, int position) =>
        (position - 1 >= 0) ? text[position - 1] : ' ';

    private static char NextChar(string text, int position, int length) =>
        position + length < text.Length ? text[position + length] : ' ';

    private static bool IsWordChar(char ch) => char.IsLetterOrDigit(ch);

    private static bool CrossesWords(bool openedInsideWord, bool closingInsideWord, bool sawWhitespace) =>
        openedInsideWord && closingInsideWord && sawWhitespace;

    private static void AddTextIfBufferNotEmpty(List<Node> nodes, StringBuilder textBuffer)
    {
        if (textBuffer.Length > 0)
        {
            nodes.Add(new Node(textBuffer.ToString(), NodeType.Text));
            textBuffer.Clear();
        }
    }

    private static void RestorePlaceholders(List<Node> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var restoredText = Restore(nodes[i].Text) ?? string.Empty;
            nodes[i] = new Node(restoredText, nodes[i].Type);
        }
    }

    private static string? Restore(string? text)
    {
        return text?
            .Replace('\uE000', '_')
            .Replace('\uE001', '\\')
            .Replace('\uE002', '#');
    }

    private static void CombineNeighborTextNodes(List<Node> nodes)
    {
        for (int i = 0; i < nodes.Count - 1;)
        {
            if (nodes[i].Type == NodeType.Text && nodes[i + 1].Type == NodeType.Text)
            {
                nodes[i] = new Node(nodes[i].Text + nodes[i + 1].Text, NodeType.Text);
                nodes.RemoveAt(i + 1);
            }
            else i++;
        }
    }
}
