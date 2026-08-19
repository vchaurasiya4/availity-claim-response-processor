# Availity Claim Response Processor

A C# ASP.NET Core application that monitors an Availity FTP server for EDI response files (TA1, 999, 277CA, 835), parses them, stores claim status in a SQL Server database, and exposes a REST API.

---

## Architecture

```
AvailityClaimResponseProcessor.sln
├── src/
│   ├── AvailityClaimResponseProcessor.Core          # Domain models, enums, interfaces
│   ├── AvailityClaimResponseProcessor.Infrastructure # EF Core, parsers, FTP service, status mapper
│   └── AvailityClaimResponseProcessor.Api           # ASP.NET Core Web API
└── tests/
    └── AvailityClaimResponseProcessor.Tests         # xUnit unit tests
```

---

## EDI File Support

| File Type | Transaction Set | Description |
|-----------|----------------|-------------|
| TA1       | TA1            | Interchange Acknowledgment – envelope-level validation |
| 999       | 999            | Implementation Acknowledgment – syntax validation (AK5/AK9) |
| 277CA     | 277            | Claim Acknowledgment – per-claim status (STC/CLM) |
| 835       | 835            | Remittance Advice – payment details (CLP) |

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (or SQL Server LocalDB for development)
- FTP access to Availity EDI/OUT folder

---

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/vchaurasiya4/availity-claim-response-processor.git
cd availity-claim-response-processor
```

### 2. Configure `appsettings.json`

Edit `src/AvailityClaimResponseProcessor.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AvailityClaimProcessor;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "FtpSettings": {
    "Host": "ftp.availity.com",
    "Port": 21,
    "Username": "YOUR_FTP_USERNAME",
    "Password": "YOUR_FTP_PASSWORD",
    "RemoteFolder": "/EDI/OUT",
    "LocalDownloadPath": "downloads",
    "RetryCount": 3,
    "RetryDelaySeconds": 5,
    "PollingIntervalSeconds": 60
  }
}
```

### 3. Apply database migrations

```bash
dotnet ef database update --project src/AvailityClaimResponseProcessor.Infrastructure \
  --startup-project src/AvailityClaimResponseProcessor.Api
```

### 4. Run the application

```bash
dotnet run --project src/AvailityClaimResponseProcessor.Api
```

---

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET`  | `/api/health` | Health check |
| `GET`  | `/api/claims` | List all claims |
| `GET`  | `/api/claims/{claimId}/status` | Get claim status |
| `GET`  | `/api/claims/{claimId}/remittance` | Get remittance/payment details |
| `GET`  | `/api/claims/errors/{claimId}` | Get processing errors |

### Example responses

**GET /api/claims/CLAIMID001/status**
```json
{
  "claimId": "CLAIMID001",
  "status": "ACCEPTED",
  "statusCode": "A1",
  "statusMessage": "Accepted for Further Processing",
  "fileType": "ClaimAcknowledgment277CA",
  "sourceFileName": "277CA_20210615.edi",
  "submittedAmount": 1000.00,
  "processedAt": "2021-06-15T12:30:00Z",
  "serviceDate": "2021-06-15T00:00:00Z"
}
```

**GET /api/claims/CLAIMID001/remittance**
```json
{
  "claimId": "CLAIMID001",
  "paymentStatusCode": "1",
  "submittedAmount": 1000.00,
  "approvedAmount": 900.00,
  "paidAmount": 900.00,
  "patientResponsibilityAmount": 100.00,
  "checkNumber": "CHECK123",
  "paymentDate": "2021-06-15T00:00:00Z"
}
```

---

## Status Code Mappings

### 277CA Claim Status

| EDI Code | Business Status | Meaning |
|----------|----------------|---------|
| A1 | ACCEPTED | Accepted for Further Processing |
| A2 | PROCESSED | Accepted and Forwarded |
| R1 | REJECTED | Claim not found |
| R2 | REJECTED | Duplicate Claim |
| R3 | REJECTED | Claim not on file |

### 835 CLP Payment Status

| Code | Business Status | Meaning |
|------|----------------|---------|
| 1    | PROCESSED | Processed as Primary |
| 4    | REJECTED  | Denied |

### TA1 / 999 Acknowledgment

| Code | Business Status | Meaning |
|------|----------------|---------|
| A    | ACCEPTED | Accepted |
| R    | REJECTED | Rejected |
| E    | ERROR    | Accepted with errors |

---

## Running Tests

```bash
dotnet test
```

29 unit tests covering:
- EDI tokenizer
- TA1, 999, 277CA, 835 parsers
- Status mapping service
- Claim repository (EF InMemory)

---

## Database Schema

| Table | Description |
|-------|-------------|
| `ClaimResponses` | Core claim record with status, amounts, and metadata |
| `ClaimErrors` | Per-claim parsing/processing errors |
| `ClaimPayments` | 835 remittance payment details |
| `EdiFileRecords` | Audit log of downloaded and processed EDI files |

---

## FTP Monitoring

The `EdiFileProcessingService` background service polls the configured FTP folder every `PollingIntervalSeconds`. For each new file:

1. Downloads the file to the local `downloads/` folder
2. Detects the EDI type (TA1 / 999 / 277CA / 835) by filename and content
3. Selects the appropriate parser
4. Persists parsed claims to the database
5. Marks the file as processed to avoid re-processing on next poll

Connection failures are retried up to `RetryCount` times with `RetryDelaySeconds` between attempts.
