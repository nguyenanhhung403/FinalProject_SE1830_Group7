using System.Globalization;
using EVWarrantyManagement.BO.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EVWarrantyManagement.Services;

public interface IInvoicePdfBuilder
{
    byte[] Build(WarrantyHistory history, IReadOnlyCollection<UsedPart> usedParts);
}

public class QuestPdfInvoiceBuilder : IInvoicePdfBuilder
{
    public byte[] Build(WarrantyHistory history, IReadOnlyCollection<UsedPart> usedParts)
    {
        ArgumentNullException.ThrowIfNull(history);

        var culture = CultureInfo.GetCultureInfo("en-US");
        var parts = usedParts ?? Array.Empty<UsedPart>();
        var totalPartsCost = parts.Sum(p => (p.PartCost ?? 0m) * Math.Max(1, p.Quantity));

        var archivedAt = history.ArchivedAt;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Spacing(4);
                        column.Item().Text("EV Warranty Service Center").FontSize(18).SemiBold();
                        column.Item().Text("Biên nhận sửa chữa & thay thế linh kiện").FontSize(12);
                        column.Item().Text($"Mã yêu cầu: #{history.ClaimId}");
                    });

                    row.ConstantItem(140).Column(column =>
                    {
                        column.Spacing(2);
                        column.Item().Text("Ngày xuất").FontSize(10).SemiBold();
                        column.Item().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm", culture));
                        column.Item().Text("Ngày lưu trữ").FontSize(10).SemiBold();
                        column.Item().Text(archivedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", culture));
                    });
                });

                page.Content().PaddingVertical(20).Column(stack =>
                {
                    stack.Spacing(12);

                    stack.Item().Background(Colors.Grey.Lighten4).Padding(12).Column(section =>
                    {
                        section.Spacing(4);
                        section.Item().Text("Thông tin xe & khách hàng").FontSize(13).SemiBold();
                        section.Item().Text(text =>
                        {
                            text.Span("Dòng xe: ").SemiBold();
                            text.Span(history.Vehicle?.Model ?? "Không xác định");
                        });
                        section.Item().Text(text =>
                        {
                            text.Span("VIN: ").SemiBold();
                            text.Span(history.Vin ?? "N/A");
                        });
                        section.Item().Text(text =>
                        {
                            text.Span("Trung tâm dịch vụ: ").SemiBold();
                            text.Span(history.ServiceCenter?.Name ?? $"#{history.ServiceCenterId}");
                        });
                        section.Item().Text(text =>
                        {
                            text.Span("Kỹ thuật viên hoàn tất: ").SemiBold();
                            text.Span(history.CompletedByUser?.FullName ?? $"User #{history.CompletedByUserId}");
                        });
                        if (!string.IsNullOrWhiteSpace(history.Note))
                        {
                            section.Item().Text(text =>
                            {
                                text.Span("Ghi chú hoàn tất: ").SemiBold();
                                text.Span(history.Note);
                            });
                        }
                    });

                    stack.Item().Column(section =>
                    {
                        section.Spacing(8);
                        section.Item().Text("Danh sách linh kiện đã sử dụng").FontSize(13).SemiBold();

                        if (parts.Count == 0)
                        {
                            section.Item().Text("Chưa ghi nhận linh kiện sử dụng.")
                                .Italic()
                                .FontColor(Colors.Grey.Darken2);
                        }
                        else
                        {
                            section.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(5);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellHeader).Text("Linh kiện");
                                    header.Cell().Element(CellHeader).AlignRight().Text("Số lượng");
                                    header.Cell().Element(CellHeader).AlignRight().Text("Đơn giá");
                                    header.Cell().Element(CellHeader).AlignRight().Text("Thành tiền");
                                });

                                foreach (var part in parts)
                                {
                                    var quantity = Math.Max(1, part.Quantity);
                                    var unitPrice = part.PartCost ?? 0m;
                                    var lineTotal = unitPrice * quantity;
                                    var partName = part.Part is null
                                        ? $"Part #{part.PartId}"
                                        : string.IsNullOrWhiteSpace(part.Part.PartCode)
                                            ? part.Part.PartName
                                            : $"{part.Part.PartCode} - {part.Part.PartName}";

                                    table.Cell().Element(CellBody).Text(partName);
                                    table.Cell().Element(CellBody).AlignRight().Text(quantity.ToString("N0", culture));
                                    table.Cell().Element(CellBody).AlignRight().Text(unitPrice.ToString("C0", culture));
                                    table.Cell().Element(CellBody).AlignRight().Text(lineTotal.ToString("C0", culture));
                                }
                            });
                        }
                    });

                    stack.Item().AlignRight().Column(section =>
                    {
                        section.Spacing(4);
                        section.Item().Text(text =>
                        {
                            text.Span("Tổng chi phí linh kiện: ").SemiBold();
                            text.Span(totalPartsCost.ToString("C0", culture));
                        });
                        if (history.TotalCost.HasValue)
                        {
                            section.Item().Text(text =>
                            {
                                text.Span("Tổng chi phí đã ghi nhận: ").SemiBold();
                                text.Span(history.TotalCost.Value.ToString("C0", culture));
                            });
                        }
                    });
                });

                page.Footer().AlignCenter().Text("Cảm ơn quý khách đã sử dụng dịch vụ của chúng tôi.")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer CellHeader(IContainer container) =>
        container.DefaultTextStyle(x => x.SemiBold())
            .PaddingVertical(4)
            .PaddingHorizontal(6)
            .Background(Colors.Grey.Lighten3);

    private static IContainer CellBody(IContainer container) =>
        container.PaddingVertical(4).PaddingHorizontal(6);
}

