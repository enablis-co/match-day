# Copilot Instructions - Pricing Service Development

## Executive Summary

This document establishes coding standards and principles to follow for all development on the Pricing Service. These rules ensure consistency, maintainability, and SOLID compliance.

---

## 🎯 Core Rules

### Rule 1: One Type Per File

**MANDATORY:** Each file contains exactly ONE of the following:
- One `interface`
- One `class`
- One `record`
- One `enum`

**❌ FORBIDDEN:**
```csharp
// ❌ BAD - Multiple types in one file
public interface IMatchWindowService { }
public record MatchWindowContext { }
public class MatchWindowService { }
```

**✅ CORRECT:**
```
src/Pricing/Services/
├── IMatchWindowService.cs     (interface only)
├── MatchWindowContext.cs      (record only)
└── MatchWindowService.cs      (class only)
```

### Rule 2: SOLID Principles - Always

**ALL** new code must comply with SOLID principles:

1. **S - Single Responsibility Principle**
   - Each class/service has ONE reason to change
   - One responsibility per class
   - Use facade services if orchestration is needed

2. **O - Open/Closed Principle**
   - Open for extension, closed for modification
   - Use interfaces for all public contracts
   - Use dependency injection, not `new` keyword

3. **L - Liskov Substitution Principle**
   - All implementations must be true substitutes
   - Don't narrow return types or widen parameter types
   - No contract violations

4. **I - Interface Segregation Principle**
   - Keep interfaces focused and minimal (1-5 methods)
   - No fat interfaces
   - Clients only depend on what they use

5. **D - Dependency Inversion Principle**
   - Depend on abstractions (interfaces), not concrete types
   - Use constructor injection
   - Register interfaces in DI container, not concrete types

---

## 📁 File Organization

### Current Structure (FOLLOW THIS)
```
src/Pricing/
├── Models/                          # Domain models
│   ├── Offer.cs                     # Class only
│   ├── Schedule.cs                  # Class only
│   ├── DiscountType.cs              # Enum only
│   ├── MatchDayRule.cs              # Enum only
│   ├── OfferStatus.cs               # Enum only
│   ├── OfferEvaluation.cs           # Record only
│   └── Dtos/
│       └── EventsDtos.cs            # Exception: DTO container (multiple DTOs OK)
│
├── Data/                            # Data access layer
│   ├── IOfferRepository.cs          # Interface only
│   ├── OfferRepository.cs           # Class only
│   ├── IProductRepository.cs        # Interface only
│   └── ProductRepository.cs         # Class only
│
├── Services/                        # Business logic layer
│   ├── IEventsService.cs            # Interface only
│   ├── EventsService.cs             # Class only
│   ├── IOfferEvaluationService.cs   # Interface only
│   ├── OfferEvaluationService.cs    # Class only
│   ├── IDiscountService.cs          # Interface only
│   ├── DiscountService.cs           # Class only
│   ├── IPricingService.cs           # Interface only
│   ├── PricingService.cs            # Class only
│   ├── IMatchWindowService.cs       # Interface only
│   ├── MatchWindowContext.cs        # Record only (SEPARATE FILE!)
│   └── MatchWindowService.cs        # Class only
│
├── Endpoints/                       # HTTP endpoint handlers
│   ├── HealthEndpoints.cs           # Endpoint mappings only
│   ├── OffersEndpoints.cs           # Endpoint mappings only
│   └── PricingEndpoints.cs          # Endpoint mappings only
│
└── Program.cs                       # DI setup and app configuration
```

### Exception: DTOs Container

**Allowed:** Multiple DTOs in one file (e.g., `EventsDtos.cs`)
```csharp
// ✅ OK - EventsDtos.cs contains related DTOs
public class EventsActiveResponse { }
public class ActiveEvent { }
public class DemandMultiplierResponse { }
```

**Why:** DTOs are thin data transfer objects, not core domain logic

---

## 🏗️ Proper File Structure Examples

### ✅ CORRECT - Interface File
```csharp
// File: IMatchWindowService.cs
namespace Pricing.Services;

public interface IMatchWindowService
{
    Task<MatchWindowContext> GetMatchWindowContextAsync(DateTime? time = null);
}
```

### ✅ CORRECT - Record File
```csharp
// File: MatchWindowContext.cs
namespace Pricing.Services;

public record MatchWindowContext(
    DateTime Timestamp,
    bool IsActive,
    double DemandMultiplier,
    DateTime? EndTime);
```

### ✅ CORRECT - Class File
```csharp
// File: MatchWindowService.cs
using Pricing.Models.Dtos;

namespace Pricing.Services;

public class MatchWindowService : IMatchWindowService
{
    private readonly IEventsService _eventsService;

    public MatchWindowService(IEventsService eventsService)
    {
        _eventsService = eventsService;
    }

    public async Task<MatchWindowContext> GetMatchWindowContextAsync(DateTime? time = null)
    {
        // Implementation
    }
}
```

### ❌ WRONG - Multiple Types
```csharp
// File: MatchWindowService.cs ❌ VIOLATES RULE 1
namespace Pricing.Services;

public interface IMatchWindowService { }      // ❌ Interface
public record MatchWindowContext { }          // ❌ Record
public class MatchWindowService { }           // ❌ Class
```

---

## 🔧 Naming Conventions

### Files
- **Interfaces:** `I{TypeName}.cs` (e.g., `IOfferRepository.cs`)
- **Classes:** `{ClassName}.cs` (e.g., `OfferRepository.cs`)
- **Records:** `{RecordName}.cs` (e.g., `MatchWindowContext.cs`)
- **Enums:** `{EnumName}.cs` (e.g., `OfferStatus.cs`)

### Namespaces
- `Pricing.Models` - Domain models
- `Pricing.Models.Dtos` - Data transfer objects
- `Pricing.Data` - Data access layer
- `Pricing.Services` - Business logic services
- `Pricing.Endpoints` - HTTP endpoint handlers

---

## 📋 SOLID Implementation Checklist

When creating new types, verify:

### Single Responsibility
- [ ] Class has exactly ONE reason to change
- [ ] Class has one primary responsibility
- [ ] No class mixing concerns (e.g., no HTTP + business logic)

### Open/Closed
- [ ] Can be extended without modifying
- [ ] Uses interfaces for contracts
- [ ] No hard-coded dependencies

### Liskov Substitution
- [ ] Implementation is true substitute for interface
- [ ] No contract violations
- [ ] No unexpected behavior

### Interface Segregation
- [ ] Interface has ≤ 5 methods (typically 1-3)
- [ ] No fat interfaces
- [ ] Client only depends on needed methods

### Dependency Inversion
- [ ] Depends on interface, not concrete type
- [ ] Uses constructor injection
- [ ] Registered in DI container as interface → implementation

---

## 🛠️ Code Style

### General
- Use modern C# 13 features appropriately
- Use nullable reference types (`#nullable enable`)
- Prefer immutability (`readonly`, records)
- Use expression-bodied members where readable

### Comments
- Minimal comments (code should be self-explanatory)
- Only add comments for WHY, not WHAT
- Match existing comment style in file

### Naming
- `PascalCase` for public members
- `_camelCase` for private fields
- `UPPER_CASE` for constants (rarely used)

### DI & Constructor Injection
```csharp
// ✅ CORRECT - Constructor injection
public class PricingService : IPricingService
{
    private readonly IProductRepository _productRepository;
    private readonly IOfferEvaluationService _offerEvaluationService;

    public PricingService(
        IProductRepository productRepository,
        IOfferEvaluationService offerEvaluationService)
    {
        _productRepository = productRepository;
        _offerEvaluationService = offerEvaluationService;
    }
}

// ❌ WRONG - New keyword, concrete type
public class PricingService
{
    private readonly ProductRepository _productRepository = new();
}
```

---

## 📝 File Template

### Interface Template
```csharp
namespace Pricing.Services;

public interface IMyService
{
    Task<MyResult> DoSomethingAsync(string input);
}
```

### Class Template
```csharp
using Pricing.Data;
using Pricing.Models;

namespace Pricing.Services;

public class MyService : IMyService
{
    private readonly IMyRepository _repository;

    public MyService(IMyRepository repository)
    {
        _repository = repository;
    }

    public async Task<MyResult> DoSomethingAsync(string input)
    {
        // Implementation
    }
}
```

### Record Template
```csharp
namespace Pricing.Services;

public record MyContext(
    DateTime Timestamp,
    bool IsActive,
    string Description);
```

### Enum Template
```csharp
namespace Pricing.Models;

public enum MyEnum
{
    Option1,
    Option2,
    Option3
}
```

---

## 🔄 Workflow - Creating New Feature

### Step 1: Identify Responsibility
- What does this feature do? (Single Responsibility)
- Could it be split? If yes, split it

### Step 2: Define Interface
```
Create: IMyNewService.cs
```

### Step 3: Define DTOs (if needed)
```
Add to: Models/Dtos/SomethingDtos.cs (or create new DTO file)
```

### Step 4: Implement Service
```
Create: MyNewService.cs
Implement: IMyNewService
```

### Step 5: Register in DI
```
Program.cs:
builder.Services.AddScoped<IMyNewService, MyNewService>();
```

### Step 6: Verify SOLID
- [ ] Single Responsibility? ✓
- [ ] Open/Closed? ✓
- [ ] Liskov Substitution? ✓
- [ ] Interface Segregation? ✓
- [ ] Dependency Inversion? ✓

---

## ❌ Common Mistakes to Avoid

### Mistake 1: Multiple Types per File
```csharp
// ❌ WRONG
public interface IService { }
public class Service { }
```
**Fix:** Separate into two files

### Mistake 2: Concrete Dependencies
```csharp
// ❌ WRONG
public class Service {
    private readonly Repository _repo = new();
}
```
**Fix:** Use interface and constructor injection

### Mistake 3: Fat Interfaces
```csharp
// ❌ WRONG
public interface IRepository {
    IEnumerable<T> GetAll();
    T? GetById(int id);
    void Add(T item);
    void Update(T item);
    void Delete(T item);
    void SaveChanges();
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    IQueryable<T> Query();
}
```
**Fix:** Split into focused interfaces or use generic repository

### Mistake 4: Mixed Concerns
```csharp
// ❌ WRONG - Mixing HTTP and business logic
public class OfferEndpoints {
    public void MapOffers(WebApplication app) {
        app.MapGet("/offers", () => {
            // HTTP mapping
            // Business logic
            // Response formatting
        });
    }
}
```
**Fix:** Separate into orchestration service and endpoint

### Mistake 5: Hard-coded Dependencies
```csharp
// ❌ WRONG
var service = new MyService(new Repository());
```
**Fix:** Use DI container

---

## 📚 Reference Materials

- **SOLID_REVIEW.md** - Detailed SOLID analysis
- **ARCHITECTURE.md** - Architecture patterns
- **QUICK_REFERENCE.md** - Quick overview

---

## 🚀 Before Committing

Checklist:

- [ ] Each file contains ONE type only
- [ ] All SOLID principles followed
- [ ] Code compiles without errors
- [ ] Follows naming conventions
- [ ] Uses constructor injection
- [ ] Interfaces registered in DI container
- [ ] No `new` keyword for dependencies
- [ ] Tests pass (if applicable)

---

## 📞 Questions?

Refer to:
1. **This document** - General guidelines
2. **SOLID_REVIEW.md** - SOLID principles details
3. **ARCHITECTURE.md** - Design patterns
4. **Existing code** - Use as reference for style

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024 | Initial instructions |

---

## Summary

**Golden Rules:**
1. ✅ **ONE type per file** (except DTOs)
2. ✅ **ALWAYS follow SOLID** principles
3. ✅ **Use interfaces** for all public contracts
4. ✅ **Inject dependencies** via constructor
5. ✅ **Register in DI** container, not `new` keyword

**Follow these rules consistently, and the codebase will remain:**
- Clean and maintainable
- Testable and mockable
- Extensible without modification
- Professional and enterprise-ready

---

**Happy coding!** 🎯
