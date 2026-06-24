using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ProjectChecking.Wpf.Models;

namespace ProjectChecking.Wpf.Services;

public static class PdfExporter
{
    public static void ExportProjects(string path, IList<ProjectDto> projects, string filterDescription)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(t => t.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("Учёт проектов").FontSize(16).SemiBold();
                    col.Item().Text($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(filterDescription))
                        col.Item().Text($"Фильтры: {filterDescription}").FontSize(9).Italic();
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); // Тема
                        c.RelativeColumn(2); // Заказчик
                        c.RelativeColumn(2); // Руководитель
                        c.RelativeColumn(1); // Дата создания
                        c.RelativeColumn(1); // Статус
                        c.RelativeColumn(1); // Дата завершения
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Тема").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Заказчик").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Руководитель").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Создан").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Статус").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Завершён").SemiBold();
                    });

                    foreach (var p in projects)
                    {
                        table.Cell().Element(BodyCell).Text(p.ActivityName ?? "");
                        table.Cell().Element(BodyCell).Text(p.Customer ?? "");
                        table.Cell().Element(BodyCell).Text(p.LeaderFullName ?? "—");
                        table.Cell().Element(BodyCell).Text(p.CreationDate.ToLocalTime().ToString("dd.MM.yyyy"));
                        table.Cell().Element(BodyCell).Text(p.Status ?? "");
                        table.Cell().Element(BodyCell).Text(p.CompletionDate?.ToLocalTime().ToString("dd.MM.yyyy") ?? "—");
                    }
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("Стр. ").FontSize(8);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" из ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        }).GeneratePdf(path);
    }

    private static IContainer HeaderCell(IContainer container)
        => container.Background(Colors.Grey.Lighten3).Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Medium);

    private static IContainer BodyCell(IContainer container)
        => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
}
