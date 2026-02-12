# Request Tracing - Quick Reference

## 🚀 How to Use

### 1. Run the Application
```bash
dotnet run --project src/Pricing/Pricing.csproj
```

### 2. Make a Request
```bash
curl "http://localhost:5000/offers/active"
```

### 3. Check Console for Tracing Output
Look for logs with timing information showing each step.

---

## 📊 Log Output Structure

```
[Log Level] [Category] [Event]
[Details]
```

**Example:**
```
info: Pricing.Services.OfferOperationTracer[0]
      Starting operation: GetActiveOffers
      
dbug: Pricing.Services.OfferOperationTracer[0]
      Operation step: GetActiveOffers -> RetrievingMatchWindowContext (5ms)
      
dbug: Pricing.Services.TracedOfferEvaluationService[0]
      Checking schedule for offer OFFER-001: Happy Hour
      
dbug: Pricing.Services.OfferOperationTracer[0]
      Operation step: GetActiveOffers -> EvaluatingOffers (12ms)
      matchWindowActive: False
      demandMultiplier: 1.0
      
info: Pricing.Services.OfferOperationTracer[0]
      Completed operation: GetActiveOffers - Success: True, Elapsed: 20ms
```

---

## ⏱️ Interpreting Timing

### Total Request Time
```
Completed operation: GetActiveOffers - Success: True, Elapsed: 20ms
                                                                  ↑
                                                        Total time in ms
```

### Step Breakdown
```
Step 1: RetrievingMatchWindowContext (5ms)
Step 2: EvaluatingOffers (12ms)        ← Longest step
Step 3: FilteringActiveOffers (2ms)
Step 4: FormattingResponse (1ms)
────────────────────────────
Total: 20ms
```

---

## 🔍 Finding Bottlenecks

### Slow EvaluatingOffers?
```
Operation step: GetActiveOffers -> EvaluatingOffers (500ms) ← Too slow!
```

**Check:**
1. Number of offers being evaluated
2. Complexity of evaluation logic
3. Consider adding caching

### Slow MatchWindowContext?
```
Operation step: GetActiveOffers -> RetrievingMatchWindowContext (100ms) ← Too slow!
```

**Check:**
1. Events Service response time
2. Network latency
3. Cache the result

---

## 📝 Reading the Logs

### Start of Operation
```
info: Pricing.Services.OfferOperationTracer[0]
      Starting operation: GetActiveOffers
```
✅ Request has started

### Step Completed
```
dbug: Pricing.Services.OfferOperationTracer[0]
      Operation step: GetActiveOffers -> EvaluatingOffers (12ms)
      matchWindowActive: False
      demandMultiplier: 1.0
```
ℹ️ Step name, elapsed time, and metadata

### Service-Level Logging
```
dbug: Pricing.Services.TracedOfferEvaluationService[0]
      Checking schedule for offer OFFER-001: Happy Hour
```
📋 Detailed service operations

### Operation Completed
```
info: Pricing.Services.OfferOperationTracer[0]
      Completed operation: GetActiveOffers - Success: True, Elapsed: 20ms
```
✅ Request successful with total time

### Error Occurred
```
error: Pricing.Services.OfferOperationTracer[0]
       Completed operation: GetActiveOffers - Success: False, Elapsed: 50ms
       System.Exception: Something went wrong
```
❌ Request failed

---

## 🎯 Common Scenarios

### Scenario 1: Normal Request (Good Performance)
```
Elapsed: 10-30ms
├─ RetrievingMatchWindowContext: 5ms
├─ EvaluatingOffers: 15ms
├─ FilteringActiveOffers: 2ms
└─ FormattingResponse: 1ms
```
✅ All within expected ranges

### Scenario 2: Slow Match Window
```
Elapsed: 150ms
├─ RetrievingMatchWindowContext: 100ms ← Problem here!
├─ EvaluatingOffers: 30ms
├─ FilteringActiveOffers: 10ms
└─ FormattingResponse: 10ms
```
❌ Events Service slow - check network/service health

### Scenario 3: Slow Evaluation
```
Elapsed: 500ms
├─ RetrievingMatchWindowContext: 5ms
├─ EvaluatingOffers: 480ms ← Problem here!
├─ FilteringActiveOffers: 10ms
└─ FormattingResponse: 5ms
```
❌ Too many offers or complex evaluation - consider optimization

---

## 🔗 Integration with Monitoring

### Manual Tracking
Save the logs to analyze patterns over time:
```bash
dotnet run > logs.txt 2>&1
```

### With Application Insights
Future enhancement to send traces to Azure:
```csharp
builder.Services.AddApplicationInsights();
```

### With OpenTelemetry
Export to distributed tracing system:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("Pricing"));
```

---

## 📋 Log Levels

| Level | What | When |
|-------|------|------|
| **Info** | Important events | Operation start/end, key milestones |
| **Debug** | Detailed info | Every step, evaluations, decisions |
| **Error** | Failures | Exceptions, failures, errors |

### Change Log Level

In `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Pricing.Services": "Debug",
      "Pricing.Endpoints": "Debug"
    }
  }
}
```

---

## 🧪 Test Cases

### Test 1: Happy Path
```bash
curl "http://localhost:5000/offers/active"
```
✅ Should see all steps complete quickly

### Test 2: With Specific Pub
```bash
curl "http://localhost:5000/offers/active?pubId=PUB-001"
```
✅ Should log pubId in RequestReceived step

### Test 3: With Timestamp
```bash
curl "http://localhost:5000/offers/active?time=2024-01-13T17:00:00Z"
```
✅ Should show different offers active depending on time

---

## 🎯 Performance Targets

### Expected Timings
| Operation | Target | Alert |
|-----------|--------|-------|
| Total Request | < 50ms | > 100ms |
| Match Window | < 10ms | > 50ms |
| Evaluating Offers | < 20ms | > 100ms |
| Filtering | < 5ms | > 20ms |
| Formatting | < 5ms | > 10ms |

---

## 💡 Tips

1. **Copy logs to file** - Use `> logs.txt 2>&1` for analysis
2. **Watch for patterns** - See if certain times are always slow
3. **Test with different data** - Try various pub IDs and times
4. **Monitor alerts** - Set up notifications for slow requests
5. **Use structured logs** - Parse JSON for analysis

---

## 🔗 Related Documentation

- **TRACING_IMPLEMENTATION.md** - Complete technical details
- **COPILOT_INSTRUCTIONS.md** - Development standards
- **ARCHITECTURE.md** - System design

---

**Happy tracing!** 🔍
