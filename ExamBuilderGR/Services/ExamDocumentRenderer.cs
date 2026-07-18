using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ExamBuilderGR.Models;

namespace ExamBuilderGR.Services;

/// <summary>
/// Δημιουργεί το ίδιο σελιδοποιημένο έγγραφο για προεπισκόπηση, εκτύπωση και PDF.
/// Υποστηρίζει μαθητικό έντυπο και προαιρετικό κλειδί λύσεων.
/// </summary>
public sealed class ExamDocumentRenderer
{
    private const double DipPerCentimeter = 96d / 2.54d;
    private const double DefaultPageWidth = 21d * DipPerCentimeter;
    private const double DefaultPageHeight = 29.7d * DipPerCentimeter;
    private const double AnswerLineHeight = 17d;

    private static readonly Thickness DefaultPagePadding = new(
        1.35d * DipPerCentimeter,
        1.15d * DipPerCentimeter,
        1.35d * DipPerCentimeter,
        1.15d * DipPerCentimeter);

    private static readonly Brush PrintTextBrush = Brushes.Black;
    private static readonly Brush LineBrush = new SolidColorBrush(Color.FromRgb(156, 163, 175));
    private static readonly Brush SoftBackgroundBrush = new SolidColorBrush(Color.FromRgb(245, 247, 250));

    public FlowDocument CreateDocument(ExamDocument exam, SchoolProfile school) =>
        CreateStudentDocument(exam, school, DefaultPageWidth, DefaultPageHeight, DefaultPagePadding);

    public FlowDocument CreateAnswerKeyDocument(ExamDocument exam, SchoolProfile school) =>
        CreateKeyDocument(exam, school, DefaultPageWidth, DefaultPageHeight, DefaultPagePadding);

    public bool PrintExam(ExamDocument exam, SchoolProfile school) =>
        ShowPrintDialog(exam, school, answerKey: false, owner: null, exportPdf: false);

    public bool ExportPdf(ExamDocument exam, SchoolProfile school, Window? owner = null) =>
        ShowPrintDialog(exam, school, answerKey: false, owner: owner, exportPdf: true);

    public bool PrintAnswerKey(ExamDocument exam, SchoolProfile school) =>
        ShowPrintDialog(exam, school, answerKey: true, owner: null, exportPdf: false);

    public bool ExportAnswerKeyPdf(ExamDocument exam, SchoolProfile school, Window? owner = null) =>
        ShowPrintDialog(exam, school, answerKey: true, owner: owner, exportPdf: true);

    private bool ShowPrintDialog(
        ExamDocument exam,
        SchoolProfile school,
        bool answerKey,
        Window? owner,
        bool exportPdf)
    {
        if (answerKey && !exam.GenerateAnswerKey)
        {
            MessageBox.Show(owner,
                "Ενεργοποίησε πρώτα την επιλογή «Δημιουργία κλειδιού λύσεων».",
                "Κλειδί λύσεων", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        if (exportPdf)
        {
            MessageBox.Show(owner,
                "Στο παράθυρο εκτύπωσης επίλεξε «Microsoft Print to PDF». " +
                "Στη συνέχεια τα Windows θα σε ρωτήσουν πού θα αποθηκευτεί το PDF.",
                answerKey ? "PDF κλειδιού λύσεων" : "Εξαγωγή PDF",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return false;

        PrintWithDialog(dialog, exam, school, answerKey);
        return true;
    }

    private static FlowDocument CreateStudentDocument(
        ExamDocument exam,
        SchoolProfile school,
        double pageWidth,
        double pageHeight,
        Thickness pagePadding)
    {
        var document = CreateBaseDocument(pageWidth, pageHeight, pagePadding);
        var contentWidth = GetContentWidth(pageWidth, pagePadding);

        AddHeader(document, exam, school, contentWidth, isAnswerKey: false);
        AddStudentDetails(document, exam, contentWidth);

        if (!string.IsNullOrWhiteSpace(exam.Instructions))
        {
            document.Blocks.Add(new Paragraph(new Run(exam.Instructions))
            {
                FontSize = 9,
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, 4, 0, 10)
            });
        }

        foreach (var section in exam.Sections)
            AddStudentSection(document, section, contentWidth);

        AddFinalGradeAndSignature(document, exam, school, contentWidth);
        return document;
    }

    private static FlowDocument CreateKeyDocument(
        ExamDocument exam,
        SchoolProfile school,
        double pageWidth,
        double pageHeight,
        Thickness pagePadding)
    {
        var document = CreateBaseDocument(pageWidth, pageHeight, pagePadding);
        var contentWidth = GetContentWidth(pageWidth, pagePadding);

        AddHeader(document, exam, school, contentWidth, isAnswerKey: true);
        document.Blocks.Add(new Paragraph(new Run(
            "Το παρόν αρχείο προορίζεται για τον/την εκπαιδευτικό και περιλαμβάνει τις απαντήσεις που έχουν καταχωριστεί στην εφαρμογή."))
        {
            FontSize = 9,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 12)
        });

        foreach (var section in exam.Sections)
            AddAnswerKeySection(document, section, contentWidth);

        return document;
    }

    private static FlowDocument CreateBaseDocument(double pageWidth, double pageHeight, Thickness pagePadding) =>
        new()
        {
            PageWidth = pageWidth,
            PageHeight = pageHeight,
            PagePadding = pagePadding,
            ColumnWidth = pageWidth,
            ColumnGap = 0,
            FontFamily = new FontFamily("Arial"),
            FontSize = 10.5,
            Foreground = PrintTextBrush,
            Background = Brushes.White
        };

    private static double GetContentWidth(double pageWidth, Thickness pagePadding) =>
        Math.Max(300d, pageWidth - pagePadding.Left - pagePadding.Right);

    private void PrintWithDialog(PrintDialog dialog, ExamDocument exam, SchoolProfile school, bool answerKey)
    {
        var pageWidth = dialog.PrintableAreaWidth > 0 ? dialog.PrintableAreaWidth : DefaultPageWidth;
        var pageHeight = dialog.PrintableAreaHeight > 0 ? dialog.PrintableAreaHeight : DefaultPageHeight;
        var padding = dialog.PrintableAreaWidth > 0 && dialog.PrintableAreaHeight > 0
            ? new Thickness(42, 38, 42, 38)
            : DefaultPagePadding;

        var document = answerKey
            ? CreateKeyDocument(exam, school, pageWidth, pageHeight, padding)
            : CreateStudentDocument(exam, school, pageWidth, pageHeight, padding);

        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        paginator.PageSize = new Size(pageWidth, pageHeight);
        var jobName = answerKey ? $"Κλειδί λύσεων - {exam.Title}" : exam.Title;
        dialog.PrintDocument(paginator, string.IsNullOrWhiteSpace(jobName) ? "ExamBuilder GR" : jobName);
    }

    private static void AddHeader(
        FlowDocument document,
        ExamDocument exam,
        SchoolProfile school,
        double contentWidth,
        bool isAnswerKey)
    {
        var identityHeader = CreateSchoolIdentityHeader(school, contentWidth);
        document.Blocks.Add(new BlockUIContainer(identityHeader)
        {
            Margin = new Thickness(0)
        });

        document.Blocks.Add(new BlockUIContainer(new Border
        {
            Width = contentWidth,
            Height = 1,
            Background = Brushes.Gray,
            Margin = new Thickness(0, 10, 0, 10)
        }));

        var title = isAnswerKey ? $"ΚΛΕΙΔΙ ΛΥΣΕΩΝ — {exam.Title}" : exam.Title;
        document.Blocks.Add(CenteredParagraph(title, 14, FontWeights.Bold, new Thickness(0, 0, 0, 10)));
    }

    private static FrameworkElement CreateSchoolIdentityHeader(SchoolProfile school, double contentWidth)
    {
        var logo = school.ShowSchoolLogo
            ? SchoolLogoService.LoadImage(school.SchoolLogoPath, school.SchoolLogoGrayscale)
            : null;

        var textPanel = CreateSchoolTextPanel(school);
        if (logo is null)
        {
            textPanel.Width = contentWidth;
            return textPanel;
        }

        var logoWidth = Math.Clamp(school.SchoolLogoWidthCm, 1.5d, 5d) * DipPerCentimeter;
        var image = new Image
        {
            Source = logo,
            Width = logoWidth,
            MaxHeight = Math.Max(logoWidth, 70),
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = true
        };

        if (string.Equals(school.SchoolLogoPosition, "Κέντρο", StringComparison.OrdinalIgnoreCase))
        {
            var centered = new StackPanel
            {
                Width = contentWidth,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            image.HorizontalAlignment = HorizontalAlignment.Center;
            image.Margin = new Thickness(0, 0, 0, 6);
            centered.Children.Add(image);
            centered.Children.Add(textPanel);
            return centered;
        }

        var grid = new Grid { Width = contentWidth };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(logoWidth + 12) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(logoWidth + 12) });

        image.HorizontalAlignment = HorizontalAlignment.Center;
        image.VerticalAlignment = VerticalAlignment.Center;
        grid.Children.Add(image);

        Grid.SetColumn(textPanel, 1);
        textPanel.VerticalAlignment = VerticalAlignment.Center;
        grid.Children.Add(textPanel);
        return grid;
    }

    private static StackPanel CreateSchoolTextPanel(SchoolProfile school)
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        panel.Children.Add(new TextBlock
        {
            Text = school.SchoolName ?? string.Empty,
            FontFamily = new FontFamily("Arial"),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = PrintTextBrush,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        if (!string.IsNullOrWhiteSpace(school.SchoolSubtitle))
        {
            panel.Children.Add(new TextBlock
            {
                Text = school.SchoolSubtitle,
                FontFamily = new FontFamily("Arial"),
                FontSize = 9.5,
                Foreground = Brushes.DimGray,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0)
            });
        }

        if (!string.IsNullOrWhiteSpace(school.SchoolYear))
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"Σχολικό έτος: {school.SchoolYear}",
                FontFamily = new FontFamily("Arial"),
                FontSize = 9.5,
                Foreground = Brushes.DimGray,
                TextAlignment = TextAlignment.Center
            });
        }

        var teacherLine = string.Join(" — ", new[] { school.TeacherName, school.TeacherSpecialty }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        if (!string.IsNullOrWhiteSpace(teacherLine))
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"Καθηγητής: {teacherLine}",
                FontFamily = new FontFamily("Arial"),
                FontSize = 9,
                Foreground = Brushes.DimGray,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0)
            });
        }

        return panel;
    }

    private static void AddStudentDetails(FlowDocument document, ExamDocument exam, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 7) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.62d) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.38d) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        group.Rows.Add(CreateFullWidthRow("Ονοματεπώνυμο: ________________________________________________"));
        group.Rows.Add(CreateTwoCellRow(
            $"Τάξη: {exam.Grade}     Τμήμα: __________",
            $"Ημερομηνία: {exam.ExamDate:dd/MM/yyyy}"));

        if (!string.IsNullOrWhiteSpace(exam.Orientation))
            group.Rows.Add(CreateTwoCellRow($"Κατεύθυνση / Προσανατολισμός: {exam.Orientation}", $"Διάρκεια: {exam.DurationMinutes} λεπτά"));
        else
            group.Rows.Add(CreateTwoCellRow("", $"Διάρκεια: {exam.DurationMinutes} λεπτά"));

        document.Blocks.Add(table);
    }

    private static TableRow CreateFullWidthRow(string text)
    {
        var row = new TableRow();
        var cell = CreateCell(text);
        cell.ColumnSpan = 2;
        row.Cells.Add(cell);
        return row;
    }

    private static TableRow CreateTwoCellRow(string left, string right)
    {
        var row = new TableRow();
        row.Cells.Add(CreateCell(left));
        row.Cells.Add(CreateCell(right));
        return row;
    }

    private static TableCell CreateCell(string text, FontWeight? weight = null, TextAlignment alignment = TextAlignment.Left)
    {
        var paragraph = new Paragraph(new Run(text ?? string.Empty))
        {
            Margin = new Thickness(0, 2, 4, 2),
            FontSize = 9.5,
            TextAlignment = alignment,
            Foreground = PrintTextBrush
        };
        if (weight.HasValue) paragraph.FontWeight = weight.Value;
        return new TableCell(paragraph) { Padding = new Thickness(0) };
    }

    private static void AddStudentSection(FlowDocument document, ExamSection section, double contentWidth)
    {
        document.Blocks.Add(new Paragraph(new Run($"{section.Title} ({section.TotalPoints} μονάδες)"))
        {
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 10, 0, 4),
            KeepWithNext = true
        });

        AddSectionIntro(document, section);

        foreach (var question in section.Questions)
        {
            switch (question.Type)
            {
                case QuestionType.TrueFalse:
                    AddTrueFalseQuestion(document, question, contentWidth, answerKey: false);
                    break;
                case QuestionType.Matching:
                    AddMatchingQuestion(document, question, contentWidth, answerKey: false);
                    break;
                case QuestionType.FillBlank:
                    AddFillBlankQuestion(document, question, contentWidth, answerKey: false);
                    break;
                case QuestionType.MultipleChoice:
                    AddMultipleChoiceQuestion(document, question, contentWidth, answerKey: false);
                    break;
                default:
                    AddDevelopmentQuestion(document, question, contentWidth);
                    break;
            }
        }
    }

    private static void AddSectionIntro(FlowDocument document, ExamSection section)
    {
        if (string.IsNullOrWhiteSpace(section.IntroText)) return;

        document.Blocks.Add(new Paragraph(new Run(section.IntroText.Trim()))
        {
            FontSize = 10,
            Margin = new Thickness(0, 1, 0, 6),
            Foreground = PrintTextBrush,
            KeepWithNext = true
        });
    }

    private static void AddDevelopmentQuestion(FlowDocument document, ExamQuestion question, double contentWidth)
    {
        AddQuestionHeading(document, question, contentWidth, question.Text);
        AddAnswerArea(document, question, contentWidth);
        AddQuestionSpacer(document);
    }

    private static void AddFillBlankQuestion(FlowDocument document, ExamQuestion question, double contentWidth, bool answerKey)
    {
        question.NormalizeLegacyStructures();
        AddQuestionHeading(document, question, contentWidth, question.Text);

        if (question.FillBlankSentences.Count == 0)
        {
            document.Blocks.Add(AnswerParagraph("Δεν έχουν καταχωριστεί προτάσεις συμπλήρωσης κενού."));
            AddQuestionSpacer(document);
            return;
        }

        for (var sentenceIndex = 0; sentenceIndex < question.FillBlankSentences.Count; sentenceIndex++)
        {
            var sentence = question.FillBlankSentences[sentenceIndex];
            var text = answerKey ? sentence.OriginalText : sentence.StudentText;
            document.Blocks.Add(new Paragraph(new Run($"{sentenceIndex + 1}. {text}"))
            {
                FontSize = 10,
                Margin = new Thickness(10, 3, 0, 3),
                Foreground = PrintTextBrush
            });

            if (answerKey)
            {
                var answerText = sentence.Answers.Count == 0
                    ? "Δεν έχουν οριστεί κενά."
                    : string.Join("  |  ", sentence.Answers.Select((answer, blankIndex) =>
                        $"{blankIndex + 1}ο κενό: {answer}"));
                document.Blocks.Add(AnswerParagraph($"Πρόταση {sentenceIndex + 1}: {answerText}"));
            }
        }

        AddQuestionSpacer(document);
    }

    private static void AddTrueFalseQuestion(FlowDocument document, ExamQuestion question, double contentWidth, bool answerKey)
    {
        AddQuestionHeading(document, question, contentWidth, question.Text);

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 3, 0, 5) };
        table.Columns.Add(new TableColumn { Width = new GridLength(32) });
        table.Columns.Add(new TableColumn { Width = new GridLength(Math.Max(180, contentWidth - 112)) });
        table.Columns.Add(new TableColumn { Width = new GridLength(40) });
        table.Columns.Add(new TableColumn { Width = new GridLength(40) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        var header = new TableRow { Background = SoftBackgroundBrush };
        header.Cells.Add(CreateStructuredCell("#", FontWeights.Bold, TextAlignment.Center));
        header.Cells.Add(CreateStructuredCell("Πρόταση", FontWeights.Bold));
        header.Cells.Add(CreateStructuredCell("Σ", FontWeights.Bold, TextAlignment.Center));
        header.Cells.Add(CreateStructuredCell("Λ", FontWeights.Bold, TextAlignment.Center));
        group.Rows.Add(header);

        var index = 1;
        foreach (var item in question.TrueFalseItems)
        {
            var row = new TableRow();
            row.Cells.Add(CreateStructuredCell(index.ToString(), null, TextAlignment.Center));
            row.Cells.Add(CreateStructuredCell(item.Statement));
            row.Cells.Add(CreateStructuredCell(answerKey && item.IsTrue ? "✓" : "□", FontWeights.Bold, TextAlignment.Center));
            row.Cells.Add(CreateStructuredCell(answerKey && !item.IsTrue ? "✓" : "□", FontWeights.Bold, TextAlignment.Center));
            group.Rows.Add(row);
            index++;
        }

        if (question.TrueFalseItems.Count == 0)
        {
            var row = new TableRow();
            var cell = CreateStructuredCell("Δεν έχουν καταχωριστεί προτάσεις.", null, TextAlignment.Center);
            cell.ColumnSpan = 4;
            row.Cells.Add(cell);
            group.Rows.Add(row);
        }

        document.Blocks.Add(table);
        AddQuestionSpacer(document);
    }

    private static void AddMultipleChoiceQuestion(FlowDocument document, ExamQuestion question, double contentWidth, bool answerKey)
    {
        AddQuestionHeading(document, question, contentWidth, question.Text);

        var options = question.MultipleChoiceOptions.ToList();
        if (options.Count == 0)
        {
            document.Blocks.Add(AnswerParagraph(answerKey
                ? "Δεν έχουν καταχωριστεί επιλογές ή σωστή απάντηση."
                : "Δεν έχουν καταχωριστεί επιλογές απάντησης."));
            AddQuestionSpacer(document);
            return;
        }

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var isCorrect = answerKey && option.IsCorrect;
            var paragraph = new Paragraph
            {
                FontSize = 10,
                Margin = new Thickness(12, 2, 0, 2),
                Foreground = PrintTextBrush
            };

            var marker = isCorrect ? "✓" : "○";
            var prefix = new Run($"{marker} {GetGreekLabel(i)}. ")
            {
                FontWeight = isCorrect ? FontWeights.Bold : FontWeights.Normal
            };
            var optionText = new Run(option.Text ?? string.Empty)
            {
                FontWeight = isCorrect ? FontWeights.Bold : FontWeights.Normal
            };

            paragraph.Inlines.Add(prefix);
            paragraph.Inlines.Add(optionText);
            document.Blocks.Add(paragraph);
        }

        if (answerKey)
        {
            var correctAnswers = options
                .Select((option, index) => new { option, index })
                .Where(item => item.option.IsCorrect)
                .Select(item => $"{GetGreekLabel(item.index)}. {item.option.Text}")
                .ToList();

            document.Blocks.Add(AnswerParagraph(correctAnswers.Count == 0
                ? "Δεν έχει οριστεί σωστή απάντηση."
                : "Σωστή απάντηση: " + string.Join(" | ", correctAnswers)));
        }

        AddQuestionSpacer(document);
    }

    private static void AddMatchingQuestion(FlowDocument document, ExamQuestion question, double contentWidth, bool answerKey)
    {
        question.NormalizeLegacyStructures();
        AddQuestionHeading(document, question, contentWidth, question.Text);

        var displayedLeft = DeterministicShuffle(question.MatchingLeftItems, question.MatchingShuffleSeed, avoidOriginalOrder: true);
        var rightSeed = unchecked((question.MatchingShuffleSeed * 397) ^ 0x5F3759DF);
        var displayedRight = DeterministicShuffle(question.MatchingRightItems, rightSeed, avoidOriginalOrder: true);

        // Αν μετά το ανεξάρτητο ανακάτεμα όλες οι σωστές απαντήσεις έτυχε να βρίσκονται
        // στην ίδια γραμμή, μετακινούμε κυκλικά τη Στήλη Β ώστε η διάταξη να μην προδίδει το κλειδί.
        if (displayedLeft.Count == displayedRight.Count && displayedLeft.Count > 1)
        {
            var allRowsAccidentallyCorrect = displayedLeft
                .Select((left, index) => question.MatchingRelations.Any(relation =>
                    relation.LeftItemId == left.Id && relation.RightItemId == displayedRight[index].Id))
                .All(value => value);

            if (allRowsAccidentallyCorrect)
                displayedRight = displayedRight.Skip(1).Concat(displayedRight.Take(1)).ToList();
        }

        var count = Math.Max(displayedLeft.Count, displayedRight.Count);

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 3, 0, 5) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5d) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5d) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        var header = new TableRow { Background = SoftBackgroundBrush };
        header.Cells.Add(CreateStructuredCell("ΣΤΗΛΗ Α", FontWeights.Bold, TextAlignment.Center));
        header.Cells.Add(CreateStructuredCell("ΣΤΗΛΗ Β", FontWeights.Bold, TextAlignment.Center));
        group.Rows.Add(header);

        for (var i = 0; i < count; i++)
        {
            var left = i < displayedLeft.Count ? $"{i + 1}. {displayedLeft[i].Text}" : string.Empty;
            var right = i < displayedRight.Count ? $"{GetGreekLabel(i)}. {displayedRight[i].Text}" : string.Empty;
            var row = new TableRow();
            row.Cells.Add(CreateStructuredCell(left));
            row.Cells.Add(CreateStructuredCell(right));
            group.Rows.Add(row);
        }

        document.Blocks.Add(table);

        if (!answerKey)
        {
            var blanks = displayedLeft.Select((_, i) => $"{i + 1}. __________");
            document.Blocks.Add(new Paragraph(new Run("Αντιστοίχιση:  " + string.Join("     ", blanks)))
            {
                FontSize = 10,
                Margin = new Thickness(0, 4, 0, 5)
            });
        }
        else
        {
            var rightPositions = displayedRight
                .Select((item, index) => new { item.Id, Index = index })
                .ToDictionary(item => item.Id, item => item.Index);

            var mappings = new List<string>();
            for (var leftIndex = 0; leftIndex < displayedLeft.Count; leftIndex++)
            {
                var rightLabels = question.MatchingRelations
                    .Where(relation => relation.LeftItemId == displayedLeft[leftIndex].Id && rightPositions.ContainsKey(relation.RightItemId))
                    .Select(relation => rightPositions[relation.RightItemId])
                    .Distinct()
                    .OrderBy(index => index)
                    .Select(GetGreekLabel)
                    .ToList();

                mappings.Add($"{leftIndex + 1} → {(rightLabels.Count == 0 ? "—" : string.Join(", ", rightLabels))}");
            }

            document.Blocks.Add(AnswerParagraph("Σωστές αντιστοιχίσεις: " + string.Join("   |   ", mappings)));
        }

        AddQuestionSpacer(document);
    }

    private static List<T> DeterministicShuffle<T>(IEnumerable<T> source, int seed, bool avoidOriginalOrder = false)
    {
        var original = source.ToList();
        var list = original.ToList();
        var random = new Random(seed);
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        if (avoidOriginalOrder && list.Count > 1 && list.SequenceEqual(original))
            list = list.Skip(1).Concat(list.Take(1)).ToList();

        return list;
    }

    private static TableCell CreateStructuredCell(string text, FontWeight? weight = null, TextAlignment alignment = TextAlignment.Left)
    {
        var cell = CreateCell(text, weight, alignment);
        cell.BorderBrush = LineBrush;
        cell.BorderThickness = new Thickness(0.5);
        cell.Padding = new Thickness(5, 4, 5, 4);
        return cell;
    }

    private static void AddQuestionHeading(FlowDocument document, ExamQuestion question, double contentWidth, string? text)
    {
        var grid = new Grid { Width = contentWidth, Margin = new Thickness(0, 3, 0, 1), Background = Brushes.Transparent };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

        var questionText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Arial"),
            FontSize = 10,
            Foreground = PrintTextBrush,
            Margin = new Thickness(0, 2, 10, 2)
        };
        questionText.Inlines.Add(new Bold(new Run($"{question.Code}  ")));
        questionText.Inlines.Add(new Run(text ?? string.Empty));
        grid.Children.Add(questionText);

        var pointsText = new TextBlock
        {
            Text = $"Μονάδες {question.Points}",
            TextAlignment = TextAlignment.Right,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            FontFamily = new FontFamily("Arial"),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = PrintTextBrush,
            Margin = new Thickness(4, 2, 0, 2)
        };
        Grid.SetColumn(pointsText, 1);
        grid.Children.Add(pointsText);
        document.Blocks.Add(new BlockUIContainer(grid) { Margin = new Thickness(0) });
    }

    private static void AddAnswerArea(FlowDocument document, ExamQuestion question, double contentWidth)
    {
        var lineCount = Math.Max(0, question.AnswerLines);
        if (question.AnswerAreaType == AnswerAreaType.None || lineCount == 0) return;

        if (question.AnswerAreaType == AnswerAreaType.BlankBox)
        {
            document.Blocks.Add(new BlockUIContainer(new Border
            {
                Width = contentWidth,
                Height = Math.Max(34, lineCount * AnswerLineHeight),
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Margin = new Thickness(0, 5, 0, 3)
            }));
            return;
        }

        for (var line = 0; line < lineCount; line++)
        {
            document.Blocks.Add(new Paragraph(new Run(" "))
            {
                FontSize = 1,
                LineHeight = AnswerLineHeight,
                Margin = new Thickness(0),
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 0, 0.6)
            });
        }
    }

    private static void AddAnswerKeySection(FlowDocument document, ExamSection section, double contentWidth)
    {
        document.Blocks.Add(new Paragraph(new Run($"{section.Title} ({section.TotalPoints} μονάδες)"))
        {
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 10, 0, 4),
            KeepWithNext = true
        });

        AddSectionIntro(document, section);

        foreach (var question in section.Questions)
        {
            switch (question.Type)
            {
                case QuestionType.TrueFalse:
                    AddTrueFalseQuestion(document, question, contentWidth, answerKey: true);
                    break;
                case QuestionType.Matching:
                    AddMatchingQuestion(document, question, contentWidth, answerKey: true);
                    break;
                case QuestionType.FillBlank:
                    AddFillBlankQuestion(document, question, contentWidth, answerKey: true);
                    break;
                case QuestionType.MultipleChoice:
                    AddMultipleChoiceQuestion(document, question, contentWidth, answerKey: true);
                    break;
                default:
                    AddQuestionHeading(document, question, contentWidth, question.Text);
                    document.Blocks.Add(AnswerParagraph(string.IsNullOrWhiteSpace(question.ModelAnswer)
                        ? "Δεν έχει καταχωριστεί ενδεικτική απάντηση."
                        : question.ModelAnswer));
                    AddQuestionSpacer(document);
                    break;
            }
        }
    }

    private static Paragraph AnswerParagraph(string text) => new(new Run(text))
    {
        FontSize = 10,
        FontWeight = FontWeights.SemiBold,
        Background = SoftBackgroundBrush,
        BorderBrush = LineBrush,
        BorderThickness = new Thickness(0.7),
        Padding = new Thickness(7),
        Margin = new Thickness(0, 3, 0, 5)
    };

    private static void AddQuestionSpacer(FlowDocument document) =>
        document.Blocks.Add(new Paragraph { Margin = new Thickness(0, 0, 0, 3), FontSize = 2 });

    private static string GetGreekLabel(int index)
    {
        string[] labels = ["Α", "Β", "Γ", "Δ", "Ε", "ΣΤ", "Ζ", "Η", "Θ", "Ι", "Κ", "Λ", "Μ", "Ν", "Ξ", "Ο"];
        return index >= 0 && index < labels.Length ? labels[index] : (index + 1).ToString();
    }

    private static void AddFinalGradeAndSignature(
        FlowDocument document,
        ExamDocument exam,
        SchoolProfile school,
        double contentWidth)
    {
        var outer = new Border
        {
            Width = contentWidth,
            Height = 150,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
            Background = Brushes.White
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        outer.Child = grid;

        var gradePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
        gradePanel.Children.Add(new TextBlock { Text = "Τελική βαθμολογία", FontFamily = new FontFamily("Arial"), FontSize = 11, FontWeight = FontWeights.Bold, Foreground = PrintTextBrush });
        gradePanel.Children.Add(new TextBlock { Text = $"Σύνολο μονάδων θεμάτων: {exam.TotalPoints} / 100", FontFamily = new FontFamily("Arial"), FontSize = 9.5, Foreground = PrintTextBrush, Margin = new Thickness(0, 10, 0, 0) });
        gradePanel.Children.Add(new TextBlock { Text = "Τελικός βαθμός:  ______ / 100     ______ / 20", FontFamily = new FontFamily("Arial"), FontSize = 9.5, Foreground = PrintTextBrush, Margin = new Thickness(0, 12, 0, 0) });
        grid.Children.Add(gradePanel);

        var signatureBorder = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1, 0, 0, 0), Padding = new Thickness(18, 0, 0, 0), Margin = new Thickness(12, 0, 0, 0) };
        Grid.SetColumn(signatureBorder, 1);
        var signaturePanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        signaturePanel.Children.Add(new TextBlock { Text = "Ο/Η Καθηγητής/τρια", FontFamily = new FontFamily("Arial"), FontSize = 10, FontWeight = FontWeights.Bold, Foreground = PrintTextBrush, HorizontalAlignment = HorizontalAlignment.Center });
        signaturePanel.Children.Add(new TextBlock { Text = "Υπογραφή", FontFamily = new FontFamily("Arial"), FontSize = 9, Foreground = Brushes.DimGray, Margin = new Thickness(0, 7, 0, 50), HorizontalAlignment = HorizontalAlignment.Center });
        signaturePanel.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(school.TeacherName) ? "________________________" : school.TeacherName, FontFamily = new FontFamily("Arial"), FontSize = 9.5, Foreground = PrintTextBrush, HorizontalAlignment = HorizontalAlignment.Center });
        signatureBorder.Child = signaturePanel;
        grid.Children.Add(signatureBorder);

        document.Blocks.Add(new BlockUIContainer(outer) { Margin = new Thickness(0, 18, 0, 0) });
    }

    private static Paragraph CenteredParagraph(string? text, double size, FontWeight weight, Thickness margin, Brush? foreground = null) =>
        new(new Run(text ?? string.Empty))
        {
            TextAlignment = TextAlignment.Center,
            FontSize = size,
            FontWeight = weight,
            Margin = margin,
            Foreground = foreground ?? PrintTextBrush
        };
}
