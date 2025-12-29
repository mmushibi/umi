using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    public interface IPaymentReconciliationService
    {
        Task<ReconciliationResult> ReconcileBankStatementAsync(BankStatementImportRequest request);
        Task<List<ReconciliationMatch>> GetPendingMatchesAsync(Guid tenantId);
        Task<MatchApprovalResult> ApproveMatchAsync(Guid matchId, bool approved, string notes);
        Task<List<ReconciliationDiscrepancy>> GetDiscrepanciesAsync(Guid tenantId);
        Task<byte[]> GenerateReconciliationReportAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<AutoReconciliationResult> RunAutoReconciliationAsync(Guid tenantId);
        Task<List<BankAccountDto>> GetBankAccountsAsync(Guid tenantId);
        Task<bool> ImportBankStatementFileAsync(Stream fileStream, string format, Guid tenantId);
    }

    public class PaymentReconciliationService : IPaymentReconciliationService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<PaymentReconciliationService> _logger;
        private readonly IPaymentVerificationService _paymentVerificationService;

        public PaymentReconciliationService(
            SharedDbContext context,
            ILogger<PaymentReconciliationService> logger,
            IPaymentVerificationService paymentVerificationService)
        {
            _context = context;
            _logger = logger;
            _paymentVerificationService = paymentVerificationService;
        }

        public async Task<ReconciliationResult> ReconcileBankStatementAsync(BankStatementImportRequest request)
        {
            var result = new ReconciliationResult
            {
                TotalTransactions = request.Transactions.Count,
                MatchedTransactions = 0,
                UnmatchedTransactions = 0,
                Discrepancies = new List<ReconciliationDiscrepancy>(),
                Matches = new List<ReconciliationMatch>()
            };

            foreach (var transaction in request.Transactions)
            {
                try
                {
                    var match = await FindMatchingTransaction(transaction, request.TenantId);
                    
                    if (match != null)
                    {
                        result.MatchedTransactions++;
                        result.Matches.Add(match);
                        
                        // Create reconciliation record
                        var reconciliation = new PaymentReconciliation
                        {
                            Id = Guid.NewGuid(),
                            TenantId = request.TenantId,
                            BankTransactionId = transaction.TransactionId,
                            PaymentTransactionId = match.PaymentTransactionId,
                            MatchType = match.MatchType,
                            MatchScore = match.MatchScore,
                            Status = "pending_approval",
                            ReconciledBy = request.ImportedBy,
                            ReconciledAt = DateTime.UtcNow,
                            BankAmount = transaction.Amount,
                            PaymentAmount = match.PaymentAmount,
                            Difference = Math.Abs(transaction.Amount - match.PaymentAmount)
                        };

                        _context.PaymentReconciliations.Add(reconciliation);
                    }
                    else
                    {
                        result.UnmatchedTransactions++;
                        
                        // Create unmatched transaction record
                        var unmatched = new UnmatchedBankTransaction
                        {
                            Id = Guid.NewGuid(),
                            TenantId = request.TenantId,
                            TransactionId = transaction.TransactionId,
                            TransactionDate = transaction.TransactionDate,
                            Description = transaction.Description,
                            Amount = transaction.Amount,
                            Reference = transaction.Reference,
                            AccountNumber = transaction.AccountNumber,
                            ImportedAt = DateTime.UtcNow,
                            ImportedBy = request.ImportedBy
                        };

                        _context.UnmatchedBankTransactions.Add(unmatched);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error reconciling transaction {transaction.TransactionId}");
                    result.UnmatchedTransactions++;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Reconciliation completed for tenant {request.TenantId}. Matched: {result.MatchedTransactions}, Unmatched: {result.UnmatchedTransactions}");

            return result;
        }

        public async Task<List<ReconciliationMatch>> GetPendingMatchesAsync(Guid tenantId)
        {
            return await _context.PaymentReconciliations
                .Where(pr => pr.TenantId == tenantId && pr.Status == "pending_approval")
                .Include(pr => pr.PaymentTransaction)
                    .ThenInclude(pt => pt.User)
                .Include(pr => pr.PaymentTransaction)
                    .ThenInclude(pt => pt.Charge)
                .Select(pr => new ReconciliationMatch
                {
                    ReconciliationId = pr.Id,
                    BankTransactionId = pr.BankTransactionId,
                    PaymentTransactionId = pr.PaymentTransactionId,
                    TransactionReference = pr.PaymentTransaction.TransactionReference,
                    CustomerName = pr.PaymentTransaction.User.FirstName + " " + pr.PaymentTransaction.User.LastName,
                    CustomerEmail = pr.PaymentTransaction.User.Email,
                    PaymentAmount = pr.PaymentAmount,
                    BankAmount = pr.BankAmount,
                    Difference = pr.Difference,
                    MatchType = pr.MatchType,
                    MatchScore = pr.MatchScore,
                    TransactionDate = pr.PaymentTransaction.TransactionDate,
                    ReconciledAt = pr.ReconciledAt
                })
                .ToListAsync();
        }

        public async Task<MatchApprovalResult> ApproveMatchAsync(Guid matchId, bool approved, string notes)
        {
            var reconciliation = await _context.PaymentReconciliations.FindAsync(matchId);
            if (reconciliation == null)
            {
                return new MatchApprovalResult
                {
                    Success = false,
                    Message = "Reconciliation record not found"
                };
            }

            reconciliation.Status = approved ? "approved" : "rejected";
            reconciliation.Notes = notes;
            reconciliation.UpdatedAt = DateTime.UtcNow;

            if (approved)
            {
                // Mark the payment transaction as reconciled
                var paymentTransaction = await _context.PaymentTransactions.FindAsync(reconciliation.PaymentTransactionId);
                if (paymentTransaction != null)
                {
                    paymentTransaction.IsReconciled = true;
                    paymentTransaction.ReconciledAt = DateTime.UtcNow;
                    paymentTransaction.ReconciliationId = reconciliation.Id;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Reconciliation {matchId} {(approved ? "approved" : "rejected")}");

            return new MatchApprovalResult
            {
                Success = true,
                Message = $"Reconciliation {(approved ? "approved" : "rejected")} successfully"
            };
        }

        public async Task<List<ReconciliationDiscrepancy>> GetDiscrepanciesAsync(Guid tenantId)
        {
            return await _context.PaymentReconciliations
                .Where(pr => pr.TenantId == tenantId && pr.Difference > 0)
                .Include(pr => pr.PaymentTransaction)
                    .ThenInclude(pt => pt.User)
                .Select(pr => new ReconciliationDiscrepancy
                {
                    ReconciliationId = pr.Id,
                    BankTransactionId = pr.BankTransactionId,
                    PaymentTransactionId = pr.PaymentTransactionId,
                    TransactionReference = pr.PaymentTransaction.TransactionReference,
                    CustomerName = pr.PaymentTransaction.User.FirstName + " " + pr.PaymentTransaction.User.LastName,
                    ExpectedAmount = pr.PaymentAmount,
                    ActualAmount = pr.BankAmount,
                    Difference = pr.Difference,
                    DifferencePercentage = pr.PaymentAmount > 0 ? (pr.Difference / pr.PaymentAmount) * 100 : 0,
                    TransactionDate = pr.PaymentTransaction.TransactionDate,
                    Status = pr.Status
                })
                .OrderByDescending(d => d.Difference)
                .ToListAsync();
        }

        public async Task<byte[]> GenerateReconciliationReportAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var reconciliations = await _context.PaymentReconciliations
                .Where(pr => pr.TenantId == tenantId && 
                           pr.ReconciledAt >= startDate && 
                           pr.ReconciledAt <= endDate)
                .Include(pr => pr.PaymentTransaction)
                    .ThenInclude(pt => pt.User)
                .Include(pr => pr.PaymentTransaction)
                    .ThenInclude(pt => pt.Charge)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Reconciliation ID,Bank Transaction ID,Payment Transaction ID,Customer Name,Expected Amount,Actual Amount,Difference,Match Type,Status,Reconciled Date");

            foreach (var rec in reconciliations)
            {
                csv.AppendLine($"{rec.Id},{rec.BankTransactionId},{rec.PaymentTransactionId}," +
                             $"{rec.PaymentTransaction.User.FirstName} {rec.PaymentTransaction.User.LastName}," +
                             $"{rec.PaymentAmount:C},{rec.BankAmount:C},{rec.Difference:C}," +
                             $"{rec.MatchType},{rec.Status},{rec.ReconciledAt:yyyy-MM-dd HH:mm:ss}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public async Task<AutoReconciliationResult> RunAutoReconciliationAsync(Guid tenantId)
        {
            var result = new AutoReconciliationResult
            {
                ProcessedTransactions = 0,
                AutoMatchedTransactions = 0,
                ManualReviewRequired = 0,
                Errors = new List<string>()
            };

            // Get unmatched bank transactions
            var unmatchedTransactions = await _context.UnmatchedBankTransactions
                .Where(ubt => ubt.TenantId == tenantId)
                .ToListAsync();

            foreach (var transaction in unmatchedTransactions)
            {
                try
                {
                    var match = await FindMatchingTransaction(new BankTransactionDto
                    {
                        TransactionId = transaction.TransactionId,
                        TransactionDate = transaction.TransactionDate,
                        Description = transaction.Description,
                        Amount = transaction.Amount,
                        Reference = transaction.Reference,
                        AccountNumber = transaction.AccountNumber
                    }, tenantId);

                    if (match != null && match.MatchScore >= 0.9) // High confidence match
                    {
                        // Auto-approve high confidence matches
                        var reconciliation = new PaymentReconciliation
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            BankTransactionId = transaction.TransactionId,
                            PaymentTransactionId = match.PaymentTransactionId,
                            MatchType = match.MatchType,
                            MatchScore = match.MatchScore,
                            Status = "approved",
                            ReconciledBy = Guid.Parse("00000000-0000-0000-0000-000000000000"), // System user
                            ReconciledAt = DateTime.UtcNow,
                            BankAmount = transaction.Amount,
                            PaymentAmount = match.PaymentAmount,
                            Difference = Math.Abs(transaction.Amount - match.PaymentAmount)
                        };

                        _context.PaymentReconciliations.Add(reconciliation);

                        // Mark payment transaction as reconciled
                        var paymentTransaction = await _context.PaymentTransactions.FindAsync(match.PaymentTransactionId);
                        if (paymentTransaction != null)
                        {
                            paymentTransaction.IsReconciled = true;
                            paymentTransaction.ReconciledAt = DateTime.UtcNow;
                            paymentTransaction.ReconciliationId = reconciliation.Id;
                        }

                        // Remove from unmatched transactions
                        _context.UnmatchedBankTransactions.Remove(transaction);

                        result.AutoMatchedTransactions++;
                    }
                    else if (match != null)
                    {
                        // Create manual review match
                        var reconciliation = new PaymentReconciliation
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            BankTransactionId = transaction.TransactionId,
                            PaymentTransactionId = match.PaymentTransactionId,
                            MatchType = match.MatchType,
                            MatchScore = match.MatchScore,
                            Status = "pending_approval",
                            ReconciledBy = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                            ReconciledAt = DateTime.UtcNow,
                            BankAmount = transaction.Amount,
                            PaymentAmount = match.PaymentAmount,
                            Difference = Math.Abs(transaction.Amount - match.PaymentAmount)
                        };

                        _context.PaymentReconciliations.Add(reconciliation);
                        _context.UnmatchedBankTransactions.Remove(transaction);

                        result.ManualReviewRequired++;
                    }

                    result.ProcessedTransactions++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error auto-reconciling transaction {transaction.TransactionId}");
                    result.Errors.Add($"Error processing transaction {transaction.TransactionId}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Auto-reconciliation completed for tenant {tenantId}. Processed: {result.ProcessedTransactions}, Auto-matched: {result.AutoMatchedTransactions}, Manual review: {result.ManualReviewRequired}");

            return result;
        }

        public async Task<List<BankAccountDto>> GetBankAccountsAsync(Guid tenantId)
        {
            return await _context.BankAccounts
                .Where(ba => ba.TenantId == tenantId && ba.IsActive)
                .Select(ba => new BankAccountDto
                {
                    BankName = ba.BankName,
                    AccountName = ba.AccountName,
                    AccountNumber = ba.AccountNumber,
                    BranchCode = ba.BranchCode,
                    RoutingNumber = ba.RoutingNumber,
                    SwiftCode = ba.SwiftCode,
                    Currency = ba.Currency,
                    IsActive = ba.IsActive
                })
                .ToListAsync();
        }

        public async Task<bool> ImportBankStatementFileAsync(Stream fileStream, string format, Guid tenantId)
        {
            try
            {
                var transactions = new List<BankTransactionDto>();

                if (format.ToLower() == "csv")
                {
                    transactions = await ParseCsvStatement(fileStream);
                }
                else if (format.ToLower() == "ofx")
                {
                    transactions = await ParseOfxStatement(fileStream);
                }
                else
                {
                    throw new ArgumentException($"Unsupported format: {format}");
                }

                var request = new BankStatementImportRequest
                {
                    TenantId = tenantId,
                    Transactions = transactions,
                    ImportedBy = Guid.Parse("00000000-0000-0000-0000-000000000000") // System user
                };

                await ReconcileBankStatementAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing bank statement file");
                return false;
            }
        }

        #region Helper Methods

        private async Task<ReconciliationMatch?> FindMatchingTransaction(BankTransactionDto bankTransaction, Guid tenantId)
        {
            // Look for payment transactions within the last 30 days
            var startDate = bankTransaction.TransactionDate.AddDays(-30);
            var endDate = bankTransaction.TransactionDate.AddDays(30);

            var candidates = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId &&
                           pt.TransactionDate >= startDate &&
                           pt.TransactionDate <= endDate &&
                           pt.Status == "completed" &&
                           !pt.IsReconciled)
                .Include(pt => pt.User)
                .Include(pt => pt.Charge)
                .ToListAsync();

            ReconciliationMatch? bestMatch = null;
            double bestScore = 0;

            foreach (var candidate in candidates)
            {
                double score = 0;
                string matchType = "";

                // Amount matching (40% weight)
                if (Math.Abs(candidate.Amount - bankTransaction.Amount) < 0.01m)
                {
                    score += 0.4;
                    matchType += "amount;";
                }

                // Date proximity (20% weight)
                var daysDiff = Math.Abs((candidate.TransactionDate - bankTransaction.TransactionDate).TotalDays);
                if (daysDiff <= 1)
                {
                    score += 0.2;
                    matchType += "date;";
                }
                else if (daysDiff <= 7)
                {
                    score += 0.1;
                    matchType += "date;";
                }

                // Reference matching (30% weight)
                if (!string.IsNullOrWhiteSpace(bankTransaction.Reference) && 
                    !string.IsNullOrWhiteSpace(candidate.TransactionReference))
                {
                    if (bankTransaction.Reference.Contains(candidate.TransactionReference) ||
                        candidate.TransactionReference.Contains(bankTransaction.Reference))
                    {
                        score += 0.3;
                        matchType += "reference;";
                    }
                }

                // Description matching (10% weight)
                if (!string.IsNullOrWhiteSpace(bankTransaction.Description))
                {
                    var description = bankTransaction.Description.ToLower();
                    if (description.Contains("payment") || 
                        description.Contains("invoice") ||
                        description.Contains("receipt"))
                    {
                        score += 0.1;
                        matchType += "description;";
                    }
                }

                if (score > bestScore && score >= 0.5) // Minimum threshold
                {
                    bestScore = score;
                    bestMatch = new ReconciliationMatch
                    {
                        PaymentTransactionId = candidate.Id,
                        TransactionReference = candidate.TransactionReference,
                        CustomerName = candidate.User.FirstName + " " + candidate.User.LastName,
                        CustomerEmail = candidate.User.Email,
                        PaymentAmount = candidate.Amount,
                        MatchType = matchType.TrimEnd(';'),
                        MatchScore = score,
                        TransactionDate = candidate.TransactionDate
                    };
                }
            }

            return bestMatch;
        }

        private async Task<List<BankTransactionDto>> ParseCsvStatement(Stream fileStream)
        {
            var transactions = new List<BankTransactionDto>();
            
            using (var reader = new StreamReader(fileStream))
            {
                var header = await reader.ReadLineAsync(); // Skip header
                
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var fields = line.Split(',');

                    if (fields.Length >= 5)
                    {
                        transactions.Add(new BankTransactionDto
                        {
                            TransactionId = fields[0]?.Trim() ?? Guid.NewGuid().ToString(),
                            TransactionDate = DateTime.TryParse(fields[1], out var date) ? date : DateTime.UtcNow,
                            Description = fields[2]?.Trim() ?? "",
                            Amount = decimal.TryParse(fields[3], out var amount) ? amount : 0,
                            Reference = fields[4]?.Trim() ?? "",
                            AccountNumber = fields.Length > 5 ? fields[5]?.Trim() ?? "" : ""
                        });
                    }
                }
            }

            return transactions;
        }

        private async Task<List<BankTransactionDto>> ParseOfxStatement(Stream fileStream)
        {
            // Simplified OFX parsing - in a real implementation, use a proper OFX library
            var transactions = new List<BankTransactionDto>();
            
            using (var reader = new StreamReader(fileStream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("<STMTTRN>"))
                    {
                        var transaction = new BankTransactionDto();
                        
                        // Parse OFX transaction fields
                        while ((line = await reader.ReadLineAsync()) != null && !line.StartsWith("</STMTTRN>"))
                        {
                            if (line.StartsWith("<TRNTYPE>"))
                                transaction.Description = line.Replace("<TRNTYPE>", "").Replace("</TRNTYPE>", "").Trim();
                            else if (line.StartsWith("<DTPOSTED>"))
                            {
                                var dateStr = line.Replace("<DTPOSTED>", "").Replace("</DTPOSTED>", "").Trim();
                                if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
                                    transaction.TransactionDate = date;
                            }
                            else if (line.StartsWith("<TRNAMT>"))
                            {
                                var amountStr = line.Replace("<TRNAMT>", "").Replace("</TRNAMT>", "").Trim();
                                if (decimal.TryParse(amountStr, out var amount))
                                    transaction.Amount = Math.Abs(amount);
                            }
                            else if (line.StartsWith("<FITID>"))
                                transaction.TransactionId = line.Replace("<FITID>", "").Replace("</FITID>", "").Trim();
                            else if (line.StartsWith("<MEMO>"))
                                transaction.Reference = line.Replace("<MEMO>", "").Replace("</MEMO>", "").Trim();
                        }
                        
                        transactions.Add(transaction);
                    }
                }
            }

            return transactions;
        }

        #endregion
    }

    // Supporting DTOs and Entities
    public class BankStatementImportRequest
    {
        public Guid TenantId { get; set; }
        public List<BankTransactionDto> Transactions { get; set; } = new();
        public Guid ImportedBy { get; set; }
    }

    public class BankTransactionDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
    }

    public class ReconciliationResult
    {
        public int TotalTransactions { get; set; }
        public int MatchedTransactions { get; set; }
        public int UnmatchedTransactions { get; set; }
        public List<ReconciliationDiscrepancy> Discrepancies { get; set; } = new();
        public List<ReconciliationMatch> Matches { get; set; } = new();
    }

    public class ReconciliationMatch
    {
        public Guid ReconciliationId { get; set; }
        public string BankTransactionId { get; set; } = string.Empty;
        public Guid PaymentTransactionId { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal PaymentAmount { get; set; }
        public decimal BankAmount { get; set; }
        public decimal Difference { get; set; }
        public string MatchType { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime ReconciledAt { get; set; }
    }

    public class MatchApprovalResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ReconciliationDiscrepancy
    {
        public Guid ReconciliationId { get; set; }
        public string BankTransactionId { get; set; } = string.Empty;
        public Guid PaymentTransactionId { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal ExpectedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Difference { get; set; }
        public decimal DifferencePercentage { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AutoReconciliationResult
    {
        public int ProcessedTransactions { get; set; }
        public int AutoMatchedTransactions { get; set; }
        public int ManualReviewRequired { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    // Additional Entities
    public class PaymentReconciliation
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string BankTransactionId { get; set; } = string.Empty;
        public Guid PaymentTransactionId { get; set; }
        public string MatchType { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public string Status { get; set; } = string.Empty; // pending_approval, approved, rejected
        public string Notes { get; set; } = string.Empty;
        public Guid ReconciledBy { get; set; }
        public DateTime ReconciledAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public decimal BankAmount { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal Difference { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual PaymentTransaction PaymentTransaction { get; set; } = null!;
    }

    public class UnmatchedBankTransaction
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public DateTime ImportedAt { get; set; }
        public Guid ImportedBy { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
    }

    public class BankAccount
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string RoutingNumber { get; set; } = string.Empty;
        public string SwiftCode { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
    }
}
