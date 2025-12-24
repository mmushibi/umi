using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "operations,super_admin")]
    public class SubscriptionApprovalController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionApprovalController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingTransactions()
        {
            try
            {
                var transactions = await _subscriptionService.GetPendingTransactionsAsync();
                return Ok(new { success = true, data = transactions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve pending transactions." });
            }
        }

        [HttpPost("approve/{transactionId}")]
        public async Task<IActionResult> ApproveTransaction(string transactionId, [FromBody] ApproveRejectRequest request)
        {
            try
            {
                var result = await _subscriptionService.ApproveTransactionAsync(transactionId, request.ApprovedBy);
                
                if (result)
                {
                    return Ok(new { success = true, message = "Transaction approved successfully." });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Transaction not found or already processed." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to approve transaction." });
            }
        }

        [HttpPost("reject/{transactionId}")]
        public async Task<IActionResult> RejectTransaction(string transactionId, [FromBody] ApproveRejectRequest request)
        {
            try
            {
                var result = await _subscriptionService.RejectTransactionAsync(transactionId, request.ApprovedBy, request.RejectionReason);
                
                if (result)
                {
                    return Ok(new { success = true, message = "Transaction rejected successfully." });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Transaction not found or already processed." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to reject transaction." });
            }
        }

        [HttpGet("transaction/{transactionId}")]
        public async Task<IActionResult> GetTransactionDetails(string transactionId)
        {
            try
            {
                // Get transaction details for audit trail
                var transaction = await _subscriptionService.GetPendingTransactionsAsync();
                var targetTransaction = transaction.FirstOrDefault(t => t.TransactionId == transactionId);
                
                if (targetTransaction == null)
                {
                    return NotFound(new { success = false, message = "Transaction not found." });
                }
                
                return Ok(new { success = true, data = targetTransaction });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve transaction details." });
            }
        }
    }

    public class ApproveRejectRequest
    {
        public string ApprovedBy { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
    }
}
