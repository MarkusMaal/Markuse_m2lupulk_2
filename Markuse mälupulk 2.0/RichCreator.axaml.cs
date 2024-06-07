using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Utils;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvRichTextBox;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;

namespace Markuse_mälupulk_2_0;

public partial class RichCreator : Window
{
    public RichCreator()
    {
        InitializeComponent();
    }

    private void Style_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) { return; }
        switch (button.Content)
        {
            case "P":
                RichTextBox1.FlowDoc.Selection.ApplyFormatting(FontWeightProperty, FontWeight.Bold);
                break;
            case "K":
                RichTextBox1.FlowDoc.Selection.ApplyFormatting(FontStyleProperty, FontStyle.Italic);
                break;
            case "H":
                RichTextBox1.FlowDoc.Selection.ApplyFormatting(FontWeightProperty, FontWeight.Regular);
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

    Paragraph GetSelectionParagraph()
    {
        Paragraph p = new();
        string selectedText;
        try
        {
            selectedText = RichTextBox1.FlowDoc.Selection.Text;
        } catch
        {
            return (Paragraph)RichTextBox1.FlowDoc.Blocks.First();
        }
        foreach (Block b in RichTextBox1.FlowDoc.Blocks)
        {
            if (b.Text.Contains(selectedText))
            {
                return (Paragraph)b;
            }
        }
        return p;
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}