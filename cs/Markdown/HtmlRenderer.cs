using Markdown.Interfaces;

namespace Markdown;

public class HtmlRenderer
{
    public string Render(IReadOnlyList<Block> blocks)
    {
        // TODO: реализовать метод
        throw new NotImplementedException();
    }

    // Далее будут добавлены методы для рендеринга блоков (h1, p) и рендеринга содержимого блоков (text, em, strong)
    // RenderHeading(HeadingBlock h), RenderParagraph(ParagraphBlock p), RenderInlines(IReadOnlyList<INode> nodes)
}
