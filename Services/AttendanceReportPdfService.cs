using HRAttendance.Api.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HRAttendance.Api.Services;

public interface IAttendanceReportPdfService
{
    byte[] Generate(AttendanceReportResponse report);
}

public sealed class AttendanceReportPdfService : IAttendanceReportPdfService
{
    public byte[] Generate(AttendanceReportResponse report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text("HR Attendance Report")
                        .FontSize(20)
                        .Bold();

                    column.Item().Text($"Period: {report.FromDate:dd/MM/yyyy} - {report.ToDate:dd/MM/yyyy}")
                        .FontSize(10);
                });

                page.Content().PaddingVertical(12).Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Row(row =>
                    {
                        SummaryCard(row, "Employees", report.EmployeeCount.ToString());
                        SummaryCard(row, "Records", report.TotalRecords.ToString());
                        SummaryCard(row, "Present", report.PresentDays.ToString());
                        SummaryCard(row, "Absent", report.AbsentDays.ToString());
                        SummaryCard(row, "Late Days", report.LateDays.ToString());
                        SummaryCard(row, "Late Minutes", report.TotalLateMinutes.ToString());
                    });

                    column.Item().Text("Employee Summary").FontSize(13).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(45);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                        });

                        table.Header(header =>
                        {
                            HeaderCell(header.Cell(), "Code");
                            HeaderCell(header.Cell(), "Employee");
                            HeaderCell(header.Cell(), "Department");
                            HeaderCell(header.Cell(), "Present");
                            HeaderCell(header.Cell(), "Absent");
                            HeaderCell(header.Cell(), "Late");
                            HeaderCell(header.Cell(), "Late Min");
                            HeaderCell(header.Cell(), "Worked");
                        });

                        foreach (var employee in report.Employees)
                        {
                            BodyCell(table.Cell(), employee.Code);
                            BodyCell(table.Cell(), employee.FullName);
                            BodyCell(table.Cell(), employee.Department);
                            BodyCell(table.Cell(), employee.PresentDays.ToString());
                            BodyCell(table.Cell(), employee.AbsentDays.ToString());
                            BodyCell(table.Cell(), employee.LateDays.ToString());
                            BodyCell(table.Cell(), employee.TotalLateMinutes.ToString());
                            BodyCell(table.Cell(), FormatMinutes(employee.TotalWorkedMinutes));
                        }
                    });

                    foreach (var employee in report.Employees)
                    {
                        column.Item().Text($"Attendance Details - {employee.FullName}")
                            .FontSize(12)
                            .Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(75);
                                columns.ConstantColumn(65);
                                columns.ConstantColumn(65);
                                columns.ConstantColumn(65);
                                columns.ConstantColumn(75);
                            });

                            table.Header(header =>
                            {
                                HeaderCell(header.Cell(), "Date");
                                HeaderCell(header.Cell(), "Status");
                                HeaderCell(header.Cell(), "Check In");
                                HeaderCell(header.Cell(), "Check Out");
                                HeaderCell(header.Cell(), "Late Min");
                                HeaderCell(header.Cell(), "Worked");
                            });

                            foreach (var day in employee.Days)
                            {
                                BodyCell(table.Cell(), day.Date.ToString("dd/MM/yyyy"));
                                BodyCell(table.Cell(), day.Status.ToString());
                                BodyCell(table.Cell(), day.CheckIn?.ToString("HH:mm") ?? "-");
                                BodyCell(table.Cell(), day.CheckOut?.ToString("HH:mm") ?? "-");
                                BodyCell(table.Cell(), day.LateMinutes.ToString());
                                BodyCell(table.Cell(), FormatMinutes(day.WorkedMinutes));
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated by HRAttendance.Api  |  Page ");
                    text.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void SummaryCard(RowDescriptor row, string title, string value)
    {
        row.RelativeItem().Padding(3).Border(1).BorderColor(Colors.Grey.Lighten2).Column(column =>
        {
            column.Item().Text(title).FontSize(8);
            column.Item().Text(value).FontSize(14).Bold();
        });
    }

    private static void HeaderCell(IContainer container, string text)
    {
        container
            .Background(Colors.Grey.Lighten2)
            .BorderBottom(1)
            .Padding(5)
            .Text(text)
            .Bold();
    }

    private static void BodyCell(IContainer container, string text)
    {
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .Padding(4)
            .Text(text);
    }

    private static string FormatMinutes(int minutes)
        => $"{minutes / 60:00}:{minutes % 60:00}";
}
