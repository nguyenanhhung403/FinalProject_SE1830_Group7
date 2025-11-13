using System.Globalization;
using System.Linq;
using EVWarrantyManagement.BO.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EVWarrantyManagement.Services;

public interface IInvoicePdfBuilder
{
    byte[] Build(WarrantyHistory history, IReadOnlyCollection<UsedPart> usedParts);

    byte[] Build(ServiceBooking booking, IReadOnlyCollection<ServiceBookingPart> bookingParts);
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

    public byte[] Build(ServiceBooking booking, IReadOnlyCollection<ServiceBookingPart> bookingParts)
    {
        ArgumentNullException.ThrowIfNull(booking);

        var culture = CultureInfo.GetCultureInfo("en-US");
        var parts = bookingParts ?? Array.Empty<ServiceBookingPart>();
        var totalPartsCost = parts.Sum(p =>
        {
            var quantity = Math.Max(1, p.Quantity);
            var unitPrice = p.PartCost ?? p.Part?.UnitPrice ?? 0m;
            return unitPrice * quantity;
        });

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
                        column.Item().Text("EV Service Center Network").FontSize(18).SemiBold();
                        column.Item().Text("Service Booking Receipt").FontSize(12);
                        column.Item().Text($"Booking ID: #{booking.ServiceBookingId}");
                    });

                    row.ConstantItem(140).Column(column =>
                    {
                        column.Spacing(2);
                        column.Item().Text("Issued At").FontSize(10).SemiBold();
                        column.Item().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm", culture));
                        column.Item().Text("Completed At").FontSize(10).SemiBold();
                        column.Item().Text((booking.CompletedAt ?? booking.UpdatedAt).ToLocalTime().ToString("dd/MM/yyyy HH:mm", culture));
                    });
                });

                page.Content().PaddingVertical(20).Column(stack =>
                {
                    stack.Spacing(12);

                    stack.Item().Background(Colors.Grey.Lighten4).Padding(12).Column(section =>
                    {
                        section.Spacing(6);
                        section.Item().Text("Customer & Vehicle Information").FontSize(13).SemiBold();

                        section.Item().Text(text =>
                        {
                            text.Span("Customer: ").SemiBold();
                            text.Span(booking.Customer?.FullName ?? $"Customer #{booking.CustomerId}");
                        });
                        if (!string.IsNullOrWhiteSpace(booking.Customer?.Email))
                        {
                            section.Item().Text(text =>
                            {
                                text.Span("Email: ").SemiBold();
                                text.Span(booking.Customer!.Email);
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(booking.Customer?.Phone))
                        {
                            section.Item().Text(text =>
                            {
                                text.Span("Phone: ").SemiBold();
                                text.Span(booking.Customer!.Phone);
                            });
                        }

                        section.Item().Text(text =>
                        {
                            text.Span("Vehicle: ").SemiBold();
                            var vehicle = booking.Vehicle;
                            text.Span(vehicle == null
                                ? $"#{booking.VehicleId}"
                                : $"{vehicle.Model} ({vehicle.Year})");
                        });

                        section.Item().Text(text =>
                        {
                            text.Span("VIN: ").SemiBold();
                            text.Span(booking.Vehicle?.Vin ?? "N/A");
                        });

                        section.Item().Text(text =>
                        {
                            text.Span("Service Center: ").SemiBold();
                            text.Span(booking.ServiceCenter?.Name ?? $"Center #{booking.ServiceCenterId}");
                        });

                        if (!string.IsNullOrWhiteSpace(booking.ServiceCenter?.Address))
                        {
                            section.Item().Text(text =>
                            {
                                text.Span("Center Address: ").SemiBold();
                                text.Span(booking.ServiceCenter!.Address!);
                            });
                        }
                    });

                    stack.Item().Column(section =>
                    {
                        section.Spacing(6);
                        section.Item().Text("Service Details").FontSize(13).SemiBold();

                        section.Item().Text(text =>
                        {
                            text.Span("Service Type: ").SemiBold();
                            text.Span(booking.ServiceType);
                        });

                        section.Item().Text(text =>
                        {
                            text.Span("Assigned Technician: ").SemiBold();
                            var technician = booking.AssignedTechnician;
                            text.Span(technician?.FullName ?? $"Technician #{booking.AssignedTechnicianId}");
                        });

                        section.Item().Text(text =>
                        {
                            text.Span("Scheduled Time: ").SemiBold();
                            var start = (booking.ConfirmedStart ?? booking.PreferredStart).ToLocalTime();
                            var end = (booking.ConfirmedEnd ?? booking.PreferredEnd ?? start.AddMinutes(Math.Max(30, booking.EstimatedDurationMinutes))).ToLocalTime();
                            text.Span($"{start:dd/MM/yyyy HH:mm} - {end:dd/MM/yyyy HH:mm}");
                        });

                        if (!string.IsNullOrWhiteSpace(booking.CustomerNote))
                        {
                            section.Item().Text(text =>
                            {
                                text.Span("Customer Note: ").SemiBold();
                                text.Span(booking.CustomerNote);
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(booking.InternalNote))
                        {
                            section.Item().Text(text =>
                            {
                                text.Span("Service Notes: ").SemiBold();
                                text.Span(booking.InternalNote);
                            });
                        }
                    });

                    stack.Item().Column(section =>
                    {
                        section.Spacing(8);
                        section.Item().Text("Parts Used During Service").FontSize(13).SemiBold();

                        if (parts.Count == 0)
                        {
                            section.Item().Text("No parts were recorded for this booking.")
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
                                    header.Cell().Element(CellHeader).Text("Part");
                                    header.Cell().Element(CellHeader).AlignRight().Text("Quantity");
                                    header.Cell().Element(CellHeader).AlignRight().Text("Unit Price");
                                    header.Cell().Element(CellHeader).AlignRight().Text("Line Total");
                                });

                                foreach (var part in parts)
                                {
                                    var quantity = Math.Max(1, part.Quantity);
                                    var unitPrice = part.PartCost ?? part.Part?.UnitPrice ?? 0m;
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
                            text.Span("Total Parts Cost: ").SemiBold();
                            text.Span(totalPartsCost.ToString("C0", culture));
                        });

                        if (!string.IsNullOrWhiteSpace(booking.InternalNote))
                        {
                            section.Item().Text(text =>
                            {
                                text.Span("Technician Remarks: ").SemiBold();
                                text.Span(booking.InternalNote);
                            });
                        }
                    });
                });

                page.Footer().AlignCenter().Text("Thank you for trusting EV Service Center Network.")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }
}

