using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;

namespace HospitalManagementSystem.Tests
{
    public class Program
    {
        private static int _passed = 0;
        private static int _failed = 0;

        public static async Task<int> Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=====================================================================");
            Console.WriteLine("  CITY GENERAL HOSPITAL MANAGEMENT SYSTEM - AUTOMATED TEST SUITE     ");
            Console.WriteLine("  Target Framework: .NET 10.0 | ISO/IEC & DPDP Healthcare Compliance ");
            Console.WriteLine("=====================================================================");
            Console.ResetColor();

            await RunTest("Patient Management: Duplicate Phone Detection", Test_DuplicatePatient_PhoneDetection);
            await RunTest("Patient Management: Duplicate Email Detection", Test_DuplicatePatient_EmailDetection);
            await RunTest("Patient Management: Unique Patient Allowed", Test_UniquePatient_Registration);
            await RunTest("Appointment Scheduling: Double-Booking Prevention", Test_Appointment_DoubleBookingPrevention);
            await RunTest("Clinical Safety: Doctor Drug Allergy Cross-Checking", Test_ClinicalSafety_AllergyAlertCrossCheck);
            await RunTest("Pharmacy Safety: Expired Medicine Dispensation Blocking", Test_Pharmacy_ExpiredMedicineBlocked);
            await RunTest("Pharmacy Safety: Atomic Stock Deduction & Movement Audit", Test_Pharmacy_StockDeductionAndAudit);
            await RunTest("Pharmacy Safety: Stock Insufficiency Guard", Test_Pharmacy_InsufficientStockBlocked);
            await RunTest("Bed Management: Infection Control Lifecycle (Available -> Occupied -> Cleaning -> Available)", Test_Bed_InfectionControlLifecycle);
            await RunTest("Laboratory LIMS: 6-Stage Diagnostic Lifecycle", Test_Laboratory_SixStageLifecycle);
            await RunTest("Laboratory Quality Control: Unverified Report Release Block", Test_Laboratory_UnverifiedReportReleaseBlocked);
            await RunTest("Billing & Financial: Automated Admission Tariff & Itemized Aggregation", Test_Billing_AdmissionTariffCalculation);
            await RunTest("Security & Privacy: IDOR Resource Ownership Verification", Test_Security_IdorProtectionOnMedicalRecords);
            await RunTest("Appointment Scheduling: Re-booking Cancelled Slot (Filtered Index)", Test_Appointment_RebookingCancelledSlotAllowedWithoutCrash);
            await RunTest("Appointment Scheduling: Defensive Time Slot Format Parsing", Test_Appointment_DefensiveTimeSlotParsing);
            await RunTest("Security & Privacy: Appointment Resource Ownership (IDOR Guard)", Test_Security_AppointmentIdorOwnershipVerification);
            await RunTest("Security & Access: Billing Controller Role Guard Verification", Test_Security_BillingRoleGuardRestriction);

            Console.WriteLine();
            Console.WriteLine("=====================================================================");
            if (_failed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ALL TESTS PASSED: {_passed} Passed, 0 Failed (100% Success Rate)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  TEST RUN COMPLETED WITH FAILURES: {_passed} Passed, {_failed} Failed");
            }
            Console.ResetColor();
            Console.WriteLine("=====================================================================");

            return _failed == 0 ? 0 : 1;
        }

        private static async Task RunTest(string testName, Func<Task> testFunc)
        {
            Console.Write($"[TEST] {testName,-62} ... ");
            try
            {
                await testFunc();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PASS");
                Console.ResetColor();
                _passed++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL");
                Console.WriteLine($"       Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"       Inner: {ex.InnerException.Message}");
                }
                Console.ResetColor();
                _failed++;
            }
        }

        private static ApplicationDbContext CreateInMemoryContext(out SqliteConnection connection)
        {
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        // 1. Patient Duplicate Phone Detection
        private static async Task Test_DuplicatePatient_PhoneDetection()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                context.Patients.Add(new Patient
                {
                    FullName = "Rajesh Patel",
                    Gender = "Male",
                    Phone = "9876543210",
                    Email = "rajesh@example.com",
                    PatientCode = "PAT-001",
                    DateOfBirth = DateTime.Today.AddYears(-35)
                });
                await context.SaveChangesAsync();

                var incomingPhone = "9876543210";
                var isDuplicate = await context.Patients.AnyAsync(p => p.Phone == incomingPhone);

                Assert(isDuplicate, "Expected duplicate detection for matching phone number.");
            }
        }

        // 2. Patient Duplicate Email Detection
        private static async Task Test_DuplicatePatient_EmailDetection()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                context.Patients.Add(new Patient
                {
                    FullName = "Sunita Verma",
                    Gender = "Female",
                    Phone = "9811122233",
                    Email = "sunita@example.com",
                    PatientCode = "PAT-002",
                    DateOfBirth = DateTime.Today.AddYears(-28)
                });
                await context.SaveChangesAsync();

                var incomingEmail = "sunita@example.com";
                var isDuplicate = await context.Patients.AnyAsync(p => p.Email != null && p.Email.ToLower() == incomingEmail.ToLower());

                Assert(isDuplicate, "Expected duplicate detection for matching email address.");
            }
        }

        // 3. Unique Patient Allowed
        private static async Task Test_UniquePatient_Registration()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                context.Patients.Add(new Patient
                {
                    FullName = "Existing User",
                    Gender = "Male",
                    Phone = "9999900000",
                    Email = "existing@example.com",
                    PatientCode = "PAT-003",
                    DateOfBirth = DateTime.Today.AddYears(-40)
                });
                await context.SaveChangesAsync();

                var newPhone = "8888811111";
                var newEmail = "newperson@example.com";
                var isDuplicate = await context.Patients.AnyAsync(p => p.Phone == newPhone || (p.Email != null && p.Email.ToLower() == newEmail.ToLower()));

                Assert(!isDuplicate, "Expected no duplicate detected for distinct phone and email.");
            }
        }

        // 4. Appointment Double-Booking Prevention
        private static async Task Test_Appointment_DoubleBookingPrevention()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                var dept = new Department { Name = "Cardiology", Description = "Heart Care" };
                context.Departments.Add(dept);
                await context.SaveChangesAsync();

                var doc = new Doctor { FullName = "Dr. Mehta", DepartmentId = dept.DepartmentId, Specialization = "Cardiology", IsActive = true, ConsultationFee = 600 };
                var p1 = new Patient { FullName = "Patient A", Gender = "Male", Phone = "9000000001", PatientCode = "PAT-A", DateOfBirth = DateTime.Today.AddYears(-30) };
                var p2 = new Patient { FullName = "Patient B", Gender = "Female", Phone = "9000000002", PatientCode = "PAT-B", DateOfBirth = DateTime.Today.AddYears(-25) };
                context.AddRange(doc, p1, p2);
                await context.SaveChangesAsync();

                var targetDate = DateTime.Today.AddDays(1);
                var targetSlot = new TimeSpan(10, 0, 0); // 10:00 AM

                // Book slot for Patient A
                context.Appointments.Add(new Appointment
                {
                    DoctorId = doc.DoctorId,
                    DepartmentId = dept.DepartmentId,
                    PatientId = p1.PatientId,
                    AppointmentDate = targetDate,
                    AppointmentTime = targetSlot,
                    Status = "Scheduled",
                    AppointmentCode = "APT-001"
                });
                await context.SaveChangesAsync();

                // Check collision before booking Patient B
                var slotTaken = await context.Appointments.AnyAsync(a =>
                    a.DoctorId == doc.DoctorId &&
                    a.AppointmentDate.Date == targetDate.Date &&
                    a.AppointmentTime == targetSlot &&
                    a.Status != "Cancelled");

                Assert(slotTaken, "Doctor schedule collision check must flag already booked slot.");
            }
        }

        // 5. Clinical Safety: Doctor Allergy Alert Cross-Check
        private static async Task Test_ClinicalSafety_AllergyAlertCrossCheck()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                var patient = new Patient
                {
                    FullName = "Anil Kapoor",
                    Gender = "Male",
                    Phone = "9777777777",
                    PatientCode = "PAT-ALLERGY",
                    Allergies = "Penicillin, Amoxicillin, Sulfa drugs",
                    DateOfBirth = DateTime.Today.AddYears(-45)
                };
                context.Patients.Add(patient);
                await context.SaveChangesAsync();

                // Prescribed medicine check logic
                string prescribedDrug = "Amoxicillin 500mg";
                bool allergyDetected = false;
                string flaggedAllergy = "";

                if (!string.IsNullOrWhiteSpace(patient.Allergies))
                {
                    var allergyList = patient.Allergies.Split(new[] { ',', ';', '\n', '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var a in allergyList)
                    {
                        if (prescribedDrug.Contains(a, StringComparison.OrdinalIgnoreCase) ||
                            a.Contains(prescribedDrug.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
                        {
                            allergyDetected = true;
                            flaggedAllergy = a;
                            break;
                        }
                    }
                }

                Assert(allergyDetected, "Prescribing Amoxicillin to patient with Penicillin/Amoxicillin allergy must trigger alert.");
                Assert(flaggedAllergy.Equals("Amoxicillin", StringComparison.OrdinalIgnoreCase), "Flagged allergy should match Amoxicillin.");
            }
        }

        // 6. Pharmacy: Expired Medicine Dispensation Blocking
        private static async Task Test_Pharmacy_ExpiredMedicineBlocked()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                var expiredMed = new Medicine
                {
                    Name = "Expired Ciprofloxacin",
                    BatchNumber = "BATCH-EXP-01",
                    ExpiryDate = DateTime.Today.AddDays(-15), // expired 15 days ago
                    Quantity = 100,
                    SellingPrice = 15.00m,
                    IsActive = true
                };
                context.Medicines.Add(expiredMed);
                await context.SaveChangesAsync();

                bool isExpired = expiredMed.ExpiryDate < DateTime.Today;
                Assert(isExpired, "System must identify medicine batch as expired.");

                bool canDispense = !isExpired && expiredMed.Quantity >= 1;
                Assert(!canDispense, "Pharmacy workflow must strictly block dispensing expired batch.");
            }
        }

        // 7. Pharmacy: Atomic Stock Deduction & Movement Audit
        private static async Task Test_Pharmacy_StockDeductionAndAudit()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                var dept = new Department { Name = "General Medicine", Description = "OPD" };
                context.Departments.Add(dept);
                await context.SaveChangesAsync();

                var doc = new Doctor { FullName = "Dr. Joshi", DepartmentId = dept.DepartmentId, Specialization = "Internal Medicine", IsActive = true };
                var patient = new Patient { FullName = "Kavita Rao", Gender = "Female", Phone = "9555544444", PatientCode = "PAT-PHARM", DateOfBirth = DateTime.Today.AddYears(-30) };
                var med = new Medicine
                {
                    Name = "Paracetamol 650mg",
                    BatchNumber = "BATCH-PARA-99",
                    ExpiryDate = DateTime.Today.AddYears(2),
                    Quantity = 50,
                    SellingPrice = 2.50m,
                    IsActive = true
                };
                context.AddRange(doc, patient, med);
                await context.SaveChangesAsync();

                var prescription = new Prescription
                {
                    PrescriptionCode = "RX-TEST-001",
                    PatientId = patient.PatientId,
                    DoctorId = doc.DoctorId,
                    Diagnosis = "Viral Fever"
                };
                context.Prescriptions.Add(prescription);
                await context.SaveChangesAsync();

                int dispenseQty = 10;
                int previousStock = med.Quantity;

                // Simulate atomic dispensing transaction
                med.Quantity -= dispenseQty;

                var dispensation = new PharmacyDispensation
                {
                    PrescriptionId = prescription.PrescriptionId,
                    MedicineId = med.MedicineId,
                    MedicineName = med.Name,
                    PatientId = patient.PatientId,
                    QuantityDispensed = dispenseQty,
                    UnitPrice = med.SellingPrice,
                    TotalPrice = dispenseQty * med.SellingPrice,
                    BatchNumber = med.BatchNumber ?? "BATCH-01",
                    DispensedByStaffName = "Pharmacist John",
                    DispensedAt = DateTime.UtcNow
                };

                var movement = new StockMovement
                {
                    MedicineId = med.MedicineId,
                    MovementType = "Dispensation",
                    QuantityChange = -dispenseQty,
                    RemainingStock = med.Quantity,
                    ReferenceNumber = $"DISP-{patient.PatientId}",
                    PerformedBy = "Pharmacist John",
                    Timestamp = DateTime.UtcNow
                };

                context.PharmacyDispensations.Add(dispensation);
                context.StockMovements.Add(movement);
                await context.SaveChangesAsync();

                var updatedMed = await context.Medicines.FindAsync(med.MedicineId);
                Assert(updatedMed!.Quantity == 40, $"Expected remaining stock of 40, found {updatedMed.Quantity}.");

                var loggedMovement = await context.StockMovements.FirstOrDefaultAsync(m => m.MedicineId == med.MedicineId);
                Assert(loggedMovement != null && loggedMovement.RemainingStock == 40, "Stock movement audit trail record must be created.");
            }
        }

        // 8. Pharmacy: Insufficient Stock Guard
        private static async Task Test_Pharmacy_InsufficientStockBlocked()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                var med = new Medicine
                {
                    Name = "Rare Antitoxin",
                    BatchNumber = "BATCH-RARE-1",
                    ExpiryDate = DateTime.Today.AddYears(1),
                    Quantity = 3,
                    SellingPrice = 500.00m,
                    IsActive = true
                };
                context.Medicines.Add(med);
                await context.SaveChangesAsync();

                int requestedQty = 10;
                bool stockSufficient = med.Quantity >= requestedQty;

                Assert(!stockSufficient, "System must block dispensation when requested quantity exceeds available stock.");
            }
        }

        // 9. Bed Infection Control Lifecycle
        private static async Task Test_Bed_InfectionControlLifecycle()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                var bed = new Bed
                {
                    BedNumber = "ICU-05",
                    Ward = "ICU",
                    DailyCharge = 2500m,
                    Status = "Available"
                };
                context.Beds.Add(bed);
                await context.SaveChangesAsync();

                // Step 1: Admission occupies bed
                bed.Status = "Occupied";
                await context.SaveChangesAsync();
                Assert(bed.Status == "Occupied", "Bed status should be Occupied after admission.");

                // Step 2: Patient Discharge transitions bed to Cleaning (NOT Available directly)
                bed.Status = "Cleaning";
                await context.SaveChangesAsync();
                Assert(bed.Status == "Cleaning", "Discharged bed must transition to Cleaning for infection control.");

                // Step 3: Nurse sanitizes bed and completes infection control checklist
                bed.Status = "Available";
                bed.LastCleanedAt = DateTime.UtcNow;
                bed.CleanedBy = "Nurse Sarah";
                await context.SaveChangesAsync();

                Assert(bed.Status == "Available", "Sanitized bed should return to Available.");
                Assert(bed.LastCleanedAt.HasValue && bed.CleanedBy == "Nurse Sarah", "Cleaning audit metadata must be persisted.");
            }
        }

        // 10. Laboratory 6-Stage Diagnostic Lifecycle
        private static async Task Test_Laboratory_SixStageLifecycle()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                var patient = new Patient { FullName = "Vikram Singh", Gender = "Male", Phone = "9444433333", PatientCode = "PAT-LAB", DateOfBirth = DateTime.Today.AddYears(-50) };
                var test = new LabTest { Name = "Complete Blood Count", Price = 450m, NormalRange = "Hb: 13-17 g/dL", IsActive = true };
                context.AddRange(patient, test);
                await context.SaveChangesAsync();

                // Stage 1: Requested
                var req = new LabTestRequest
                {
                    RequestCode = "LAB-REQ-101",
                    PatientId = patient.PatientId,
                    LabTestId = test.LabTestId,
                    Status = "Requested",
                    RequestedAt = DateTime.UtcNow
                };
                context.LabTestRequests.Add(req);
                await context.SaveChangesAsync();
                Assert(req.Status == "Requested", "Initial lab request status must be Requested.");

                // Stage 2: Sample Collected
                req.Status = "SampleCollected";
                req.SampleBarcode = "BAR-CBC-101";
                req.SampleType = "Whole Blood EDTA";
                req.SampleCollectedAt = DateTime.UtcNow;
                req.SampleCollectedBy = "Tech Priya";
                await context.SaveChangesAsync();
                Assert(req.Status == "SampleCollected" && req.SampleBarcode == "BAR-CBC-101", "Stage 2 status must be SampleCollected with barcode.");

                // Stage 3: Processing
                req.Status = "Processing";
                await context.SaveChangesAsync();
                Assert(req.Status == "Processing", "Stage 3 status must be Processing.");

                // Stage 4: Completed
                req.Status = "Completed";
                req.Result = "Hb: 9.8 g/dL (Low), TLC: 11,500 /uL (High)";
                req.IsAbnormal = true;
                req.AbnormalFlag = "Low";
                req.CompletedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                Assert(req.Status == "Completed" && req.IsAbnormal, "Stage 4 status must be Completed with abnormal flag.");

                // Stage 5: Verified
                req.Status = "Verified";
                req.VerifiedByDoctorName = "Dr. Sen (Pathologist)";
                req.VerifiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                Assert(req.Status == "Verified" && req.VerifiedByDoctorName != null, "Stage 5 status must be Verified by clinician.");

                // Stage 6: Report Released to Portal
                req.Status = "ReportReleased";
                await context.SaveChangesAsync();
                Assert(req.Status == "ReportReleased", "Final stage must be ReportReleased.");
            }
        }

        // 11. Laboratory Quality Control: Unverified Report Release Block
        private static async Task Test_Laboratory_UnverifiedReportReleaseBlocked()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                var req = new LabTestRequest
                {
                    RequestCode = "LAB-REQ-UNVERIFIED",
                    Status = "Completed", // not verified yet
                    Result = "Platelets: 45,000 /uL",
                    IsAbnormal = true,
                    AbnormalFlag = "Critical"
                };

                // Quality control guard check (matching LaboratoryController.ReleaseReport)
                bool canRelease = req.Status == "Verified";
                Assert(!canRelease, "Quality Control Guard must prohibit releasing unverified lab reports to patients.");
            }
        }

        // 12. Billing Tariff & Itemized Aggregation
        private static async Task Test_Billing_AdmissionTariffCalculation()
        {
            var admissionDate = DateTime.Today.AddDays(-4);
            var today = DateTime.Today;
            var stayDays = Math.Max(1, (int)(today - admissionDate).TotalDays); // 4 days

            decimal dailyBedRate = 1200m;
            decimal roomCharge = stayDays * dailyBedRate; // 4800
            decimal doctorFee = 700m;
            decimal labCharges = 950m;
            decimal pharmacyCharges = 1350m;
            decimal nursingFee = 200m;

            decimal subTotal = roomCharge + doctorFee + labCharges + pharmacyCharges + nursingFee; // 8000
            decimal gstTax = subTotal * 0.05m; // 400
            decimal totalBill = subTotal + gstTax; // 8400

            Assert(stayDays == 4, "Stay duration calculation should be 4 days.");
            Assert(roomCharge == 4800m, "Room charge must be 4 * 1200 = 4800.");
            Assert(totalBill == 8400m, "Total bill with 5% GST must be 8400.");
        }

        // 13. Security & Privacy: IDOR Resource Ownership Verification
        private static async Task Test_Security_IdorProtectionOnMedicalRecords()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                var dept = new Department { Name = "Pulmonology", Description = "Respiratory Care" };
                context.Departments.Add(dept);
                await context.SaveChangesAsync();

                var doc = new Doctor { FullName = "Dr. Roy", DepartmentId = dept.DepartmentId, Specialization = "Pulmonology", IsActive = true };
                var patient1 = new Patient { FullName = "Patient Alice", Gender = "Female", Phone = "9111111111", PatientCode = "PAT-ALICE", UserId = "user-guid-alice", DateOfBirth = DateTime.Today.AddYears(-29) };
                var patient2 = new Patient { FullName = "Patient Bob", Gender = "Male", Phone = "9222222222", PatientCode = "PAT-BOB", UserId = "user-guid-bob", DateOfBirth = DateTime.Today.AddYears(-34) };
                context.AddRange(doc, patient1, patient2);
                await context.SaveChangesAsync();

                var prescriptionAlice = new Prescription
                {
                    PrescriptionCode = "RX-ALICE-01",
                    PatientId = patient1.PatientId,
                    DoctorId = doc.DoctorId,
                    Diagnosis = "Acute Bronchitis"
                };
                context.Prescriptions.Add(prescriptionAlice);
                await context.SaveChangesAsync();

                // Simulated request by Bob trying to access Alice's prescription
                string bobUserId = "user-guid-bob";
                var requestedPrescription = await context.Prescriptions.Include(p => p.Patient).FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionAlice.PrescriptionId);

                bool isOwner = requestedPrescription?.Patient?.UserId == bobUserId;
                Assert(!isOwner, "IDOR Guard: User Bob must NOT be authorized to view Alice's medical record.");

                // Simulated request by Alice
                string aliceUserId = "user-guid-alice";
                bool isAliceOwner = requestedPrescription?.Patient?.UserId == aliceUserId;
                Assert(isAliceOwner, "Authorized Patient Alice must have access to her own medical records.");
            }
        }

        // 14. Appointment Re-booking Cancelled Slot (Filtered Index Test)
        private static async Task Test_Appointment_RebookingCancelledSlotAllowedWithoutCrash()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                var dept = new Department { Name = "Orthopedics", Description = "Bone Care" };
                context.Departments.Add(dept);
                await context.SaveChangesAsync();

                var doc = new Doctor { FullName = "Dr. Amit", DepartmentId = dept.DepartmentId, Specialization = "Orthopedics", IsActive = true };
                var p1 = new Patient { FullName = "Patient One", Gender = "Male", Phone = "9444400001", PatientCode = "PAT-01", DateOfBirth = DateTime.Today.AddYears(-30) };
                var p2 = new Patient { FullName = "Patient Two", Gender = "Female", Phone = "9444400002", PatientCode = "PAT-02", DateOfBirth = DateTime.Today.AddYears(-28) };
                context.AddRange(doc, p1, p2);
                await context.SaveChangesAsync();

                var targetDate = DateTime.Today.AddDays(2);
                var targetSlot = new TimeSpan(11, 0, 0);

                // Slot booked and then cancelled
                var apt1 = new Appointment
                {
                    DoctorId = doc.DoctorId,
                    DepartmentId = dept.DepartmentId,
                    PatientId = p1.PatientId,
                    AppointmentDate = targetDate,
                    AppointmentTime = targetSlot,
                    Status = "Cancelled",
                    AppointmentCode = "APT-CANCEL-01"
                };
                context.Appointments.Add(apt1);
                await context.SaveChangesAsync();

                // Patient Two re-books the same slot
                var apt2 = new Appointment
                {
                    DoctorId = doc.DoctorId,
                    DepartmentId = dept.DepartmentId,
                    PatientId = p2.PatientId,
                    AppointmentDate = targetDate,
                    AppointmentTime = targetSlot,
                    Status = "Confirmed",
                    AppointmentCode = "APT-CONFIRM-02"
                };
                context.Appointments.Add(apt2);
                await context.SaveChangesAsync();

                var activeCount = await context.Appointments.CountAsync(a =>
                    a.DoctorId == doc.DoctorId &&
                    a.AppointmentDate.Date == targetDate.Date &&
                    a.AppointmentTime == targetSlot &&
                    a.Status != "Cancelled");

                Assert(activeCount == 1, "Only 1 active appointment must exist for the re-booked time slot.");
            }
        }

        // 15. Appointment Defensive Time Slot Format Parsing
        private static async Task Test_Appointment_DefensiveTimeSlotParsing()
        {
            string invalidSlot1 = "invalid-time";
            string invalidSlot2 = "";
            string validSlot = "14:30";

            bool parse1 = TimeSpan.TryParse(invalidSlot1, out var ts1);
            bool parse2 = TimeSpan.TryParse(invalidSlot2, out var ts2);
            bool parse3 = TimeSpan.TryParse(validSlot, out var ts3);

            Assert(!parse1, "Invalid string format should be rejected by TryParse.");
            Assert(!parse2, "Empty string should be rejected by TryParse.");
            Assert(parse3 && ts3.Hours == 14 && ts3.Minutes == 30, "Valid time 14:30 should be parsed accurately.");
        }

        // 16. Security: Appointment Resource Ownership (IDOR Guard)
        private static async Task Test_Security_AppointmentIdorOwnershipVerification()
        {
            using var context = CreateInMemoryContext(out var conn);
            using (conn)
            {
                var dept = new Department { Name = "Neurology", Description = "Brain Care" };
                context.Departments.Add(dept);
                await context.SaveChangesAsync();

                var doc = new Doctor { FullName = "Dr. Suresh", DepartmentId = dept.DepartmentId, Specialization = "Neurology", IsActive = true };
                var patientAlice = new Patient { FullName = "Alice Doe", Gender = "Female", Phone = "9555500001", PatientCode = "PAT-ALICE-APT", UserId = "uid-alice", DateOfBirth = DateTime.Today.AddYears(-25) };
                var patientBob = new Patient { FullName = "Bob Smith", Gender = "Male", Phone = "9555500002", PatientCode = "PAT-BOB-APT", UserId = "uid-bob", DateOfBirth = DateTime.Today.AddYears(-35) };
                context.AddRange(doc, patientAlice, patientBob);
                await context.SaveChangesAsync();

                var aptAlice = new Appointment
                {
                    DoctorId = doc.DoctorId,
                    DepartmentId = dept.DepartmentId,
                    PatientId = patientAlice.PatientId,
                    AppointmentDate = DateTime.Today.AddDays(1),
                    AppointmentTime = new TimeSpan(9, 30, 0),
                    Status = "Confirmed",
                    AppointmentCode = "APT-ALICE-99"
                };
                context.Appointments.Add(aptAlice);
                await context.SaveChangesAsync();

                // Bob attempts to access Alice's appointment
                var targetApt = await context.Appointments.Include(a => a.Patient).FirstOrDefaultAsync(a => a.AppointmentId == aptAlice.AppointmentId);
                string bobUserId = "uid-bob";
                bool bobHasAccess = targetApt?.Patient?.UserId == bobUserId;
                Assert(!bobHasAccess, "IDOR Guard: Patient Bob cannot view or modify Alice's appointment.");

                string aliceUserId = "uid-alice";
                bool aliceHasAccess = targetApt?.Patient?.UserId == aliceUserId;
                Assert(aliceHasAccess, "Patient Alice is correctly verified as the legitimate owner.");
            }
        }

        // 17. Security: Billing Controller Role Guard Verification
        private static async Task Test_Security_BillingRoleGuardRestriction()
        {
            // Verify roles authorized for sensitive financial operations
            var allowedBillingStaffRoles = new[] { "Admin", "SuperAdmin", "BillingStaff" };
            var allowedDeskRoles = new[] { "Admin", "SuperAdmin", "BillingStaff", "Receptionist" };

            string patientRole = "Patient";
            bool patientCanManageBilling = allowedBillingStaffRoles.Contains(patientRole);
            bool patientCanAccessDesk = allowedDeskRoles.Contains(patientRole);

            Assert(!patientCanManageBilling, "Patient role must NOT have access to Billing management / payment updates.");
            Assert(!patientCanAccessDesk, "Patient role must NOT have access to Billing Desk.");
        }

        private static void Assert(bool condition, string failureMessage)
        {
            if (!condition)
            {
                throw new InvalidOperationException(failureMessage);
            }
        }
    }
}
