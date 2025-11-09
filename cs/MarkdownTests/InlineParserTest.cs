using FluentAssertions;
using System.Text;

namespace Markdown.Tests;

[TestFixture]
public class InlineParserTest
{
    private InlineParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new InlineParser();
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
    public void Em_PartOfWord_Start_Middle_End_FromSpec()
    {
        var nodes = _parser.Parse("Однако выделять часть слова они могут: и в _нач_але, и в сер_еди_не, и в кон_це._");
        ShouldBeInlines(nodes,
            (NodeType.Text, "Однако выделять часть слова они могут: и в "),
            (NodeType.Em, "нач"),
            (NodeType.Text, "але, и в сер"),
            (NodeType.Em, "еди"),
            (NodeType.Text, "не, и в кон"),
            (NodeType.Em, "це.")
        );
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
    public void UnpairedWithinParagraph_NotMarkup_FromSpec()
    {
        var nodes = _parser.Parse("__Непарные_ символы в рамках одного абзаца не считаются выделением.");
        ShouldBeInlines(nodes, (NodeType.Text, "__Непарные_ символы в рамках одного абзаца не считаются выделением."));
    }

    [Test]
    public void CrossingStrongAndEm_NoneCounts_FromSpec()
    {
        var nodes = _parser.Parse("В случае __пересечения _двойных__ и одинарных_ подчерков ни один из них не считается выделением.");
        ShouldBeInlines(nodes, (NodeType.Text, "В случае __пересечения _двойных__ и одинарных_ подчерков ни один из них не считается выделением."));
    }

    [Test]
    public void Strong_PartOfWord_Start_Middle_End()
    {
        var nodes = _parser.Parse("и в __нач__але, и в сер__еди__не, и в кон__це__.");
        ShouldBeInlines(nodes,
            (NodeType.Text, "и в "),
            (NodeType.Strong, "нач"),
            (NodeType.Text, "але, и в сер"),
            (NodeType.Strong, "еди"),
            (NodeType.Text, "не, и в кон"),
            (NodeType.Strong, "це"),
            (NodeType.Text, "."));
    }

    [Test]
    public void Escaping_DoubleStrongToken_AllText()
    {
        var nodes = _parser.Parse(@"\__стронг__");
        ShouldBeInlines(nodes, (NodeType.Text, "__стронг__"));
    }

    [Test]
    public void Delimiters_WithPunctuation_AfterAndBefore()
    {
        var left = _parser.Parse("_a_,");
            ShouldBeInlines(left,
            (NodeType.Em, "a"),
            (NodeType.Text, ","));

        var right = _parser.Parse("._b_");
            ShouldBeInlines(right,
            (NodeType.Text, "."),
            (NodeType.Em, "b"));
    }

    [Test]
    public void Em_And_Strong_AtStringEdges()
    {
        var start = _parser.Parse("_abc_ в начале");
        ShouldBeInlines(start,
            (NodeType.Em, "abc"),
            (NodeType.Text, " в начале"));

        var end = _parser.Parse("в конце __abc__");
        ShouldBeInlines(end,
            (NodeType.Text, "в конце "),
            (NodeType.Strong, "abc"));
    }

    [Test]
    public void Punctuation_BeforeOpening_Underscore_StillOpens()
    {
        var nodes = _parser.Parse("!_a_");
        ShouldBeInlines(nodes,
            (NodeType.Text, "!"),
            (NodeType.Em, "a"));
    }

    [Test]
    public void Digits_WithStrong_NotMarkup()
    {
        var nodes = _parser.Parse("цифры__12__3");
        ShouldBeInlines(nodes,
            (NodeType.Text, "цифры__12__3"));
    }

    [Test]
    public void Escape_DoubleSlash_BeforeStrong_StrongWorks()
    {
        var nodes = _parser.Parse(@"\\__a__");
        ShouldBeInlines(nodes,
            (NodeType.Text, @"\"),
            (NodeType.Strong, "a"));
    }
}
