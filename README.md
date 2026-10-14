# TECH Prize Draw — Premium Limited-Ticket Event Platform

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4?logo=dotnet)](https://docs.microsoft.com/ef/core/)
[![Database](https://img.shields.io/badge/Database-SQL%20Server-CC292B?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/License-Proprietary-blue.svg)]()
[![Build & Tests](https://img.shields.io/badge/Tests-15%20Passed-success)]()

A high-concurrency, enterprise-grade ASP.NET Core MVC web application built specifically for limited-inventory digital prize draws in Bangladesh. Designed with a deep navy and gold aesthetic, strict database-level serializable concurrency protection, manual Mobile Financial Service (MFS) payment verification (bKash & Nagad), sequential ticket voucher generation with QRCoder verification, hardware-grade cryptographic draw execution, and a compliance configuration gate.

---

## 📋 Table of Contents
1. [Architectural Overview](#architectural-overview)
2. [Compliance, Legal & Production Gate](#compliance-legal--production-gate)
3. [Core Specifications & Business Rules](#core-specifications--business-rules)
4. [Prerequisites & Technology Stack](#prerequisites--technology-stack)
5. [Database Setup & Migrations](#database-setup--migrations)
6. [Configuration & Environment Variables](#configuration--environment-variables)
7. [Default Seed Data & Admin Credentials](#default-seed-data--admin-credentials)
8. [Running Locally](#running-locally)
9. [Operator & Administrator Handbook](#operator--administrator-handbook)
   - [1. Changing Ticket Price](#1-changing-ticket-price)
   - [2. Changing Maximum Ticket Count](#2-changing-maximum-ticket-count)
   - [3. Managing MFS Payment Numbers](#3-managing-mfs-payment-numbers)
   - [4. Customizing Prizes](#4-customizing-prizes)
   - [5. Activating / Pausing the Campaign](#5-activating--pausing-the-campaign)
   - [6. Reviewing Pending Payments](#6-reviewing-pending-payments)
   - [7. Approving Payments & Issuing Tickets](#7-approving-payments--issuing-tickets)
   - [8. Rejecting Invalid Payments](#8-rejecting-invalid-payments)
   - [9. Resending Ticket Vouchers](#9-resending-ticket-vouchers)
   - [10. Executing the Final Cryptographic Draw](#10-executing-the-final-cryptographic-draw)
10. [Security & Concurrency Architecture](#security--concurrency-architecture)
11. [Production Deployment Guide (IIS & Linux Nginx)](#production-deployment-guide)
12. [Testing & Quality Assurance Checklist](#testing--quality-assurance-checklist)

---

## 🏛 Architectural Overview

```
                      +---------------------------------------+
                      |               CUSTOMER                |
                      | (Views Live Inventory & Prize Tiers)  |
                      +-------------------+-------------------+
                                          |
                                          v
                      +---------------------------------------+
                      |         CHECKOUT SUBMISSION           |
                      | Enters Name, Mobile, Email, TrxID     |
                      +-------------------+-------------------+
                                          |
                                          v
                      +---------------------------------------+
                      |      ORDER PENDING VERIFICATION       |
                      |  * Atomic inventory hold decremented  |
                      |  * NO ticket issued yet               |
                      |  * NO voucher or QR generated yet     |
                      |  * Reservation timeout timer started  |
                      +-------------------+-------------------+
                                          |
                        +-----------------+-----------------+
                        |                                   |
                [Admin APPROVAL]                    [Admin REJECTION / EXPIRY]
                        |                                   |
                        v                                   v
    +---------------------------------------+   +---------------------------------------+
    |           DATABASE COMMIT             |   |           DATABASE RELEASE            |
    |  * Serializable Transaction           |   |  * Status -> Rejected / Expired       |
    |  * Sequential Ticket Assignment       |   |  * Reserved inventory released        |
    |    (TECH-0001 ... TECH-5000)          |   |  * Audit log recorded                 |
    |  * Order Status -> Approved           |   |  * Optional notice emailed            |
    |  * Ticket Status -> Valid             |   +---------------------------------------+
    |  * QRCoder verification generated     |
    |  * Digital Voucher created            |
    |  * Delivery email logged & sent       |
    +-------------------+-------------------+
                        |
                        v
    +---------------------------------------+
    |           OFFICIAL TICKET             |
    | Public QR verify: /ticket/verify/TECH |
    | Printable Voucher: /ticket/TECH/vouch |
    +-------------------+-------------------+
                        |
                        v (Campaign Ends)
    +---------------------------------------+
    |      CRYPTOGRAPHIC PRIZE DRAW         |
    |  * Role: SuperAdmin ONLY              |
    |  * System.Security.Cryptography.RNG   |
    |  * Immutable Draw & Winner records    |
    |  * Public Results: /winners           |
    +---------------------------------------+
```

---

## ⚠️ Compliance, Legal & Production Gate

> **CRITICAL COMPLIANCE NOTICE FOR BANGLADESH OPERATORS:**
>
> This platform implements a paid random prize-draw architecture. **Under no circumstances should this software be assumed to be legally authorized for live commercial operation out of the box.**
>
> By default, the software ships with:
> ```json
> "Campaign": {
>   "Enabled": false
> }
> ```
>
> Prior to setting `"Enabled": true` in production, the operating entity must independently obtain and document:
> 1. **Statutory Authorization / Licensing:** Compliance with the *Public Gambling Act, 1867*, *Lotteries Act*, *Penal Code (Sections 294A/294B)*, and all applicable Bangladesh municipal, commercial, and national regulatory permissions.
> 2. **Mobile Financial Service (MFS) Terms:** Written authorization and commercial merchant account approval from bKash Limited and Nagad (Postal Department) for collecting prize draw contest funds.
> 3. **Tax & Revenue Compliance:** National Board of Revenue (NBR) source tax deduction protocols (TDS on lottery winnings under the Bangladesh Income Tax Code).
> 4. **Consumer Protection:** Compliance with the *Consumers' Right Protection Act, 2009*.
> 5. **Age & Eligibility Controls:** Mandatory age verification (18+) and clear terms prohibiting employee or affiliated operator participation.
> 6. **Truthful Representation:** The application purposefully avoids misleading phrases such as *"Government Approved"*, *"100% Legal"*, or *"Official National Lottery"*. Only configure factual legal statements if your legal counsel has explicitly authorized them.

---

## 🎯 Core Specifications & Business Rules

| Attribute | Specification | Enforcement Mechanism |
| :--- | :--- | :--- |
| **Project Name** | TECH Prize Draw | System configuration & view rendering |
| **Hard Ticket Cap** | **5,000 Tickets** | Database unique constraint + atomic reservation check |
| **Ticket Price** | **৳50 BDT** | Server-side decimal calculation (`Order.Amount`) |
| **Max Gross Sales** | **৳250,000 BDT** | Hard limit based on 5,000 × ৳50 BDT |
| **Ticket Format** | `TECH-0001` ... `TECH-5000` | Server-side sequential zero-padded generator |
| **Payment Workflow** | Manual bKash / Nagad Send Money | No automated gateway (SSLCommerz/Stripe excluded) |
| **Core Invariant** | Customer Submission ≠ Approval | Valid tickets are **only** generated upon admin approval |
| **Concurrency Safe** | Zero overselling | `IsolationLevel.Serializable` database transactions |
| **Reservation Timeout** | Configurable (default 60 mins) | Background `OrderCleanupHostedService` execution |
| **Draw System** | Cryptographic random selection | `System.Security.Cryptography.RandomNumberGenerator` |

---

## 💻 Prerequisites & Technology Stack

- **Operating System:** Windows 10/11, Windows Server 2019/2022, or Linux (Ubuntu 22.04+).
- **Runtime:** [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later.
- **Database Engine:** Microsoft SQL Server 2019+, SQL Server 2022, or SQL Server Express (`MSSQL$SQLEXPRESS`).
- **Core Frameworks:**
  - ASP.NET Core MVC 10.0
  - Entity Framework Core 10.0 (SQL Server Provider)
  - ASP.NET Core Identity (Role-based security)
  - QRCoder (Server-side SVG/PNG QR verification generation)
  - xUnit & Moq (Automated regression testing)

---

## 🗄 Database Setup & Migrations

### 1. Configure the Connection String
Edit `TechPrizeDraw/appsettings.json` (or `appsettings.Production.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=TechPrizeDrawDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### 2. Apply Migrations
Open PowerShell or your command prompt in the solution folder:
```powershell
dotnet ef database update --project TechPrizeDraw/TechPrizeDraw.csproj
```
*Note: If Entity Framework tools are not installed globally, run `dotnet tool install --global dotnet-ef`.*

When the application boots, `DbInitializer.cs` will automatically verify migrations, seed default roles (`SuperAdmin`, `Admin`, `Support`), create administrative accounts, seed the campaign settings, seed initial prize tiers, and configure default payment methods.

---

## ⚙️ Configuration & Environment Variables

Key parameters in `appsettings.json`:

```json
{
  "Campaign": {
    "Name": "TECH Prize Draw",
    "TicketPrefix": "TECH",
    "TicketPrice": 50.00,
    "MaximumTickets": 5000,
    "ReservationTimeoutMinutes": 60,
    "Enabled": false,
    "DrawDate": "2026-10-23T20:00:00Z"
  },
  "Email": {
    "Enabled": false,
    "Host": "smtp.mailtrap.io",
    "Port": 587,
    "Username": "YOUR_SMTP_USER",
    "Password": "YOUR_SMTP_PASSWORD",
    "FromEmail": "techprizedraw@inbox.eu",
    "FromName": "TECH Prize Draw Official"
  }
}
```

### Environment Variable Overrides (Production)
For security, credentials can be set via system environment variables:
```bash
ConnectionStrings__DefaultConnection="Server=tcp:sqlserver.internal;Database=TechPrizeDrawDb;User Id=dbuser;Password=StrongPassword!;TrustServerCertificate=False"
Campaign__Enabled="true"
Email__Enabled="true"
Email__Host="smtp.sendgrid.net"
Email__Port="587"
Email__Username="apikey"
Email__Password="SG.YourSendGridApiKey"
```

---

## 🔐 Default Seed Data & Admin Credentials

On first run, the database is seeded with the following role-based administrative accounts:

| Role | Username / Email | Default Password | Permissions |
| :--- | :--- | :--- | :--- |
| **SuperAdmin** | `superadmin@techprizedraw.com` | `SuperAdmin@2026!` | Full administrative access, Cryptographic Draw Execution, Campaign Configuration, Staff Management |
| **Admin** | `admin@techprizedraw.com` | `Admin@2026!` | Payment verification, Order approvals/rejections, CSV Exports, Ticket Management |
| **Support** | `techprizedraw@inbox.eu` | `Support@2026!` | Read-only order/ticket lookup, customer service inquiry assistance |

> **IMPORTANT:** In production, immediately log into `/Admin` as SuperAdmin, navigate to profile/user settings, and change these default passwords!

---

## 🚀 Running Locally

1. **Clone or navigate to the repository:**
   ```bash
   cd d:\loT
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore TechPrizeDraw.slnx
   ```

3. **Run the test suite (15 automated unit tests):**
   ```bash
   dotnet test TechPrizeDraw.Tests/TechPrizeDraw.Tests.csproj
   ```

4. **Start the web application:**
   ```bash
   dotnet run --project TechPrizeDraw/TechPrizeDraw.csproj --launch-profile http
   ```
   The portal will be live at `http://localhost:5078`.

---

## 📖 Operator & Administrator Handbook

### 1. Changing Ticket Price
1. Navigate to `/Admin/Campaign`.
2. Locate the **Ticket Price (BDT)** field.
3. Update to the desired value (e.g., `100.00`).
4. **Precaution:** Modifying ticket prices after orders have been placed triggers an audit record and may cause historical accounting discrepancies. Ensure all existing orders have settled before modifying pricing.
5. Click **Save Campaign Settings**.

### 2. Changing Maximum Ticket Count
1. Navigate to `/Admin/Campaign`.
2. Locate **Maximum Tickets Available**.
3. Enter the new ceiling (e.g., `10000`). The system will not allow setting a value lower than the count of currently issued valid tickets.
4. Click **Save Campaign Settings**.

### 3. Managing MFS Payment Numbers
1. Navigate to `/Admin/PaymentMethods`.
2. Click **Edit** next to bKash or Nagad.
3. Update the **Account Number** (e.g., change from placeholder `01XXXXXXXXX` to your verified business Send Money number `01712345678`).
4. Update instructions if necessary (e.g., specify "Use Send Money option, Personal/Merchant").
5. Toggle **Is Active** to enable or disable the provider.
6. Click **Save Changes**. The public checkout page immediately reflects the updated number.

### 4. Customizing Prizes
1. Navigate to `/Admin/Prizes`.
2. To modify an existing prize, click **Edit** (e.g., Prize 1: Laptop).
3. Update the Name, Description, Image URL, or Display Order.
4. To add or retire prize tiers, toggle **Active** or create a new prize.
5. Save changes. The homepage and `/prizes` showcase update instantly.

### 5. Activating / Pausing the Campaign
1. Navigate to `/Admin/Campaign`.
2. Select **Campaign Status**:
   - `Draft`: Pre-launch staging; public purchases disabled.
   - `Active`: Live for customer purchases and reservations.
   - `Paused`: Temporary suspension; checkout disabled, inventory preserved.
   - `Ended`: Campaign concluded; ready for prize draw execution.
   - `DrawCompleted`: Official draw finalized; winners published.
3. Ensure **Campaign Enabled (Legal Gate)** is checked if all statutory requirements have been satisfied.
4. Click **Save Campaign Settings**.

### 6. Reviewing Pending Payments
1. Navigate to `/Admin/PaymentVerification`.
2. You will see an operational table displaying:
   - Order Number
   - Customer Full Name, Mobile, Email
   - MFS Provider (bKash / Nagad)
   - Submitted Transaction ID
   - Reservation Expiration Countdown
3. Click **Review & Verify** on any pending order.

### 7. Approving Payments & Issuing Tickets
1. From the **Review & Verify** screen:
   - Independently check your physical MFS phone or merchant portal to confirm receipt of the ৳50 BDT credit matching the customer's Transaction ID.
2. Enter an **Admin Note** (e.g., *"Verified via bKash statement #BK99283746 on 2026-09-22"*).
3. Click **Approve Payment & Issue Ticket**.
4. Confirm the confirmation prompt.
5. The system performs an atomic database commit:
   - Next sequential ticket (e.g., `TECH-0001`) is minted.
   - Order transitions to `Approved`.
   - Ticket transitions to `Valid`.
   - QR verification data is calculated.
   - Confirmation email with the digital voucher is dispatched.

### 8. Rejecting Invalid Payments
1. From the **Review & Verify** screen:
   - If the customer provided a fabricated TrxID, sent an incorrect amount, or payment was not received:
2. In the Rejection form, select or enter a **Rejection Reason** (e.g., *"Payment Not Received"*, *"Invalid Transaction ID"*).
3. Enter an optional **Admin Note**.
4. Click **Reject Payment & Release Inventory**.
5. The order status updates to `Rejected`, the reserved inventory slot is immediately returned to the available pool, and an audit entry is created.

### 9. Resending Ticket Vouchers
1. If a participant didn't receive their email due to spam filters or transient SMTP errors:
2. Navigate to `/Admin/Tickets` or `/Admin/EmailLogs`.
3. Locate the customer's valid ticket.
4. Click **Resend Email**.
5. The system regenerates the voucher email and logs the delivery attempt. The ticket status and sequence remain unaltered.

### 10. Executing the Final Cryptographic Draw
1. Ensure the campaign status is set to `Ended` (or `Active` with draw scheduled).
2. Log in with an account having the **SuperAdmin** role.
3. Navigate to `/Admin/Draw`.
4. The console verifies:
   - Eligible valid ticket count (must be ≥ active prize tiers).
   - Confirmation that draw has not previously run.
5. Enter **Operator Notes** (e.g., *"Conducted in presence of official oversight board"*).
6. Click **Execute Official Cryptographic Draw** and confirm.
7. The platform executes the selection using hardware-grade pseudorandomness:
   ```csharp
   int winningIndex = RandomNumberGenerator.GetInt32(0, pool.Count);
   ```
8. Winners are locked into `DrawWinners`, the draw is marked `IsFinalized = true`, an immutable audit record is committed, and results are published to the public `/winners` page.

---

## 🔒 Security & Concurrency Architecture

### 1. Serializable Isolation Concurrency Protection
To prevent race conditions where simultaneous submissions could exceed the 5,000 ticket ceiling, the platform uses explicit serializable transactions:
```csharp
using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
```
Ticket assignment uses an atomic sequence evaluation (`MAX(SequenceNumber) + 1`), backed by a unique database constraint `UQ_Tickets_SequenceNumber` and `UQ_Tickets_TicketNumber`.

### 2. Customer Submission Is Untrusted Reference Data
Customer-entered Transaction IDs and mobile numbers are treated strictly as customer-supplied assertions. The application does not automatically issue tickets or validate transactions without human administrator verification against genuine MFS accounts.

### 3. Duplicate Transaction ID Safeguard
Duplicate Transaction IDs within active or pending orders are checked prior to order acceptance to deter fraudulent replay attempts.

### 4. Public QR Code Data Privacy Protection
The public verification endpoint (`/ticket/verify/{ticketNumber}`) and QR code do not expose the customer's full phone number, email address, transaction reference, or internal system IDs. The customer name is privacy-masked (e.g., `T***** A****`).

### 5. Anti-Forgery & Role-Based Authorization
All public and administrative forms require ASP.NET Core anti-forgery tokens (`@Html.AntiForgeryToken()`). Administrative controllers enforce role-based access (`[Authorize(Roles = "SuperAdmin,Admin")]`), with high-impact operations like draw execution restricted exclusively to `SuperAdmin`.

---

## 🌐 Production Deployment Guide

### Deployment on Windows Server with IIS
1. **Install IIS Prerequisites:**
   - Install IIS with ASP.NET Core Module v2 (via [.NET 10.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0)).
   - Install URL Rewrite module.

2. **Publish the Application:**
   ```powershell
   dotnet publish TechPrizeDraw/TechPrizeDraw.csproj -c Release -o C:\inetpub\TechPrizeDraw
   ```

3. **Configure IIS Application Pool:**
   - Name: `TechPrizeDrawPool`
   - .NET CLR Version: `No Managed Code`
   - Identity: `ApplicationPoolIdentity` (or designated service account with SQL Server access).

4. **Create IIS Website:**
   - Physical Path: `C:\inetpub\TechPrizeDraw`
   - Bindings: Port 80 (redirect to HTTPS) and Port 443 with your SSL/TLS Certificate.

5. **Configure Production Settings:**
   - Edit `appsettings.Production.json` to point to production SQL Server and SMTP credentials.

### Deployment on Linux (Ubuntu 22.04 + Nginx Reverse Proxy)
1. **Install .NET 10 Runtime:**
   ```bash
   sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0
   ```

2. **Create Systemd Service (`/etc/systemd/system/techprizedraw.service`):**
   ```ini
   [Unit]
   Description=TECH Prize Draw Web Application
   After=network.target

   [Service]
   WorkingDirectory=/var/www/techprizedraw
   ExecStart=/usr/bin/dotnet /var/www/techprizedraw/TechPrizeDraw.dll
   Restart=always
   RestartSec=10
   SyslogIdentifier=techprizedraw
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

   [Install]
   WantedBy=multi-user.target
   ```

3. **Configure Nginx Reverse Proxy:**
   ```nginx
   server {
       listen 80;
       server_name draw.yourdomain.com;
       return 301 https://$host$request_uri;
   }

   server {
       listen 443 ssl http2;
       server_name draw.yourdomain.com;

       ssl_certificate /etc/letsencrypt/live/draw.yourdomain.com/fullchain.pem;
       ssl_certificate_key /etc/letsencrypt/live/draw.yourdomain.com/privkey.pem;

       location / {
           proxy_pass         http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header   Upgrade $http_upgrade;
           proxy_set_header   Connection keep-alive;
           proxy_set_header   Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header   X-Forwarded-Proto $scheme;
       }
   }
   ```

4. **Enable & Start Service:**
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable techprizedraw.service
   sudo systemctl start techprizedraw.service
   ```

---

## ✅ Testing & Quality Assurance Checklist

Prior to public launch, verify that each operational scenario functions as expected:

- [x] **TEST 1: Customer Submits Payment Information**
  - Result: Order status is `PendingVerification`. No valid ticket is generated. No ticket email is sent. Available inventory decrements to prevent overselling.
- [x] **TEST 2: Admin Approves Payment**
  - Result: Order status becomes `Approved`. Next sequential ticket (e.g., `TECH-0001`) is generated. Ticket status is `Valid`. Voucher is rendered with QR code. Delivery email is logged and sent.
- [x] **TEST 3: Admin Rejects Payment**
  - Result: Order status becomes `Rejected`. No ticket is minted. Reserved inventory slot is released back to available pool.
- [x] **TEST 4: Duplicate Transaction ID Prevention**
  - Result: Submitting an already pending/approved TrxID displays a validation error preventing duplicate processing.
- [x] **TEST 5: Concurrent Submissions Under Load**
  - Result: Database serializable transactions and sequential indexing prevent overselling or duplicate sequence numbers.
- [x] **TEST 6: 5,000 Inventory Cap Reached**
  - Result: Homepage dynamic counter displays "SOLD OUT", checkout submissions are disabled, and API returns `isSoldOut: true`.
- [x] **TEST 7: Campaign Expiration / Pause**
  - Result: Public checkout forms are disabled with an informative advisory banner.
- [x] **TEST 8: Public QR Ticket Verification**
  - Result: Navigating to `/ticket/verify/{ticketNumber}` renders "VALID TICKET" with privacy-masked customer data and draw eligibility.
- [x] **TEST 9: Invalid Ticket Verification**
  - Result: Navigating to an unissued ticket displays "INVALID / NOT FOUND".
- [x] **TEST 10: Email Failure Handling**
  - Result: If SMTP is offline, ticket remains valid. Admin can review status in `/Admin/EmailLogs` and trigger "Resend Email".
- [x] **TEST 11: Role Authorization on Draw**
  - Result: Non-SuperAdmin accounts attempting to access `/Admin/Draw` are denied access.
- [x] **TEST 12: Cryptographic Draw Execution**
  - Result: Executed via `System.Security.Cryptography.RandomNumberGenerator`. Winners are locked, immutable, and published to `/winners`.
