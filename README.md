🌐 Live Demo

Live Website: "🚀 Click Here to Open Medicare Hospital Management System" (https://medicare-hospital-system-e4deftfrfghjcdb9.indiasouthcentral-01.azurewebsites.net)

The project is deployed on Microsoft Azure App Service.

# City General Hospital — Smart Healthcare Management Platform (2026–2030)

> **Enterprise-Grade ASP.NET Core 10.0 MVC + Entity Framework Core + ASP.NET Core Identity**  
> Modern, secure, multi-role hospital management ecosystem compliant with DPDP Act 2023 & HIPAA security principles.

---

## 🌟 Executive Summary

**City General Hospital Platform** is an enterprise-ready healthcare platform engineered with **ASP.NET Core 10.0**, **Entity Framework Core**, and **Bootstrap 5 + Custom Clinical UI**. It modernizes the entire patient lifecycle—from smart registration with duplicate detection and online self-service booking to clinical doctor consultations with allergy cross-checking, electronic Medication Administration Records (MAR), infection-control bed state management, 6-stage Laboratory Information Management (LIMS), closed-loop pharmacy dispensing with batch expiry enforcement, itemized billing, and waiting-hall TV queue displays.

---

## 🚀 Quick Start Guide

### Prerequisites
- **.NET 10.0 SDK** (or .NET 8 / 9 runtime)
- Any modern web browser (Edge, Chrome, Firefox, Safari)

### Running the Application
```bash
# 1. Open the project root directory
cd "d:\Marwadi university\Semester 5th\.NET\HospitalManagementSystem"

# 2. Build the solution
dotnet build

# 3. Launch the web application
dotnet run --urls "http://localhost:5000"
```
Open **[http://localhost:5000](http://localhost:5000)** in your browser.

### Running the Automated Test Suite
```bash
# Run the 13 automated clinical, pharmacy, LIMS, and security test fixtures
dotnet run --project HospitalManagementSystem.Tests --no-restore
```

---

## 🔑 Demo Login Credentials (9 Roles)

The database automatically seeds demo accounts, medical departments, doctors, patient records, beds, medicines, and sample diagnostic tests on first launch:

| Role | Email | Password | Dedicated Workspace / Access |
| :--- | :--- | :--- | :--- |
| **Super Admin** | `superadmin@hospital.com` | `Super@123` | Master executive dashboard, audit logs, system roles & configuration |
| **Hospital Admin** | `admin@hospital.com` | `Admin@123` | Department management, doctors roster, financial analytics & bed control |
| **Doctor** | `doctor@hospital.com` | `Doctor@123` | Clinical Workspace (`/Doctor/Workspace`), SOAP notes, allergy checks, e-prescribing |
| **Nurse** | `nurse@hospital.com` | `Nurse@123` | Inpatient Nurse Station (`/Nurse/Station`), vitals recording, MAR, bed sanitization |
| **Receptionist** | `receptionist@hospital.com` | `Recept@123` | Smart registration (`/Patients/Create`), OPD token generation, doctor appointments |
| **Pharmacist** | `pharmacy@hospital.com` | `Pharma@123` | Pharmacy Desk (`/Pharmacy/Desk`), batch dispensing, expiry enforcement, stock logs |
| **Lab Technician** | `lab@hospital.com` | `Lab@123` | Laboratory Workbench (`/Laboratory/Workbench`), sample barcodes, 6-stage reports |
| **Billing Staff** | `billing@hospital.com` | `Billing@123` | Billing & Accounts Desk (`/Billing/Desk`), auto discharge bills, receipt generation |
| **Patient** | `patient@hospital.com` | `Patient@123` | Patient Self-Service Portal (`/Portal`), lab reports, prescriptions, QR Health Card |

---

## 🏥 Platform Architecture & Workspaces

### 1. Patient Experience & Self-Service Portal (`/Portal`)
- **Smart Patient Registration**: Duplicate patient detection via phone and email prevents split medical records.
- **Personal Dashboard**: Track today's appointments, active prescriptions, released diagnostic reports, and outstanding bills.
- **Self-Service Appointments**: Pick specialty department, select clinician, and schedule open slots with instant confirmation.
- **Tamper-Proof QR Health Card**: Encrypted QR code token for instant identification at triage and hospital desks without exposing raw medical records.
- **Online Bill Settlements**: Review itemized medical bills and simulate secure instant UPI / Card payments.

### 2. Clinical Doctor Workspace (`/Doctor/Workspace`)
- **Today's Patient Queue**: Instant overview of scheduled appointments and pending OPD consultations.
- **Clinical Consultation Desk**: Record SOAP notes (Subjective symptoms, Objective observations, Assessment/Diagnosis, Plan).
- **Allergy Cross-Checking**: Live safety alerts when prescribing medications conflicting with patient's recorded drug allergies.
- **Non-Autonomous Decision Support**: All clinical aids are explicitly labeled as clinician decision support requiring qualified doctor sign-off.
- **Integrated Ordering**: Order laboratory investigations and digital prescriptions directly during the consultation.

### 3. Inpatient Nursing Station & Care (`/Nurse/Station`)
- **Inpatient Ward Monitoring**: Ward and bed census tracking active inpatient care.
- **Vital Signs Charting**: Capture Blood Pressure, Pulse, Body Temperature, SpO2 (Oxygen Saturation), Respiratory Rate, and Pain Scale.
- **Medication Administration Record (MAR)**: Verify scheduled doses, mark medication as administered with dosage timestamps, or flag delayed/missed reasons.
- **SBAR Shift Handovers**: Standardized nursing shift handover logs (Situation, Background, Assessment, Recommendation).
- **Infection-Control Bed Sanitization**: Discharged beds enter `Cleaning` status and cannot be re-allocated until signed off by nursing staff.

### 4. Closed-Loop Pharmacy Desk (`/Pharmacy/Desk`)
- **Prescription Dispensing Queue**: Real-time queue of active doctor prescriptions awaiting fulfillment.
- **Zero-Tolerance Expiry Blocking**: System hard-blocks dispensing any medicine batch past its expiration date.
- **Atomic Stock Management**: Real-time inventory deductions with safeguards preventing negative inventory.
- **Audit Ledger**: Comprehensive `StockMovement` logging of all stock adjustments, purchases, and dispensations.

### 5. Laboratory Information Management (LIMS) (`/Laboratory/Workbench`)
- **6-Stage Sample Lifecycle**: `Requested` ➔ `SampleCollected` ➔ `Processing` ➔ `Completed` ➔ `Verified` ➔ `ReportReleased`.
- **Sample Barcode Assignment**: Generates unique barcode tags and registers specimen type (e.g. EDTA Blood, Serum, Urine).
- **Abnormal Results Flagging**: Flags values outside biological reference ranges (`High`, `Low`, `Critical / Panic Value`).
- **Pathologist Verification Gate**: Unverified preliminary results cannot be released to patients and carry a "PRELIMINARY REPORT - NOT VERIFIED" watermark.

### 6. Billing & Financial Management (`/Billing/Desk`)
- **Discharge Bill Calculator**: One-click aggregation of inpatient room tariff (stay days × bed rate), ordered laboratory tests, and dispensed medications.
- **Itemized Invoicing**: Standardized tax invoicing with 5% GST and payment tracking across Cash, UPI, Card, and Net Banking.

### 7. Waiting Hall / TV Screen Queue Board (`/OPD/LiveQueue`)
- **Lobby Monitor Mode**: Full-screen, high-contrast display designed for waiting area displays (`Layout = null`).
- **Real-Time Token Calling**: Large token numbers indicating currently summoned patients by consulting chamber and doctor.
- **Digital Clock & Auto-Refresh**: Seamless client-side clock and auto-updating token queue.

### 8. Accessibility, Dark Mode & Multilingual Support
- **Multilingual Support**: Instant switcher between **English**, **Hindi (हिन्दी)**, and **Gujarati (ગુજરાતી)**.
- **High-Contrast Dark Mode**: One-click dark mode toggle for low-light clinical environments with persistent local storage.
- **IDOR Protection**: Strict resource-level ownership validation ensuring patients cannot view other patients' records or invoices.

---

## 🧪 Automated Test Suite Specification

The test suite in `HospitalManagementSystem.Tests` executes isolated in-memory SQLite database transactions testing critical healthcare invariants:

```
[TEST] Patient Management: Duplicate Phone Detection                  ... PASS
[TEST] Patient Management: Duplicate Email Detection                  ... PASS
[TEST] Patient Management: Unique Patient Allowed                     ... PASS
[TEST] Appointment Scheduling: Double-Booking Prevention              ... PASS
[TEST] Clinical Safety: Doctor Drug Allergy Cross-Checking            ... PASS
[TEST] Pharmacy Safety: Expired Medicine Dispensation Blocking        ... PASS
[TEST] Pharmacy Safety: Atomic Stock Deduction & Movement Audit       ... PASS
[TEST] Pharmacy Safety: Stock Insufficiency Guard                     ... PASS
[TEST] Bed Management: Infection Control Lifecycle (Avail->Occup->Clean) PASS
[TEST] Laboratory LIMS: 6-Stage Diagnostic Lifecycle                  ... PASS
[TEST] Laboratory Quality Control: Unverified Report Release Block    ... PASS
[TEST] Billing & Financial: Automated Admission Tariff & Itemized Agg ... PASS
[TEST] Security & Privacy: IDOR Resource Ownership Verification       ... PASS

=====================================================================
  ALL TESTS PASSED: 13 Passed, 0 Failed (100% Success Rate)
=====================================================================
```

---

## 🔒 Security & Privacy Compliance

- **Authentication & Sessions**: ASP.NET Core Identity with PBKDF2 with HMAC-SHA256 password hashing and secure cookie authentication (`HttpOnly`, `SameSite=Lax`).
- **Authorization**: Granular Role-Based Access Control (`[Authorize(Roles = "...")]`) combined with resource-level claim checking to defeat Insecure Direct Object References (IDOR).
- **Audit Trails**: Non-repudiable audit logging for sensitive clinical, pharmacy, and diagnostic operations.
- **Data Minimization**: QR tokens carry internal random GUID tokens rather than raw patient identifiers or medical diagnoses.

---

## 📄 License & Academic Attribution
Developed for academic curriculum requirements, technical evaluations, and real-world clinical modernization (2026–2030).
All rights reserved.
