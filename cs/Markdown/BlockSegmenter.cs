using Markdown.Entities;

namespace Markdown;

public class BlockSegmenter
{
    public IReadOnlyList<Block> Segment(string text)
    {
        var paragraphs = SplitToParagraphs(text);
        var blocks = new List<Block>();

        foreach (var paragraph in paragraphs)
        {
            var blockType = IdentifyBlockType(paragraph);
            var rawText = ClearRawText(paragraph, blockType);
            var block = CreateBlock(rawText, blockType);
            blocks.Add(block);
        }

        return blocks;
    }

    /// <summary>
    /// Делит текст на абзацы
    /// </summary>
    private static string[] SplitToParagraphs(string text)
    {
        return text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Определяет тип абзаца: обычный или заголовок
    /// </summary>
    private static BlockType IdentifyBlockType(string paragraph)
    {
        return paragraph.StartsWith("# ")
            ? BlockType.Heading
            : BlockType.Paragraph;
    }

    /// <summary>
    /// Очищает текст от управляющих символов в зависимости от типа блока (например, убирает символы '#' для заголовков)
    /// </summary>
    private static string ClearRawText(string rawText, BlockType blockType)
    {
        return blockType switch
        {
            BlockType.Heading => rawText.TrimStart('#', ' '),
            BlockType.Paragraph => rawText,
            _ => throw new ArgumentOutOfRangeException(nameof(blockType), $"Неизвестный тип блока: {blockType}")
        };
    }

    /// <summary>
    /// Создает блок заданного типа из текста
    /// </summary>
    private static Block CreateBlock(string rawText, BlockType blockType)
    {
        return blockType switch
        {
            BlockType.Heading => new Block(rawText, blockType),
            BlockType.Paragraph => new Block(rawText, blockType),
            _ => throw new ArgumentOutOfRangeException(nameof(blockType), $"Неизвестный тип блока: {blockType}")
        };
    }
}
