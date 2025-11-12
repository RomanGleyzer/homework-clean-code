using System.Diagnostics;
using System.Text;
using FluentAssertions;

using Markdown;

namespace MarkdownTests;

[TestFixture]
public class PerformanceTests
{
    private const string Paragraph =
        "# Заголовок __с _разными_ символами__\n\n" +
        "Текст, _окруженный_ и __сильный__ и \\_экранированный\\_ 12_3 " +
        "слово ра_зных сл_овах не работает. __Непарные_ и _непарные__ . " +
        "Внутри __двойного _одинарное_ тоже__ работает. " +
        "Но _внутри __одинарного__ не_ работает. Конец\\\\\n\n";

    private const double AllowedGrowth = 2.0;

    private static string BuildInput(int targetChars)
    {
        var sb = new StringBuilder(targetChars + 1024);
        while (sb.Length < targetChars)
            sb.Append(Paragraph);
        return sb.ToString();
    }

    [Test]
    public void Render_InputSizeIncreases_ScalesLinearlyOrBetter()
    {
        var md = new Md();
        var segmenter = new BlockSegmenter();
        var parser = new Markdown.Inlines.InlineParser();
        var renderer = new HtmlRenderer();

        var sizes = new[] { 2_000, 16_000, 128_000, 1_000_000 };
        var timesMs = new double[sizes.Length];

        for (int i = 0; i < sizes.Length; i++)
        {
            var input = BuildInput(sizes[i]);

            var sw = Stopwatch.StartNew();
            var html = md.Render(input, segmenter, parser, renderer);
            sw.Stop();

            html.Should().NotBeNullOrEmpty();

            timesMs[i] = sw.Elapsed.TotalMilliseconds;
        }

        var costPerChar = new double[sizes.Length];
        for (int i = 0; i < sizes.Length; i++)
            costPerChar[i] = timesMs[i] / sizes[i];

        for (int i = 1; i < sizes.Length; i++)
            (costPerChar[i] / costPerChar[i - 1]).Should().BeLessThanOrEqualTo(AllowedGrowth);
    }
}
