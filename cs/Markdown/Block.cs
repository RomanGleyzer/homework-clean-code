using Markdown.Interfaces;

namespace Markdown;

public class Block(string rawText, BlockType type)
{
    public string RawText { get; } = rawText;

    public BlockType Type { get; } = type;

    public IReadOnlyList<INode> Inlines { get; set; } = [];
}

public enum BlockType
{
    Heading,
    Paragraph
}