using Microsoft.Extensions.Logging;
using UmiHealth.Core.Interfaces;
using UmiHealth.Core.Entities;
using UmiHealth.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace UmiHealth.Application.Services;

public class PatientService : IPatientService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PatientService> _logger;

    public PatientService(ApplicationDbContext context, ILogger<PatientService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Patient>> GetPatientsAsync(Guid tenantId, PatientFilter? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Patients
                .Where(p => p.TenantId == tenantId);

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var searchTerm = filter.Search.ToLower();
                    query = query.Where(p => 
                        p.FirstName.ToLower().Contains(searchTerm) ||
                        p.LastName.ToLower().Contains(searchTerm) ||
                        p.PatientNumber.ToLower().Contains(searchTerm) ||
                        p.Email.ToLower().Contains(searchTerm) ||
                        p.Phone.Contains(searchTerm));
                }

                if (!string.IsNullOrWhiteSpace(filter.Gender))
                {
                    query = query.Where(p => p.Gender == filter.Gender);
                }

                if (filter.DateOfBirthFrom.HasValue)
                {
                    query = query.Where(p => p.DateOfBirth >= filter.DateOfBirthFrom.Value);
                }

                if (filter.DateOfBirthTo.HasValue)
                {
                    query = query.Where(p => p.DateOfBirth <= filter.DateOfBirthTo.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.BloodType))
                {
                    query = query.Where(p => p.BloodType == filter.BloodType);
                }

                if (!string.IsNullOrWhiteSpace(filter.InsuranceProvider))
                {
                    query = query.Where(p => p.InsuranceProvider == filter.InsuranceProvider);
                }

                if (filter.IsActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == filter.IsActive.Value);
                }
            }

            var patients = await query
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync(cancellationToken);

            return patients.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patients for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Patient?> GetPatientAsync(Guid patientId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId, cancellationToken);

            return patient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient {PatientId} for tenant {TenantId}", patientId, tenantId);
            throw;
        }
    }

    public async Task<Patient> CreatePatientAsync(Guid tenantId, CreatePatientRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for duplicate email or phone
            var existingPatient = await _context.Patients
                .FirstOrDefaultAsync(p => 
                    p.TenantId == tenantId && 
                    (p.Email == request.Email || p.Phone == request.Phone), 
                    cancellationToken);

            if (existingPatient != null)
            {
                if (existingPatient.Email == request.Email)
                {
                    throw new InvalidOperationException("A patient with this email already exists");
                }
                if (existingPatient.Phone == request.Phone)
                {
                    throw new InvalidOperationException("A patient with this phone number already exists");
                }
            }

            var patient = new Patient
            {
                TenantId = tenantId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                NationalId = request.NationalId,
                PassportNumber = request.PassportNumber,
                Email = request.Email,
                Phone = request.Phone,
                AlternativePhone = request.AlternativePhone,
                Address = request.Address,
                City = request.City,
                Country = request.Country,
                PostalCode = request.PostalCode,
                BloodType = request.BloodType,
                Allergies = request.Allergies != null ? string.Join(",", request.Allergies) : null,
                ChronicConditions = request.ChronicConditions != null ? string.Join(",", request.ChronicConditions) : null,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone,
                EmergencyContactRelationship = request.EmergencyContactRelationship,
                InsuranceProvider = request.InsuranceProvider,
                InsuranceNumber = request.InsurancePolicyNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new patient {PatientId} for tenant {TenantId}", patient.Id, tenantId);
            return patient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating patient for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Patient> UpdatePatientAsync(Guid patientId, Guid tenantId, UpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId, cancellationToken);

            if (patient == null)
            {
                throw new InvalidOperationException("Patient not found");
            }

            // Check for duplicate email or phone (excluding current patient)
            var existingPatient = await _context.Patients
                .FirstOrDefaultAsync(p => 
                    p.TenantId == tenantId && 
                    p.Id != patientId &&
                    (p.Email == request.Email || p.Phone == request.Phone), 
                    cancellationToken);

            if (existingPatient != null)
            {
                if (existingPatient.Email == request.Email)
                {
                    throw new InvalidOperationException("A patient with this email already exists");
                }
                if (existingPatient.Phone == request.Phone)
                {
                    throw new InvalidOperationException("A patient with this phone number already exists");
                }
            }

            patient.FirstName = request.FirstName;
            patient.LastName = request.LastName;
            patient.Gender = request.Gender;
            patient.NationalId = request.NationalId;
            patient.PassportNumber = request.PassportNumber;
            patient.Email = request.Email;
            patient.Phone = request.Phone;
            patient.AlternativePhone = request.AlternativePhone;
            patient.Address = request.Address;
            patient.City = request.City;
            patient.Country = request.Country;
            patient.PostalCode = request.PostalCode;
            patient.BloodType = request.BloodType;
            patient.Allergies = request.Allergies != null ? string.Join(",", request.Allergies) : null;
            patient.ChronicConditions = request.ChronicConditions != null ? string.Join(",", request.ChronicConditions) : null;
            patient.EmergencyContactName = request.EmergencyContactName;
            patient.EmergencyContactPhone = request.EmergencyContactPhone;
            patient.EmergencyContactRelationship = request.EmergencyContactRelationship;
            patient.InsuranceProvider = request.InsuranceProvider;
            patient.InsuranceNumber = request.InsurancePolicyNumber;
            patient.IsActive = request.IsActive;
            patient.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated patient {PatientId} for tenant {TenantId}", patientId, tenantId);
            return patient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient {PatientId} for tenant {TenantId}", patientId, tenantId);
            throw;
        }
    }

    public async Task<bool> DeletePatientAsync(Guid patientId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId, cancellationToken);

            if (patient == null)
            {
                return false;
            }

            // Check if patient has any prescriptions or sales
            var hasPrescriptions = await _context.Prescriptions
                .AnyAsync(p => p.PatientId == patientId, cancellationToken);

            var hasSales = await _context.Sales
                .AnyAsync(s => s.PatientId == patientId, cancellationToken);

            if (hasPrescriptions || hasSales)
            {
                // Soft delete instead of hard delete
                patient.IsActive = false;
                patient.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Soft deleted patient {PatientId} for tenant {TenantId} (has prescriptions/sales)", patientId, tenantId);
            }
            else
            {
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Hard deleted patient {PatientId} for tenant {TenantId}", patientId, tenantId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting patient {PatientId} for tenant {TenantId}", patientId, tenantId);
            throw;
        }
    }

    public async Task<PatientHistory> GetPatientHistoryAsync(Guid patientId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId, cancellationToken);

            if (patient == null)
            {
                throw new InvalidOperationException("Patient not found");
            }

            // Get prescriptions
            var prescriptions = await _context.Prescriptions
                .Where(p => p.PatientId == patientId && p.TenantId == tenantId)
                .Select(p => new PrescriptionSummary
                {
                    Id = p.Id,
                    PrescriptionNumber = p.PrescriptionNumber,
                    PrescriptionDate = p.CreatedAt,
                    DoctorName = p.Prescriber != null ? $"{p.Prescriber.FirstName} {p.Prescriber.LastName}" : "Unknown",
                    Status = p.Status,
                    MedicationCount = _context.PrescriptionItems.Count(pi => pi.PrescriptionId == p.Id)
                })
                .ToListAsync(cancellationToken);

            // Get sales
            var sales = await _context.Sales
                .Where(s => s.PatientId == patientId && s.TenantId == tenantId)
                .Select(s => new SaleSummary
                {
                    Id = s.Id,
                    SaleNumber = s.SaleNumber,
                    SaleDate = s.CreatedAt,
                    TotalAmount = s.TotalAmount,
                    Status = s.Status,
                    ItemCount = _context.SaleItems.Count(si => si.SaleId == s.Id)
                })
                .ToListAsync(cancellationToken);

            // Parse allergies and conditions
            var allergies = new List<AllergyInfo>();
            if (!string.IsNullOrWhiteSpace(patient.Allergies))
            {
                var allergyList = patient.Allergies.Split(',', StringSplitOptions.RemoveEmptyEntries);
                allergies = allergyList.Select(allergy => new AllergyInfo
                {
                    Allergy = allergy.Trim(),
                    Severity = "Unknown",
                    RecordedAt = patient.CreatedAt
                }).ToList();
            }

            var conditions = new List<ConditionInfo>();
            if (!string.IsNullOrWhiteSpace(patient.ChronicConditions))
            {
                var conditionList = patient.ChronicConditions.Split(',', StringSplitOptions.RemoveEmptyEntries);
                conditions = conditionList.Select(condition => new ConditionInfo
                {
                    Condition = condition.Trim(),
                    DiagnosisDate = patient.CreatedAt,
                    Status = "Active"
                }).ToList();
            }

            var patientInfo = new PatientInfo
            {
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                Email = patient.Email,
                PhoneNumber = patient.Phone
            };

            return new PatientHistory
            {
                PatientId = patientId,
                Patient = patientInfo,
                Prescriptions = prescriptions,
                Sales = sales,
                Allergies = allergies,
                ChronicConditions = conditions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient history {PatientId} for tenant {TenantId}", patientId, tenantId);
            throw;
        }
    }
}
