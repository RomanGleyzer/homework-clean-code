using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Markdown.Tests;

[TestFixture]
public class InlineParserTests
{
    private InlineParser _parser = null!;

    [SetUp]
    public void SetUp() => _parser = new InlineParser();

    private static void ShouldBe(IReadOnlyList<Node> nodes, params (NodeType type, string text)[] expected)
    {
        nodes.Count.Should().Be(expected.Length, "кол-во узлов должно совпадать");
        for (int i = 0; i < expected.Length; i++)
        {
            nodes[i].Type.Should().Be(expected[i].type, $"тип узла #{i}");
            (nodes[i].Text ?? string.Empty).Should().Be(expected[i].text, $"текст узла #{i}");
        }
    }

    [Test]
    public void PlainText_NoMarkup()
    {
        var nodes = _parser.Parse("просто текст");
        ShouldBe(nodes, (NodeType.Text, "просто текст"));
    }

    [Test]
    public void Em_Simple_InBetween()
    {
        var nodes = _parser.Parse("a _x_ b");
        ShouldBe(nodes,
            (NodeType.Text, "a "),
            (NodeType.Em, "x"),
            (NodeType.Text, " b"));
    }

    [Test]
    public void Em_PartOfWord_Begin_Middle_End()
    {
        var nodes = _parser.Parse("нач_ало сер_еди_на кон_це");
        ShouldBe(nodes,
            (NodeType.Text, "нач"),
            (NodeType.Em, "ало"),
            (NodeType.Text, " сер"),
            (NodeType.Em, "еди"),
            (NodeType.Text, "на кон"),
            (NodeType.Em, "це"));
    }

    [Test]
    public void Em_Unpaired_Underscore_ShouldStayText()
    {
        var nodes = _parser.Parse("_Непарные символы");
        ShouldBe(nodes, (NodeType.Text, "_Непарные символы"));
    }

    [Test]
    public void Em_OpeningFollowedBySpace_NotOpening()
    {
        var nodes = _parser.Parse("эти_ подчерки_");
        ShouldBe(nodes, (NodeType.Text, "эти_ подчерки_"));
    }

    [Test]
    public void Em_ClosingPrecededBySpace_NotClosing()
    {
        var nodes = _parser.Parse("эти _подчерки");
        ShouldBe(nodes, (NodeType.Text, "эти _подчерки"));
    }

    [Test]
    public void Em_AcrossWords_NotWorking()
    {
        var nodes = _parser.Parse("в ра_зных сл_овах не работает");
        ShouldBe(nodes, (NodeType.Text, "в ра_зных сл_овах не работает"));
    }

    [Test]
    public void Em_InsideDigits_NotMarkup()
    {
        var nodes = _parser.Parse("цифры_12_3");
        ShouldBe(nodes, (NodeType.Text, "цифры_12_3"));
    }

    [Test]
    public void Em_EmptyBetweenDelimiters_FourUnderscores_StayText()
    {
        var nodes = _parser.Parse("____");
        ShouldBe(nodes, (NodeType.Text, "____"));
    }

    [Test]
    public void Strong_Simple()
    {
        var nodes = _parser.Parse("a __x__ b");
        ShouldBe(nodes,
            (NodeType.Text, "a "),
            (NodeType.Strong, "x"),
            (NodeType.Text, " b"));
    }

    [Test]
    public void Strong_EmptyBetweenDelimiters_ShouldStayText()
    {
        var nodes = _parser.Parse("____");
        ShouldBe(nodes, (NodeType.Text, "____"));
    }

    [Test]
    public void Em_Inside_Strong_Works()
    {
        var nodes = _parser.Parse("__с _разными_ символами__");
        ShouldBe(nodes,
            (NodeType.Strong, "с "),
            (NodeType.Em, "разными"),
            (NodeType.Strong, " символами"));
    }

    [Test]
    public void Strong_Inside_Em_NotWorking()
    {
        var nodes = _parser.Parse("_a __b__ c_");
        ShouldBe(nodes,
            (NodeType.Em, "a __b__ c"));
    }

    [Test]
    public void Crossing_Em_And_Strong_NoMarkup()
    {
        var nodes = _parser.Parse("__a_b__");
        ShouldBe(nodes, (NodeType.Text, "__a_b__"));
    }

    [Test]
    public void Crossing_Em_And_Strong_OtherPattern_NoMarkup()
    {
        var nodes = _parser.Parse("_a__b_");
        ShouldBe(nodes, (NodeType.Text, "_a__b_"));
    }

    [Test]
    public void Escape_Underscore_ShouldStayLiteral()
    {
        var nodes = _parser.Parse(@"\_это\_ не курсив");
        ShouldBe(nodes, (NodeType.Text, "_это_ не курсив"));
    }

    [Test]
    public void Escape_Backslash_DoubleSlash_To_SingleLiteral()
    {
        var nodes = _parser.Parse(@"сим\\вол");
        ShouldBe(nodes, (NodeType.Text, @"сим\вол"));
    }

    [Test]
    public void Escape_Char_Alone_At_End_ShouldStay()
    {
        var nodes = _parser.Parse(@"abc\");
        ShouldBe(nodes, (NodeType.Text, @"abc\"));
    }

    [Test]
    public void Escape_Backslash_Then_Em_ShouldWork()
    {
        var nodes = _parser.Parse(@"\\_x_");
        ShouldBe(nodes,
            (NodeType.Text, @"\"),
            (NodeType.Em, "x"));
    }

    [Test]
    public void Escape_Hash_ShouldRestoreHash()
    {
        var nodes = _parser.Parse(@"Hello \# world");
        ShouldBe(nodes, (NodeType.Text, "Hello # world"));
    }

    [Test]
    public void Mixed_Text_Em_Strong_With_Escapes()
    {
        var nodes = _parser.Parse(@"Текст __с \__экранами__ и _кучей\_символов_.");

        ShouldBe(nodes,
            (NodeType.Text, "Текст "),
            (NodeType.Strong, "с __экранами"),
            (NodeType.Text, " и "),
            (NodeType.Em, "кучей_символов"),
            (NodeType.Text, "."));
    }

    [Test]
    public void Multiple_Text_Nodes_ShouldBeCombined()
    {
        var nodes = _parser.Parse("a _b_ c");
        nodes.Count.Should().Be(3);
        ShouldBe(nodes,
            (NodeType.Text, "a "),
            (NodeType.Em, "b"),
            (NodeType.Text, " c"));
    }

    [Test]
    public void Spec_Example_Inline_From_Heading()
    {
        var nodes = _parser.Parse("Заголовок с _разными_ символами");
        ShouldBe(nodes,
            (NodeType.Text, "Заголовок с "),
            (NodeType.Em, "разными"),
            (NodeType.Text, " символами"));
    }
}
