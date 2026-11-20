using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Department Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; } = "fa-hospital";

        [Display(Name = "Head of Department")]
        public int? HeadDoctorId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();
    }

    public class Doctor
    {
        [Key]
        public int DoctorId { get; set; }

        public string? UserId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Specialization { get; set; } = string.Empty;

        [Required]
        [ForeignKey("Department")]
        public int DepartmentId { get; set; }

        [MaxLength(100)]
        public string? Qualification { get; set; }

        [Display(Name = "Experience (Years)")]
        public int ExperienceYears { get; set; }

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, MaxLength(15)]
        public string Phone { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Consultation Fee")]
        public decimal ConsultationFee { get; set; }

        [MaxLength(200)]
        [Display(Name = "Available Days")]
        public string? AvailableDays { get; set; }

        [MaxLength(50)]
        [Display(Name = "Available Time")]
        public string? AvailableTime { get; set; }

        [MaxLength(500)]
        public string? Biography { get; set; }

        [MaxLength(200)]
        [Display(Name = "Profile Picture")]
        public string? ProfilePicture { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Department? Department { get; set; }
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public virtual ICollection<Admission> Admissions { get; set; } = new List<Admission>();
        public virtual ICollection<OPDRecord> OPDRecords { get; set; } = new List<OPDRecord>();
    }

    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        public string? UserId { get; set; }

        [Display(Name = "Patient ID")]
        [MaxLength(20)]
        public string PatientCode { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [NotMapped]
        public int Age => DateTime.Today.Year - DateOfBirth.Year - (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

        [Required, Phone, MaxLength(15)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress, MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(5)]
        [Display(Name = "Blood Group")]
        public string? BloodGroup { get; set; }

        [MaxLength(200)]
        [Display(Name = "Emergency Contact")]
        public string? EmergencyContact { get; set; }

        [MaxLength(500)]
        public string? Allergies { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Medical History")]
        public string? MedicalHistory { get; set; }

        [MaxLength(50)]
        [Display(Name = "Patient QR Token")]
        public string? QrToken { get; set; }

        [MaxLength(10)]
        [Display(Name = "Aadhaar Last 4")]
        public string? AadhaarLastFour { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<Admission> Admissions { get; set; } = new List<Admission>();
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
        public virtual ICollection<LabTestRequest> LabTestRequests { get; set; } = new List<LabTestRequest>();
        public virtual ICollection<OPDRecord> OPDRecords { get; set; } = new List<OPDRecord>();
        public virtual ICollection<EmergencyCase> EmergencyCases { get; set; } = new List<EmergencyCase>();
    }

    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Display(Name = "Appointment No.")]
        [MaxLength(20)]
        public string AppointmentCode { get; set; } = string.Empty;

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        [Required, ForeignKey("Doctor")]
        public int DoctorId { get; set; }

        [Required, ForeignKey("Department")]
        public int DepartmentId { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [Display(Name = "Time Slot")]
        public TimeSpan AppointmentTime { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Patient? Patient { get; set; }
        public virtual Doctor? Doctor { get; set; }
        public virtual Department? Department { get; set; }
    }

    public class OPDRecord
    {
        [Key]
        public int OPDRecordId { get; set; }

        [Display(Name = "Token Number")]
        public int TokenNumber { get; set; }

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        [Required, ForeignKey("Doctor")]
        public int DoctorId { get; set; }

        public int? AppointmentId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Visit Date")]
        public DateTime VisitDate { get; set; } = DateTime.Today;

        [MaxLength(20)]
        public string Status { get; set; } = "Waiting"; // Waiting, InConsultation, Completed

        [MaxLength(500)]
        [Display(Name = "Chief Complaint")]
        public string? ChiefComplaint { get; set; }

        [MaxLength(500)]
        [Display(Name = "Vital Signs")]
        public string? VitalSigns { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Patient? Patient { get; set; }
        public virtual Doctor? Doctor { get; set; }
    }

    public class EmergencyCase
    {
        [Key]
        public int EmergencyCaseId { get; set; }

        [MaxLength(20)]
        public string CaseCode { get; set; } = string.Empty;

        public int? PatientId { get; set; }

        [MaxLength(100)]
        [Display(Name = "Patient Name (Walk-in)")]
        public string? WalkInPatientName { get; set; }

        [MaxLength(15)]
        [Display(Name = "Contact")]
        public string? WalkInContact { get; set; }

        [Required, MaxLength(200)]
        [Display(Name = "Emergency Type")]
        public string EmergencyType { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Priority { get; set; } = "Stable"; // Critical, Serious, Stable

        public int? DoctorId { get; set; }

        public int? BedId { get; set; }

        [Display(Name = "Arrival Time")]
        public DateTime ArrivalTime { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        [Display(Name = "Treatment Status")]
        public string TreatmentStatus { get; set; } = "Active"; // Active, Admitted, Discharged, Transferred

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Treatment Notes")]
        public string? TreatmentNotes { get; set; }

        // Navigation
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }
        [ForeignKey("BedId")]
        public virtual Bed? Bed { get; set; }
    }

    public class Bed
    {
        [Key]
        public int BedId { get; set; }

        [Required, MaxLength(20)]
        [Display(Name = "Bed Number")]
        public string BedNumber { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        [Display(Name = "Ward / Category")]
        public string Ward { get; set; } = string.Empty; // General, ICU, Private, Emergency

        [MaxLength(50)]
        public string? Room { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Available"; // Available, Occupied, Reserved, Maintenance

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Daily Charge")]
        public decimal DailyCharge { get; set; }

        public DateTime? LastCleanedAt { get; set; }

        [MaxLength(100)]
        [Display(Name = "Cleaned By")]
        public string? CleanedBy { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Admission> Admissions { get; set; } = new List<Admission>();
    }

    public class Admission
    {
        [Key]
        public int AdmissionId { get; set; }

        [MaxLength(20)]
        public string AdmissionCode { get; set; } = string.Empty;

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        [Required, ForeignKey("Doctor")]
        public int DoctorId { get; set; }

        [Required, ForeignKey("Bed")]
        public int BedId { get; set; }

        [Required, MaxLength(50)]
        public string Ward { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Room { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Admission Date")]
        public DateTime AdmissionDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Expected Discharge Date")]
        public DateTime? ExpectedDischargeDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Actual Discharge Date")]
        public DateTime? ActualDischargeDate { get; set; }

        [MaxLength(500)]
        public string? Diagnosis { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Treatment Summary")]
        public string? TreatmentSummary { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Doctor Notes")]
        public string? DoctorNotes { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Admitted"; // Admitted, Discharged

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Patient? Patient { get; set; }
        public virtual Doctor? Doctor { get; set; }
        public virtual Bed? Bed { get; set; }
        // Optional Bill link handled via Bill.AdmissionId
    }

    public class Prescription
    {
        [Key]
        public int PrescriptionId { get; set; }

        [MaxLength(20)]
        public string PrescriptionCode { get; set; } = string.Empty;

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        [Required, ForeignKey("Doctor")]
        public int DoctorId { get; set; }

        public int? AppointmentId { get; set; }

        [Required, MaxLength(500)]
        public string Diagnosis { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Follow-up Date")]
        public DateTime? FollowUpDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Patient? Patient { get; set; }
        public virtual Doctor? Doctor { get; set; }
        public virtual ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
    }

    public class PrescriptionItem
    {
        [Key]
        public int PrescriptionItemId { get; set; }

        [Required, ForeignKey("Prescription")]
        public int PrescriptionId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Medicine Name")]
        public string MedicineName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Dosage { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Frequency { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Duration { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Instructions { get; set; }

        // Navigation
        public virtual Prescription? Prescription { get; set; }
    }

    public class LabTest
    {
        [Key]
        public int LabTestId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        [Display(Name = "Normal Range")]
        public string? NormalRange { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class LabTestRequest
    {
        [Key]
        public int LabTestRequestId { get; set; }

        [MaxLength(20)]
        public string RequestCode { get; set; } = string.Empty;

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        [ForeignKey("Doctor")]
        public int? DoctorId { get; set; }

        [Required, ForeignKey("LabTest")]
        public int LabTestId { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Requested"; // Requested, SampleCollected, Processing, Completed, Verified, ReportReleased

        [MaxLength(50)]
        [Display(Name = "Sample Barcode")]
        public string? SampleBarcode { get; set; }

        [MaxLength(50)]
        [Display(Name = "Sample Type")]
        public string? SampleType { get; set; } = "Blood";

        public DateTime? SampleCollectedAt { get; set; }

        [MaxLength(100)]
        [Display(Name = "Collected By")]
        public string? SampleCollectedBy { get; set; }

        public bool IsAbnormal { get; set; } = false;

        [MaxLength(50)]
        [Display(Name = "Abnormal Flag")]
        public string? AbnormalFlag { get; set; } = "Normal"; // Normal, High, Low, Critical

        [MaxLength(100)]
        [Display(Name = "Reference Range")]
        public string? ReferenceRange { get; set; }

        public int? VerifiedByDoctorId { get; set; }

        [MaxLength(100)]
        [Display(Name = "Verified By")]
        public string? VerifiedByDoctorName { get; set; }

        public DateTime? VerifiedAt { get; set; }

        [MaxLength(1000)]
        public string? Result { get; set; }

        [MaxLength(200)]
        [Display(Name = "Result File")]
        public string? ResultFile { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public virtual Patient? Patient { get; set; }
        public virtual Doctor? Doctor { get; set; }
        public virtual LabTest? LabTest { get; set; }
    }

    public class RadiologyRequest
    {
        [Key]
        public int RadiologyRequestId { get; set; }

        [MaxLength(20)]
        public string RequestCode { get; set; } = string.Empty;

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        [ForeignKey("Doctor")]
        public int? DoctorId { get; set; }

        [Required, MaxLength(50)]
        [Display(Name = "Scan Type")]
        public string ScanType { get; set; } = string.Empty; // X-Ray, CT, MRI, Ultrasound

        [MaxLength(200)]
        [Display(Name = "Body Part")]
        public string? BodyPart { get; set; }

        [MaxLength(500)]
        public string? ClinicalNotes { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Requested"; // Requested, InProgress, Completed, Reviewed

        [MaxLength(2000)]
        public string? Report { get; set; }

        [MaxLength(200)]
        [Display(Name = "Report File")]
        public string? ReportFile { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Charge { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public virtual Patient? Patient { get; set; }
        public virtual Doctor? Doctor { get; set; }
    }

    public class Medicine
    {
        [Key]
        public int MedicineId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Category { get; set; }

        [MaxLength(50)]
        [Display(Name = "Batch Number")]
        public string? BatchNumber { get; set; }

        [MaxLength(100)]
        public string? Supplier { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Purchase Price")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Selling Price")]
        public decimal SellingPrice { get; set; }

        [Display(Name = "Quantity in Stock")]
        public int Quantity { get; set; }

        [Display(Name = "Minimum Stock Level")]
        public int MinStockLevel { get; set; } = 10;

        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; } = "Tablet";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public bool IsLowStock => Quantity <= MinStockLevel;

        [NotMapped]
        public bool IsExpiringSoon => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.Today.AddDays(30);
    }

    public class BloodBank
    {
        [Key]
        public int BloodBankId { get; set; }

        [Required, MaxLength(5)]
        [Display(Name = "Blood Group")]
        public string BloodGroup { get; set; } = string.Empty;

        [Display(Name = "Units Available")]
        public int UnitsAvailable { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class BloodDonor
    {
        [Key]
        public int BloodDonorId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Donor Name")]
        public string DonorName { get; set; } = string.Empty;

        [Required, MaxLength(5)]
        [Display(Name = "Blood Group")]
        public string BloodGroup { get; set; } = string.Empty;

        [Required, Phone, MaxLength(15)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress, MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last Donation Date")]
        public DateTime? LastDonationDate { get; set; }

        [Display(Name = "Units Donated")]
        public int UnitsDonated { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }

    public class Bill
    {
        [Key]
        public int BillId { get; set; }

        [MaxLength(20)]
        public string BillCode { get; set; } = string.Empty;

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        public int? AdmissionId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Consultation Charge")]
        public decimal ConsultationCharge { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Room Charge")]
        public decimal RoomCharge { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Medicine Charge")]
        public decimal MedicineCharge { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Lab Charge")]
        public decimal LabCharge { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Radiology Charge")]
        public decimal RadiologyCharge { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Other Charges")]
        public decimal OtherCharges { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [MaxLength(20)]
        [Display(Name = "Payment Status")]
        public string PaymentStatus { get; set; } = "Pending"; // Paid, Pending, Partial

        [MaxLength(50)]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; } // Cash, UPI, Card, Insurance

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        // Navigation
        public virtual Patient? Patient { get; set; }
        public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
    }

    public class BillItem
    {
        [Key]
        public int BillItemId { get; set; }

        [Required, ForeignKey("Bill")]
        public int BillId { get; set; }

        [Required, MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        // Navigation
        public virtual Bill? Bill { get; set; }
    }

    public class Insurance
    {
        [Key]
        public int InsuranceId { get; set; }

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        [Required, MaxLength(100)]
        public string Provider { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Policy Number")]
        public string PolicyNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Coverage Amount")]
        public decimal CoverageAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Claim Amount")]
        public decimal ClaimAmount { get; set; }

        [MaxLength(20)]
        [Display(Name = "Claim Status")]
        public string ClaimStatus { get; set; } = "Pending"; // Pending, Submitted, Approved, Rejected

        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Patient? Patient { get; set; }
    }

    public class Staff
    {
        [Key]
        public int StaffId { get; set; }

        public string? UserId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Role { get; set; } = string.Empty; // Nurse, Receptionist, LabTech, Pharmacist, Support

        [Required, Phone, MaxLength(15)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress, MaxLength(150)]
        public string? Email { get; set; }

        [ForeignKey("Department")]
        public int? DepartmentId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Joining Date")]
        public DateTime JoiningDate { get; set; } = DateTime.Today;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Salary { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual Department? Department { get; set; }
    }

    public class InventoryItem
    {
        [Key]
        public int InventoryItemId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Category { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        public int Quantity { get; set; }

        [Display(Name = "Minimum Stock Level")]
        public int MinStockLevel { get; set; } = 10;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [MaxLength(100)]
        public string? Supplier { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public bool IsLowStock => Quantity <= MinStockLevel;
    }

    public class Expense
    {
        [Key]
        public int ExpenseId { get; set; }

        [Required, MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty; // Medicine, Equipment, Salary, Electricity, Maintenance, Other

        [Required, Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Expense Date")]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        [Display(Name = "Entered By")]
        public string? EnteredBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Type { get; set; } = "Info"; // Info, Warning, Success, Danger

        [MaxLength(50)]
        public string? Icon { get; set; }

        public string? UserId { get; set; }

        public bool IsRead { get; set; } = false;

        public bool IsGlobal { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [Required, MaxLength(200)]
        public string Action { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Module { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Details { get; set; }

        [MaxLength(150)]
        public string? UserId { get; set; }

        [MaxLength(150)]
        public string? UserName { get; set; }

        [MaxLength(50)]
        public string? IPAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // ===== 2026 CLINICAL INPATIENT & NURSING WORKFLOW ENTITIES =====

    public class VitalSign
    {
        [Key]
        public int VitalSignId { get; set; }

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        public int? AdmissionId { get; set; }

        [MaxLength(100)]
        [Display(Name = "Recorded By")]
        public string? RecordedByNurseName { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Temperature (°F)")]
        public decimal Temperature { get; set; } = 98.6m;

        [Display(Name = "Systolic BP (mmHg)")]
        public int BloodPressureSystolic { get; set; } = 120;

        [Display(Name = "Diastolic BP (mmHg)")]
        public int BloodPressureDiastolic { get; set; } = 80;

        [Display(Name = "Pulse Rate (bpm)")]
        public int PulseRate { get; set; } = 72;

        [Display(Name = "Respiratory Rate (/min)")]
        public int RespiratoryRate { get; set; } = 18;

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Oxygen Saturation (SpO2 %)")]
        public decimal OxygenSaturation { get; set; } = 98m;

        [Display(Name = "Pain Score (0-10)")]
        public int PainScore { get; set; } = 0;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Patient? Patient { get; set; }
        [ForeignKey("AdmissionId")]
        public virtual Admission? Admission { get; set; }
    }

    public class MedicationAdministration
    {
        [Key]
        public int MedicationAdministrationId { get; set; }

        [Required, ForeignKey("Admission")]
        public int AdmissionId { get; set; }

        public int? PrescriptionItemId { get; set; }

        [Required, MaxLength(150)]
        [Display(Name = "Medicine Name")]
        public string MedicineName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Dosage { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Route { get; set; } = "Oral"; // Oral, IV, IM, SC, Topical

        public DateTime ScheduledTime { get; set; }

        public DateTime? AdministeredTime { get; set; }

        [MaxLength(100)]
        [Display(Name = "Administered By (Nurse)")]
        public string? AdministeredByNurseName { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Scheduled"; // Scheduled, Administered, Missed, Delayed, Refused

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Admission? Admission { get; set; }
        [ForeignKey("PrescriptionItemId")]
        public virtual PrescriptionItem? PrescriptionItem { get; set; }
    }

    public class NursingNote
    {
        [Key]
        public int NursingNoteId { get; set; }

        [Required, ForeignKey("Admission")]
        public int AdmissionId { get; set; }

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        [Required, MaxLength(100)]
        public string NurseName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Category { get; set; } = "Routine"; // Routine, Critical, Physician Order, Discharge Planning

        [Required, MaxLength(2000)]
        [Display(Name = "Observation & Care Provided")]
        public string NoteText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Admission? Admission { get; set; }
        public virtual Patient? Patient { get; set; }
    }

    public class ShiftHandover
    {
        [Key]
        public int ShiftHandoverId { get; set; }

        [Required, MaxLength(50)]
        public string Ward { get; set; } = "General"; // General, ICU, Private, Emergency

        [Required, MaxLength(30)]
        public string ShiftType { get; set; } = "Morning"; // Morning, Afternoon, Night

        [Required, MaxLength(100)]
        [Display(Name = "Outgoing Nurse")]
        public string OutgoingNurseName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Incoming Nurse")]
        public string IncomingNurseName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime HandoverDate { get; set; } = DateTime.Today;

        public int TotalPatientsCount { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Critical Patients & Alerts (SBAR)")]
        public string? CriticalPatientsSummary { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Pending Medications & Procedures")]
        public string? PendingTasks { get; set; }

        [MaxLength(1000)]
        public string? GeneralNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class DoctorClinicalNote
    {
        [Key]
        public int DoctorClinicalNoteId { get; set; }

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        [Required, ForeignKey("Doctor")]
        public int DoctorId { get; set; }

        public int? AppointmentId { get; set; }

        [Required, MaxLength(2000)]
        [Display(Name = "Subjective (Symptoms & History)")]
        public string Subjective { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        [Display(Name = "Objective (Exam & Vitals)")]
        public string Objective { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        [Display(Name = "Assessment (Diagnosis & Impression)")]
        public string Assessment { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        [Display(Name = "Plan (Treatment & Investigations)")]
        public string Plan { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual Patient? Patient { get; set; }
        public virtual Doctor? Doctor { get; set; }
    }

    public class PharmacyDispensation
    {
        [Key]
        public int PharmacyDispensationId { get; set; }

        [MaxLength(20)]
        public string DispensationCode { get; set; } = string.Empty;

        [Required, ForeignKey("Prescription")]
        public int PrescriptionId { get; set; }

        [Required, ForeignKey("Patient")]
        public int PatientId { get; set; }

        [Required, ForeignKey("Medicine")]
        public int MedicineId { get; set; }

        [Required, MaxLength(100)]
        public string MedicineName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string BatchNumber { get; set; } = string.Empty;

        public int QuantityDispensed { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        [MaxLength(100)]
        public string? DispensedByStaffName { get; set; }

        public DateTime DispensedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Status { get; set; } = "Dispensed"; // Dispensed, Returned, Cancelled

        [MaxLength(500)]
        public string? InstructionsGiven { get; set; }

        // Navigation
        public virtual Prescription? Prescription { get; set; }
        public virtual Patient? Patient { get; set; }
        public virtual Medicine? Medicine { get; set; }
    }

    public class StockMovement
    {
        [Key]
        public int StockMovementId { get; set; }

        [Required, ForeignKey("Medicine")]
        public int MedicineId { get; set; }

        [MaxLength(50)]
        public string? BatchNumber { get; set; }

        [Required, MaxLength(30)]
        public string MovementType { get; set; } = "Purchase"; // Purchase, Dispensation, Adjustment, Return, ExpiredWriteOff

        public int QuantityChange { get; set; } // +ve or -ve

        public int RemainingStock { get; set; }

        [MaxLength(50)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(100)]
        public string? PerformedBy { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation
        public virtual Medicine? Medicine { get; set; }
    }
}
