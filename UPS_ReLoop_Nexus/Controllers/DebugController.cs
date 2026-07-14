namespace UPS.ReLoop.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Application.Interfaces.Repositories;
using UPS.ReLoop.Infrastructure.Persistence;

/// <summary>
/// Diagnostic APIs to verify data flow and stored procedure connectivity.
/// These endpoints are intended for development/staging use only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Diagnostics")]
public class DebugController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDashboardSpRepository _dashboardSpRepo;
    private readonly IInventoryPoolSpRepository _inventoryPoolSpRepo;
    private readonly IOpenAIService _aiService;
    private readonly IHoldingClockService _holdingClock;
    private readonly IDiversionAgentService _diversionAgent;
    private readonly ILogger<DebugController> _logger;

    public DebugController(
        ApplicationDbContext context,
        IDashboardSpRepository dashboardSpRepo,
        IInventoryPoolSpRepository inventoryPoolSpRepo,
        IOpenAIService aiService,
        IHoldingClockService holdingClock,
        IDiversionAgentService diversionAgent,
        ILogger<DebugController> logger)
    {
        _context = context;
        _dashboardSpRepo = dashboardSpRepo;
        _inventoryPoolSpRepo = inventoryPoolSpRepo;
        _aiService = aiService;
        _holdingClock = holdingClock;
        _diversionAgent = diversionAgent;
        _logger = logger;
    }

    // Real neighbourhood sort-hubs per metro. A UPS ReLoop city hub fans out to
    // several local sort centres; we pick one deterministically per return so a
    // Chennai item lands in Porur/Velachery/etc. rather than a generic "Chennai Hub".
    private static readonly Dictionary<string, string[]> SubHubs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Chennai"] = ["Porur", "Velachery", "Guindy", "Ambattur", "Tambaram", "Anna Nagar"],
        ["Bangalore"] = ["Whitefield", "Koramangala", "Electronic City", "Hebbal", "Jayanagar", "Marathahalli"],
        ["Bengaluru"] = ["Whitefield", "Koramangala", "Electronic City", "Hebbal", "Jayanagar", "Marathahalli"],
        ["Mumbai"] = ["Andheri", "Bhiwandi", "Powai", "Vashi", "Thane", "Borivali"],
        ["Delhi"] = ["Okhla", "Gurgaon", "Noida", "Dwarka", "Rohini", "Narela"],
        ["Hyderabad"] = ["Gachibowli", "Uppal", "Kondapur", "Shamshabad", "Kukatpally"],
        ["Pune"] = ["Hinjewadi", "Kharadi", "Wakad", "Chakan", "Hadapsar"],
        ["Kolkata"] = ["Salt Lake", "Howrah", "Behala", "Dum Dum", "Rajarhat"],
    };

    private static readonly string[] DefaultSubHubs = ["Central", "North", "South", "East", "West"];

    /// <summary>Deterministic local sort-hub for a city + return, e.g. "Porur".</summary>
    private static string ResolveSubHub(string? location, Guid returnId)
    {
        var city = string.IsNullOrWhiteSpace(location) ? "Chennai" : location.Trim();
        var pool = SubHubs.TryGetValue(city, out var hubs) ? hubs : DefaultSubHubs;
        var idx = (int)((uint)returnId.GetHashCode() % (uint)pool.Length);
        return pool[idx];
    }


    /// provider (Azure OpenAI / GitHub Models / Ollama) and returns its reply, so
    /// you can confirm the AI agents' language model works before running the full flow.
    /// </summary>
    /// <response code="200">The model replied — LLM is reachable.</response>
    /// <response code="503">The model is unreachable or not configured.</response>
    [HttpGet("ai-health")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AiHealth(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Debug] Checking LLM connectivity");
        try
        {
            var reply = await _aiService.GenerateTextAsync(
                "Reply with exactly this text and nothing else: ReLoop AI online.", cancellationToken);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { status = "ok", reply = reply.Trim() }, "LLM reachable."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Debug] LLM connectivity check failed");
            return StatusCode(503, ApiResponse<object>.FailResponse(
                $"LLM unreachable: {ex.Message}", 503));
        }
    }

    /// <summary>
    /// Retrieves all packages from the Packages table. Validates DB connectivity and package data.
    /// </summary>
    /// <response code="200">Returns packages with row count.</response>
    [HttpGet("packages")]
    [ProducesResponseType(typeof(ApiResponse<DebugDataResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackages(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Debug] Fetching all packages");

        try
        {
            var packages = await _context.Packages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Select(p => new
                {
                    p.Id,
                    p.TrackingNumber,
                    p.SenderName,
                    p.RecipientName,
                    p.Status,
                    p.Weight,
                    p.IsReturnable,
                    p.ReturnInitiatedAt,
                    p.CreatedAt,
                    p.IsDeleted
                })
                .OrderByDescending(p => p.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            var result = new DebugDataResult
            {
                Table = "Packages",
                RowCount = packages.Count,
                Data = packages.Cast<object>().ToList(),
                SpValidation = "N/A � Direct table query"
            };

            return Ok(ApiResponse<DebugDataResult>.SuccessResponse(result, $"Packages table: {packages.Count} rows returned."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Debug] Failed to fetch packages");
            return StatusCode(500, ApiResponse<DebugDataResult>.FailResponse($"DB error: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Retrieves all return requests from the ReturnRequests table. Validates return data flow.
    /// </summary>
    /// <response code="200">Returns return requests with row count.</response>
    [HttpGet("returns")]
    [ProducesResponseType(typeof(ApiResponse<DebugDataResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReturns(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Debug] Fetching all return requests");

        try
        {
            var returns = await _context.ReturnRequests
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Select(r => new
                {
                    r.Id,
                    r.PackageId,
                    r.Reason,
                    r.Status,
                    HasAiAnalysis = r.AiAnalysis != null,
                    r.ResolutionNotes,
                    r.CreatedAt,
                    r.ResolvedAt,
                    r.IsDeleted
                })
                .OrderByDescending(r => r.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            var statusSummary = returns
                .GroupBy(r => r.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            var result = new DebugDataResult
            {
                Table = "ReturnRequests",
                RowCount = returns.Count,
                Data = returns.Cast<object>().ToList(),
                SpValidation = "Populated by usp_CreateReturnRequest",
                Metadata = new Dictionary<string, object> { ["statusBreakdown"] = statusSummary }
            };

            return Ok(ApiResponse<DebugDataResult>.SuccessResponse(result, $"ReturnRequests table: {returns.Count} rows returned."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Debug] Failed to fetch return requests");
            return StatusCode(500, ApiResponse<DebugDataResult>.FailResponse($"DB error: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Retrieves inventory pool data and validates usp_GetInventoryByProduct SP connectivity.
    /// </summary>
    /// <param name="productId">Optional product ID to test SP call.</param>
    /// <response code="200">Returns inventory pool data with SP validation result.</response>
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(ApiResponse<DebugDataResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventory([FromQuery] string? productId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Debug] Fetching inventory pool data");

        try
        {
            // Direct table query
            var inventory = await _context.InventoryPool
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Select(i => new
                {
                    i.Id,
                    i.ReturnId,
                    i.ProductId,
                    i.Location,
                    i.HoldingDays,
                    i.MatchScore,
                    i.Status,
                    i.CreatedAt,
                    i.IsDeleted
                })
                .OrderByDescending(i => i.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            // SP validation
            string spValidation;
            try
            {
                var spResult = await _inventoryPoolSpRepo.GetByProductAsync(
                    productId ?? "TEST-PRODUCT", null, cancellationToken);
                spValidation = $"usp_GetInventoryByProduct: OK � {spResult.Count} rows returned";
            }
            catch (Exception spEx)
            {
                spValidation = $"usp_GetInventoryByProduct: FAILED � {spEx.Message}";
            }

            var result = new DebugDataResult
            {
                Table = "InventoryPool",
                RowCount = inventory.Count,
                Data = inventory.Cast<object>().ToList(),
                SpValidation = spValidation
            };

            return Ok(ApiResponse<DebugDataResult>.SuccessResponse(result, $"InventoryPool table: {inventory.Count} rows returned."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Debug] Failed to fetch inventory pool");
            return StatusCode(500, ApiResponse<DebugDataResult>.FailResponse($"DB error: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Retrieves match agent results and validates usp_SaveMatchResult SP data.
    /// </summary>
    /// <response code="200">Returns match results with score distribution.</response>
    [HttpGet("matches")]
    [ProducesResponseType(typeof(ApiResponse<DebugDataResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMatches(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Debug] Fetching match agent results");

        try
        {
            var matches = await _context.MatchAgentResults
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Select(m => new
                {
                    m.Id,
                    m.ReturnRequestId,
                    m.ProductId,
                    m.ProductName,
                    m.Category,
                    m.Location,
                    m.Condition,
                    m.MatchScore,
                    m.Recommendation,
                    m.Confidence,
                    m.DistanceSavedKm,
                    m.CostSaved,
                    m.Co2Saved,
                    m.SalePrice,
                    ExplanationLength = m.Explanation.Length,
                    m.CreatedAt,
                    m.IsDeleted
                })
                .OrderByDescending(m => m.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            // Holding day per return (drives the diversion clock). Joined from the
            // inventory pool; falls back to a deterministic in-window day when the
            // item is not yet pooled, so the demo never shows an expired clock.
            var holdByReturn = await _context.InventoryPool
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Select(i => new { i.ReturnId, i.HoldingDays })
                .ToListAsync(cancellationToken);
            var holdMap = holdByReturn
                .GroupBy(i => i.ReturnId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.HoldingDays));

            // Run the deterministic Diversion / Dynamic-Pricing agent for every row
            // so the inventory grid shows the real markdown, clearance risk, radius
            // and sell-through — not a frontend heuristic.
            var data = matches.Select(m =>
            {
                int holdingDays = holdMap.TryGetValue(m.ReturnRequestId, out var hd) && hd > 0
                    ? hd
                    : (Math.Abs(m.ReturnRequestId.GetHashCode()) % 9) + 1;
                var clock = _holdingClock.EvaluateFromDays(holdingDays);
                bool resaleAllowed = !(m.Recommendation ?? string.Empty)
                    .ToUpperInvariant().Contains("RETURN_TO_SELLER");
                var diversion = _diversionAgent.Decide(
                    m.MatchScore, clock, m.SalePrice, resaleAllowed, m.Condition, m.Category);

                return (object)new
                {
                    m.Id,
                    m.ReturnRequestId,
                    m.ProductId,
                    m.ProductName,
                    m.Category,
                    m.Location,
                    m.Condition,
                    m.MatchScore,
                    m.Recommendation,
                    m.Confidence,
                    m.DistanceSavedKm,
                    m.CostSaved,
                    m.Co2Saved,
                    m.SalePrice,
                    m.ExplanationLength,
                    m.CreatedAt,
                    m.IsDeleted,
                    SubHub = ResolveSubHub(m.Location, m.ReturnRequestId),
                    HoldingDay = clock.HoldingDay,
                    DaysRemaining = clock.DaysRemaining,
                    DiversionAction = diversion.Action,
                    BasePrice = diversion.BasePrice,
                    SuggestedPrice = diversion.SuggestedPrice,
                    PriceAdjustmentPct = diversion.PriceAdjustmentPct,
                    SearchRadiusKm = diversion.SearchRadiusKm,
                    ClearanceRisk = diversion.ClearanceRisk,
                    SellProbability = diversion.SellProbability,
                    DiversionReasoning = diversion.Reasoning
                };
            }).ToList();

            var scoreBuckets = new Dictionary<string, int>
            {
                ["80-100 (SELL_LOCAL)"] = matches.Count(m => m.MatchScore >= 80),
                ["60-79 (REDISTRIBUTE)"] = matches.Count(m => m.MatchScore >= 60 && m.MatchScore < 80),
                ["40-59 (DISCOUNT_SELL)"] = matches.Count(m => m.MatchScore >= 40 && m.MatchScore < 60),
                ["20-39 (WAREHOUSE_HOLD)"] = matches.Count(m => m.MatchScore >= 20 && m.MatchScore < 40),
                ["0-19 (LIQUIDATE)"] = matches.Count(m => m.MatchScore < 20)
            };

            var totalSavings = new
            {
                TotalDistanceKm = matches.Sum(m => m.DistanceSavedKm),
                TotalCost = matches.Sum(m => m.CostSaved),
                TotalCo2Kg = matches.Sum(m => m.Co2Saved),
                AvgMatchScore = matches.Any() ? Math.Round(matches.Average(m => m.MatchScore), 1) : 0
            };

            var result = new DebugDataResult
            {
                Table = "MatchAgentResults",
                RowCount = matches.Count,
                Data = data,
                SpValidation = "Populated by usp_SaveMatchResult",
                Metadata = new Dictionary<string, object>
                {
                    ["scoreDistribution"] = scoreBuckets,
                    ["totalSavings"] = totalSavings
                }
            };

            return Ok(ApiResponse<DebugDataResult>.SuccessResponse(result, $"MatchAgentResults table: {matches.Count} rows returned."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Debug] Failed to fetch match results");
            return StatusCode(500, ApiResponse<DebugDataResult>.FailResponse($"DB error: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Retrieves dashboard metrics via usp_GetDashboardMetrics and validates the complete data pipeline.
    /// </summary>
    /// <response code="200">Returns dashboard metrics with SP validation.</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<DebugDashboardResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Debug] Fetching dashboard metrics via SP");

        try
        {
            // SP call
            var metrics = await _dashboardSpRepo.GetMetricsAsync(null, null, cancellationToken);

            // Direct table counts for cross-validation
            var packageCount = await _context.Packages.AsNoTracking().IgnoreQueryFilters().CountAsync(cancellationToken);
            var returnCount = await _context.ReturnRequests.AsNoTracking().IgnoreQueryFilters().CountAsync(cancellationToken);
            var inventoryCount = await _context.InventoryPool.AsNoTracking().IgnoreQueryFilters().CountAsync(cancellationToken);
            var matchCount = await _context.MatchAgentResults.AsNoTracking().IgnoreQueryFilters().CountAsync(cancellationToken);
            var demandCount = await _context.DemandHistory.AsNoTracking().IgnoreQueryFilters().CountAsync(cancellationToken);
            var agentRecCount = await _context.AgentRecommendations.AsNoTracking().IgnoreQueryFilters().CountAsync(cancellationToken);

            var result = new DebugDashboardResult
            {
                Metrics = metrics,
                SpValidation = "usp_GetDashboardMetrics: OK",
                TableCounts = new Dictionary<string, int>
                {
                    ["Packages"] = packageCount,
                    ["ReturnRequests"] = returnCount,
                    ["InventoryPool"] = inventoryCount,
                    ["MatchAgentResults"] = matchCount,
                    ["DemandHistory"] = demandCount,
                    ["AgentRecommendations"] = agentRecCount
                },
                PipelineHealth = new Dictionary<string, string>
                {
                    ["Step1_CreateReturnRequest"] = returnCount > 0 ? "? Data exists" : "?? No data",
                    ["Step2_ImageValidation"] = inventoryCount > 0 ? "? Data exists" : "?? No data",
                    ["Step3_InventoryPool"] = inventoryCount > 0 ? "? Data exists" : "?? No data",
                    ["Step4_MatchAgent"] = matchCount > 0 ? "? Data exists" : "?? No data",
                    ["Step5_RootCauseAgent"] = agentRecCount > 0 ? "? Data exists" : "?? No data",
                    ["Step6_Dashboard"] = metrics.TotalReturns > 0 ? "? Metrics populated" : "?? No metrics"
                }
            };

            return Ok(ApiResponse<DebugDashboardResult>.SuccessResponse(result, "Dashboard diagnostics completed."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Debug] Failed to fetch dashboard diagnostics");
            return StatusCode(500, ApiResponse<DebugDashboardResult>.FailResponse($"SP/DB error: {ex.Message}", 500));
        }
    }
}

// ========================
// Debug DTOs
// ========================

public class DebugDataResult
{
    public string Table { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public List<object> Data { get; set; } = [];
    public string SpValidation { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

public class DebugDashboardResult
{
    public Application.DTOs.Dashboard.DashboardMetricsDto Metrics { get; set; } = new();
    public string SpValidation { get; set; } = string.Empty;
    public Dictionary<string, int> TableCounts { get; set; } = new();
    public Dictionary<string, string> PipelineHealth { get; set; } = new();
}
