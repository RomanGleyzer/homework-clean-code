namespace Markdown.Nodes;

public interface INode
{
    // Далее планируется добавление трех наследников: Text, Em, Strong

    public string? Text { get; }

    public IReadOnlyList<INode> Children { get; }
}
