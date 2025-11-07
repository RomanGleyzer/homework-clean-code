namespace Markdown.Interfaces;

public interface IBlock
{
    string RawText { get; }

    BlockType Type { get; }

    public IReadOnlyList<INode> Inlines { get; set; }
}

public enum BlockType
{
    Heading,
    Paragraph
}