using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Models;

namespace HospitalManagementSystem.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Create Roles (All 9 Healthcare Platform Roles)
            string[] roles = { "SuperAdmin", "Admin", "Doctor", "Receptionist", "Nurse", "LabTechnician", "Pharmacist", "BillingStaff", "Patient" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Ensure extended 2026-2030 database tables exist
            await EnsureExtendedSchemaAsync(context);

            // 2. Create Super Admin User
            if (await userManager.FindByEmailAsync("superadmin@hospital.com") == null)
            {
                var superAdmin = new ApplicationUser
                {
                    UserName = "superadmin@hospital.com",
                    Email = "superadmin@hospital.com",
                    FullName = "Executive Director / Super Admin",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(superAdmin, "Super@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                    await userManager.AddToRoleAsync(superAdmin, "Admin");
                }
            }

            // Create Hospital Administrator User
            if (await userManager.FindByEmailAsync("admin@hospital.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@hospital.com",
                    Email = "admin@hospital.com",
                    FullName = "Hospital Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // 3. Seed Departments (only if empty)
            if (!context.Departments.Any())
            {
                var departments = new List<Department>
                {
                    new() { Name = "General Medicine", Description = "General outpatient and inpatient care", Icon = "fa-stethoscope", IsActive = true },
                    new() { Name = "Cardiology", Description = "Heart and cardiovascular diseases", Icon = "fa-heart-pulse", IsActive = true },
                    new() { Name = "Neurology", Description = "Brain and nervous system disorders", Icon = "fa-brain", IsActive = true },
                    new() { Name = "Orthopedics", Description = "Bone and joint disorders", Icon = "fa-bone", IsActive = true },
                    new() { Name = "Pediatrics", Description = "Children's healthcare", Icon = "fa-baby", IsActive = true },
                    new() { Name = "Dermatology", Description = "Skin disorders and treatment", Icon = "fa-hand-dots", IsActive = true },
                    new() { Name = "Gynecology", Description = "Women's health and reproductive care", Icon = "fa-venus", IsActive = true },
                    new() { Name = "ENT", Description = "Ear, nose and throat disorders", Icon = "fa-ear-listen", IsActive = true },
                    new() { Name = "Emergency", Description = "24/7 emergency medical care", Icon = "fa-truck-medical", IsActive = true },
                    new() { Name = "Radiology", Description = "Medical imaging and diagnostics", Icon = "fa-x-ray", IsActive = true },
                    new() { Name = "Laboratory", Description = "Pathology and lab testing", Icon = "fa-flask", IsActive = true }
                };
                context.Departments.AddRange(departments);
                await context.SaveChangesAsync();
            }

            // 4. Seed Demo Doctors (only if empty)
            if (!context.Doctors.Any())
            {
                // Get department IDs
                var genMed = context.Departments.First(d => d.Name == "General Medicine").DepartmentId;
                var cardio = context.Departments.First(d => d.Name == "Cardiology").DepartmentId;
                var ortho = context.Departments.First(d => d.Name == "Orthopedics").DepartmentId;
                var pedi = context.Departments.First(d => d.Name == "Pediatrics").DepartmentId;
                var neuro = context.Departments.First(d => d.Name == "Neurology").DepartmentId;

                var doctors = new List<Doctor>
                {
                    new() { FullName = "Dr. Rahul Sharma", Specialization = "General Physician", DepartmentId = genMed, Qualification = "MBBS, MD", ExperienceYears = 10, Email = "dr.rahul@hospital.com", Phone = "9876543210", ConsultationFee = 500, AvailableDays = "Mon,Tue,Wed,Thu,Fri", AvailableTime = "09:00 - 13:00", IsActive = true },
                    new() { FullName = "Dr. Priya Patel", Specialization = "Cardiologist", DepartmentId = cardio, Qualification = "MBBS, MD, DM", ExperienceYears = 15, Email = "dr.priya@hospital.com", Phone = "9876543211", ConsultationFee = 1000, AvailableDays = "Mon,Wed,Fri", AvailableTime = "10:00 - 14:00", IsActive = true },
                    new() { FullName = "Dr. Amit Shah", Specialization = "Orthopedic Surgeon", DepartmentId = ortho, Qualification = "MBBS, MS (Ortho)", ExperienceYears = 12, Email = "dr.amit@hospital.com", Phone = "9876543212", ConsultationFee = 800, AvailableDays = "Tue,Thu,Sat", AvailableTime = "11:00 - 15:00", IsActive = true },
                    new() { FullName = "Dr. Meera Joshi", Specialization = "Pediatrician", DepartmentId = pedi, Qualification = "MBBS, DCH", ExperienceYears = 8, Email = "dr.meera@hospital.com", Phone = "9876543213", ConsultationFee = 600, AvailableDays = "Mon,Tue,Thu,Fri", AvailableTime = "09:00 - 12:00", IsActive = true },
                    new() { FullName = "Dr. Suresh Verma", Specialization = "Neurologist", DepartmentId = neuro, Qualification = "MBBS, MD, DM", ExperienceYears = 20, Email = "dr.suresh@hospital.com", Phone = "9876543214", ConsultationFee = 1200, AvailableDays = "Mon,Wed,Fri", AvailableTime = "14:00 - 18:00", IsActive = true }
                };
                context.Doctors.AddRange(doctors);
                await context.SaveChangesAsync();
            }

            // 5. Seed Sample Patients (only if empty)
            if (!context.Patients.Any())
            {
                var patients = new List<Patient>
                {
                    new() { PatientCode = "PAT-001", FullName = "Arjun Kumar", Gender = "Male", DateOfBirth = new DateTime(1990, 5, 15), Phone = "9012345678", Email = "arjun@example.com", BloodGroup = "O+", Address = "123, MG Road, Rajkot", EmergencyContact = "9012345679", Allergies = "Penicillin", RegistrationDate = DateTime.UtcNow.AddDays(-30), IsActive = true },
                    new() { PatientCode = "PAT-002", FullName = "Sunita Devi", Gender = "Female", DateOfBirth = new DateTime(1975, 8, 22), Phone = "9012345680", BloodGroup = "A+", Address = "456, Station Road, Rajkot", EmergencyContact = "9012345681", RegistrationDate = DateTime.UtcNow.AddDays(-20), IsActive = true },
                    new() { PatientCode = "PAT-003", FullName = "Ravi Mehta", Gender = "Male", DateOfBirth = new DateTime(2000, 3, 10), Phone = "9012345682", BloodGroup = "B+", Address = "789, University Road, Rajkot", RegistrationDate = DateTime.UtcNow.AddDays(-10), IsActive = true },
                    new() { PatientCode = "PAT-004", FullName = "Pooja Singh", Gender = "Female", DateOfBirth = new DateTime(1985, 11, 5), Phone = "9012345683", BloodGroup = "AB+", Address = "321, Civil Road, Rajkot", EmergencyContact = "9012345684", RegistrationDate = DateTime.UtcNow.AddDays(-5), IsActive = true },
                    new() { PatientCode = "PAT-005", FullName = "Vikram Rao", Gender = "Male", DateOfBirth = new DateTime(1960, 7, 18), Phone = "9012345685", BloodGroup = "O-", Address = "654, Park Colony, Rajkot", EmergencyContact = "9012345686", Allergies = "Sulfa drugs", RegistrationDate = DateTime.UtcNow.AddDays(-2), IsActive = true }
                };
                context.Patients.AddRange(patients);
                await context.SaveChangesAsync();
            }

            // 6. Seed Beds
            if (!context.Beds.Any())
            {
                var beds = new List<Bed>();
                // General ward: 10 beds
                for (int i = 1; i <= 10; i++)
                    beds.Add(new Bed { BedNumber = $"G-{i:D3}", Ward = "General", Room = $"Room {(i + 1) / 2}", DailyCharge = 500, Status = i <= 7 ? "Available" : "Occupied", IsActive = true });
                // ICU: 5 beds
                for (int i = 1; i <= 5; i++)
                    beds.Add(new Bed { BedNumber = $"ICU-{i:D3}", Ward = "ICU", Room = "ICU Ward", DailyCharge = 3000, Status = i <= 3 ? "Available" : "Occupied", IsActive = true });
                // Private: 8 beds
                for (int i = 1; i <= 8; i++)
                    beds.Add(new Bed { BedNumber = $"P-{i:D3}", Ward = "Private", Room = $"Suite {i}", DailyCharge = 2000, Status = "Available", IsActive = true });
                // Emergency: 4 beds
                for (int i = 1; i <= 4; i++)
                    beds.Add(new Bed { BedNumber = $"E-{i:D3}", Ward = "Emergency", Room = "Emergency Ward", DailyCharge = 1000, Status = "Available", IsActive = true });
                context.Beds.AddRange(beds);
                await context.SaveChangesAsync();
            }

            // 7. Seed Lab Tests
            if (!context.LabTests.Any())
            {
                var labTests = new List<LabTest>
                {
                    new() { Name = "Complete Blood Count (CBC)", Category = "Hematology", Description = "Full blood cell analysis", NormalRange = "RBC: 4.5-5.5 M/µL", Price = 200, IsActive = true },
                    new() { Name = "Blood Sugar (Fasting)", Category = "Biochemistry", Description = "Fasting glucose level", NormalRange = "70-100 mg/dL", Price = 80, IsActive = true },
                    new() { Name = "Blood Sugar (Random)", Category = "Biochemistry", Description = "Random glucose level", NormalRange = "<140 mg/dL", Price = 80, IsActive = true },
                    new() { Name = "Lipid Profile", Category = "Biochemistry", Description = "Cholesterol and triglycerides", NormalRange = "Total Cholesterol < 200 mg/dL", Price = 400, IsActive = true },
                    new() { Name = "Urine Routine", Category = "Microbiology", Description = "Complete urine analysis", NormalRange = "Clear, pH 4.5-8", Price = 100, IsActive = true },
                    new() { Name = "Liver Function Test (LFT)", Category = "Biochemistry", Description = "Liver enzymes and function", NormalRange = "SGPT: 7-56 U/L", Price = 500, IsActive = true },
                    new() { Name = "Kidney Function Test (KFT)", Category = "Biochemistry", Description = "Creatinine, urea, electrolytes", NormalRange = "Creatinine: 0.7-1.3 mg/dL", Price = 500, IsActive = true },
                    new() { Name = "Thyroid Function Test", Category = "Endocrinology", Description = "TSH, T3, T4 levels", NormalRange = "TSH: 0.4-4.0 mIU/L", Price = 600, IsActive = true },
                    new() { Name = "HbA1c", Category = "Biochemistry", Description = "3-month average blood sugar", NormalRange = "< 5.7%", Price = 350, IsActive = true },
                    new() { Name = "Malaria Test", Category = "Microbiology", Description = "Rapid malaria antigen test", NormalRange = "Negative", Price = 150, IsActive = true }
                };
                context.LabTests.AddRange(labTests);
                await context.SaveChangesAsync();
            }

            // 8. Seed Medicines
            if (!context.Medicines.Any())
            {
                var medicines = new List<Medicine>
                {
                    new() { Name = "Paracetamol 500mg", Category = "Analgesic", BatchNumber = "B2024-001", Supplier = "Sun Pharma", PurchasePrice = 2, SellingPrice = 5, Quantity = 500, MinStockLevel = 50, ExpiryDate = DateTime.Today.AddYears(2), Unit = "Tablet", IsActive = true },
                    new() { Name = "Amoxicillin 250mg", Category = "Antibiotic", BatchNumber = "B2024-002", Supplier = "Cipla", PurchasePrice = 8, SellingPrice = 15, Quantity = 200, MinStockLevel = 30, ExpiryDate = DateTime.Today.AddMonths(18), Unit = "Capsule", IsActive = true },
                    new() { Name = "Metformin 500mg", Category = "Antidiabetic", BatchNumber = "B2024-003", Supplier = "Dr. Reddy's", PurchasePrice = 3, SellingPrice = 8, Quantity = 8, MinStockLevel = 50, ExpiryDate = DateTime.Today.AddYears(1), Unit = "Tablet", IsActive = true },
                    new() { Name = "Amlodipine 5mg", Category = "Antihypertensive", BatchNumber = "B2024-004", Supplier = "Lupin", PurchasePrice = 5, SellingPrice = 12, Quantity = 150, MinStockLevel = 30, ExpiryDate = DateTime.Today.AddYears(2), Unit = "Tablet", IsActive = true },
                    new() { Name = "Omeprazole 20mg", Category = "Antacid", BatchNumber = "B2024-005", Supplier = "Zydus", PurchasePrice = 4, SellingPrice = 10, Quantity = 5, MinStockLevel = 40, ExpiryDate = DateTime.Today.AddDays(20), Unit = "Capsule", IsActive = true },
                    new() { Name = "Cetirizine 10mg", Category = "Antihistamine", BatchNumber = "B2024-006", Supplier = "GSK", PurchasePrice = 2, SellingPrice = 6, Quantity = 300, MinStockLevel = 50, ExpiryDate = DateTime.Today.AddYears(1), Unit = "Tablet", IsActive = true },
                    new() { Name = "Normal Saline 500ml", Category = "IV Fluid", BatchNumber = "B2024-007", Supplier = "Baxter", PurchasePrice = 40, SellingPrice = 80, Quantity = 50, MinStockLevel = 20, ExpiryDate = DateTime.Today.AddYears(2), Unit = "Bottle", IsActive = true },
                    new() { Name = "Dextrose 5% 500ml", Category = "IV Fluid", BatchNumber = "B2024-008", Supplier = "Baxter", PurchasePrice = 45, SellingPrice = 90, Quantity = 40, MinStockLevel = 20, ExpiryDate = DateTime.Today.AddYears(2), Unit = "Bottle", IsActive = true }
                };
                context.Medicines.AddRange(medicines);
                await context.SaveChangesAsync();
            }

            // 9. Seed Sample Appointments (only if empty)
            if (!context.Appointments.Any())
            {
                var doctor1Id = context.Doctors.First(d => d.FullName == "Dr. Rahul Sharma").DoctorId;
                var doctor2Id = context.Doctors.First(d => d.FullName == "Dr. Priya Patel").DoctorId;
                var patient1Id = context.Patients.First(p => p.PatientCode == "PAT-001").PatientId;
                var patient2Id = context.Patients.First(p => p.PatientCode == "PAT-002").PatientId;
                var patient3Id = context.Patients.First(p => p.PatientCode == "PAT-003").PatientId;
                var genMedId = context.Departments.First(d => d.Name == "General Medicine").DepartmentId;
                var cardioId = context.Departments.First(d => d.Name == "Cardiology").DepartmentId;

                var today = DateTime.Today;
                var appointments = new List<Appointment>
                {
                    new() { AppointmentCode = "APT-001", PatientId = patient1Id, DoctorId = doctor1Id, DepartmentId = genMedId, AppointmentDate = today, AppointmentTime = new TimeSpan(9, 0, 0), Reason = "Fever and cold", Status = "Confirmed", CreatedAt = DateTime.UtcNow },
                    new() { AppointmentCode = "APT-002", PatientId = patient2Id, DoctorId = doctor2Id, DepartmentId = cardioId, AppointmentDate = today, AppointmentTime = new TimeSpan(10, 30, 0), Reason = "Chest pain follow-up", Status = "Pending", CreatedAt = DateTime.UtcNow },
                    new() { AppointmentCode = "APT-003", PatientId = patient3Id, DoctorId = doctor1Id, DepartmentId = genMedId, AppointmentDate = today.AddDays(1), AppointmentTime = new TimeSpan(11, 0, 0), Reason = "Routine check-up", Status = "Pending", CreatedAt = DateTime.UtcNow }
                };
                context.Appointments.AddRange(appointments);
                await context.SaveChangesAsync();
            }

            // 10. Seed Inventory
            if (!context.InventoryItems.Any())
            {
                var items = new List<InventoryItem>
                {
                    new() { Name = "Surgical Gloves (Box)", Category = "PPE", Unit = "Box", Quantity = 50, MinStockLevel = 10, UnitPrice = 200, Supplier = "MedSupply Co.", LastUpdated = DateTime.UtcNow },
                    new() { Name = "N95 Masks", Category = "PPE", Unit = "Piece", Quantity = 200, MinStockLevel = 50, UnitPrice = 25, Supplier = "SafeGuard Pvt.", LastUpdated = DateTime.UtcNow },
                    new() { Name = "Disposable Syringes 5ml", Category = "Consumable", Unit = "Piece", Quantity = 8, MinStockLevel = 50, UnitPrice = 8, Supplier = "MedSupply Co.", LastUpdated = DateTime.UtcNow },
                    new() { Name = "Bandages (Roll)", Category = "Consumable", Unit = "Roll", Quantity = 100, MinStockLevel = 30, UnitPrice = 15, Supplier = "FirstAid Plus", LastUpdated = DateTime.UtcNow },
                    new() { Name = "Stethoscope", Category = "Equipment", Unit = "Piece", Quantity = 15, MinStockLevel = 5, UnitPrice = 1500, Supplier = "MedDevice Ltd.", LastUpdated = DateTime.UtcNow }
                };
                context.InventoryItems.AddRange(items);
                await context.SaveChangesAsync();
            }

            // 11. Seed Sample Expenses
            if (!context.Expenses.Any())
            {
                var expenses = new List<Expense>
                {
                    new() { Description = "Monthly Medicine Purchase", Category = "Medicine", Amount = 50000, ExpenseDate = DateTime.Today.AddDays(-15), EnteredBy = "Admin", CreatedAt = DateTime.UtcNow },
                    new() { Description = "Electricity Bill", Category = "Electricity", Amount = 12000, ExpenseDate = DateTime.Today.AddDays(-5), EnteredBy = "Admin", CreatedAt = DateTime.UtcNow },
                    new() { Description = "Cleaning Staff Salary", Category = "Salary", Amount = 30000, ExpenseDate = DateTime.Today.AddDays(-1), EnteredBy = "Admin", CreatedAt = DateTime.UtcNow }
                };
                context.Expenses.AddRange(expenses);
                await context.SaveChangesAsync();
            }

            // 12. Seed Demo Staff Users & Healthcare Roles
            var staffAccounts = new[]
            {
                ("doctor@hospital.com", "Dr. Rahul Sharma Demo", "Doctor", "Doctor@123"),
                ("receptionist@hospital.com", "Front Desk Receptionist", "Receptionist", "Recept@123"),
                ("nurse@hospital.com", "Nurse Anjali Verma", "Nurse", "Nurse@123"),
                ("lab@hospital.com", "Pathology Lab Tech", "LabTechnician", "Lab@123"),
                ("pharmacy@hospital.com", "Chief Pharmacist Rajesh", "Pharmacist", "Pharma@123"),
                ("billing@hospital.com", "Billing Specialist Priya", "BillingStaff", "Billing@123"),
                ("patient@hospital.com", "Arjun Kumar (Patient)", "Patient", "Patient@123"),
            };

            foreach (var (email, name, role, password) in staffAccounts)
            {
                var existing = await userManager.FindByEmailAsync(email);
                if (existing == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = name,
                        EmailConfirmed = true,
                        IsActive = true
                    };
                    var res = await userManager.CreateAsync(user, password);
                    if (res.Succeeded)
                        await userManager.AddToRoleAsync(user, role);
                }
            }

            // Link Demo Doctor Account to Doctor Record
            var doctorUser = await userManager.FindByEmailAsync("doctor@hospital.com");
            if (doctorUser != null)
            {
                var doctorRecord = context.Doctors.FirstOrDefault(d => d.Email == "dr.rahul@hospital.com" || d.FullName == "Dr. Rahul Sharma");
                if (doctorRecord != null && string.IsNullOrEmpty(doctorRecord.UserId))
                {
                    doctorRecord.UserId = doctorUser.Id;
                    await context.SaveChangesAsync();
                }
            }

            // Link Demo Patient Account to Patient Record (PAT-001)
            var patientUser = await userManager.FindByEmailAsync("patient@hospital.com");
            if (patientUser != null)
            {
                var patientRecord = context.Patients.FirstOrDefault(p => p.PatientCode == "PAT-001");
                if (patientRecord != null)
                {
                    patientRecord.UserId = patientUser.Id;
                    if (string.IsNullOrEmpty(patientRecord.QrToken))
                        patientRecord.QrToken = Guid.NewGuid().ToString("N")[..16];
                    await context.SaveChangesAsync();
                }
            }

            // 13. Seed Sample Prescriptions
            if (!context.Prescriptions.Any())
            {
                var p1 = context.Patients.FirstOrDefault(p => p.PatientCode == "PAT-001");
                var d1 = context.Doctors.FirstOrDefault();
                if (p1 != null && d1 != null)
                {
                    var rx = new Prescription
                    {
                        PrescriptionCode = "RX-0001",
                        PatientId = p1.PatientId,
                        DoctorId = d1.DoctorId,
                        Diagnosis = "Viral Upper Respiratory Tract Infection",
                        Instructions = "Drink plenty of warm fluids, rest for 3 days, avoid cold drinks.",
                        FollowUpDate = DateTime.Today.AddDays(7),
                        CreatedAt = DateTime.UtcNow,
                        Items = new List<PrescriptionItem>
                        {
                            new() { MedicineName = "Paracetamol 500mg", Dosage = "1 tablet", Frequency = "Three times daily", Duration = "3 days", Instructions = "After meals" },
                            new() { MedicineName = "Cetirizine 10mg", Dosage = "1 tablet", Frequency = "Once daily (Night)", Duration = "5 days", Instructions = "Before bed" }
                        }
                    };
                    context.Prescriptions.Add(rx);
                    await context.SaveChangesAsync();
                }
            }

            // 14. Seed Sample Bills
            if (!context.Bills.Any())
            {
                var p1 = context.Patients.FirstOrDefault(p => p.PatientCode == "PAT-001");
                var p2 = context.Patients.FirstOrDefault(p => p.PatientCode == "PAT-002");
                if (p1 != null && p2 != null)
                {
                    var bills = new List<Bill>
                    {
                        new()
                        {
                            BillCode = "BILL-0001",
                            PatientId = p1.PatientId,
                            ConsultationCharge = 500,
                            MedicineCharge = 150,
                            LabCharge = 350,
                            TotalAmount = 1000,
                            AmountPaid = 1000,
                            PaymentStatus = "Paid",
                            PaymentMethod = "UPI",
                            PaidAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        },
                        new()
                        {
                            BillCode = "BILL-0002",
                            PatientId = p2.PatientId,
                            ConsultationCharge = 1000,
                            LabCharge = 800,
                            TotalAmount = 1800,
                            AmountPaid = 0,
                            PaymentStatus = "Pending",
                            PaymentMethod = "Cash",
                            CreatedAt = DateTime.UtcNow
                        }
                    };
                    context.Bills.AddRange(bills);
                    await context.SaveChangesAsync();
                }
            }
        }

        private static async Task EnsureExtendedSchemaAsync(ApplicationDbContext context)
        {
            try
            {
                string[] createTableStatements = new[]
                {
                    @"CREATE TABLE IF NOT EXISTS VitalSigns (
                        VitalSignId INTEGER PRIMARY KEY AUTOINCREMENT,
                        PatientId INTEGER NOT NULL,
                        AdmissionId INTEGER NULL,
                        RecordedByNurseName TEXT NULL,
                        Temperature NUMERIC NOT NULL,
                        BloodPressureSystolic INTEGER NOT NULL,
                        BloodPressureDiastolic INTEGER NOT NULL,
                        PulseRate INTEGER NOT NULL,
                        RespiratoryRate INTEGER NOT NULL,
                        OxygenSaturation NUMERIC NOT NULL,
                        PainScore INTEGER NOT NULL,
                        Notes TEXT NULL,
                        RecordedAt TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS MedicationAdministrations (
                        MedicationAdministrationId INTEGER PRIMARY KEY AUTOINCREMENT,
                        AdmissionId INTEGER NOT NULL,
                        PrescriptionItemId INTEGER NULL,
                        MedicineName TEXT NOT NULL,
                        Dosage TEXT NOT NULL,
                        Route TEXT NULL,
                        ScheduledTime TEXT NOT NULL,
                        AdministeredTime TEXT NULL,
                        AdministeredByNurseName TEXT NULL,
                        Status TEXT NOT NULL,
                        Notes TEXT NULL,
                        CreatedAt TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS NursingNotes (
                        NursingNoteId INTEGER PRIMARY KEY AUTOINCREMENT,
                        AdmissionId INTEGER NOT NULL,
                        PatientId INTEGER NOT NULL,
                        NurseName TEXT NOT NULL,
                        Category TEXT NOT NULL,
                        NoteText TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS ShiftHandovers (
                        ShiftHandoverId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Ward TEXT NOT NULL,
                        ShiftType TEXT NOT NULL,
                        OutgoingNurseName TEXT NOT NULL,
                        IncomingNurseName TEXT NOT NULL,
                        HandoverDate TEXT NOT NULL,
                        TotalPatientsCount INTEGER NOT NULL,
                        CriticalPatientsSummary TEXT NULL,
                        PendingTasks TEXT NULL,
                        GeneralNotes TEXT NULL,
                        CreatedAt TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS DoctorClinicalNotes (
                        DoctorClinicalNoteId INTEGER PRIMARY KEY AUTOINCREMENT,
                        PatientId INTEGER NOT NULL,
                        DoctorId INTEGER NOT NULL,
                        AppointmentId INTEGER NULL,
                        Subjective TEXT NOT NULL,
                        Objective TEXT NOT NULL,
                        Assessment TEXT NOT NULL,
                        Plan TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS PharmacyDispensations (
                        PharmacyDispensationId INTEGER PRIMARY KEY AUTOINCREMENT,
                        DispensationCode TEXT NOT NULL,
                        PrescriptionId INTEGER NOT NULL,
                        PatientId INTEGER NOT NULL,
                        MedicineId INTEGER NOT NULL,
                        MedicineName TEXT NOT NULL,
                        BatchNumber TEXT NOT NULL,
                        QuantityDispensed INTEGER NOT NULL,
                        UnitPrice NUMERIC NOT NULL,
                        TotalPrice NUMERIC NOT NULL,
                        DispensedByStaffName TEXT NULL,
                        DispensedAt TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        InstructionsGiven TEXT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS StockMovements (
                        StockMovementId INTEGER PRIMARY KEY AUTOINCREMENT,
                        MedicineId INTEGER NOT NULL,
                        BatchNumber TEXT NULL,
                        MovementType TEXT NOT NULL,
                        QuantityChange INTEGER NOT NULL,
                        RemainingStock INTEGER NOT NULL,
                        ReferenceNumber TEXT NULL,
                        PerformedBy TEXT NULL,
                        Timestamp TEXT NOT NULL,
                        Notes TEXT NULL
                    );"
                };

                foreach (var sql in createTableStatements)
                {
                    await context.Database.ExecuteSqlRawAsync(sql);
                }

                string[] alterColumns = new[]
                {
                    "ALTER TABLE Patients ADD COLUMN QrToken TEXT NULL;",
                    "ALTER TABLE Patients ADD COLUMN AadhaarLastFour TEXT NULL;",
                    "ALTER TABLE Beds ADD COLUMN LastCleanedAt TEXT NULL;",
                    "ALTER TABLE Beds ADD COLUMN CleanedBy TEXT NULL;",
                    "ALTER TABLE LabTestRequests ADD COLUMN SampleBarcode TEXT NULL;",
                    "ALTER TABLE LabTestRequests ADD COLUMN SampleType TEXT NULL;",
                    "ALTER TABLE LabTestRequests ADD COLUMN SampleCollectedAt TEXT NULL;",
                    "ALTER TABLE LabTestRequests ADD COLUMN SampleCollectedBy TEXT NULL;",
                    "ALTER TABLE LabTestRequests ADD COLUMN IsAbnormal INTEGER NOT NULL DEFAULT 0;",
                    "ALTER TABLE LabTestRequests ADD COLUMN AbnormalFlag TEXT NULL;",
                    "ALTER TABLE LabTestRequests ADD COLUMN ReferenceRange TEXT NULL;",
                    "ALTER TABLE LabTestRequests ADD COLUMN VerifiedByDoctorId INTEGER NULL;",
                    "ALTER TABLE LabTestRequests ADD COLUMN VerifiedByDoctorName TEXT NULL;",
                    "ALTER TABLE LabTestRequests ADD COLUMN VerifiedAt TEXT NULL;"
                };

                foreach (var alterSql in alterColumns)
                {
                    try
                    {
                        await context.Database.ExecuteSqlRawAsync(alterSql);
                    }
                    catch { /* column already exists, safe to ignore */ }
                }

                // Update Appointment partial index in SQLite (ignore cancelled appointments)
                try
                {
                    await context.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS UQ_Appointment_Doctor_DateTime;");
                    await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS UQ_Appointment_Doctor_DateTime ON Appointments (DoctorId, AppointmentDate, AppointmentTime) WHERE Status <> 'Cancelled';");
                }
                catch { /* index update handled by provider */ }
            }
            catch { /* non-fatal schema check */ }
        }
    }
}
