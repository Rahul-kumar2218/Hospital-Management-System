using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Models;

namespace HospitalManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Domain DbSets
        public DbSet<Department> Departments { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<OPDRecord> OPDRecords { get; set; }
        public DbSet<EmergencyCase> EmergencyCases { get; set; }
        public DbSet<Bed> Beds { get; set; }
        public DbSet<Admission> Admissions { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
        public DbSet<LabTest> LabTests { get; set; }
        public DbSet<LabTestRequest> LabTestRequests { get; set; }
        public DbSet<RadiologyRequest> RadiologyRequests { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<BloodBank> BloodBanks { get; set; }
        public DbSet<BloodDonor> BloodDonors { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<Insurance> Insurances { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<VitalSign> VitalSigns { get; set; }
        public DbSet<MedicationAdministration> MedicationAdministrations { get; set; }
        public DbSet<NursingNote> NursingNotes { get; set; }
        public DbSet<ShiftHandover> ShiftHandovers { get; set; }
        public DbSet<DoctorClinicalNote> DoctorClinicalNotes { get; set; }
        public DbSet<PharmacyDispensation> PharmacyDispensations { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Rename Identity tables to cleaner names
            builder.Entity<ApplicationUser>().ToTable("Users");

            // Department relationships
            builder.Entity<Department>()
                .HasMany(d => d.Doctors)
                .WithOne(doc => doc.Department)
                .HasForeignKey(doc => doc.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment - prevent cascade conflicts
            builder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Department)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // OPD
            builder.Entity<OPDRecord>()
                .HasOne(o => o.Patient)
                .WithMany(p => p.OPDRecords)
                .HasForeignKey(o => o.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OPDRecord>()
                .HasOne(o => o.Doctor)
                .WithMany(d => d.OPDRecords)
                .HasForeignKey(o => o.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Emergency
            builder.Entity<EmergencyCase>()
                .HasOne(e => e.Patient)
                .WithMany(p => p.EmergencyCases)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<EmergencyCase>()
                .HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Admission
            builder.Entity<Admission>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Admissions)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Admission>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Admissions)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Admission>()
                .HasOne(a => a.Bed)
                .WithMany(b => b.Admissions)
                .HasForeignKey(a => a.BedId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bill - Admission (optional link)
            builder.Entity<Bill>()
                .HasOne<Admission>()
                .WithMany()
                .HasForeignKey(b => b.AdmissionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Prescription
            builder.Entity<Prescription>()
                .HasOne(p => p.Patient)
                .WithMany(pat => pat.Prescriptions)
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Prescription>()
                .HasOne(p => p.Doctor)
                .WithMany(d => d.Prescriptions)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // LabTestRequest
            builder.Entity<LabTestRequest>()
                .HasOne(l => l.Patient)
                .WithMany(p => p.LabTestRequests)
                .HasForeignKey(l => l.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LabTestRequest>()
                .HasOne(l => l.Doctor)
                .WithMany()
                .HasForeignKey(l => l.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Radiology
            builder.Entity<RadiologyRequest>()
                .HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RadiologyRequest>()
                .HasOne(r => r.Doctor)
                .WithMany()
                .HasForeignKey(r => r.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bill
            builder.Entity<Bill>()
                .HasOne(b => b.Patient)
                .WithMany(p => p.Bills)
                .HasForeignKey(b => b.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Insurance
            builder.Entity<Insurance>()
                .HasOne(i => i.Patient)
                .WithMany()
                .HasForeignKey(i => i.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: Doctor cannot have two appointments at same date+time (excluding Cancelled appointments)
            builder.Entity<Appointment>()
                .HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.AppointmentTime })
                .IsUnique()
                .HasFilter("Status <> 'Cancelled'")
                .HasDatabaseName("UQ_Appointment_Doctor_DateTime");

            // Unique constraint on PatientCode
            builder.Entity<Patient>()
                .HasIndex(p => p.PatientCode)
                .IsUnique()
                .HasDatabaseName("UQ_Patient_Code");

            // 2026 Inpatient & Clinical Relationships
            builder.Entity<VitalSign>()
                .HasOne(v => v.Patient)
                .WithMany()
                .HasForeignKey(v => v.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<VitalSign>()
                .HasOne(v => v.Admission)
                .WithMany()
                .HasForeignKey(v => v.AdmissionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MedicationAdministration>()
                .HasOne(m => m.Admission)
                .WithMany()
                .HasForeignKey(m => m.AdmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<NursingNote>()
                .HasOne(n => n.Admission)
                .WithMany()
                .HasForeignKey(n => n.AdmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<NursingNote>()
                .HasOne(n => n.Patient)
                .WithMany()
                .HasForeignKey(n => n.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DoctorClinicalNote>()
                .HasOne(d => d.Patient)
                .WithMany()
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DoctorClinicalNote>()
                .HasOne(d => d.Doctor)
                .WithMany()
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PharmacyDispensation>()
                .HasOne(p => p.Prescription)
                .WithMany()
                .HasForeignKey(p => p.PrescriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PharmacyDispensation>()
                .HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PharmacyDispensation>()
                .HasOne(p => p.Medicine)
                .WithMany()
                .HasForeignKey(p => p.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StockMovement>()
                .HasOne(s => s.Medicine)
                .WithMany()
                .HasForeignKey(s => s.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed blood bank initial inventory (using static date for EF Core model determinism)
            var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            builder.Entity<BloodBank>().HasData(
                new BloodBank { BloodBankId = 1, BloodGroup = "A+", UnitsAvailable = 15, LastUpdated = seedDate },
                new BloodBank { BloodBankId = 2, BloodGroup = "A-", UnitsAvailable = 8, LastUpdated = seedDate },
                new BloodBank { BloodBankId = 3, BloodGroup = "B+", UnitsAvailable = 20, LastUpdated = seedDate },
                new BloodBank { BloodBankId = 4, BloodGroup = "B-", UnitsAvailable = 5, LastUpdated = seedDate },
                new BloodBank { BloodBankId = 5, BloodGroup = "O+", UnitsAvailable = 25, LastUpdated = seedDate },
                new BloodBank { BloodBankId = 6, BloodGroup = "O-", UnitsAvailable = 10, LastUpdated = seedDate },
                new BloodBank { BloodBankId = 7, BloodGroup = "AB+", UnitsAvailable = 7, LastUpdated = seedDate },
                new BloodBank { BloodBankId = 8, BloodGroup = "AB-", UnitsAvailable = 3, LastUpdated = seedDate }
            );
        }
    }
}
