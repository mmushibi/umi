using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using UmiHealth.Core.Interfaces;
using UmiHealth.Persistence;
using UmiHealth.Application.DTOs;
using UmiHealth.Shared.DTOs;
using UmiHealth.Domain.Entities;

namespace UmiHealth.Application.Services
{
    public interface IUserInvitationService
    {
        Task<bool> SendUserInvitationAsync(Guid tenantId, Guid invitedByUserId, CreateUserRequest userRequest, CancellationToken cancellationToken = default);
        Task<bool> ValidateInvitationTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<UserInvitationResult> AcceptInvitationAsync(string token, string password, CancellationToken cancellationToken = default);
    }

    public class UserInvitationService : IUserInvitationService
    {
        private readonly UmiHealthDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserInvitationService> _logger;
        private readonly IUserService _userService;

        public UserInvitationService(
            UmiHealthDbContext context,
            IEmailService emailService,
            ILogger<UserInvitationService> logger,
            IUserService userService)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _userService = userService;
        }

        public async Task<bool> SendUserInvitationAsync(Guid tenantId, Guid invitedByUserId, CreateUserRequest userRequest, CancellationToken cancellationToken = default)
        {
            try
            {
                // Generate invitation token
                var invitationToken = Guid.NewGuid().ToString("N");
                var expiryDate = DateTime.UtcNow.AddDays(7); // 7 days expiry

                // Create invitation record
                var invitation = new UserInvitation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    InvitedByUserId = invitedByUserId,
                    Email = userRequest.Email,
                    FirstName = userRequest.FirstName,
                    LastName = userRequest.LastName,
                    Role = userRequest.Role,
                    BranchId = userRequest.BranchId,
                    Token = invitationToken,
                    ExpiresAt = expiryDate,
                    CreatedAt = DateTime.UtcNow,
                    IsAccepted = false
                };

                _context.UserInvitations.Add(invitation);
                await _context.SaveChangesAsync(cancellationToken);

                // Send invitation email
                var invitationLink = $"{GetBaseUrl()}/accept-invitation?token={invitationToken}";
                var subject = "Welcome to Umi Health - Account Invitation";
                var body = $@"
                    <h2>Welcome to Umi Health!</h2>
                    <p>You have been invited to join {userRequest.FirstName} {userRequest.LastName} at Umi Health.</p>
                    <p><strong>Role:</strong> {userRequest.Role}</p>
                    <p><strong>Branch:</strong> {userRequest.BranchId?.ToString() ?? "Main Branch"}</p>
                    <p>Click the link below to set up your account:</p>
                    <p><a href='{invitationLink}' style='background-color: #2563EB; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Set Up Your Account</a></p>
                    <p>This invitation will expire in 7 days.</p>
                    <p>If you didn't expect this invitation, please ignore this email.</p>
                ";

                await _emailService.SendEmailAsync(userRequest.Email, subject, body, cancellationToken);

                _logger.LogInformation("User invitation sent to {Email} for tenant {TenantId}", userRequest.Email, tenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending user invitation to {Email}", userRequest.Email);
                return false;
            }
        }

        public async Task<bool> ValidateInvitationTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var invitation = await _context.UserInvitations
                    .FirstOrDefaultAsync(i => i.Token == token && !i.IsAccepted && i.ExpiresAt > DateTime.UtcNow, cancellationToken);

                return invitation != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating invitation token");
                return false;
            }
        }

        public async Task<UserInvitationResult> AcceptInvitationAsync(string token, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                var invitation = await _context.UserInvitations
                    .FirstOrDefaultAsync(i => i.Token == token && !i.IsAccepted && i.ExpiresAt > DateTime.UtcNow, cancellationToken);

                if (invitation == null)
                {
                    return new UserInvitationResult { Success = false, Message = "Invalid or expired invitation token" };
                }

                // Create user from invitation
                var createUserRequest = new CreateUserRequest
                {
                    FirstName = invitation.FirstName,
                    LastName = invitation.LastName,
                    Email = invitation.Email,
                    Role = invitation.Role,
                    BranchId = invitation.BranchId,
                    Password = password,
                    SendInviteEmail = false
                };

                var user = await _userService.CreateUserAsync(invitation.TenantId, createUserRequest, cancellationToken);

                // Update user with branch assignment if specified
                if (invitation.BranchId.HasValue)
                {
                    var userEntity = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
                    if (userEntity != null)
                    {
                        userEntity.BranchId = invitation.BranchId.Value;
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                // Mark invitation as accepted
                invitation.IsAccepted = true;
                invitation.AcceptedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Invitation accepted by {Email}, user created: {UserId}", invitation.Email, user.Id);

                // Determine redirect URL based on user role
                var redirectUrl = GetRoleBasedRedirectUrl(invitation.Role);

                return new UserInvitationResult
                {
                    Success = true,
                    Message = "Account created successfully",
                    User = user,
                    RedirectUrl = redirectUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting invitation");
                return new UserInvitationResult { Success = false, Message = "Failed to create account from invitation" };
            }
        }

        private string GetRoleBasedRedirectUrl(string role)
        {
            return role.ToLower() switch
            {
                "admin" => "/portals/admin/home.html",
                "pharmacist" => "/portals/pharmacist/home.html",
                "cashier" => "/portals/cashier/home.html",
                "operations" => "/portals/operations/home.html",
                _ => "/portals/admin/home.html" // Default fallback
            };
        }

        private string GetBaseUrl()
        {
            // This should be configurable based on environment
            return "https://umihealth.com";
        }
    }

    public class UserInvitationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UmiHealth.Core.Interfaces.IUserDto? User { get; set; }
        public string RedirectUrl { get; set; } = string.Empty;
    }
}
