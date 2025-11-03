using Markdown.Nodes;

namespace Markdown.Blocks;

public interface IBlock
{
    // Далее планируется добавление двух наследников: HeadingBlock и ParagraphBlock

    string RawText { get; }

    public IReadOnlyList<INode> Inlines { get; }

    void SetInlines(IReadOnlyList<INode> inlines);
}
