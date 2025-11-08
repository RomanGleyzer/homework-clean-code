using FluentAssertions;
using System.Text;

namespace Markdown.Tests;

[TestFixture]
public class InlineParserTest
{
    private InlineParser _parser = null!;
    private BlockSegmenter _segmenter = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new InlineParser();
        _segmenter = new BlockSegmenter();
    }

    private static void ShouldBeInlines(IReadOnlyList<Node> nodes, params (NodeType type, string text)[] expected)
    {
        nodes.Count.Should().Be(expected.Length, "кол-во узлов должно совпадать");
        for (int i = 0; i < expected.Length; i++)
        {
            nodes[i].Type.Should().Be(expected[i].type, $"тип узла #{i}");
            (nodes[i].Text ?? string.Empty).Should().Be(expected[i].text, $"текст узла #{i}");
        }
    }

    private IReadOnlyList<Block> RunPipeline(string text)
    {
        var blocks = _segmenter.Segment(text);
        foreach (var block in blocks)
            block.Inlines = _parser.Parse(block.RawText).ToList();
        return blocks;
    }

    private static string ToDebugString(IReadOnlyList<Block> blocks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Blocks [");
        foreach (var b in blocks)
        {
            sb.AppendLine("  Block {");
            sb.AppendLine($"    RawText: \"{b.RawText}\",");
            sb.AppendLine("    Inlines: [");
            for (int i = 0; i < b.Inlines.Count; i++)
            {
                var n = b.Inlines[i];
                var type = n.Type switch
                {
                    NodeType.Text => "Text",
                    NodeType.Em => "Em",
                    NodeType.Strong => "Strong",
                    _ => n.Type.ToString()
                };
                sb.AppendLine($"      {type}(\"{n.Text}\"){(i < b.Inlines.Count - 1 ? "," : "")}");
            }
            sb.AppendLine("    ]");
            sb.AppendLine("  }");
        }
        sb.Append("]");
        return sb.ToString();
    }

    private static void ShouldBeBlocks(IReadOnlyList<Block> blocks, string expected)
    {
        var actual = ToDebugString(blocks);
        actual.Should().Be(expected);
    }

    [Test]
    public void Em_Simple_FromSpec()
    {
        var nodes = _parser.Parse("Текст, _окруженный с двух сторон_ одинарными символами подчерка");
        ShouldBeInlines(nodes,
            (NodeType.Text, "Текст, "),
            (NodeType.Em, "окруженный с двух сторон"),
            (NodeType.Text, " одинарными символами подчерка"));
    }

    [Test]
    public void Em_WholeSentence_FromSpec()
    {
        var nodes = _parser.Parse("Текст, <em>окруженный с двух сторон</em> одинарными символами подчерка, должен помещаться в HTML-тег <em>.");
        ShouldBeInlines(nodes, (NodeType.Text, "Текст, <em>окруженный с двух сторон</em> одинарными символами подчерка, должен помещаться в HTML-тег <em>."));
    }

    [Test]
    public void Em_PartOfWord_Start_Middle_End_FromSpec()
    {
        var nodes = _parser.Parse("Однако выделять часть слова они могут: и в _нач_але, и в сер_еди_не, и в кон_це._");
        ShouldBeInlines(nodes,
            (NodeType.Text, "Однако выделять часть слова они могут: и в "),
            (NodeType.Em, "нач_але, и в сер_еди_не, и в кон_це."));
    }

    [Test]
    public void Strong_Simple_FromSpec()
    {
        var nodes = _parser.Parse("__Выделенный двумя символами текст__ должен становиться полужирным с помощью тега <strong>.");
        ShouldBeInlines(nodes,
            (NodeType.Strong, "Выделенный двумя символами текст"),
            (NodeType.Text, " должен становиться полужирным с помощью тега <strong>."));
    }

    [Test]
    public void Escaping_Simple_FromSpec()
    {
        var nodes = _parser.Parse(@"\_Вот это\_, не должно выделиться тегом <em>.");
        ShouldBeInlines(nodes, (NodeType.Text, "_Вот это_, не должно выделиться тегом <em>."));
    }

    [Test]
    public void Escaping_CharDisappearsOnlyWhenEscapesSomething_FromSpec()
    {
        var nodes = _parser.Parse(@"Здесь сим\волы экранирования\ \должны остаться.\");
        ShouldBeInlines(nodes, (NodeType.Text, @"Здесь сим\волы экранирования\ \должны остаться.\"));
    }

    [Test]
    public void Escaping_EscapeItself_FromSpec()
    {
        var nodes = _parser.Parse(@"Символ экранирования тоже можно экранировать: \\_вот это будет выделено тегом_ <em>");
        ShouldBeInlines(nodes,
            (NodeType.Text, @"Символ экранирования тоже можно экранировать: \"),
            (NodeType.Em, "вот это будет выделено тегом"),
            (NodeType.Text, " <em>"));
    }

    [Test]
    public void EmInsideStrong_Works_FromSpec()
    {
        var nodes = _parser.Parse("Внутри __двойного выделения _одинарное_ тоже__ работает.");
        ShouldBeInlines(nodes,
            (NodeType.Text, "Внутри "),
            (NodeType.Strong, "двойного выделения "),
            (NodeType.Em, "одинарное"),
            (NodeType.Strong, " тоже"),
            (NodeType.Text, " работает."));
    }

    [Test]
    public void StrongInsideEm_NotWorking_FromSpec()
    {
        var nodes = _parser.Parse("Но не наоборот — внутри _одинарного __двойное__ не_ работает.");
        ShouldBeInlines(nodes,
            (NodeType.Text, "Но не наоборот — внутри "),
            (NodeType.Em, "одинарного __двойное__ не"),
            (NodeType.Text, " работает."));
    }

    [Test]
    public void UnderscoreBetweenDigits_NotMarkup_FromSpec()
    {
        var nodes = _parser.Parse("Подчерки внутри текста c цифрами_12_3 не считаются выделением и должны оставаться символами подчерка.");
        ShouldBeInlines(nodes, (NodeType.Text, "Подчерки внутри текста c цифрами_12_3 не считаются выделением и должны оставаться символами подчерка."));
    }

    [Test]
    public void AcrossWords_NotWorking_FromSpec()
    {
        var nodes = _parser.Parse("В то же время выделение в ра_зных сл_овах не работает.");
        ShouldBeInlines(nodes, (NodeType.Text, "В то же время выделение в ра_зных сл_овах не работает."));
    }

    [Test]
    public void UnpairedWithinParagraph_NotMarkup_FromSpec()
    {
        var nodes = _parser.Parse("_Непарные символы в рамках одного абзаца не считаются выделением.");
        ShouldBeInlines(nodes, (NodeType.Text, "_Непарные символы в рамках одного абзаца не считаются выделением."));
    }

    [Test]
    public void OpeningUnderscoreMustBeFollowedByNonSpace_FromSpec()
    {
        var nodes = _parser.Parse("За подчерками, начинающими выделение, должен следовать непробельный символ. Иначе эти_ подчерки_ не считаются выделением и остаются просто символами подчерка.");
        ShouldBeInlines(nodes, (NodeType.Text, "За подчерками, начинающими выделение, должен следовать непробельный символ. Иначе эти_ подчерки_ не считаются выделением и остаются просто символами подчерка."));
    }

    [Test]
    public void ClosingUnderscoreMustFollowNonSpace_FromSpec()
    {
        var nodes = _parser.Parse("эти _подчерки _ не считаются окончанием выделения");
        ShouldBeInlines(nodes, (NodeType.Text, "эти _подчерки _ не считаются окончанием выделения"));
    }

    [Test]
    public void CrossingStrongAndEm_NoneCounts_FromSpec()
    {
        var nodes = _parser.Parse("В случае __пересечения _двойных__ и одинарных_ подчерков ни один из них не считается выделением.");
        ShouldBeInlines(nodes, (NodeType.Text, "В случае __пересечения _двойных__ и одинарных_ подчерков ни один из них не считается выделением."));
    }

    [Test]
    public void EmptyBetweenDelimiters_FourUnderscores_FromSpec()
    {
        var nodes = _parser.Parse("Если внутри подчерков пустая строка ____, то они остаются символами подчерка.");
        ShouldBeInlines(nodes, (NodeType.Text, "Если внутри подчерков пустая строка ____, то они остаются символами подчерка."));
    }

    [Test]
    public void ClosingUnderscore_PrecededBySpace_NotClosing()
    {
        var nodes = _parser.Parse("a _b _ c");
        ShouldBeInlines(nodes, (NodeType.Text, "a _b _ c"));
    }

    [Test]
    public void Heading_Paragraphs_Pipeline_FromSpec()
    {
        var input = "# Заголовок __с _разными_ символами__";
        var blocks = RunPipeline(input);

        var expected =
            @"Blocks [
              Block {
                RawText: ""Заголовок __с _разными_ символами__"",
                Inlines: [
                  Strong(""с ""),
                  Em(""разными""),
                  Strong("" символами"")
                ]
              }
            ]";

        ShouldBeBlocks(blocks, expected);
    }

    [Test]
    public void Pipeline_Italic_Simple_BlockFormat()
    {
        var input = "сер_еди_на тест";
        var blocks = RunPipeline(input);

        var expected =
            @"Blocks [
              Block {
                RawText: ""сер_еди_на тест"",
                Inlines: [
                  Text(""сер""),
                  Em(""еди""),
                  Text(""на тест"")
                ]
              }
            ]";

        ShouldBeBlocks(blocks, expected);
    }

    [Test]
    public void Pipeline_StrongWithInnerEm_BlockFormat()
    {
        var input = "__с _разными_ символами__";
        var blocks = RunPipeline(input);

        var expected =
            @"Blocks [
              Block {
                RawText: ""__с _разными_ символами__"",
                Inlines: [
                  Strong(""с ""),
                  Em(""разными""),
                  Strong("" символами"")
                ]
              }
            ]";

        ShouldBeBlocks(blocks, expected);
    }
}
