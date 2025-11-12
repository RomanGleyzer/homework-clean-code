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

    private static string[] SplitToParagraphs(string text)
    {
        return text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);
    }

    private static BlockType IdentifyBlockType(string paragraph)
    {
        return paragraph.StartsWith("# ")
            ? BlockType.Heading
            : BlockType.Paragraph;
    }

    private static string ClearRawText(string rawText, BlockType blockType)
    {
        return blockType switch
        {
            BlockType.Heading => rawText.TrimStart('#', ' '),
            BlockType.Paragraph => rawText,
            _ => throw new ArgumentOutOfRangeException(nameof(blockType), $"Неизвестный тип блока: {blockType}")
        };
    }

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
