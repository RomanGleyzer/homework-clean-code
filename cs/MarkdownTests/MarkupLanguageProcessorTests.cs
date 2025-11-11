using FluentAssertions;
using Markdown;
using Markdown.Inlines;

namespace MarkdownTests;

[TestFixture]
public class MarkupLanguageProcessorTests
{
    private static string RenderHtml(string input)
    {
        var md = new Md();
        return md.Render(input, new BlockSegmenter(), new InlineParser(), new HtmlRenderer());
    }

    private static void ShouldBeHtmlParagraph(string input, string innerHtml)
    {
        RenderHtml(input).Should().Be($"<p>{innerHtml}</p>");
    }

    private static void ShouldBeHtmlH1(string input, string innerHtml)
    {
        RenderHtml(input).Should().Be($"<h1>{innerHtml}</h1>");
    }

    [Test]
    public void ParseAndRender_EmSimple_ProducesExpectedInlinesAndHtml()
    {
        var input = "Текст, _окруженный с двух сторон_ одинарными символами подчерка";

        ShouldBeHtmlParagraph(input, "Текст, <em>окруженный с двух сторон</em> одинарными символами подчерка");
    }

    [Test]
    public void ParseAndRender_EmWholeSentenceFromSpec_ProducesPlainTextHtml()
    {
        var input = "Текст, <em>окруженный с двух сторон</em> одинарными символами подчерка, должен помещаться в HTML-тег <em>.";

        ShouldBeHtmlParagraph(input, "Текст, <em>окруженный с двух сторон</em> одинарными символами подчерка, должен помещаться в HTML-тег <em>.");
    }

    [Test]
    public void ParseAndRender_StrongSimple_ProducesExpectedInlinesAndHtml()
    {
        var input = "__Выделенный двумя символами текст__ должен становиться полужирным с помощью тега <strong>.";

        ShouldBeHtmlParagraph(input, "<strong>Выделенный двумя символами текст</strong> должен становиться полужирным с помощью тега <strong>.");
    }

    [Test]
    public void ParseAndRender_EscapingUnderscore_ProducesPlainText()
    {
        var input = @"\_Вот это\_, не должно выделиться тегом <em>.";

        ShouldBeHtmlParagraph(input, "_Вот это_, не должно выделиться тегом <em>.");
    }

    [Test]
    public void ParseAndRender_EscapeCharStaysWhenNotEscaping_ProducesPlainText()
    {
        var input = @"Здесь сим\волы экранирования\ \должны остаться.\";

        ShouldBeHtmlParagraph(input, @"Здесь сим\волы экранирования\ \должны остаться.\");
    }

    [Test]
    public void ParseAndRender_EscapeEscape_ProducesEmAndPlainText()
    {
        var input = @"Символ экранирования тоже можно экранировать: \\_вот это будет выделено тегом_ <em>";

        ShouldBeHtmlParagraph(input, "Символ экранирования тоже можно экранировать: \\<em>вот это будет выделено тегом</em> <em>");
    }

    [Test]
    public void ParseAndRender_EmInsideStrong_ProducesExpectedNesting()
    {
        var input = "Внутри __двойного выделения _одинарное_ тоже__ работает.";

        ShouldBeHtmlParagraph(input, "Внутри <strong>двойного выделения </strong><em>одинарное</em><strong> тоже</strong> работает.");
    }

    [Test]
    public void ParseAndRender_UnderscoreBetweenDigits_NotMarkup()
    {
        var input = "Подчерки внутри текста c цифрами_12_3 не считаются выделением и должны оставаться символами подчерка.";

        ShouldBeHtmlParagraph(input, "Подчерки внутри текста c цифрами_12_3 не считаются выделением и должны оставаться символами подчерка.");
    }

    [Test]
    public void ParseAndRender_CrossWords_EmDoesNotWork()
    {
        var input = "В то же время выделение в ра_зных сл_овах не работает.";

        ShouldBeHtmlParagraph(input, "В то же время выделение в ра_зных сл_овах не работает.");
    }

    [Test]
    public void ParseAndRender_OpeningUnderscoreMustBeFollowedByNonSpace_NotOpening()
    {
        var input = "За подчерками, начинающими выделение, должен следовать непробельный символ. Иначе эти_ подчерки_ не считаются выделением и остаются просто символами подчерка.";

        ShouldBeHtmlParagraph(input, "За подчерками, начинающими выделение, должен следовать непробельный символ. Иначе эти_ подчерки_ не считаются выделением и остаются просто символами подчерка.");
    }

    [Test]
    public void ParseAndRender_ClosingUnderscoreMustFollowNonSpace_NotClosing()
    {
        var input = "эти _подчерки _ не считаются окончанием выделения";

        ShouldBeHtmlParagraph(input, "эти _подчерки _ не считаются окончанием выделения");
    }

    [Test]
    public void ParseAndRender_EmptyBetweenDelimiters_FourUnderscoresRemain()
    {
        var input = "Если внутри подчерков пустая строка ____, то они остаются символами подчерка.";

        ShouldBeHtmlParagraph(input, "Если внутри подчерков пустая строка ____, то они остаются символами подчерка.");
    }

    [Test]
    public void ParseAndRender_ClosingUnderscorePrecededBySpace_NotClosing()
    {
        var input = "a _b _ c";

        ShouldBeHtmlParagraph(input, "a _b _ c");
    }

    [Test]
    public void ParseAndRender_EmPartOfWord_StartMiddleEnd()
    {
        var input = "Однако выделять часть слова они могут: и в _нач_але, и в сер_еди_не, и в кон_це._";

        ShouldBeHtmlParagraph(input, "Однако выделять часть слова они могут: и в <em>нач</em>але, и в сер<em>еди</em>не, и в кон<em>це.</em>");
    }

    [Test]
    public void ParseAndRender_StrongInsideEm_NotWorking()
    {
        var input = "Но не наоборот — внутри _одинарного __двойное__ не_ работает.";

        ShouldBeHtmlParagraph(input, "Но не наоборот — внутри <em>одинарного __двойное__ не</em> работает.");
    }

    [Test]
    public void ParseAndRender_UnpairedWithinParagraph_NotMarkup()
    {
        var input = "__Непарные_ символы в рамках одного абзаца не считаются выделением.";

        ShouldBeHtmlParagraph(input, "__Непарные_ символы в рамках одного абзаца не считаются выделением.");
    }

    [Test]
    public void ParseAndRender_CrossingStrongAndEm_NoneCounts()
    {
        var input = "В случае __пересечения _двойных__ и одинарных_ подчерков ни один из них не считается выделением.";

        ShouldBeHtmlParagraph(input, "В случае __пересечения _двойных__ и одинарных_ подчерков ни один из них не считается выделением.");
    }

    [Test]
    public void ParseAndRender_StrongPartOfWord_StartMiddleEnd()
    {
        var input = "и в __нач__але, и в сер__еди__не, и в кон__це__.";

        ShouldBeHtmlParagraph(input, "и в <strong>нач</strong>але, и в сер<strong>еди</strong>не, и в кон<strong>це</strong>.");
    }

    [Test]
    public void ParseAndRender_EscapingDoubleStrongToken_AllText()
    {
        var input = @"\__стронг__";

        ShouldBeHtmlParagraph(input, "__стронг__");
    }

    [Test]
    public void ParseAndRender_DelimitersWithPunctuation_AfterAndBefore()
    {
        var leftInput = "_a_,";
        ShouldBeHtmlParagraph(leftInput, "<em>a</em>,");

        var rightInput = "._b_";
        ShouldBeHtmlParagraph(rightInput, ".<em>b</em>");
    }

    [Test]
    public void ParseAndRender_EmAndStrongAtStringEdges_ProducesExpectedHtml()
    {
        var startInput = "_abc_ в начале";
        ShouldBeHtmlParagraph(startInput, "<em>abc</em> в начале");

        var endInput = "в конце __abc__";
        ShouldBeHtmlParagraph(endInput, "в конце <strong>abc</strong>");
    }

    [Test]
    public void ParseAndRender_PunctuationBeforeOpeningUnderscore_StillOpens()
    {
        var input = "!_a_";

        ShouldBeHtmlParagraph(input, "!<em>a</em>");
    }

    [Test]
    public void ParseAndRender_DigitsWithStrong_NotMarkup()
    {
        var input = "цифры__12__3";

        ShouldBeHtmlParagraph(input, "цифры__12__3");
    }

    [Test]
    public void ParseAndRender_EscapeDoubleSlashBeforeStrong_StrongWorks()
    {
        var input = @"\\__a__";

        ShouldBeHtmlParagraph(input, @"\<strong>a</strong>");
    }

    [Test]
    public void ParseAndRender_H1_Simple()
    {
        var input = "# Заголовок";
        ShouldBeHtmlH1(input, "Заголовок");
    }
}
