using Markdown.Entities;
using System.Text;

namespace Markdown.Inlines;

public static class InlineExtensions
{
    public static void CommitText(this StringBuilder buffer, List<Node> nodes)
    {
        if (buffer.Length <= 0) return;
        nodes.Add(new Node(buffer.ToString(), NodeType.Text));
        buffer.Clear();
    }

    public static void MergeTextNodes(this List<Node> nodes)
    {
        for (int i = 0; i < nodes.Count - 1;)
        {
            if (nodes[i].Type == NodeType.Text && nodes[i + 1].Type == NodeType.Text)
            {
                nodes[i] = new Node(nodes[i].Text + nodes[i + 1].Text, NodeType.Text);
                nodes.RemoveAt(i + 1);
            }
            else
            {
                i++;
            }
        }
    }

    public static void InsertFromEnd(this StringBuilder target, List<(int index, string text)> inserts)
    {
        if (inserts.Count == 0) return;

        inserts.Sort((a, b) => b.index.CompareTo(a.index));
        foreach (var (index, text) in inserts)
            target.Insert(index, text);
    }
}
