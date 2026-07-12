namespace UPS.ReLoop.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using UPS.ReLoop.Application.DTOs.Decision;
using UPS.ReLoop.Application.Services;
using Xunit;

public class HoldingClockServiceTests
{
    private readonly HoldingClockService _clock = new(NullLogger<HoldingClockService>.Instance);

    [Theory]
    [InlineData(0, 10, false, "OnTrack")]
    [InlineData(3, 7, false, "OnTrack")]
    [InlineData(8, 2, false, "ClosingWindow")]
    [InlineData(9, 1, false, "ClosingWindow")]
    [InlineData(10, 0, true, "Expired")]
    [InlineData(12, 0, true, "Expired")]
    public void EvaluateFromDays_ComputesRemainingAndStatus(int day, int remaining, bool expired, string status)
    {
        var result = _clock.EvaluateFromDays(day);

        Assert.Equal(remaining, result.DaysRemaining);
        Assert.Equal(expired, result.IsExpired);
        Assert.Equal(expired, result.AutoReturnTriggered);
        Assert.Equal(status, result.ClockStatus);
    }

    [Fact]
    public void Evaluate_FromPickupDate_ComputesElapsedDays()
    {
        var now = new DateTime(2026, 1, 20);
        var pickup = new DateTime(2026, 1, 15);

        var result = _clock.Evaluate(pickup, now);

        Assert.Equal(5, result.HoldingDay);
        Assert.Equal(5, result.DaysRemaining);
        Assert.False(result.IsExpired);
    }

    [Fact]
    public void EvaluateFromDays_NegativeInput_ClampsToZero()
    {
        var result = _clock.EvaluateFromDays(-3);
        Assert.Equal(0, result.HoldingDay);
    }
}

public class RetailerPolicyServiceTests
{
    private readonly RetailerPolicyService _policy = new(
        NullLogger<RetailerPolicyService>.Instance,
        new PolicyRetriever(NullLogger<PolicyRetriever>.Instance));

    [Theory]
    [InlineData("apparel", true)]
    [InlineData("general", true)]
    [InlineData("footwear", true)]
    public void Evaluate_AllowedCategories_PermitsResale(string category, bool allowed)
    {
        var result = _policy.Evaluate(category);
        Assert.Equal(allowed, result.ResaleAllowed);
        Assert.False(result.IsRestrictedCategory);
    }

    [Theory]
    [InlineData("hygiene")]
    [InlineData("food")]
    [InlineData("serialized")]
    [InlineData("medical")]
    public void Evaluate_RestrictedCategories_BlockResaleWithCitation(string category)
    {
        var result = _policy.Evaluate(category);

        Assert.False(result.ResaleAllowed);
        Assert.True(result.IsRestrictedCategory);
        Assert.NotEmpty(result.PolicyRef);
    }

    [Fact]
    public void Evaluate_UnknownCategory_DefaultsToReturnToSeller()
    {
        var result = _policy.Evaluate("mystery-goods");
        Assert.False(result.ResaleAllowed);
        Assert.Equal("RP-DEF-0.0", result.PolicyRef);
    }

    [Fact]
    public void GetCitation_ReturnsPolicySource()
    {
        var citation = _policy.GetCitation("apparel");
        Assert.Equal("policy", citation.SourceType);
        Assert.Equal("RP-APP-2.1", citation.RefId);
    }
}

public class DiversionAgentServiceTests
{
    private readonly DiversionAgentService _agent = new(NullLogger<DiversionAgentService>.Instance);
    private readonly HoldingClockService _clock = new(NullLogger<HoldingClockService>.Instance);

    [Fact]
    public void Decide_StrongMatchEarly_SellsLocalAtBasePrice()
    {
        var decision = _agent.Decide(85, _clock.EvaluateFromDays(2), 40m, resaleAllowed: true);

        Assert.Equal("SELL_LOCAL", decision.Action);
        Assert.Equal(40m, decision.SuggestedPrice);
    }

    [Fact]
    public void Decide_PolicyBlocked_ReturnsToSeller()
    {
        var decision = _agent.Decide(90, _clock.EvaluateFromDays(1), 40m, resaleAllowed: false);
        Assert.Equal("RETURN_TO_SELLER", decision.Action);
    }

    [Fact]
    public void Decide_ClockExpired_ReturnsToSeller()
    {
        var decision = _agent.Decide(80, _clock.EvaluateFromDays(10), 40m, resaleAllowed: true);
        Assert.Equal("RETURN_TO_SELLER", decision.Action);
    }

    [Fact]
    public void Decide_LateWindowMediumMatch_DiscountsAndWidensRadius()
    {
        var decision = _agent.Decide(50, _clock.EvaluateFromDays(9), 40m, resaleAllowed: true);

        Assert.True(decision.SuggestedPrice < 40m);
        Assert.True(decision.SearchRadiusKm > 10.0);
        Assert.Equal("OFFER_ACCESS_POINTS", decision.Action);
    }

    [Fact]
    public void Decide_WeakDemandLateWindow_Escalates()
    {
        var decision = _agent.Decide(10, _clock.EvaluateFromDays(9), 40m, resaleAllowed: true);

        Assert.Equal("ESCALATE", decision.Action);
        Assert.True(decision.Escalated);
    }
}

public class DecisionConfidenceEvaluatorTests
{
    [Fact]
    public void Evaluate_PolicyRestricted_IsHighConfidenceNoEscalation()
    {
        var result = DecisionConfidenceEvaluator.Evaluate(null, 0, policyResolved: true, policyRestricted: true);

        Assert.Equal("High", result.Band);
        Assert.False(result.ShouldEscalate);
    }

    [Fact]
    public void Evaluate_StrongSignals_HighConfidence()
    {
        var result = DecisionConfidenceEvaluator.Evaluate(0.95, 0.90, policyResolved: true, policyRestricted: false);

        Assert.False(result.ShouldEscalate);
        Assert.True(result.Score >= 0.8);
    }

    [Fact]
    public void Evaluate_WeakSignals_Escalates()
    {
        var result = DecisionConfidenceEvaluator.Evaluate(0.2, 0.10, policyResolved: false, policyRestricted: false);

        Assert.True(result.ShouldEscalate);
        Assert.Equal("Low", result.Band);
    }
}

public class RevenueCalculatorTests
{
    [Fact]
    public void Calculate_ProducesPositiveNetValue()
    {
        var result = RevenueCalculator.Calculate(freightAvoided: 9m, salePrice: 40m, co2SavedKg: 1.8);

        Assert.True(result.TotalNetValue > 0);
        Assert.Equal(9m, result.FreightAvoided);
        Assert.True(result.ResaleServiceFee > 0);
    }

    [Fact]
    public void Calculate_SubtractsAiCost()
    {
        var noValue = RevenueCalculator.Calculate(0m, 0m, 0);
        Assert.Equal(-0.5m, noValue.TotalNetValue);
    }
}

public class AutoApprovalPolicyTests
{
    private static DecisionConfidence Confidence(double score, bool escalate) => new()
    {
        Score = score,
        Band = score >= 0.8 ? "High" : score >= 0.6 ? "Medium" : "Low",
        ShouldEscalate = escalate
    };

    [Fact]
    public void HighConfidence_LowValue_AutoApproves()
    {
        var result = AutoApprovalPolicy.Evaluate(
            Confidence(0.9, escalate: false), itemValue: 40m,
            policyRestricted: false, clockExpired: false, stableKey: "key-1");

        Assert.Equal(AutoApprovalPolicy.RouteAutoApprove, result.Route);
        Assert.False(result.RequiresHumanReview);
    }

    [Fact]
    public void HighConfidence_HighValue_RequiresHumanReview()
    {
        var result = AutoApprovalPolicy.Evaluate(
            Confidence(0.95, escalate: false), itemValue: 8000m,
            policyRestricted: false, clockExpired: false, stableKey: "key-2");

        Assert.Equal(AutoApprovalPolicy.RouteHumanReview, result.Route);
        Assert.True(result.RequiresHumanReview);
    }

    [Fact]
    public void MediumConfidence_RequiresHumanReview()
    {
        var result = AutoApprovalPolicy.Evaluate(
            Confidence(0.7, escalate: false), itemValue: 40m,
            policyRestricted: false, clockExpired: false, stableKey: "key-3");

        Assert.Equal(AutoApprovalPolicy.RouteHumanReview, result.Route);
        Assert.True(result.RequiresHumanReview);
    }

    [Fact]
    public void LowConfidence_Escalates()
    {
        var result = AutoApprovalPolicy.Evaluate(
            Confidence(0.45, escalate: true), itemValue: 40m,
            policyRestricted: false, clockExpired: false, stableKey: "key-4");

        Assert.Equal(AutoApprovalPolicy.RouteEscalate, result.Route);
        Assert.True(result.RequiresHumanReview);
    }

    [Fact]
    public void PolicyRestricted_AutoCommitsWithoutHuman()
    {
        var result = AutoApprovalPolicy.Evaluate(
            Confidence(0.95, escalate: false), itemValue: 500m,
            policyRestricted: true, clockExpired: false, stableKey: "key-5");

        Assert.Equal(AutoApprovalPolicy.RouteAutoApprove, result.Route);
        Assert.False(result.RequiresHumanReview);
    }

    [Fact]
    public void ExpiredClock_AutoCommitsWithoutHuman()
    {
        var result = AutoApprovalPolicy.Evaluate(
            Confidence(0.5, escalate: true), itemValue: 500m,
            policyRestricted: false, clockExpired: true, stableKey: "key-6");

        Assert.Equal(AutoApprovalPolicy.RouteAutoApprove, result.Route);
        Assert.False(result.RequiresHumanReview);
    }

    [Fact]
    public void QaSampling_IsDeterministicForSameKey()
    {
        var a = AutoApprovalPolicy.Evaluate(
            Confidence(0.9, escalate: false), 40m, false, false, stableKey: "stable-key");
        var b = AutoApprovalPolicy.Evaluate(
            Confidence(0.9, escalate: false), 40m, false, false, stableKey: "stable-key");

        Assert.Equal(a.SampledForQaAudit, b.SampledForQaAudit);
    }
}

public class PolicyRetrieverTests
{
    private readonly PolicyRetriever _retriever = new(NullLogger<PolicyRetriever>.Instance);

    [Theory]
    [InlineData("apparel", "RP-APP-2.1")]
    [InlineData("footwear", "RP-FTW-1.4")]
    [InlineData("hygiene", "RP-HYG-1.0")]
    [InlineData("serialized", "RP-SER-2.0")]
    public void Retrieve_ByCategory_RanksGoverningPolicyFirst(string query, string expectedRef)
    {
        var matches = _retriever.Retrieve(query);

        Assert.NotEmpty(matches);
        Assert.Equal(expectedRef, matches[0].Document.PolicyRef);
        Assert.True(matches[0].Score > 0);
    }

    [Fact]
    public void Retrieve_FreeTextDescription_GroundsToRestrictedPolicy()
    {
        var matches = _retriever.Retrieve("opened shampoo bottle with broken seal");

        Assert.NotEmpty(matches);
        Assert.False(matches[0].Document.ResaleAllowed);
        Assert.Equal("RP-HYG-1.0", matches[0].Document.PolicyRef);
    }

    [Fact]
    public void Retrieve_UnrelatedQuery_ReturnsNoConfidentMatch()
    {
        var matches = _retriever.Retrieve("zzzzq wibble frobnicate");
        Assert.Empty(matches);
    }
}

public class AutoApprovalMetricsTests
{
    [Fact]
    public void Snapshot_TalliesRoutesAndComputesStpRate()
    {
        var metrics = new AutoApprovalMetrics();

        metrics.Record(new AutoApprovalResult { Route = AutoApprovalPolicy.RouteAutoApprove });
        metrics.Record(new AutoApprovalResult { Route = AutoApprovalPolicy.RouteAutoApprove, SampledForQaAudit = true });
        metrics.Record(new AutoApprovalResult { Route = AutoApprovalPolicy.RouteHumanReview });
        metrics.Record(new AutoApprovalResult { Route = AutoApprovalPolicy.RouteEscalate });

        var snapshot = metrics.Snapshot();

        Assert.Equal(4, snapshot.Total);
        Assert.Equal(2, snapshot.AutoApproved);
        Assert.Equal(1, snapshot.HumanReview);
        Assert.Equal(1, snapshot.Escalated);
        Assert.Equal(1, snapshot.QaSampled);
        Assert.Equal(50.0, snapshot.StpRate);
    }
}

