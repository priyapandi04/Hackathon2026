# ReLoop — Decision Engine Enhancements

**Branch:** `feature/reloop-winning-enhancements`
**Scope:** Additive layer on top of the team's Azure / .NET 8 base build. No existing behavior removed — every change is backward compatible and covered by unit tests.

This document records **what the base build was missing** (measured against the UPS problem statement and our pitch) and **what we added to close each gap**, so the team can review, demo, and defend every decision.

---

## 1. Why these changes

The base build delivered a solid Clean-Architecture .NET 8 API with Azure OpenAI GPT-4o (text + vision), Azure SQL + EF Core + stored procedures, a rules-based `MatchCalculator`, and a per-return root-cause call. It processed a return and produced a recommendation and a rough savings number.

What it did **not** yet do — and what the UPS brief and the judges' scorecard reward — was turn that recommendation into a **trustworthy, compliant, auditable decision** driven by the **10-day holding constraint**, and quantify the **full business value**. The enhancements below add exactly that, as deterministic C# services (never in the LLM).

---

## 2. Gap → Enhancement map

| # | Gap in base build | What we added | Where |
|---|---|---|---|
| 1 | **10-day holding clock** existed only as a DB column (`InventoryPool.HoldingDays`) — no countdown logic, no auto-return-to-seller on day 10. This is the core UPS constraint. | Deterministic `HoldingClockService` (10-day max, day-8 "closing window" flag, `Expired` → auto return-to-seller). Wired as **Step 1a** in the orchestrator. | `HoldingClockService.cs`, `IHoldingClockService.cs` |
| 2 | **Diversion / dynamic-pricing agent** was promised in the pitch but not in code. | `DiversionAgentService` — an escalation ladder that widens radius / discounts / offers access points / escalates as the clock runs down. Wired as **Step 4a**. | `DiversionAgentService.cs`, `IDiversionAgentService.cs` |
| 3 | **No policy grounding / citations** — GPT-4o was called raw with no evidence trail (hallucination risk). | `RetailerPolicyService` with an in-memory policy catalog and **citations** (`Citation` record: source, ref id, snippet) attached to every decision. In-memory now → Azure AI Search / policy table in prod. | `RetailerPolicyService.cs`, `IRetailerPolicyService.cs`, `DecisionDtos.cs` |
| 4 | **No policy-first compliance block** — eligibility was inferred from the photo only. Hygiene / food / serialized / medical items could be wrongly resold. | Category compliance block: restricted categories force `RETURN_TO_SELLER` **regardless of photo condition**. Wired as **Step 1b** (before pricing). | `RetailerPolicyService.cs`, `ReturnProcessingOrchestrator.cs` |
| 5 | **No confidence / refusal gate** at the pipeline level. | `DecisionConfidenceEvaluator` (threshold 0.60) → sets an **Escalate** outcome when image + match confidence is low or policy is unresolved. Wired as **Step 5a**. | `DecisionConfidenceEvaluator.cs` |
| 6 | **No human accept / modify / reject + feedback loop** — the "learns daily" moat. | `FeedbackService` + `FeedbackController` (`POST /api/feedback`, `GET /api/feedback/summary`) capturing associate corrections and field-level correction stats. | `FeedbackService.cs`, `IFeedbackService.cs`, `FeedbackController.cs`, `FeedbackDtos.cs` |
| 7 | **Root cause was per-single-return** — our story promises clustering ("40% of apparel returns = size-chart error"). | `ClusterReturns` on `RootCauseAgentService` — deterministic aggregation into priced fix-tickets, annualized (×260 operating days). New `POST /api/rootcauseagent/cluster`. | `RootCauseAgentService.cs`, `RootCauseDtos.cs`, `RootCauseAgentController.cs` |
| 8 | **Arbitrary savings math** and an **empty "Revenue Opportunity"** metric. | `RevenueCalculator` — freight avoided + resale margin + CO2 credit − AI cost, plus a Resale-as-a-Service fee stream. Wired as **Step 5b**. | `RevenueCalculator.cs`, `DecisionDtos.cs` |
| 9 | **Pitch docs still described a Google Cloud stack** (Vertex / Gemini / FastAPI / React / ChromaDB / BigQuery), contradicting the Azure/.NET build. | Reconciled every deck and doc to **Azure OpenAI GPT-4o + ASP.NET Core 8 + Angular 18 + Azure SQL**. | `generate_reloop_deliverables.py` (our workspace) |
| 10 | **Inconsistent naming** (HIEN / Nexus / ReLoop). | Standardized the product story on **ReLoop**. Team code already uses `UPS ReLoop Nexus`; no code rename needed. | pitch docs |
| 11 | **Human accept/modify/reject cannot scale** — with thousands of returns a day, an associate cannot review every one. | `AutoApprovalPolicy` straight-through router: auto-approves the confident, policy-clean, low-value tail; sends only the uncertain / high-value minority to a human; escalates low-confidence to a supervisor queue. A small deterministic QA sample audits auto-approvals. | `AutoApprovalPolicy.cs`, `DecisionDtos.cs` (`AutoApprovalResult`) |
| 12 | **Broken stored procedures** — `usp_SaveImageValidationResult` and `usp_GetInventoryByProduct` referenced a non-existent `[dbo].[Returns]` table (deferred name resolution hid it until runtime), which would break image-save and hyperlocal matching against the real DB. | Repointed both to `[dbo].[ImageValidationResults]` (the actual FK target; columns match exactly). Verified against SQL Server with thousands of rows. | `ReloopAI_StoreProcedures.sql` (SQL-Queries repo) |
| 13 | **Policy grounding was a hardcoded keyword lookup**, not the RAG we planned; it could not handle free-text item descriptions and had no real retrieval or evidence. | Added a policy **RAG** pipeline: a synthetic retailer-policy corpus + in-process TF-IDF/cosine retriever. `RetailerPolicyService` now grounds resale eligibility on the top retrieved policy, returns its similarity score and cited snippet, and falls back to "return to seller" below a confidence threshold. Runs offline; swappable for Azure embeddings. | `IPolicyRetriever.cs`, `SyntheticPolicyCorpus.cs`, `PolicyRetriever.cs`, `RetailerPolicyService.cs` |
| 14 | **No visibility into the auto-vs-manual split** the auto-approval router produces. | `AutoApprovalMetrics` (thread-safe singleton) tallies AUTO_APPROVE / HUMAN_REVIEW / ESCALATE and QA samples; surfaced as an `AutoApproval` block (with STP rate) on the feedback summary the dashboard already calls. | `AutoApprovalMetrics.cs`, `FeedbackService.cs`, `FeedbackDtos.cs` |

---

## 2a. Session update (2026-07-14) — risk-weighted dynamic pricing

Enhancement #2 (`DiversionAgentService`) originally marked items down on the **calendar alone**
(a fixed per-day discount ladder). It now computes a **risk-weighted markdown** from three
independent signals plus category economics — still fully deterministic/auditable (never the LLM):

- `discount% = categoryCeiling × clearanceRisk × valueGuard`
  - **clearanceRisk** = `0.45·timePressure + 0.35·demandRisk + 0.20·conditionRisk` (0 = safe, 1 = high dead-stock risk)
  - **categoryCeiling** (elasticity): Books 20 · Home 26 · Sports 34 · Electronics 35 · Accessories 38 · Footwear 40 · Apparel 42 · Beauty 45
  - **valueGuard**: premium items (> ₹5,000) discounted more gently (×0.8) — recover via reach, not margin
- **search radius** = `10 km × (1 + 1.5·clearanceRisk)` → widens up to 25 km as risk rises
- **sellProbability** = `0.60·match + 0.40·condition`, surfaced on the decision + UI
- `Decide(...)` gained optional `condition` + `category` params; `DiversionDecision` gained
  `SellProbability` + `ClearanceRisk`. All 50 unit tests still pass.

_Where:_ `DiversionAgentService.cs`, `IDiversionAgentService.cs`, `DecisionDtos.cs`,
`ReturnProcessingOrchestrator.cs` (Step 4a passes condition + category).

The diagnostics endpoint `GET /api/debug/matches` now also runs this agent **per row** (joining the
holding clock + sale price) so the Returns Inventory grid shows the real markdown, clearance risk,
radius and sell-through — not a frontend heuristic. _Where:_ `DebugController.cs`.

---

## 3. New decision object

Every processed return now returns a richer, auditable decision. New fields on the integration response:

- `HoldingClock` — days used / remaining, status (`OnTrack` / `ClosingWindow` / `Expired`).
- `PolicyCompliance` — allowed/blocked, matched policy, reason.
- `Diversion` — action + pricing/radius escalation.
- `DecisionConfidence` — score + whether it escalated to a human.
- `RevenueOpportunity` — freight avoided, resale margin, CO2 credit, service fee, net value.
- `Citations` — evidence behind the decision (policy ref + precedent).

Request additions: `HoldingDaysCompleted`, `PickupDate`, `BasePrice` (all optional; safe defaults applied).

---

## 4. Orchestrator flow (additions in **bold**)

1. Create return request (SP)
2. **1a. Holding-clock evaluation** → short-circuits to auto return-to-seller if expired
3. **1b. Policy-first compliance block** → restricted categories forced to return-to-seller
4. Image validation (GPT-4o vision)
5. Inventory pool lookup
6. Match agent (`MatchCalculator`)
7. **4a. Diversion agent** → pricing / radius escalation
8. Root cause (GPT-4o)
9. **5a. Confidence gate** → Escalate on low confidence
10. **5c. Auto-approval routing** → AUTO_APPROVE / HUMAN_REVIEW / ESCALATE (throughput at scale)
11. **5b. Revenue calculation + precedent citation**
12. Response + savings + full decision object

Order matters: **policy and the clock run before anything else**, so a non-compliant or expired item never reaches resale pricing.

---

## 5. Design principles honored

- **Determinism over LLM math** — the clock, pricing, CO2, revenue, and compliance all run in plain C# services. The LLM reasons and grades; it never does arithmetic or commits a final action.
- **Additive & reversible** — new services are DI-registered alongside existing ones; existing endpoints and tests are untouched.
- **Backward compatible** — new request fields are optional with safe defaults; existing callers keep working.
- **Testable** — `DecisionEngineTests.cs` covers the holding clock, policy service, diversion agent, confidence evaluator, and revenue calculator.

---

## 6. Production follow-ups (documented as MVP shortcuts)

- Retailer policy catalog is **in-memory** for the MVP → move to a `RetailerPolicies` table / **Azure AI Search** vector index.
- Feedback is stored in a process-scoped `ConcurrentBag` → move to a `Feedback` table in **Azure SQL** and feed **Azure ML** for the daily-learning loop.
- Demand forecast is rule-based → **Azure ML** model.
- Cache is in-memory → **Azure Cache for Redis**.

---

## 7. File inventory

**New files**
- `UPS.ReLoop.Application/DTOs/Decision/DecisionDtos.cs`
- `UPS.ReLoop.Application/Interfaces/IHoldingClockService.cs`
- `UPS.ReLoop.Application/Services/HoldingClockService.cs`
- `UPS.ReLoop.Application/Interfaces/IRetailerPolicyService.cs`
- `UPS.ReLoop.Application/Services/RetailerPolicyService.cs`
- `UPS.ReLoop.Application/Interfaces/IDiversionAgentService.cs`
- `UPS.ReLoop.Application/Services/DiversionAgentService.cs`
- `UPS.ReLoop.Application/Services/DecisionConfidenceEvaluator.cs`
- `UPS.ReLoop.Application/Services/AutoApprovalPolicy.cs`
- `UPS.ReLoop.Application/Services/RevenueCalculator.cs`
- `UPS.ReLoop.Application/DTOs/Feedback/FeedbackDtos.cs`
- `UPS.ReLoop.Application/Interfaces/IFeedbackService.cs`
- `UPS.ReLoop.Application/Services/FeedbackService.cs`
- `UPS_ReLoop_Nexus/Controllers/FeedbackController.cs`
- `UPS.ReLoop.Tests/DecisionEngineTests.cs`

**SQL artifacts (SQL-Queries repo)**
- `Reloop_SyntheticBulkData.sql` — generates thousands of FK-consistent synthetic rows (idempotent, tagged `synthetic-gen`).
- `Reloop_ValidationTests.sql` — SP smoke tests + analytical queries (10-day clock buckets, auto-approval simulation, dashboards).
- `ReloopAI_StoreProcedures.sql` — fixed `dbo.Returns` → `dbo.ImageValidationResults` (2 procedures).

**Modified files**
- `UPS.ReLoop.Application/DTOs/RootCauseAgent/RootCauseDtos.cs` (return clustering)
- `UPS.ReLoop.Application/Interfaces/IRootCauseAgentService.cs` (`ClusterReturns`)
- `UPS.ReLoop.Application/Services/RootCauseAgentService.cs` (`ClusterReturns`)
- `UPS.ReLoop.Application/DTOs/Integration/IntegrationDtos.cs` (decision fields)
- `UPS.ReLoop.Application/DependencyInjection.cs` (DI registration)
- `UPS.ReLoop.Application/Services/ReturnProcessingOrchestrator.cs` (new steps)
- `UPS_ReLoop_Nexus/Controllers/RootCauseAgentController.cs` (`/cluster`)
