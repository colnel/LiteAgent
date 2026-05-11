using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using static System.Net.Mime.MediaTypeNames;

namespace FunctionTools.Document;

internal class DocxDocument
{
}

/// <summary>
/// Word 文档生成工具类
/// </summary>
public static class DocxHelper
{
    /// <summary>
    /// 根据文本内容生成 Word 文档
    /// </summary>
    /// <param name="outputPath">输出文件路径（.docx）</param>
    /// <param name="content">文本内容，支持多行（自动分行）</param>
    /// <param name="title">可选文档标题，若提供则单独一行粗体大标题</param>
    /// <param name="fontSize">正文字体大小（半磅，例如 24 表示 12 号字）</param>
    /// <param name="titleFontSize">标题字体大小（半磅）</param>
    public static void CreateFromText(string outputPath, string content, string? title = null, int fontSize = 24, int titleFontSize = 32)
    {
        // 确保目标目录存在
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using (var wordDocument = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document))
        {
            // 添加主文档部分
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = mainPart.Document.AppendChild(new Body());

            // 如果提供了标题，添加标题段落
            if (!string.IsNullOrWhiteSpace(title))
            {
                var titleParagraph = CreateParagraph(title, isTitle: true, titleFontSize);
                body.AppendChild(titleParagraph);
                // 可选：添加标题后空行
                body.AppendChild(CreateEmptyParagraph());
            }

            // 处理正文内容（按换行符分割）
            var lines = content.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    body.AppendChild(CreateEmptyParagraph());
                }
                else
                {
                    body.AppendChild(CreateParagraph(line, isTitle: false, fontSize));
                }
            }

            mainPart.Document.Save();
        }
    }

    /// <summary>
    /// 创建段落
    /// </summary>
    private static Paragraph CreateParagraph(string text, bool isTitle, int fontSize)
    {
        var run = new Run();
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));

        var runProperties = new RunProperties();
        // 设置字体大小（半磅）
        runProperties.AppendChild(new FontSize { Val = fontSize.ToString() });
        if (isTitle)
        {
            runProperties.AppendChild(new Bold());
        }
        run.PrependChild(runProperties);

        var paragraph = new Paragraph();
        paragraph.AppendChild(run);

        // 设置段落样式（行间距、缩进等）
        var paragraphProperties = new ParagraphProperties();
        if (isTitle)
        {
            // 标题居中
            paragraphProperties.AppendChild(new Justification() { Val = JustificationValues.Center });
            // 段前段后间距
            paragraphProperties.AppendChild(new SpacingBetweenLines { Before = "240", After = "120" });
        }
        else
        {
            // 正文首行缩进 2 字符（约 0.75 厘米，这里用 480 二十磅单位）
            paragraphProperties.AppendChild(new Indentation { FirstLine = "480" });
            // 行距 1.5 倍
            paragraphProperties.AppendChild(new SpacingBetweenLines { Line = "360", LineRule = LineSpacingRuleValues.Auto });
        }
        paragraph.PrependChild(paragraphProperties);

        return paragraph;
    }

    /// <summary>
    /// 创建空段落（用于换行）
    /// </summary>
    private static Paragraph CreateEmptyParagraph()
    {
        var run = new Run();
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(""));
        var paragraph = new Paragraph();
        paragraph.AppendChild(run);
        var paragraphProperties = new ParagraphProperties();
        paragraphProperties.AppendChild(new SpacingBetweenLines { After = "0", Before = "0" });
        paragraph.PrependChild(paragraphProperties);
        return paragraph;
    }

    /// <summary>
    /// 从文本文件读取内容并生成 Word 文档
    /// </summary>
    /// <param name="outputPath">输出 docx 路径</param>
    /// <param name="inputTextFilePath">输入文本文件路径</param>
    /// <param name="title">文档标题</param>
    public static void CreateFromTextFile(string outputPath, string inputTextFilePath, string? title = null)
    {
        if (!File.Exists(inputTextFilePath))
            throw new FileNotFoundException($"文本文件不存在: {inputTextFilePath}");

        var content = File.ReadAllText(inputTextFilePath);
        CreateFromText(outputPath, content, title);
    }
}