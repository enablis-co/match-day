# Development Standards - Quick Checklist

## 🎯 For Every Request to Copilot

### Before Implementation
- [ ] Is this ONE type (interface/class/record/enum) only?
- [ ] Does this follow SOLID principles?
- [ ] Am I using an interface for contracts?
- [ ] Am I using constructor injection?
- [ ] Will this be registered in DI?

### During Implementation
- [ ] File contains exactly ONE type
- [ ] Created interface file if needed (I*.cs)
- [ ] Created implementation file (ClassName.cs)
- [ ] Created record file if needed (RecordName.cs)
- [ ] All dependencies are injected via constructor
- [ ] Using interfaces, not concrete types
- [ ] No `new` keyword for dependencies
- [ ] Proper namespace organization

### After Implementation
- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] Code compiles
- [ ] Each file has ONE type
- [ ] SOLID principles verified:
  - [ ] S - Single responsibility?
  - [ ] O - Open/closed?
  - [ ] L - Liskov substitution?
  - [ ] I - Interface segregation?
  - [ ] D - Dependency inversion?
- [ ] Interfaces registered in Program.cs
- [ ] No breaking changes
- [ ] Ready to commit

---

## 📋 File Organization Checklist

### Models Folder
- [ ] `Models/Offer.cs` - One class
- [ ] `Models/Schedule.cs` - One class
- [ ] `Models/DiscountType.cs` - One enum
- [ ] `Models/MatchDayRule.cs` - One enum
- [ ] `Models/OfferStatus.cs` - One enum
- [ ] `Models/OfferEvaluation.cs` - One record
- [ ] `Models/Dtos/EventsDtos.cs` - Multiple DTOs (allowed)

### Data Folder
- [ ] `Data/IOfferRepository.cs` - One interface
- [ ] `Data/OfferRepository.cs` - One class
- [ ] `Data/IProductRepository.cs` - One interface
- [ ] `Data/ProductRepository.cs` - One class

### Services Folder
- [ ] `Services/IEventsService.cs` - One interface
- [ ] `Services/EventsService.cs` - One class
- [ ] `Services/IOfferEvaluationService.cs` - One interface
- [ ] `Services/OfferEvaluationService.cs` - One class
- [ ] `Services/IDiscountService.cs` - One interface
- [ ] `Services/DiscountService.cs` - One class
- [ ] `Services/IPricingService.cs` - One interface
- [ ] `Services/PricingService.cs` - One class
- [ ] `Services/IMatchWindowService.cs` - One interface
- [ ] `Services/MatchWindowContext.cs` - One record
- [ ] `Services/MatchWindowService.cs` - One class

### Endpoints Folder
- [ ] `Endpoints/HealthEndpoints.cs` - Static class with extension method
- [ ] `Endpoints/OffersEndpoints.cs` - Static class with extension method
- [ ] `Endpoints/PricingEndpoints.cs` - Static class with extension method

---

## 🔧 DI Registration Checklist

In `Program.cs`, verify ALL of these are registered:

### Repositories
- [ ] `builder.Services.AddSingleton<IOfferRepository, OfferRepository>();`
- [ ] `builder.Services.AddSingleton<IProductRepository, ProductRepository>();`

### Services
- [ ] `builder.Services.AddScoped<IEventsService, EventsService>();`
- [ ] `builder.Services.AddScoped<IOfferEvaluationService, OfferEvaluationService>();`
- [ ] `builder.Services.AddScoped<IDiscountService, DiscountService>();`
- [ ] `builder.Services.AddScoped<IPricingService, PricingService>();`
- [ ] `builder.Services.AddScoped<IMatchWindowService, MatchWindowService>();`

---

## ⚠️ Common Violations

### Don't Do This ❌
```csharp
// ❌ WRONG - Multiple types in one file
public interface IService { }
public class Service { }

// ❌ WRONG - Concrete dependency
private readonly Repository _repo = new();

// ❌ WRONG - No interface
public class MyService { }

// ❌ WRONG - Hard-coded dependency
var service = new MyService(new Repository());

// ❌ WRONG - Fat interface
public interface IRepository {
    Get();
    Add();
    Update();
    Delete();
    GetAsync();
    AddAsync();
    UpdateAsync();
    DeleteAsync();
    SaveChanges();
}
```

### Do This Instead ✅
```csharp
// ✅ CORRECT - One type per file
// IService.cs - interface only
public interface IService { }

// Service.cs - class only
public class Service { }

// ✅ CORRECT - Interface dependency
private readonly IRepository _repo;

public Service(IRepository repo) {
    _repo = repo;
}

// ✅ CORRECT - Register in DI
builder.Services.AddScoped<IRepository, Repository>();
```

---

## 📝 Creating a New Service - Template

### Step 1: Create Interface
```
File: Services/IMyNewService.cs

namespace Pricing.Services;

public interface IMyNewService
{
    Task<MyResult> DoSomethingAsync(string input);
}
```

### Step 2: Create Result Type (if needed)
```
File: Services/MyResult.cs

namespace Pricing.Services;

public record MyResult(
    string Value,
    bool Success);
```

### Step 3: Create Implementation
```
File: Services/MyNewService.cs

using Pricing.Data;

namespace Pricing.Services;

public class MyNewService : IMyNewService
{
    private readonly IMyRepository _repository;

    public MyNewService(IMyRepository repository)
    {
        _repository = repository;
    }

    public async Task<MyResult> DoSomethingAsync(string input)
    {
        // Implementation
    }
}
```

### Step 4: Register in DI
```csharp
// In Program.cs

builder.Services.AddScoped<IMyNewService, MyNewService>();
```

### Step 5: Use in Endpoint or Other Service
```csharp
// In endpoint or another service - NEVER use 'new'

public static async Task<IResult> MyEndpoint(
    IMyNewService myNewService)  // ← Injected via DI
{
    var result = await myNewService.DoSomethingAsync("input");
    return Results.Ok(result);
}
```

---

## 🚀 Pre-Commit Verification

Run this checklist before committing:

```
Code Quality
├── [ ] Builds without errors
├── [ ] Builds without warnings
├── [ ] No #pragma warnings
└── [ ] No TODO comments left

Architecture
├── [ ] Each file has ONE type only
├── [ ] All dependencies are interfaces
├── [ ] Constructor injection used
├── [ ] No 'new' keyword for dependencies
└── [ ] Registered in DI container

SOLID Principles
├── [ ] S - Single Responsibility
│   └── [ ] One reason to change per class
├── [ ] O - Open/Closed
│   └── [ ] Extensible without modification
├── [ ] L - Liskov Substitution
│   └── [ ] True substitute for interface
├── [ ] I - Interface Segregation
│   └── [ ] Interface is focused (1-5 methods)
└── [ ] D - Dependency Inversion
    └── [ ] Depends on abstractions, not concrete

Naming
├── [ ] File names match type names
├── [ ] Interfaces start with 'I'
├── [ ] Namespaces match folder structure
└── [ ] PascalCase for public members

Documentation
├── [ ] Complex logic has comments
├── [ ] Methods are self-explanatory
└── [ ] No commented-out code
```

---

## 📚 Reference

- **COPILOT_INSTRUCTIONS.md** - Full development standards
- **QUICK_REFERENCE.md** - SOLID overview
- **SOLID_REVIEW.md** - Detailed principles
- **ARCHITECTURE.md** - Design patterns

---

## ✨ Golden Rules

```
🥇 Rule 1: ONE TYPE PER FILE
   ├── Interface in I{Name}.cs
   ├── Class in {Name}.cs
   ├── Record in {Name}.cs
   └── Enum in {Name}.cs

🥇 Rule 2: SOLID ALWAYS
   ├── S - Single Responsibility
   ├── O - Open/Closed
   ├── L - Liskov Substitution
   ├── I - Interface Segregation
   └── D - Dependency Inversion

🥇 Rule 3: USE DI ALWAYS
   ├── Constructor injection
   ├── Register interfaces
   └── Never use 'new' for dependencies

🥇 Rule 4: INTERFACES FIRST
   ├── Define contracts first
   ├── Then implement
   └── Register in container
```

---

**Print this page and keep it handy!** 📋

When in doubt, check COPILOT_INSTRUCTIONS.md for detailed guidance.
