using FluentAssertions;
using Markdown;
using Markdown.Inlines;

namespace MarkdownTests;

[TestFixture]
public class MarkupLanguageProcessorTests
{
    private static string RenderHtml(string input)
    {
        var md = new Md(new BlockSegmenter(), new InlineParser(), new HtmlRenderer());
        return md.Render(input);
    }

    private static string Normalize(string s)
    {
        return s.ReplaceLineEndings("");
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
    public void ParseAndRender_EmPartOfWord_RendersAtStartMiddleEnd()
    {
        var input = "Однако выделять часть слова они могут: и в _нач_але, и в сер_еди_не, и в кон_це._";

        ShouldBeHtmlParagraph(input, "Однако выделять часть слова они могут: и в <em>нач</em>але, и в сер<em>еди</em>не, и в кон<em>це.</em>");
    }

    [Test]
    public void ParseAndRender_StrongInsideEm_NotApplied()
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
    public void ParseAndRender_StrongPartOfWord_RendersAtStartMiddleEnd()
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
    public void ParseAndRender_DelimiterFollowedByPunctuation_CommaAfter()
    {
        var leftInput = "_a_,";
        ShouldBeHtmlParagraph(leftInput, "<em>a</em>,");
    }

    [Test]
    public void ParseAndRender_PunctuationBeforeDelimiter_PeriodBefore()
    {
        var rightInput = "._b_";
        ShouldBeHtmlParagraph(rightInput, ".<em>b</em>");
    }

    [Test]
    public void ParseAndRender_EmAtStringStart_ProducesExpectedHtml()
    {
        var startInput = "_abc_ в начале";
        ShouldBeHtmlParagraph(startInput, "<em>abc</em> в начале");
    }

    [Test]
    public void ParseAndRender_StrongAtStringEnd_ProducesExpectedHtml()
    {
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
    public void ParseAndRender_H1_Simple_ProducesH1()
    {
        var input = "# Заголовок";
        ShouldBeHtmlH1(input, "Заголовок");
    }

    [Test]
    public void ParseAndRender_TwoParagraphs_Simple_ProducesTwoParagraphs()
    {
        var input = "первый абзац\n\nвторой абзац";
        Normalize(RenderHtml(input)).Should().Be("<p>первый абзац</p><p>второй абзац</p>");
    }

    [Test]
    public void ParseAndRender_H1ThenParagraph_WithInlineMarkup_ProducesExpectedHtml()
    {
        var input =
            "# Заголовок __с _разными_ символами__\n\n" +
            "Текст про _эм_ и __стронг__.";
        var expected =
            "<h1>Заголовок <strong>с <em>разными</em> символами</strong></h1>" +
            "<p>Текст про <em>эм</em> и <strong>стронг</strong>.</p>";
        Normalize(RenderHtml(input)).Should().Be(expected);
    }

    [Test]
    public void ParseAndRender_EmphasisAcrossParagraphs_DoesNotSpan()
    {
        var input = "Начало _абзаца\n\nпродолжение_ абзаца";
        var expected = "<p>Начало _абзаца</p><p>продолжение_ абзаца</p>";
        Normalize(RenderHtml(input)).Should().Be(expected);
    }

    [Test]
    public void ParseAndRender_EscapedHash_IsNotHeading()
    {
        var input = @"\# Не заголовок";
        ShouldBeHtmlParagraph(input, "# Не заголовок");
    }

    [Test]
    public void ParseAndRender_H1_WithEscapesInside_ProducesExpectedHtml()
    {
        var input = @"# Тест \_эм\_ и \\__стронг__";
        var expected = "<h1>Тест _эм_ и \\<strong>стронг</strong></h1>";
        Normalize(RenderHtml(input)).Should().Be(expected);
    }

    [Test]
    public void ParseAndRender_MultipleEmptyLines_BetweenParagraphs_ProducesTwoParagraphs()
    {
        var input = "a\n\n\n\nb";
        Normalize(RenderHtml(input)).Should().Be("<p>a</p><p>b</p>");
    }

    [Test]
    public void ParseAndRender_TrailingBackslash_StaysInOutput()
    {
        var input = @"abc\";
        ShouldBeHtmlParagraph(input, @"abc\");
    }

    [Test]
    public void ParseAndRender_ThreeParagraphs_MixedBlocks_ProducesExpectedHtml()
    {
        var input = "_эм_ абзац\n\n# Заголовок\n\ntext __bold__";
        var expected = "<p><em>эм</em> абзац</p><h1>Заголовок</h1><p>text <strong>bold</strong></p>";
        Normalize(RenderHtml(input)).Should().Be(expected);
    }

    [Test]
    public void ParseAndRender_ParagraphFromSpec_AllRulesTogether_ProducesExpectedHtml()
    {
        const string Paragraph =
            "# Заголовок __с _разными_ символами__\n\n" +
            "Текст, _окруженный_ и __сильный__ и \\_экранированный\\_ 12_3 " +
            "слово ра_зных сл_овах не работает. __Непарные_ и _непарные__ . " +
            "Внутри __двойного _одинарное_ тоже__ работает. " +
            "Но _внутри __одинарного__ не_ работает. Конец\\\\\n\n";

        var expected =
            "<h1>Заголовок <strong>с <em>разными</em> символами</strong></h1>" +
            "<p>Текст, <em>окруженный</em> и <strong>сильный</strong> и _экранированный_ 12_3 " +
            "слово ра_зных сл_овах не работает. __Непарные_ и _непарные__ . " +
            "Внутри <strong>двойного </strong><em>одинарное</em><strong> тоже</strong> работает. " +
            "Но <em>внутри __одинарного__ не</em> работает. Конец\\\\</p>";

        Normalize(RenderHtml(Paragraph)).Should().Be(expected);
    }
}
