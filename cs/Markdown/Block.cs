namespace Markdown;

public class Block(string rawText, BlockType type)
{
    public string RawText { get; } = rawText;

    public BlockType Type { get; } = type;

    public IReadOnlyList<Node> Inlines { get; set; } = [];
}

public enum BlockType
{
    Heading,
    Paragraph
}