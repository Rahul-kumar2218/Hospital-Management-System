// =============================================
// City General Hospital - Smart Client Platform (2026)
// =============================================

// ---- DARK MODE SUPPORT ----
function initDarkMode() {
    const isDark = localStorage.getItem('hospital_theme') === 'dark';
    if (isDark) {
        document.body.classList.add('dark-mode');
        updateDarkIcon(true);
    }
}

function toggleDarkMode() {
    const isDark = document.body.classList.toggle('dark-mode');
    localStorage.setItem('hospital_theme', isDark ? 'dark' : 'light');
    updateDarkIcon(isDark);
}

function updateDarkIcon(isDark) {
    const icon = document.getElementById('darkToggleIcon');
    if (icon) {
        icon.className = isDark ? 'fas fa-sun text-warning' : 'fas fa-moon text-secondary';
    }
}

// ---- MULTILINGUAL SUPPORT (English, Hindi, Gujarati) ----
const hospitalDictionary = {
    hi: {
        "Dashboard": "डैशबोर्ड",
        "Patients": "मरीज़ (रोगी)",
        "Appointments": "नियुक्तियां (अपॉइंटमेंट)",
        "OPD": "ओपीडी (बाह्य रोगी)",
        "Emergency": "आपातकालीन सेवा",
        "Admissions": "भर्ती मरीज़",
        "Beds": "अस्पताल के बिस्तर",
        "Prescriptions": "पर्चे (नुस्खे)",
        "Laboratory": "प्रयोगशाला (लैब)",
        "Pharmacy": "दवाखाना (फार्मेसी)",
        "Billing": "बिलिंग एवं शुल्क",
        "Book Appointment": "अपॉइंटमेंट बुक करें",
        "Waiting Room Live Queue": "लाइव प्रतीक्षा कतार",
        "Welcome back!": "स्वागत है!"
    },
    gu: {
        "Dashboard": "ડેશબોર્ડ",
        "Patients": "દર્દીઓ",
        "Appointments": "મુલાકાતો (એપોઇન્ટમેન્ટ)",
        "OPD": "ઓપીડી",
        "Emergency": "ઇમરજન્સી સેવા",
        "Admissions": "દાખલ દર્દીઓ",
        "Beds": "હોસ્પિટલ પથારી",
        "Prescriptions": "દવાઓની યાદી",
        "Laboratory": "લેબોરેટરી",
        "Pharmacy": "દવા વિતરણ (ફાર્મસી)",
        "Billing": "બિલિંગ અને હિસાબ",
        "Book Appointment": "નવી એપોઇન્ટમેન્ટ બુક કરો",
        "Waiting Room Live Queue": "લાઈવ કતાર સ્ટેટસ",
        "Welcome back!": "આપનું સ્વાગત છે!"
    }
};

function changeLanguage(lang) {
    localStorage.setItem('hospital_lang', lang);
    applyLanguage(lang);
}

function applyLanguage(lang) {
    if (!lang || lang === 'en' || !hospitalDictionary[lang]) return;
    const dict = hospitalDictionary[lang];

    document.querySelectorAll('.nav-link, .stat-label, .page-title, h1, h2, h3, h4, h5, button, th').forEach(el => {
        const text = el.textContent?.trim();
        if (text && dict[text]) {
            el.setAttribute('data-original-text', text);
            el.innerHTML = el.innerHTML.replace(text, dict[text]);
        }
    });
}

// ---- DUPLICATE PATIENT DETECTION ----
function initDuplicatePatientCheck() {
    const phoneInput = document.getElementById('Patient_Phone') || document.querySelector('input[name="Phone"]');
    const alertBox = document.getElementById('duplicatePatientWarning');

    if (phoneInput) {
        let timer = null;
        phoneInput.addEventListener('input', function () {
            clearTimeout(timer);
            const val = this.value.trim();
            if (val.length >= 8) {
                timer = setTimeout(() => {
                    fetch('/Patients/CheckDuplicate?phone=' + encodeURIComponent(val))
                        .then(r => r.json())
                        .then(data => {
                            if (data.isDuplicate) {
                                if (alertBox) {
                                    alertBox.innerHTML = `
                                        <div class="alert alert-warning border border-warning shadow-sm d-flex align-items-center gap-3 py-2 px-3 mb-3">
                                            <i class="fas fa-exclamation-triangle fs-4 text-warning"></i>
                                            <div>
                                                <strong>⚠️ Duplicate Patient Warning:</strong> A patient is already registered with this phone:
                                                <strong>${data.fullName}</strong> (${data.patientCode}) on ${data.registrationDate}.
                                                <a href="/Patients/Details/${data.patientId}" target="_blank" class="fw-bold ms-2 text-primary">View Record &rarr;</a>
                                            </div>
                                        </div>`;
                                    alertBox.style.display = 'block';
                                }
                            } else if (alertBox) {
                                alertBox.style.display = 'none';
                            }
                        })
                        .catch(() => { });
                }, 350);
            } else if (alertBox) {
                alertBox.style.display = 'none';
            }
        });
    }
}

// ---- SAFE PRESCRIPTION ALLERGY WARNING ----
function checkPrescriptionAllergy(patientId, medicineName, rowElement) {
    if (!medicineName || !patientId) return;
    fetch(`/Doctor/CheckAllergy?patientId=${patientId}&medicineName=${encodeURIComponent(medicineName)}`)
        .then(r => r.json())
        .then(data => {
            const warningSpan = rowElement ? rowElement.querySelector('.allergy-alert-msg') : null;
            if (data.hasAllergy) {
                if (warningSpan) {
                    warningSpan.textContent = data.message;
                    warningSpan.style.display = 'block';
                } else {
                    alert(data.message);
                }
            } else if (warningSpan) {
                warningSpan.style.display = 'none';
            }
        })
        .catch(() => { });
}

// ---- INITIALIZE ON DOM READY ----
document.addEventListener('DOMContentLoaded', function () {
    initDarkMode();
    initDuplicatePatientCheck();
    const savedLang = localStorage.getItem('hospital_lang');
    if (savedLang && savedLang !== 'en') {
        applyLanguage(savedLang);
    }
});
