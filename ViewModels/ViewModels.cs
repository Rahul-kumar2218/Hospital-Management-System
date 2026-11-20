using System.ComponentModel.DataAnnotations;
using HospitalManagementSystem.Models;

namespace HospitalManagementSystem.ViewModels
{
    // ===== AUTH ViewModels =====
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required, Display(Name = "Full Name")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone, Display(Name = "Phone Number")]
        [MaxLength(15)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Account Type / Role")]
        public string Role { get; set; } = "Patient";

        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(6)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ===== DASHBOARD ViewModels =====
    public class DashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int TodayAppointments { get; set; }
        public int AvailableBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int TotalBeds { get; set; }
        public int PendingBills { get; set; }
        public decimal TodayRevenue { get; set; }
        public int EmergencyCases { get; set; }
        public int LowStockMedicines { get; set; }
        public decimal TodayExpenses { get; set; }

        public List<AppointmentSummary> TodayAppointmentsList { get; set; } = new();
        public List<PatientSummary> RecentPatients { get; set; } = new();
        public List<ActivityItem> RecentActivities { get; set; } = new();
        public List<MonthlyData> MonthlyRevenue { get; set; } = new();
        public List<DepartmentStat> DepartmentStats { get; set; } = new();
    }

    public class AppointmentSummary
    {
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public TimeSpan Time { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PatientSummary
    {
        public int PatientId { get; set; }
        public string PatientCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? BloodGroup { get; set; }
        public DateTime RegistrationDate { get; set; }
    }

    public class ActivityItem
    {
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MonthlyData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yy");
    }

    public class DepartmentStat
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int PatientCount { get; set; }
    }

    // ===== PATIENT ViewModels =====
    public class PatientListViewModel
    {
        public List<Patient> Patients { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? FilterGender { get; set; }
        public string? FilterBloodGroup { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
    }

    public class PatientProfileViewModel
    {
        public Patient Patient { get; set; } = null!;
        public List<Appointment> Appointments { get; set; } = new();
        public List<Prescription> Prescriptions { get; set; } = new();
        public List<LabTestRequest> LabResults { get; set; } = new();
        public List<Admission> Admissions { get; set; } = new();
        public List<Bill> Bills { get; set; } = new();
        public List<RadiologyRequest> RadiologyReports { get; set; } = new();
        public List<VitalSign> VitalSigns { get; set; } = new();
        public List<DoctorClinicalNote> SoapNotes { get; set; } = new();
    }

    // ===== DOCTOR ViewModels =====
    public class DoctorListViewModel
    {
        public List<Doctor> Doctors { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int? FilterDepartmentId { get; set; }
        public List<Department> Departments { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
    }

    // ===== APPOINTMENT ViewModels =====
    public class AppointmentCreateViewModel
    {
        [Required]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [Required]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [Display(Name = "Time Slot")]
        public string AppointmentTime { get; set; } = "09:00";

        [MaxLength(500)]
        public string? Reason { get; set; }

        // Dropdown data
        public List<Patient>? Patients { get; set; }
        public List<Department>? Departments { get; set; }
        public List<Doctor>? Doctors { get; set; }
    }

    // ===== PRESCRIPTION ViewModels =====
    public class PrescriptionCreateViewModel
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        public int? AppointmentId { get; set; }

        [Required, MaxLength(500)]
        public string Diagnosis { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Follow-up Date")]
        public DateTime? FollowUpDate { get; set; }

        public List<PrescriptionItemInput> Items { get; set; } = new() { new PrescriptionItemInput() };

        // Dropdown data
        public List<Patient>? Patients { get; set; }
        public List<Doctor>? Doctors { get; set; }
    }

    public class PrescriptionItemInput
    {
        [Required, MaxLength(100)]
        [Display(Name = "Medicine Name")]
        public string MedicineName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Dosage { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Frequency { get; set; } = "Twice daily";

        [Required, MaxLength(50)]
        public string Duration { get; set; } = "7 days";

        [MaxLength(500)]
        public string? Instructions { get; set; }
    }

    // ===== BILLING ViewModels =====
    public class BillCreateViewModel
    {
        [Required]
        public int PatientId { get; set; }

        public int? AdmissionId { get; set; }

        [Display(Name = "Consultation Charge")]
        public decimal ConsultationCharge { get; set; }

        [Display(Name = "Room Charge")]
        public decimal RoomCharge { get; set; }

        [Display(Name = "Medicine Charge")]
        public decimal MedicineCharge { get; set; }

        [Display(Name = "Lab Charge")]
        public decimal LabCharge { get; set; }

        [Display(Name = "Radiology Charge")]
        public decimal RadiologyCharge { get; set; }

        [Display(Name = "Other Charges")]
        public decimal OtherCharges { get; set; }

        public decimal Discount { get; set; }

        public decimal Tax { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Dropdown data
        public List<Patient>? Patients { get; set; }
    }

    // ===== ADMISSION ViewModels =====
    public class AdmissionCreateViewModel
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public int BedId { get; set; }

        [Required, MaxLength(50)]
        public string Ward { get; set; } = "General";

        [MaxLength(50)]
        public string? Room { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Admission Date")]
        public DateTime AdmissionDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Expected Discharge Date")]
        public DateTime? ExpectedDischargeDate { get; set; }

        [MaxLength(500)]
        public string? Diagnosis { get; set; }

        // Dropdown data
        public List<Patient>? Patients { get; set; }
        public List<Doctor>? Doctors { get; set; }
        public List<Bed>? AvailableBeds { get; set; }
    }

    // ===== OPD ViewModels =====
    public class OPDCreateViewModel
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [MaxLength(500)]
        [Display(Name = "Chief Complaint")]
        public string? ChiefComplaint { get; set; }

        [MaxLength(500)]
        [Display(Name = "Vital Signs")]
        public string? VitalSigns { get; set; }

        // Dropdown data
        public List<Patient>? Patients { get; set; }
        public List<Doctor>? Doctors { get; set; }
    }

    // ===== REPORT ViewModels =====
    public class ReportFilterViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        public int? DepartmentId { get; set; }
        public int? DoctorId { get; set; }
        public string? Status { get; set; }
        public string ReportType { get; set; } = "Patient";

        public List<Department>? Departments { get; set; }
        public List<Doctor>? Doctors { get; set; }

        // Report results
        public object? ReportData { get; set; }
    }

    // ===== 2026-2030 ROLE-SPECIFIC WORKSPACE VIEWMODELS =====

    public class DoctorWorkspaceViewModel
    {
        public Doctor Doctor { get; set; } = null!;
        public List<Appointment> TodayAppointments { get; set; } = new();
        public List<OPDRecord> TodayOPDQueue { get; set; } = new();
        public List<Admission> AssignedInpatients { get; set; } = new();
        public List<Prescription> RecentPrescriptions { get; set; } = new();
        public List<LabTestRequest> PendingLabReviews { get; set; } = new();
        public int TotalConsultationsToday { get; set; }
        public int WaitingCount { get; set; }
    }

    public class NurseStationViewModel
    {
        public List<Bed> Beds { get; set; } = new();
        public List<Admission> AdmittedPatients { get; set; } = new();
        public List<VitalSign> RecentVitals { get; set; } = new();
        public List<MedicationAdministration> TodayMAR { get; set; } = new();
        public List<ShiftHandover> RecentHandovers { get; set; } = new();
        public List<NursingNote> RecentNotes { get; set; } = new();
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int CleaningBeds { get; set; }
        public int PendingMedicationsCount { get; set; }
    }

    public class ReceptionDeskViewModel
    {
        public List<OPDRecord> TodayOPDQueue { get; set; } = new();
        public List<Appointment> TodayAppointments { get; set; } = new();
        public List<Doctor> AvailableDoctors { get; set; } = new();
        public List<Patient> RecentPatients { get; set; } = new();
        public int WaitingInQueue { get; set; }
        public int TodayTokensIssued { get; set; }
        public int AvailableBedsCount { get; set; }
    }

    public class PharmacyDeskViewModel
    {
        public List<Prescription> PendingPrescriptions { get; set; } = new();
        public List<Medicine> LowStockMedicines { get; set; } = new();
        public List<Medicine> ExpiringMedicines { get; set; } = new();
        public List<PharmacyDispensation> RecentDispensations { get; set; } = new();
        public List<StockMovement> RecentStockMovements { get; set; } = new();
        public int PendingDispensesCount { get; set; }
        public int TotalActiveMedicines { get; set; }
    }

    public class LaboratoryWorkbenchViewModel
    {
        public List<LabTestRequest> PendingSampleCollection { get; set; } = new();
        public List<LabTestRequest> InProcessing { get; set; } = new();
        public List<LabTestRequest> CompletedAwaitingVerification { get; set; } = new();
        public List<LabTestRequest> VerifiedReports { get; set; } = new();
        public List<LabTest> TestCatalog { get; set; } = new();
    }

    public class BillingDeskViewModel
    {
        public List<Bill> PendingBills { get; set; } = new();
        public List<Bill> RecentPaidBills { get; set; } = new();
        public List<Admission> ActiveAdmissionsForBilling { get; set; } = new();
        public decimal TodayRevenue { get; set; }
        public decimal TotalPendingAmount { get; set; }
    }

    public class PatientPortalViewModel
    {
        public Patient Patient { get; set; } = null!;
        public List<Appointment> UpcomingAppointments { get; set; } = new();
        public List<Appointment> PastAppointments { get; set; } = new();
        public List<Prescription> Prescriptions { get; set; } = new();
        public List<LabTestRequest> LabReports { get; set; } = new();
        public List<Admission> HospitalAdmissions { get; set; } = new();
        public List<Bill> Invoices { get; set; } = new();
        public List<VitalSign> VitalSignsHistory { get; set; } = new();
        public string? ActiveOpdToken { get; set; }
        public int QueuePosition { get; set; }
        public int EstimatedWaitMinutes { get; set; }
    }

    public class RecordVitalsInputModel
    {
        [Required]
        public int PatientId { get; set; }
        public int? AdmissionId { get; set; }

        [Required, Range(90, 110)]
        [Display(Name = "Temperature (°F)")]
        public decimal Temperature { get; set; } = 98.6m;

        [Required, Range(50, 260)]
        [Display(Name = "Systolic BP")]
        public int BloodPressureSystolic { get; set; } = 120;

        [Required, Range(30, 160)]
        [Display(Name = "Diastolic BP")]
        public int BloodPressureDiastolic { get; set; } = 80;

        [Required, Range(30, 220)]
        [Display(Name = "Pulse Rate (bpm)")]
        public int PulseRate { get; set; } = 72;

        [Required, Range(6, 60)]
        [Display(Name = "Respiratory Rate")]
        public int RespiratoryRate { get; set; } = 18;

        [Required, Range(50, 100)]
        [Display(Name = "Oxygen Saturation (SpO2 %)")]
        public decimal OxygenSaturation { get; set; } = 98m;

        [Range(0, 10)]
        public int PainScore { get; set; } = 0;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class DispensePrescriptionViewModel
    {
        public Prescription Prescription { get; set; } = null!;
        public List<Medicine> AvailableMedicines { get; set; } = new();
        public Dictionary<int, List<string>> MedicineBatches { get; set; } = new();
    }
}
