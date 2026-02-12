# Pub Filtering - Quick Test Guide

## 🧪 Testing the Implementation

### Test Data Available

**PUB-001** (The Fox & Hound)
- OFFER-001: Happy Hour (50% off pints, M-F 16:00-18:00)
- OFFER-002: 2-for-1 Cocktails (Daily 12:00-22:00)
- OFFER-003: Weekend Special (£1 off pints, Sat-Sun 12:00-20:00)

**PUB-002** (The Queen's Arms)
- OFFER-004: Super Saturday (£2 off all drinks, Sat 10:00-22:00)
- OFFER-005: Midweek Madness (30% off, Tue-Wed 17:00-19:00)

---

## 📍 API Endpoints

### Get Active Offers for a Pub
```
GET /offers/active?pubId=PUB-001
```

**Response:** All active offers for PUB-001

```json
{
  "pubId": "PUB-001",
  "timestamp": "2024-01-15T17:30:00Z",
  "activeOffers": [
    {
      "offerId": "OFFER-002",
      "name": "2-for-1 Cocktails",
      "description": "Buy one cocktail, get one free",
      "status": "ACTIVE",
      "endsAt": "2024-01-15T22:00:00Z"
    }
  ],
  "suspendedOffers": []
}
```

### Get Offers for a Different Pub
```
GET /offers/active?pubId=PUB-002
```

**Response:** Different offers for PUB-002

### Get Specific Offer Details
```
GET /offers/OFFER-001
```

**Response:** Now includes `pubId`

```json
{
  "offerId": "OFFER-001",
  "pubId": "PUB-001",
  "name": "Happy Hour",
  "description": "50% off selected pints",
  "discountType": "PERCENTAGE",
  "discountValue": 50,
  "applicableProducts": ["PINT_LAGER", "PINT_ALE", "PINT_STELLA"],
  "schedule": {
    "days": ["MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY"],
    "startTime": "16:00",
    "endTime": "18:00"
  },
  "matchDayRule": "END_EARLY"
}
```

---

## ✅ Test Scenarios

### Scenario 1: Default Pub (PUB-001)
```
GET /offers/active
```
**Expected:** Returns PUB-001 offers (default)

### Scenario 2: Explicit Pub Parameter
```
GET /offers/active?pubId=PUB-002
```
**Expected:** Returns PUB-002 offers (different from PUB-001)

### Scenario 3: With Timestamp (Happy Hour Time)
```
GET /offers/active?pubId=PUB-001&time=2024-01-15T16:30:00Z
```
**Expected:** OFFER-001 (Happy Hour) should be ACTIVE

### Scenario 4: With Timestamp (Outside Happy Hour)
```
GET /offers/active?pubId=PUB-001&time=2024-01-15T19:30:00Z
```
**Expected:** OFFER-001 should be INACTIVE

### Scenario 5: Weekend Offers
```
GET /offers/active?pubId=PUB-001&time=2024-01-13T14:00:00Z
```
**Expected:** OFFER-003 (Weekend Special) should be ACTIVE

### Scenario 6: Non-existent Pub
```
GET /offers/active?pubId=PUB-999
```
**Expected:** Returns empty activeOffers list

---

## 🔍 Verification Checklist

- [ ] PUB-001 and PUB-002 return different offers
- [ ] Default pub (no parameter) uses PUB-001
- [ ] Offer details include pubId
- [ ] Schedule times work correctly
- [ ] Match day rules apply correctly
- [ ] Non-existent pubs return empty lists (not errors)

---

## 🚀 Curl Examples

### Get PUB-001 Offers
```bash
curl -X GET "https://localhost:5001/offers/active?pubId=PUB-001" \
  -H "accept: application/json"
```

### Get PUB-002 Offers
```bash
curl -X GET "https://localhost:5001/offers/active?pubId=PUB-002" \
  -H "accept: application/json"
```

### Get Offer Details
```bash
curl -X GET "https://localhost:5001/offers/OFFER-001" \
  -H "accept: application/json"
```

### Get Offers with Timestamp
```bash
curl -X GET "https://localhost:5001/offers/active?pubId=PUB-001&time=2024-01-15T17:00:00Z" \
  -H "accept: application/json"
```

---

## 📊 Expected Behavior

| Pub ID | Offers | Notes |
|--------|--------|-------|
| PUB-001 | 3 offers (OFFER-001, 002, 003) | Default pub |
| PUB-002 | 2 offers (OFFER-004, 005) | Different offers |
| PUB-999 | Empty list | Non-existent pub |
| (none) | 3 offers (PUB-001) | Uses default |

---

## 🎯 Now It Works!

Before: Same offers for any pub ID ❌

Now: Different offers per pub ✅

```
PUB-001 → Happy Hour, 2-for-1 Cocktails, Weekend Special
PUB-002 → Super Saturday, Midweek Madness
PUB-999 → (empty)
```

---

**Happy testing!** 🧪
