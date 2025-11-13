using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.Configuration;
using EVWarrantyManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace EVWarrantyManagement.Pages.Claims;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IWarrantyClaimService _claimService;
    private readonly IPartService _partService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<N8nSettings> _n8nOptions;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly IServiceCenterService _serviceCenterService;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    public DetailsModel(
        IWarrantyClaimService claimService,
        IPartService partService,
        IHttpClientFactory httpClientFactory,
        IOptions<N8nSettings> n8nOptions,
        IHubContext<NotificationHub> notificationHub,
        IServiceCenterService serviceCenterService)
    {
        _claimService = claimService;
        _partService = partService;
        _httpClientFactory = httpClientFactory;
        _n8nOptions = n8nOptions;
        _notificationHub = notificationHub;
        _serviceCenterService = serviceCenterService;
    }

    [BindProperty]
    public int ClaimId { get; set; }

    public WarrantyClaim? Claim { get; private set; }
    public IEnumerable<PartOptionViewModel> PartOptions { get; private set; } = Enumerable.Empty<PartOptionViewModel>();
    public IReadOnlyList<ServiceCenterTechnician> ServiceCenterTechnicians { get; private set; } = Array.Empty<ServiceCenterTechnician>();

    [BindProperty]
    public AddPartInputModel AddPartInput { get; set; } = new();

    [BindProperty]
    public int? SelectedTechnicianId { get; set; }

    public class AddPartInputModel
    {
        [Required]
        public int PartId { get; set; }
        [Range(1, 999)]
        public int Quantity { get; set; } = 1;
        [Range(0, double.MaxValue)]
        public decimal? PartCost { get; set; }
    }

    public record PartOptionViewModel(int PartId, string Display, decimal UnitPrice);

    private record N8nSuggestRequest(
        int ClaimId,
        string? Description,
        string? ImageUrl,
        string? VehicleModel,
        string? Vin,
        int? VehicleYear,
        string? Status,
        string? Note,
        DateTime? DateDiscovered,
        DateTime? CreatedAt,
        int RequestedByUserId,
        string? RequestedByUserName,
        string? ServiceCenterName,
        string? ServiceCenterAddress,
        N8nCustomerInfo? Customer,
        N8nTechnicianInfo? AssignedTechnician,
        IReadOnlyCollection<N8nUsedPart> UsedParts,
        IReadOnlyCollection<N8nAvailablePart> AvailableParts);

    private record N8nUsedPart(
        int PartId,
        string? PartCode,
        string? PartName,
        int Quantity,
        decimal? UnitCost);

    private record N8nAvailablePart(
        int PartId,
        string? PartCode,
        string? PartName,
        decimal? UnitPrice,
        int? WarrantyPeriodMonths);

    private record N8nCustomerInfo(
        int? CustomerId,
        string? FullName,
        string? Email,
        string? Phone);

    private record N8nTechnicianInfo(
        int? TechnicianId,
        string? FullName,
        string? Email);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ClaimId = id;
        Claim = await _claimService.GetClaimAsync(id);
        if (Claim == null) return RedirectToPage("Index");
        
        // Permission check for SC Technician: can only view claims assigned to them
        var isScTechnician = User.IsInRole("SC Technician") || (!User.IsInRole("SC Staff") && User.IsInRole("SC"));
        if (isScTechnician)
        {
            var userId = GetUserId();
            if (!Claim.TechnicianId.HasValue || Claim.TechnicianId.Value != userId)
            {
                return Forbid(); // Technician can only view claims assigned to them
            }
        }
        
        await HydrateUsedPartsAsync(Claim.UsedParts);
        await LoadLookupsAsync();
        
        // Load technicians for this service center
        if (Claim.ServiceCenterId > 0)
        {
            try
            {
                ServiceCenterTechnicians = await _serviceCenterService.GetTechniciansAsync(Claim.ServiceCenterId);
            }
            catch
            {
                ServiceCenterTechnicians = Array.Empty<ServiceCenterTechnician>();
            }
        }
        else
        {
            ServiceCenterTechnicians = Array.Empty<ServiceCenterTechnician>();
        }
        
        // Khôi phục AI suggestion từ Session nếu có (khi TempData đã bị xóa)
        var sessionKey = $"AiSuggestion_{id}";
        if (TempData["AiSuggestion"] == null && HttpContext.Session.GetString(sessionKey) is string savedSuggestion)
        {
            TempData["AiSuggestion"] = savedSuggestion;
        }
        
        return Page();
    }

    public async Task<IActionResult> OnPostAddPartAsync()
    {
        if (!ModelState.IsValid) return await Reload();

        var claim = await _claimService.GetClaimAsync(ClaimId);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id = ClaimId });
        }
        if (string.Equals(claim.StatusCode, "Completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(claim.StatusCode, "Archived", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Cannot add parts to a completed or archived claim.";
            return RedirectToPage(new { id = ClaimId });
        }

        if (AddPartInput.PartId <= 0)
        {
            ModelState.AddModelError(nameof(AddPartInput.PartId), "Please select a part.");
            return await Reload();
        }

        var part = await _partService.GetPartAsync(AddPartInput.PartId);
        if (part is null)
        {
            ModelState.AddModelError(nameof(AddPartInput.PartId), "Selected part not found.");
            return await Reload();
        }

        var quantity = Math.Max(1, AddPartInput.Quantity);
        decimal totalCost = AddPartInput.PartCost ?? part.UnitPrice ?? 0m;
        if (totalCost <= 0 && part.UnitPrice.HasValue)
        {
            totalCost = part.UnitPrice.Value * quantity;
        }
        var perUnitCost = quantity > 0 ? Math.Round(totalCost / quantity, 2) : totalCost;
        if (perUnitCost <= 0)
        {
            ModelState.AddModelError(nameof(AddPartInput.PartCost), "Cost must be greater than zero.");
            return await Reload();
        }

        var usedPart = new UsedPart
        {
            ClaimId = ClaimId,
            PartId = AddPartInput.PartId,
            Quantity = quantity,
            PartCost = perUnitCost
        };

        // Reserve stock before adding part
        try
        {
            await _partService.ReserveStockAsync(AddPartInput.PartId, quantity, ClaimId, GetUserId());
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return await Reload();
        }

        await _claimService.AddUsedPartAsync(usedPart, GetUserId());
        TempData["Success"] = $"Part added. {quantity} unit(s) reserved from stock.";
        
        // Get inventory after reservation
        var inventory = await _partService.GetInventoryAsync(AddPartInput.PartId);
        var remainingStock = inventory?.StockQuantity ?? 0;
        
        // Send notification about stock movement to Admin/EVM
        await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
            .SendAsync("ReceiveNotification", new
            {
                Type = "stock_movement",
                Title = "Stock Reserved",
                Message = $"📦 {quantity} unit(s) of {part.PartName} reserved for Claim #{ClaimId}. Remaining stock: {remainingStock}",
                PartId = AddPartInput.PartId,
                PartName = part.PartName,
                Quantity = quantity,
                ClaimId = ClaimId,
                StockQuantity = remainingStock
            });
        
        // Send SignalR update for Recent Part Updates (stock movement)
        await _notificationHub.Clients.All
            .SendAsync("ReceivePartUpdate", new
            {
                Type = "stock_movement",
                PartId = AddPartInput.PartId,
                PartName = part.PartName,
                Message = $"Stock reserved: {quantity} unit(s) of {part.PartName} for Claim #{ClaimId}",
                StockQuantity = remainingStock,
                MovementType = "RESERVED",
                Quantity = quantity,
                ReferenceType = "CLAIM",
                ReferenceId = ClaimId
            });
        
        // Check for low stock alert
        if (inventory != null && inventory.MinStockLevel.HasValue && inventory.StockQuantity < inventory.MinStockLevel.Value)
        {
            // Send low stock alert via SignalR
            await _notificationHub.Clients.Groups("Admin", "EVM Staff", "EVM")
                .SendAsync("ReceiveNotification", new
                {
                    Type = "low_stock_alert",
                    Title = "Low Stock Alert",
                    Message = $"⚠️ {part.PartName} is low on stock. Only {inventory.StockQuantity} units remaining (Min: {inventory.MinStockLevel})",
                    PartId = AddPartInput.PartId,
                    PartName = part.PartName,
                    StockQuantity = inventory.StockQuantity,
                    MinStockLevel = inventory.MinStockLevel.Value
                });
        }
        
        // Send SignalR notification to claim group
        var updatedClaim = await _claimService.GetClaimAsync(ClaimId);
        if (updatedClaim != null)
        {
            await _notificationHub.Clients.Group($"Claim_{ClaimId}")
                .SendAsync("ReceiveClaimUpdate", new
                {
                    ClaimId = ClaimId,
                    Type = "part_added",
                    PartName = part.PartName,
                    Quantity = quantity,
                    Message = $"Part {part.PartName} ({quantity} unit(s)) added to claim #{ClaimId}",
                    TotalCost = updatedClaim.TotalCost
                });
        }

        // Send SignalR update for Recent Part Updates (stock movement)
        await _notificationHub.Clients.All
            .SendAsync("ReceivePartUpdate", new
            {
                Type = "stock_movement",
                PartId = AddPartInput.PartId,
                PartName = part.PartName,
                Message = $"Stock reserved: {quantity} unit(s) of {part.PartName} for Claim #{ClaimId}",
                StockQuantity = remainingStock,
                MovementType = "RESERVED",
                Quantity = -quantity,
                ReferenceType = "CLAIM",
                ReferenceId = ClaimId
            });
        
        // Preserve AI suggestion từ Session khi redirect
        var sessionKey = $"AiSuggestion_{ClaimId}";
        if (HttpContext.Session.GetString(sessionKey) is string savedSuggestion)
        {
            TempData["AiSuggestion"] = savedSuggestion;
        }
        
        return RedirectToPage(new { id = ClaimId });
    }

    public async Task<IActionResult> OnPostDeleteUsedPartAsync(int id, int usedPartId)
    {
        ClaimId = id;

        if (!User.IsInRole("SC Technician") && !User.IsInRole("SC") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var claim = await _claimService.GetClaimAsync(id);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id });
        }

        if (string.Equals(claim.StatusCode, "Completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(claim.StatusCode, "Archived", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(claim.StatusCode, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Cannot modify parts for a completed/archived claim.";
            return RedirectToPage(new { id });
        }

        var usedParts = claim.UsedParts?.ToList() ?? new List<UsedPart>();
        await HydrateUsedPartsAsync(usedParts);
        var partToRemove = usedParts.FirstOrDefault(p => p.UsedPartId == usedPartId);
        if (partToRemove == null)
        {
            TempData["Error"] = "Used part not found in this claim.";
            return RedirectToPage(new { id });
        }

        // Get part info before removing
        var part = await _partService.GetPartAsync(partToRemove.PartId);
        var partName = part?.PartName ?? $"Part #{partToRemove.PartId}";
        var quantity = partToRemove.Quantity;

        await _claimService.RemoveUsedPartAsync(usedPartId, GetUserId());
        
        // Release stock when part is removed
        int remainingStock = 0;
        try
        {
            await _partService.ReleaseStockAsync(partToRemove.PartId, quantity, id, GetUserId());
            var inventory = await _partService.GetInventoryAsync(partToRemove.PartId);
            remainingStock = inventory?.StockQuantity ?? 0;
        }
        catch (Exception ex)
        {
            // Log error but don't fail the operation
            TempData["Warning"] = $"Part removed but stock release failed: {ex.Message}";
        }

        // Send SignalR update for Recent Part Updates (stock movement)
        await _notificationHub.Clients.All
            .SendAsync("ReceivePartUpdate", new
            {
                Type = "stock_movement",
                PartId = partToRemove.PartId,
                PartName = partName,
                Message = $"Stock released: {quantity} unit(s) of {partName} from Claim #{id}",
                StockQuantity = remainingStock,
                MovementType = "RELEASED",
                Quantity = quantity,
                ReferenceType = "CLAIM",
                ReferenceId = id
            });
        
        TempData["Success"] = "Đã xoá linh kiện khỏi yêu cầu.";
        
        // Preserve AI suggestion từ Session khi redirect
        var sessionKey = $"AiSuggestion_{id}";
        if (HttpContext.Session.GetString(sessionKey) is string savedSuggestion)
        {
            TempData["AiSuggestion"] = savedSuggestion;
        }
        
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAiSuggestAsync(int id)
    {
        ClaimId = id;
        if (!User.IsInRole("SC Technician") && !User.IsInRole("SC") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Reload claim from database to ensure we have latest data including UsedParts
        var claim = await _claimService.GetClaimAsync(id);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id });
        }

        // Ensure UsedParts are hydrated with Part information
        await HydrateUsedPartsAsync(claim.UsedParts);
        
        // Debug: Log used parts count
        var usedPartsCount = claim.UsedParts?.Count ?? 0;

        var settings = _n8nOptions.Value;
        if (string.IsNullOrWhiteSpace(settings.BaseUrl) || string.IsNullOrWhiteSpace(settings.WorkflowPath))
        {
            TempData["Error"] = "AI Suggest endpoint is not configured. Add N8n:BaseUrl and N8n:WorkflowPath to appsettings.json.";
            return RedirectToPage(new { id });
        }

        // Get user info for requested by
        var requestedByUser = claim.CreatedByUser;
        var requestedByName = requestedByUser?.FullName ?? $"User #{claim.CreatedByUserId}";

        // Get technician info if assigned
        N8nTechnicianInfo? technicianInfo = null;
        if (claim.TechnicianId.HasValue && claim.Technician != null)
        {
            technicianInfo = new N8nTechnicianInfo(
                claim.TechnicianId.Value,
                claim.Technician.FullName,
                claim.Technician.Email);
        }

        // Get customer info from vehicle
        N8nCustomerInfo? customerInfo = null;
        if (claim.Vehicle?.Customer != null)
        {
            var customer = claim.Vehicle.Customer;
            customerInfo = new N8nCustomerInfo(
                customer.CustomerId,
                customer.FullName,
                customer.Email,
                customer.Phone);
        }

        // Build used parts list - ensure we have Part information
        var usedPartsList = new List<N8nUsedPart>();
        if (claim.UsedParts != null && claim.UsedParts.Any())
        {
            foreach (var usedPart in claim.UsedParts)
            {
                // Ensure Part is loaded
                if (usedPart.Part == null && usedPart.PartId > 0)
                {
                    var part = await _partService.GetPartAsync(usedPart.PartId);
                    if (part is not null)
                    {
                        usedPart.Part = part;
                    }
                }

                usedPartsList.Add(new N8nUsedPart(
                    usedPart.PartId,
                    usedPart.Part?.PartCode,
                    usedPart.Part?.PartName,
                    usedPart.Quantity,
                    usedPart.PartCost));
            }
        }
        
        // Debug info
        if (usedPartsCount > 0 && usedPartsList.Count == 0)
        {
            // This shouldn't happen, but log it
            TempData["Warning"] = $"Warning: Found {usedPartsCount} used parts but couldn't build list. Check Part data.";
        }

        // Load all available parts from database for AI to recommend
        var allParts = await _partService.GetPartsAsync();
        var availablePartsList = allParts
            .Select(p => new N8nAvailablePart(
                p.PartId,
                p.PartCode,
                p.PartName,
                p.UnitPrice,
                p.WarrantyPeriodMonths))
            .OrderBy(p => p.PartName)
            .ToList();

        var payload = new N8nSuggestRequest(
            claim.ClaimId,
            claim.Description,
            BuildImageUrl(claim.ImageUrl),
            claim.Vehicle?.Model,
            claim.Vin,
            claim.Vehicle?.Year,
            claim.StatusCode,
            claim.Note,
            claim.DateDiscovered.ToDateTime(TimeOnly.MinValue),
            claim.CreatedAt,
            GetUserId(),
            requestedByName,
            claim.ServiceCenter?.Name,
            claim.ServiceCenter?.Address,
            customerInfo,
            technicianInfo,
            usedPartsList,
            availablePartsList);

        try
        {
            var client = _httpClientFactory.CreateClient("n8n");
            var endpoint = settings.WorkflowPath!.TrimStart('/');
            if (client.BaseAddress is null && Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            var response = await client.PostAsJsonAsync(endpoint, payload, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    
                    // n8n có thể trả về array hoặc object
                    var responseJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseBody, JsonOptions);
                    
                    string? aiSuggestion = null;
                    
                    // Kiểm tra nếu là array (format phổ biến từ n8n)
                    if (responseJson.ValueKind == System.Text.Json.JsonValueKind.Array && responseJson.GetArrayLength() > 0)
                    {
                        var firstItem = responseJson[0];
                        
                        // Thử lấy trực tiếp từ "output"
                        if (firstItem.TryGetProperty("output", out var outputProp))
                        {
                            aiSuggestion = outputProp.GetString();
                        }
                        // Thử lấy từ "json.output" (nếu n8n wrap trong json)
                        else if (firstItem.TryGetProperty("json", out var jsonProp))
                        {
                            if (jsonProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                if (jsonProp.TryGetProperty("output", out var jsonOutput))
                                {
                                    aiSuggestion = jsonOutput.GetString();
                                }
                            }
                            else if (jsonProp.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                // Nếu json là string, thử parse lại
                                try
                                {
                                    var innerJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(jsonProp.GetString()!, JsonOptions);
                                    if (innerJson.TryGetProperty("output", out var innerOutput))
                                    {
                                        aiSuggestion = innerOutput.GetString();
                                    }
                                }
                                catch
                                {
                                    // Ignore parse error
                                }
                            }
                        }
                    }
                    // Kiểm tra nếu là object
                    else if (responseJson.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (responseJson.TryGetProperty("output", out var outputProp))
                        {
                            aiSuggestion = outputProp.GetString();
                        }
                        else if (responseJson.TryGetProperty("json", out var jsonProp))
                        {
                            if (jsonProp.ValueKind == System.Text.Json.JsonValueKind.Object && jsonProp.TryGetProperty("output", out var jsonOutput))
                            {
                                aiSuggestion = jsonOutput.GetString();
                            }
                        }
                    }
                    
                    if (!string.IsNullOrWhiteSpace(aiSuggestion))
                    {
                        // Lưu AI suggestion vào Session để giữ lại khi thêm/xóa linh kiện
                        var sessionKey = $"AiSuggestion_{id}";
                        HttpContext.Session.SetString(sessionKey, aiSuggestion);
                        
                        // Cũng lưu vào TempData để hiển thị ngay
                        TempData["AiSuggestion"] = aiSuggestion;
                        var partsInfo = usedPartsList.Count > 0 
                            ? $" (Đã gửi {usedPartsList.Count} linh kiện đã sử dụng)" 
                            : " (Chưa có linh kiện đã sử dụng)";
                        TempData["Success"] = $"AI đã phân tích và đưa ra gợi ý{partsInfo}. Xem bên dưới.";
                    }
                    else
                    {
                        // Debug: log response body nếu không parse được
                        var partsInfo = usedPartsList.Count > 0 
                            ? $" (Đã gửi {usedPartsList.Count} linh kiện)" 
                            : "";
                        TempData["Warning"] = $"Đã gửi thông tin tới AI Suggest{partsInfo}, nhưng không tìm thấy 'output' trong response. Response: {responseBody.Substring(0, Math.Min(200, responseBody.Length))}...";
                        TempData["Success"] = $"Đã gửi thông tin tới AI Suggest{partsInfo}. Đang chờ phản hồi...";
                    }
                }
                catch (Exception parseEx)
                {
                    TempData["Error"] = $"Không thể parse response từ n8n: {parseEx.Message}";
                }
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"AI Suggest trả về lỗi {(int)response.StatusCode}. Hãy kiểm tra workflow n8n. Phản hồi: {body}";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Không thể kết nối tới n8n: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        if (!User.IsInRole("EVM Staff") && !User.IsInRole("EVM") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        
        var existingClaim = await _claimService.GetClaimAsync(id);
        await _claimService.ApproveClaimAsync(id, GetUserId(), "Approved", null);
        TempData["Success"] = "Approved.";
 
        var updatedClaim = await _claimService.GetClaimAsync(id);
        var updatePayload = BuildClaimUpdatePayload(id, updatedClaim, updatedClaim?.StatusCode ?? "Approved", existingClaim?.StatusCode ?? "Pending", $"Claim #{id} has been approved");

        await BroadcastClaimUpdateAsync(id, updatePayload);
 
        // Send notification to SC Staff and SC Technician
        await _notificationHub.Clients.Groups("SC Staff", "SC Technician", "SC")
            .SendAsync("ReceiveNotification", new
            {
                Type = "info",
                Title = "Claim Approved",
                Message = $"Claim #{id} has been approved by EVM" + (updatedClaim != null && !string.IsNullOrEmpty(updatedClaim.Vin) ? $" (VIN: {updatedClaim.Vin})" : ""),
                ClaimId = id
            });
        
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        if (!User.IsInRole("EVM Staff") && !User.IsInRole("EVM") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Get claim and used parts before rejecting
        var claim = await _claimService.GetClaimAsync(id);
        if (claim != null)
        {
            // Release all reserved stock for this claim
            var usedParts = claim.UsedParts?.ToList() ?? new List<UsedPart>();
            await HydrateUsedPartsAsync(usedParts);
            foreach (var usedPart in usedParts)
            {
                try
                {
                    await _partService.ReleaseStockAsync(usedPart.PartId, usedPart.Quantity, id, GetUserId());
                }
                catch (Exception)
                {
                    // Log error but continue releasing other parts
                }
            }
        }

        await _claimService.RejectClaimAsync(id, GetUserId(), "Rejected");
        TempData["Success"] = "Rejected. All reserved stock has been released.";
        
        var rejectedClaim = await _claimService.GetClaimAsync(id);
        var rejectPayload = BuildClaimUpdatePayload(id, rejectedClaim, rejectedClaim?.StatusCode ?? "Rejected", claim?.StatusCode, $"Claim #{id} has been rejected");

        await BroadcastClaimUpdateAsync(id, rejectPayload);
        
        // Send notification to SC Staff and SC Technician
        await _notificationHub.Clients.Groups("SC Staff", "SC Technician", "SC")
            .SendAsync("ReceiveNotification", new
            {
                Type = "warning",
                Title = "Claim Rejected",
                Message = $"Claim #{id} has been rejected by EVM" + (rejectedClaim != null && !string.IsNullOrEmpty(rejectedClaim.Vin) ? $" (VIN: {rejectedClaim.Vin})" : ""),
                ClaimId = id
            });
        
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostOnHoldAsync(int id)
    {
        if (!User.IsInRole("EVM Staff") && !User.IsInRole("EVM") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        var claim = await _claimService.GetClaimAsync(id);
        await _claimService.PutClaimOnHoldAsync(id, GetUserId(), "On hold");
        TempData["Success"] = "On hold.";
        
        // Send SignalR notification to claim group
        await _notificationHub.Clients.Group($"Claim_{id}")
            .SendAsync("ReceiveClaimUpdate", new
            {
                ClaimId = id,
                Type = "status_change",
                NewStatus = "OnHold",
                Message = $"Claim #{id} has been put on hold"
            });
        
        // Send notification to SC Staff and SC Technician
        await _notificationHub.Clients.Groups("SC Staff", "SC Technician", "SC")
            .SendAsync("ReceiveNotification", new
            {
                Type = "warning",
                Title = "Claim On Hold",
                Message = $"Claim #{id} has been put on hold by EVM" + (claim != null && !string.IsNullOrEmpty(claim.Vin) ? $" (VIN: {claim.Vin})" : ""),
                ClaimId = id
            });
        
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRevertToPendingAsync(int id)
    {
        if (!User.IsInRole("EVM Staff") && !User.IsInRole("EVM") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var claim = await _claimService.GetClaimAsync(id);
        if (claim == null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id });
        }

        if (!string.Equals(claim.StatusCode, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only Approved claims can be reverted to Pending.";
            return RedirectToPage(new { id });
        }

        // Release all reserved stock for this claim (similar to reject logic)
        if (claim.UsedParts != null)
        {
            var usedParts = claim.UsedParts.ToList();
            await HydrateUsedPartsAsync(usedParts);
            foreach (var usedPart in usedParts)
            {
                try
                {
                    await _partService.ReleaseStockAsync(usedPart.PartId, usedPart.Quantity, id, GetUserId());
                }
                catch (Exception)
                {
                    // Log error but continue releasing other parts
                }
            }
        }

        await _claimService.RevertToPendingAsync(id, GetUserId(), "Reverted to Pending");
        TempData["Success"] = "Claim reverted to Pending status. All reserved stock has been released.";

        var revertedClaim = await _claimService.GetClaimAsync(id);
        var revertPayload = BuildClaimUpdatePayload(id, revertedClaim, revertedClaim?.StatusCode ?? "Pending", claim.StatusCode, $"Claim #{id} has been reverted to Pending");

        await BroadcastClaimUpdateAsync(id, revertPayload);

        // Send notification to SC Staff and SC Technician
        await _notificationHub.Clients.Groups("SC Staff", "SC Technician", "SC")
            .SendAsync("ReceiveNotification", new
            {
                Type = "info",
                Title = "Claim Reverted to Pending",
                Message = $"Claim #{id} has been reverted to Pending by EVM" + (revertedClaim != null && !string.IsNullOrEmpty(revertedClaim.Vin) ? $" (VIN: {revertedClaim.Vin})" : ""),
                ClaimId = id
            });

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id, string? technicianNote)
    {
        if (!User.IsInRole("SC Technician") && !User.IsInRole("SC") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        var claim = await _claimService.GetClaimAsync(id);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id });
        }
        if (!string.Equals(claim.StatusCode, "InProgress", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Claim must be InProgress before completion.";
            return RedirectToPage(new { id });
        }

        // Mark reserved stock as consumed (OUT movement)
        var usedParts = claim.UsedParts?.ToList() ?? new List<UsedPart>();
        await HydrateUsedPartsAsync(usedParts);
        foreach (var usedPart in usedParts)
        {
            try
            {
                await _partService.ConsumeStockAsync(usedPart.PartId, usedPart.Quantity, id, GetUserId());
            }
            catch (Exception ex)
            {
                // Log error but continue
            }
        }

        var note = string.IsNullOrWhiteSpace(technicianNote) ? "Work completed" : technicianNote;
        await _claimService.CompleteClaimAsync(id, GetUserId(), DateOnly.FromDateTime(DateTime.UtcNow), note);
        TempData["Success"] = "Completed.";
        
        // Send SignalR notification
        var completedClaim = await _claimService.GetClaimAsync(id);
        await _notificationHub.Clients.Group($"Claim_{id}")
            .SendAsync("ReceiveClaimUpdate", new
            {
                ClaimId = id,
                Type = "status_change",
                NewStatus = "Completed",
                Message = $"Claim #{id} has been completed",
                TotalCost = completedClaim?.TotalCost
            });
        
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostStartAsync(int id, string? technicianNote)
    {
        if (!User.IsInRole("SC Technician") && !User.IsInRole("SC") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        var claim = await _claimService.GetClaimAsync(id);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id });
        }
        if (!string.Equals(claim.StatusCode, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Claim must be Approved before starting repair.";
            return RedirectToPage(new { id });
        }
        var note = string.IsNullOrWhiteSpace(technicianNote) ? "Start repair" : technicianNote;
        await _claimService.StartRepairAsync(id, GetUserId(), note);
        TempData["Success"] = "Started repair.";
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("EVM Staff") && !User.IsInRole("EVM"))
        {
            return Forbid();
        }
        var claim = await _claimService.GetClaimAsync(id);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id });
        }
        if (!string.Equals(claim.StatusCode, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only completed claims can be archived.";
            return RedirectToPage(new { id });
        }
        await _claimService.ArchiveClaimAsync(id, GetUserId(), "Archived");
        TempData["Success"] = "Archived.";
        
        // Send SignalR notification
        await _notificationHub.Clients.Group($"Claim_{id}")
            .SendAsync("ReceiveClaimUpdate", new
            {
                ClaimId = id,
                Type = "status_change",
                NewStatus = "Archived",
                Message = $"Claim #{id} has been archived"
            });
        
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignTechnicianAsync(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("EVM Staff") && !User.IsInRole("EVM"))
        {
            return Forbid();
        }

        if (!SelectedTechnicianId.HasValue)
        {
            TempData["Error"] = "Please select a technician.";
            return RedirectToPage(new { id });
        }

        var claim = await _claimService.GetClaimAsync(id);
        if (claim is null)
        {
            TempData["Error"] = "Claim not found.";
            return RedirectToPage(new { id });
        }

        if (string.Equals(claim.StatusCode, "Completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(claim.StatusCode, "Closed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(claim.StatusCode, "Archived", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Cannot assign technician to completed, closed, or archived claims.";
            return RedirectToPage(new { id });
        }

        var userId = GetUserId();
        await _claimService.AssignTechnicianAsync(id, SelectedTechnicianId.Value, userId);

        // SignalR notification
        var updatedClaim = await _claimService.GetClaimAsync(id);
        if (updatedClaim != null)
        {
            // Send notification to assigned technician
            await _notificationHub.Clients.User(SelectedTechnicianId.Value.ToString())
                .SendAsync("ReceiveNotification", new
                {
                    Type = "claim_assigned",
                    Title = "New Claim Assigned",
                    Message = $"Claim #{id} has been assigned to you",
                    ClaimId = id,
                    Vin = updatedClaim.Vin
                });

            // Build comprehensive claim update payload
            var claimUpdatePayload = BuildClaimUpdatePayload(id, updatedClaim, updatedClaim.StatusCode, claim?.StatusCode, $"Claim #{id} has been assigned to you");
            
            // Send both ReceiveNewClaim and ReceiveClaimUpdate to technician so claim appears in their list
            await _notificationHub.Clients.User(SelectedTechnicianId.Value.ToString())
                .SendAsync("ReceiveNewClaim", claimUpdatePayload);
            
            await _notificationHub.Clients.User(SelectedTechnicianId.Value.ToString())
                .SendAsync("ReceiveClaimUpdate", claimUpdatePayload);

            // Also send to SC Technician group so all technicians see the update
            await _notificationHub.Clients.Groups("SC Technician", "SC")
                .SendAsync("ReceiveClaimUpdate", claimUpdatePayload);

            // Send to claim group for details page updates
            await _notificationHub.Clients.Group($"Claim_{id}")
                .SendAsync("ReceiveClaimUpdate", new
                {
                    ClaimId = id,
                    Type = "technician_assigned",
                    TechnicianId = SelectedTechnicianId.Value,
                    Message = $"Technician assigned to claim #{id}"
                });
        }

        TempData["Success"] = "Technician assigned successfully.";
        return RedirectToPage(new { id });
    }

    private object BuildClaimUpdatePayload(int claimId, WarrantyClaim? claim, string newStatus, string? oldStatus, string message)
    {
        var resolvedStatus = string.IsNullOrWhiteSpace(newStatus)
            ? (claim?.StatusCode ?? string.Empty)
            : newStatus;

        return new
        {
            ClaimId = claim?.ClaimId ?? claimId,
            Type = "status_change",
            NewStatus = resolvedStatus,
            OldStatus = oldStatus,
            Message = message,
            StatusCode = resolvedStatus,
            Vin = claim?.Vin,
            VehicleModel = claim?.Vehicle?.Model,
            Description = claim?.Description,
            ServiceCenterName = claim?.ServiceCenter?.Name,
            DateDiscovered = claim != null ? claim.DateDiscovered.ToString("yyyy-MM-dd") : null,
            TechnicianId = claim?.TechnicianId,
            TechnicianName = claim?.Technician?.FullName,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Task BroadcastClaimUpdateAsync(int claimId, object payload)
    {
        var roleGroups = new[] { "Admin", "EVM Staff", "EVM", "SC Staff", "SC Technician", "SC" };

        return Task.WhenAll(
            _notificationHub.Clients.Group($"Claim_{claimId}").SendAsync("ReceiveClaimUpdate", payload),
            _notificationHub.Clients.Groups(roleGroups).SendAsync("ReceiveClaimUpdate", payload)
        );
    }

    private async Task<IActionResult> Reload()
    {
        Claim = await _claimService.GetClaimAsync(ClaimId);
        await HydrateUsedPartsAsync(Claim?.UsedParts);
        await LoadLookupsAsync();
        return Page();
    }

    private async Task LoadLookupsAsync()
    {
        var parts = await _partService.GetPartsAsync();
        PartOptions = parts
            .OrderBy(p => p.PartName)
            .Select(p => new PartOptionViewModel(
                p.PartId,
                string.IsNullOrWhiteSpace(p.PartCode) ? p.PartName ?? $"Part #{p.PartId}" : $"{p.PartCode} - {p.PartName}",
                p.UnitPrice ?? 0m))
            .ToList();
    }

    private string? BuildImageUrl(string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl))
        {
            return null;
        }

        if (Uri.TryCreate(storedUrl, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        var request = HttpContext?.Request;
        if (request is null)
        {
            return storedUrl;
        }

        // For localhost, use HTTP instead of HTTPS to avoid SSL issues with n8n
        var scheme = request.Scheme;
        var host = request.Host;
        if (host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || 
            host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            scheme = "http";
            
            // Check if ImageHostOverride is configured (for Docker scenarios)
            var imageHost = _n8nOptions.Value.ImageHostOverride;
            var targetHost = string.IsNullOrWhiteSpace(imageHost) ? "127.0.0.1" : imageHost;
            
            // Use HTTP port (5125) instead of HTTPS port (7092)
            if (host.Port == 7092)
            {
                host = new Microsoft.AspNetCore.Http.HostString($"{targetHost}:5125");
            }
            else if (host.Port == null)
            {
                host = new Microsoft.AspNetCore.Http.HostString($"{targetHost}:5125");
            }
            else
            {
                // Keep the port but change host
                host = new Microsoft.AspNetCore.Http.HostString($"{targetHost}:{host.Port}");
            }
        }

        return $"{scheme}://{host}{storedUrl}";
    }

    private async Task HydrateUsedPartsAsync(IEnumerable<UsedPart>? parts)
    {
        if (parts is null)
        {
            return;
        }

        var missingIds = parts
            .Where(p => p.Part is null)
            .Select(p => p.PartId)
            .Distinct()
            .ToList();

        foreach (var id in missingIds)
        {
            var part = await _partService.GetPartAsync(id);
            foreach (var usedPart in parts.Where(p => p.PartId == id))
            {
                usedPart.Part = part ?? usedPart.Part;
            }
        }
    }

    private int GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }
}

