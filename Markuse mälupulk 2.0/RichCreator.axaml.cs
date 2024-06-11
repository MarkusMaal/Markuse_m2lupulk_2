using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Utils;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvRichTextBox;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using DocumentFormat.OpenXml.Packaging;
using System.Runtime.Serialization.Formatters;
using System.IO;
using TextAlignment = Avalonia.Media.TextAlignment;
using Xceed.Words.NET;
using Xceed.Document.NET;


namespace Markuse_mälupulk_2_0;

public partial class RichCreator : Window
{
    string? loadDoc;
    public RichCreator(string? loadDoc = null)
    {
        InitializeComponent();
        this.loadDoc = loadDoc;
    }

    private void Style_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) { return; }
        switch (button.Content)
        {
            case "P":
                RichTextBox1.FlowDoc.Selection.ApplyFormatting(FontWeightProperty, Avalonia.Media.FontWeight.Bold);
                break;
            case "K":
                RichTextBox1.FlowDoc.Selection.ApplyFormatting(FontStyleProperty, FontStyle.Italic);
                break;
            case "H":
                RichTextBox1.FlowDoc.Selection.ApplyFormatting(FontWeightProperty, Avalonia.Media.FontWeight.Regular);
                RichTextBox1.FlowDoc.Selection.ApplyFormatting(FontStyleProperty, FontStyle.Normal);
                RichTextBox1.FlowDoc.Selection.ApplyFormatting(Inline.TextDecorationsProperty, TextDecorations.Baseline);
                break;
            case "&lt;--":
            case "<--":
                GetSelectionParagraph().TextAlignment = TextAlignment.Left;
                break;
            case "-":
                GetSelectionParagraph().TextAlignment = TextAlignment.Center;
                break;
            case "--&gt;":
            case "-->":
                GetSelectionParagraph().TextAlignment = TextAlignment.Right;
                break;
            case "Aa":
                if (button.Foreground is ImmutableSolidColorBrush)
                {
                    SwapFg();
                } else
                {
                    SwapBg();
                }
                break;
            case "T+":
                if (GetSelectionParagraph().Margin.Left < 50)
                {
                    GetSelectionParagraph().Margin += new Avalonia.Thickness(10, 0, 0, 0);
                }
                break;
            case "T-":
                if (GetSelectionParagraph().Margin.Left > 0)
                {
                    GetSelectionParagraph().Margin -= new Avalonia.Thickness(10, 0, 0, 0);
                }
                break;
            case "*": // no selection bullet available on AvRichTextBox :(
                break;

        }
        
        if (button.Content is TextBlock textBlock)
        {
            switch (textBlock.Text)
            {
                case "A":
                    RichTextBox1.FlowDoc.Selection.ApplyFormatting(Inline.TextDecorationsProperty, TextDecorations.Underline);
                    break;
                case "L":
                    RichTextBox1.FlowDoc.Selection.ApplyFormatting(Inline.TextDecorationsProperty, TextDecorations.Strikethrough); // strikethrough doesn't work on current version of AvRichTextBox for some reason...
                    break;
            }
        }
    }

    async void SwapFg()
    {
        ColorDialog cd = new();
        await cd.ShowDialog(this).WaitAsync(CancellationToken.None);
        RichTextBox1.FlowDoc.Selection.ApplyFormatting(ForegroundProperty, new SolidColorBrush(cd.Color.Color));
    }

    async void SwapBg()
    {
        ColorDialog cd = new();
        await cd.ShowDialog(this).WaitAsync(CancellationToken.None);
        RichTextBox1.FlowDoc.Selection.ApplyFormatting(BackgroundProperty, new SolidColorBrush(cd.Color.Color));
    }

    AvRichTextBox.Paragraph GetSelectionParagraph()
    {
        AvRichTextBox.Paragraph p = new();
        string selectedText;
        try
        {
            selectedText = RichTextBox1.FlowDoc.Selection.Text;
        } catch
        {
            return (AvRichTextBox.Paragraph)RichTextBox1.FlowDoc.Blocks.First();
        }
        foreach (Block b in RichTextBox1.FlowDoc.Blocks)
        {
            if (b.Text.Contains(selectedText))
            {
                return (AvRichTextBox.Paragraph)b;
            }
        }
        return p;
    }

    static string RtfToHtml(string rtf)
    {
        // register encoding provider to support Windows-1257 encoding
        EncodingProvider ppp = CodePagesEncodingProvider.Instance;
        Encoding.RegisterProvider(ppp);

        // Simple RTF to HTML conversion
        string html = RtfPipe.Rtf.ToHtml(rtf);

        // Your RTF to HTML conversion logic here
        // This is a placeholder for a more complex conversion

        return html;
    }

    static void HtmlToDocx(string html, string docxPath)
    {
        // Create a new DocX document
        using var document = DocX.Create(docxPath);
        // Add HTML content to the document
        var htmlParser = new AngleSharp.Html.Parser.HtmlParser();
        var htmlDoc = htmlParser.ParseDocument(html);

        foreach (var element in htmlDoc.Body.Children)
        {
            // Add HTML element content to the DOCX document
            ParseHtmlElement(element, document);
            document.InsertParagraph("\r\n");
        }

        // Save the document
        document.Save();
        document.Dispose();
    }

    static void ParseHtmlElement(AngleSharp.Dom.IElement element, DocX document)
    {
        // Handle different HTML elements and apply formatting
        switch (element.TagName.ToLower())
        {
            case "p":
                var paragraph = document.InsertParagraph();
                AddFormattedText(paragraph, element);
                paragraph.AppendLine(); // Ensure line separation for paragraphs
                break;
            case "br":
                document.InsertParagraph(); // Add a new paragraph for <br> tags
                break;
            default:
                var defaultParagraph = document.InsertParagraph();
                AddFormattedText(defaultParagraph, element);
                defaultParagraph.AppendLine(); // Ensure line separation for other elements
                break;
        }
    }

    static void AddFormattedText(Xceed.Document.NET.Paragraph paragraph, AngleSharp.Dom.IElement element, bool bold = false, bool italic = false, bool underline = false)
    {
        foreach (var child in element.ChildNodes)
        {
            if (child is AngleSharp.Dom.IText textNode)
            {
                var text = textNode.TextContent;
                var formattedText = paragraph.Append(text);

                if (bold)
                    formattedText.Bold();
                if (italic)
                    formattedText.Italic();
                if (underline)
                    formattedText.UnderlineStyle(UnderlineStyle.singleLine);

            }
            else if (child is AngleSharp.Dom.IElement childElement)
            {
                switch (childElement.TagName.ToLower())
                {
                    case "b":
                    case "strong":
                        AddFormattedText(paragraph, childElement, true, italic, underline);
                        break;
                    case "i":
                    case "em":
                        AddFormattedText(paragraph, childElement, bold, true, underline);
                        break;
                    case "u":
                        AddFormattedText(paragraph, childElement, bold, italic, true);
                        break;
                    default:
                        AddFormattedText(paragraph, childElement, bold, italic, underline);
                        break;
                }
            }
        }
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        if (loadDoc != null)
        {
            if (!File.Exists(loadDoc.Replace("//", "/").Replace(".rtf", ".docx")))
            {
                string rtfPath = loadDoc.Replace("//", "/");
                string docxPath = loadDoc.Replace("//", "/").Replace(".rtf", ".docx");
                // Read RTF content
                string rtfContent = File.ReadAllText(rtfPath);

                // Convert RTF to HTML
                string htmlContent = RtfToHtml(rtfContent);

                // Convert HTML to DOCX
                HtmlToDocx(htmlContent, docxPath);
            }
            RichTextBox1.LoadWordDoc(loadDoc.Replace("//", "/").Replace(".rtf", ".docx"));
            RichTextBox1.FlowDoc.PagePadding = new Avalonia.Thickness(0);
        }
    }
}