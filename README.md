# Availity Claim Response Processor

A C# (.NET 8) application to capture and parse Availity EDI response files submitted via FTP, extract claim status information and benefit data, and store it in a database with full status tracking.

## Features

- **FTP Monitoring Service** — polls Availity FTP server, downloads new `.edi`, `.ebr`, and `.ebt` response files
- **EDI Parsers** — TA1, 999, 277CA, 835, .EBR, .EBT
- **Database Storage** — Entity Framework Core with SQLite (or SQL Server)
- **Status Mapping Engine** — maps EDI codes (A1, R1, etc.) to business statuses
- **REST API** — endpoints for claims, remittance, errors, benefits, files, and health check
- **Unit Tests** — full parser coverage with xUnit

---

## Project Structure

```
AvailityClaimProcessor.slnx
├── AvailityClaimProcessor.API          # ASP.NET Core Web API (entry point)
│   ├── Controllers/
│   │   ├── ClaimsController.cs
│   │   ├── BenefitsController.cs
│   │   ├── FilesController.cs
│   │   └── HealthController.cs
│   ├── Program.cs
│   └── appsettings.json
├── AvailityClaimProcessor.Core         # Domain models and interfaces
│   ├── Models/
│   │   ├── ClaimResponse.cs
│   │   ├── BenefitRecord.cs
│   │   ├── EdiFile.cs
│   │   ├── RemittanceDetail.cs
│   │   ├── ProcessingError.cs
│   │   ├── ClaimStatus.cs
│   │   └── ParseResult.cs
│   └── Interfaces/
│       ├── IEdiParser.cs
│       └── IStatusMappingService.cs
├── AvailityClaimProcessor.Infrastructure  # Parsers, services, EF Core
│   ├── Data/
│   │   └── ClaimProcessorDbContext.cs
│   ├── Parsers/
│   │   ├── Ta1Parser.cs
│   │   ├── Parser999.cs
│   │   ├── Parser277Ca.cs
│   │   ├── Parser835.cs
│   │   ├── EbrParser.cs
│   │   └── EbtParser.cs
│   └── Services/
│       ├── StatusMappingService.cs
│       ├── EdiProcessingService.cs
│       ├── FtpMonitoringService.cs
│       └── FtpSettings.cs
└── AvailityClaimProcessor.Tests        # xUnit tests
    └── Parsers/
        ├── Ta1ParserTests.cs
        ├── Parser999Tests.cs
        ├── Parser277CaTests.cs
        ├── Parser835Tests.cs
        ├── EbrParserTests.cs
        ├── EbtParserTests.cs
        └── StatusMappingServiceTests.cs
```

---

## Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQLite (included) **or** SQL Server

### Configuration

Edit `AvailityClaimProcessor.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=availity_claims.db"
  },
  "Ftp": {
    "Host": "ftp.availity.com",
    "Port": 21,
    "Username": "your_ftp_username",
    "Password": "your_ftp_password",
    "RemotePath": "/EDI/OUT",
    "LocalDownloadPath": "./downloads",
    "PollingIntervalSeconds": 60,
    "RetryCount": 3,
    "RetryDelaySeconds": 5
  }
}
```

To use **SQL Server**, change the connection string and update `Program.cs` to use `UseSqlServer(...)` instead of `UseSqlite(...)`.

### Build & Run

```bash
# Build solution
dotnet build

# Run the API
cd AvailityClaimProcessor.API
dotnet run
```

The API starts on `https://localhost:5001` by default.

### Run Tests

```bash
dotnet test
```

---

## API Endpoints

| Method | Endpoint                           | Description                             |
|--------|------------------------------------|-----------------------------------------|
| GET    | `/api/health`                      | Health check                            |
| GET    | `/api/claims`                      | List all claims (optional `?status=`)   |
| GET    | `/api/claims/{claimId}/status`     | Get claim status by claim ID            |
| GET    | `/api/claims/{claimId}/remittance` | Get remittance/payment details          |
| GET    | `/api/claims/errors/{claimId}`     | Get processing errors for a claim       |
| GET    | `/api/benefits/{memberId}`         | Get benefit/eligibility info (EBR/EBT)  |
| GET    | `/api/files`                       | List all processed EDI/EBR/EBT files    |

### Example: Get claim status

```bash
curl https://localhost:5001/api/claims/CLAIM001/status
```

```json
{
  "claimId": "CLAIM001",
  "status": "ACCEPTED",
  "rawStatusCode": "A1",
  "fileType": "277CA",
  "submittedAmount": 1000.00,
  "serviceDate": "2021-06-15"
}
```

### Example: Get benefits for member

```bash
curl https://localhost:5001/api/benefits/M12345
```

---

## EDI File Types

### TA1 — Interchange Acknowledgment
Validates ISA envelope. Parser reads the `TA1` segment acknowledgment code (`A`=Accepted, `E`/`R`=Rejected).

### 999 — Implementation Acknowledgment
Syntactical validation. Parser reads `AK5` (per transaction set) and `AK9` (group level) acknowledgment codes.

### 277CA — Claim Acknowledgment
Claim-level status. Parser reads `CLM` + `STC` segments.  
Status codes: `A1`=Accepted, `A2`=Processed, `R1`–`R8`=Rejected, `P1`–`P3`=Pending.

### 835 — Remittance Advice
Payment information. Parser reads `CLP` segments for claim payment data and `BPR`/`TRN` for check details.

### .EBR — Electronic Benefit Response
Eligibility and benefit coverage data. Supports both EDI 271-style and Availity proprietary `key=value` format.

### .EBT — Electronic Benefit Transaction
Benefit transaction details with AMT segments for financial data. Supports both EDI and proprietary formats. Links to claim IDs when present.

---

## Status Codes

| EDI Code | Business Status |
|----------|----------------|
| A1       | ACCEPTED        |
| A2       | PROCESSED       |
| R1–R8    | REJECTED        |
| P1–P3    | PENDING         |
| WQ       | PENDING         |
| DR       | REJECTED        |

TA1: `A`=ACCEPTED, `E`/`R`=REJECTED  
999: `A`/`M`/`W`=ACCEPTED, `R`/`E`/`X`=REJECTED

---

## FTP Monitoring

The `FtpMonitoringService` runs as a background hosted service. It:
1. Connects to the Availity FTP server on startup
2. Lists files in `RemotePath` (default `/EDI/OUT`)
3. Downloads `.edi`, `.ebr`, and `.ebt` files not already downloaded
4. Passes each file to `EdiProcessingService` which selects the correct parser
5. Stores results in the database
6. Retries connection failures up to `RetryCount` times
7. Sleeps for `PollingIntervalSeconds` between polls

---

## Database Schema

The application uses **Entity Framework Core** with automatic database creation (`EnsureCreated`).

**Tables:**
- `EdiFiles` — metadata for each downloaded file
- `ClaimResponses` — parsed claim status records
- `RemittanceDetails` — 835 payment/check details
- `ProcessingErrors` — segment-level parse errors
- `BenefitRecords` — EBR/EBT eligibility and benefit data
