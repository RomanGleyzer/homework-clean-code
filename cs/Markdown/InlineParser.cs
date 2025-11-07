using System.Text;

namespace Markdown;

public class InlineParser
{
    /// <summary>
    /// Разбирает строку с разметкой и возвращает список узлов
    /// </summary>
    public IReadOnlyList<Node> Parse(string text)
    {
        var preprocessedText = PreprocessEscapes(text);

        var nodes = new List<Node>();

        var textBuffer = new StringBuilder();

        var strongOpen = false;
        var emOpen = false;
        var strongHasSpace = false;
        var emHasSpace = false;
        var strongTextCheckpoint = 0;
        var emTextCheckpoint = 0;

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
                    else if (strongOpen && CanOpenOrClose(preprocessedText, i, 2, false) && !strongHasSpace && textBuffer.Length > strongTextCheckpoint)
                    {
                        // Закрываем сильное выделение
                        var content = textBuffer.ToString(strongTextCheckpoint, textBuffer.Length - strongTextCheckpoint);
                        textBuffer.Length = strongTextCheckpoint;
                        nodes.Add(new Node(content, NodeType.Strong));
                        strongOpen = false;
                        strongHasSpace = false;
                        i += 2;
                        continue;
                    }
                    else if (!strongOpen && CanOpenOrClose(preprocessedText, i, 2, true))
                    {
                        strongOpen = true;
                        strongTextCheckpoint = textBuffer.Length;
                        strongHasSpace = false;
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
                    if (emOpen && CanOpenOrClose(preprocessedText, i, 1, false) && !emHasSpace && textBuffer.Length > emTextCheckpoint)
                    {
                        var content = textBuffer.ToString(emTextCheckpoint, textBuffer.Length - emTextCheckpoint);
                        textBuffer.Length = emTextCheckpoint;
                        nodes.Add(new Node(content, NodeType.Em));
                        emOpen = false;
                        emHasSpace = false;
                        i += 1;
                        continue;
                    }
                    else if (!emOpen && CanOpenOrClose(preprocessedText, i, 1, true))
                    {
                        emOpen = true;
                        emTextCheckpoint = textBuffer.Length;
                        emHasSpace = false;
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
                    if (strongOpen)
                        strongHasSpace = true;
                    if (emOpen)
                        emHasSpace = true;
                }
                textBuffer.Append(ch);
                i++;
            }
        }

        // Проверяем, остались ли открытые выделения
        if (strongOpen || emOpen)
        {
            var insterts = new List<(int index, string text)>(2)
            {
                (strongTextCheckpoint, strongOpen ? "__" : ""),
                (emTextCheckpoint, emOpen ? "_" : "")
            };

            insterts.Sort((a, b) => b.index.CompareTo(a.index));

            var shift = 0;
            foreach (var insert in insterts)
            {
                textBuffer.Insert(insert.index + shift, insert.text);
                shift += insert.text.Length;
            }

            strongOpen = false;
            emOpen = false;
            strongHasSpace = false;
            emHasSpace = false;
        }

        AddTextIfBufferNotEmpty(nodes, textBuffer);
        RestorePlaceholders(nodes);
        CombineNeighborTextNodes(nodes);

        return nodes;
    }

    /// <summary>
    /// Заменяет экранированные символы (\_, \#, \\) на временные маркеры
    /// </summary>
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
                    if (nextChar == '_')
                    {
                        sb.Append('\uE000'); // ESC_UNDERSCORE
                        i++;
                    }
                    else if (nextChar == '\\')
                    {
                        sb.Append('\uE001'); // ESC_BACKSLASH
                        i++;
                    }
                    else if (nextChar == '#')
                    {
                        sb.Append('\uE002'); // ESC_HASH
                        i++;
                    }
                    else
                    {
                        sb.Append("\\");
                        sb.Append(nextChar);
                        i++;
                    }
                }
                else
                {
                    sb.Append('\\');
                }
            }
            else
            {
                sb.Append(text[i]);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Длина последовательности символов подчеркивания
    /// </summary>
    private static int GetUnderlineLength(string text, int position)
    {
        var length = 0;
        while (position + length < text.Length && text[position + length] == '_')
        {
            length++;
        }
        return length;
    }

    /// <summary>
    /// Может ли текущий символ подчеркивания открыть или закрыть выделение
    /// </summary>
    private static bool CanOpenOrClose(string text, int position, int length, bool open)
    {
        var prevChar = (position - 1 >= 0) ? text[position - 1] : ' ';
        var nextChar = position + length < text.Length ? text[position + length] : ' ';

        if (open)
        {
            if (char.IsWhiteSpace(nextChar))
                return false;
        }
        else
        {
            if (char.IsWhiteSpace(prevChar))
                return false;
        }

        if (char.IsDigit(prevChar) && char.IsDigit(nextChar))
            return false;

        return true;
    }

    /// <summary>
    /// Добавляет текст из буфера в список узлов, если буфер не пуст
    /// </summary>
    private static void AddTextIfBufferNotEmpty(List<Node> nodes, StringBuilder textBuffer)
    {
        if (textBuffer.Length > 0)
        {
            nodes.Add(new Node(textBuffer.ToString(), NodeType.Text));
            textBuffer.Clear();
        }
    }

    /// <summary>
    /// Возвращает временный маркер в исходный символ
    /// </summary>
    private static void RestorePlaceholders(List<Node> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var restoredText = Restore(nodes[i].Text) ?? string.Empty;
            nodes[i] = new Node(restoredText, nodes[i].Type);
        }
    }

    /// <summary>
    /// Восстанавливает экранированные символы из временных маркеров
    /// </summary>
    private static string? Restore(string? text)
    {
        return text?
            .Replace('\uE000', '_')
            .Replace('\uE001', '\\')
            .Replace('\uE002', '#');
    }

    /// <summary>
    /// Объединяет соседние текстовые узлы в один
    /// </summary>
    private static void CombineNeighborTextNodes(List<Node> nodes)
    {
        for (int i = 0; i < nodes.Count - 1;)
        {
            if (nodes[i].Type == NodeType.Text && nodes[i + 1].Type == NodeType.Text)
            {
                var combinedText = nodes[i].Text + nodes[i + 1].Text;
                nodes[i] = new Node(combinedText, NodeType.Text);
                nodes.RemoveAt(i + 1);
            }
            else
            {
                i++;
            }
        }
    }
}
