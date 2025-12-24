using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace UmiHealth.Application.Authorization
{
    public static class AuthorizationPolicies
    {
        public const string SUPER_ADMIN = "SuperAdmin";
        public const string ADMIN = "Admin";
        public const string PHARMACIST = "Pharmacist";
        public const string CASHIER = "Cashier";
        public const string OPERATIONS = "Operations";

        public const string SYSTEM_ACCESS = "SystemAccess";
        public const string TENANT_MANAGEMENT = "TenantManagement";
        public const string USER_MANAGEMENT = "UserManagement";
        public const string INVENTORY_MANAGEMENT = "InventoryManagement";
        public const string REPORTS_ACCESS = "ReportsAccess";
        public const string POS_ACCESS = "PosAccess";
        public const string PRESCRIPTION_MANAGEMENT = "PrescriptionManagement";
        public const string PATIENT_MANAGEMENT = "PatientManagement";
        public const string SUBSCRIPTION_MANAGEMENT = "SubscriptionManagement";
        public const string BRANCH_MANAGEMENT = "BranchManagement";
        public const string CROSS_BRANCH_ACCESS = "CrossBranchAccess";
    }

    public class RoleRequirement : IAuthorizationRequirement
    {
        public string[] Roles { get; }

        public RoleRequirement(params string[] roles)
        {
            Roles = roles;
        }
    }

    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    public class RoleHandler : AuthorizationHandler<RoleRequirement>
    {
        private readonly Services.IAuthorizationService _authService;

        public RoleHandler(Services.IAuthorizationService authService)
        {
            _authService = authService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            var user = context.User;
            
            if (!user.Identity.IsAuthenticated)
            {
                context.Fail();
                return;
            }

            foreach (var role in requirement.Roles)
            {
                if (await _authService.IsInRoleAsync(user, role))
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            context.Fail();
        }
    }

    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly Services.IAuthorizationService _authService;

        public PermissionHandler(Services.IAuthorizationService authService)
        {
            _authService = authService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var user = context.User;
            
            if (!user.Identity.IsAuthenticated)
            {
                context.Fail();
                return;
            }

            var hasPermission = await _authService.HasPermissionAsync(user, requirement.Permission);
            
            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }

    public static class PolicyExtensions
    {
        public static IServiceCollection AddUmiAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Role-based policies
                options.AddPolicy(AuthorizationPolicies.SUPER_ADMIN, policy =>
                    policy.Requirements.Add(new RoleRequirement(Services.Roles.SUPER_ADMIN)));

                options.AddPolicy(AuthorizationPolicies.ADMIN, policy =>
                    policy.Requirements.Add(new RoleRequirement(Services.Roles.ADMIN)));

                options.AddPolicy(AuthorizationPolicies.PHARMACIST, policy =>
                    policy.Requirements.Add(new RoleRequirement(Services.Roles.PHARMACIST)));

                options.AddPolicy(AuthorizationPolicies.CASHIER, policy =>
                    policy.Requirements.Add(new RoleRequirement(Services.Roles.CASHIER)));

                options.AddPolicy(AuthorizationPolicies.OPERATIONS, policy =>
                    policy.Requirements.Add(new RoleRequirement(Services.Roles.OPERATIONS)));

                // Permission-based policies
                options.AddPolicy(AuthorizationPolicies.SYSTEM_ACCESS, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Services.Permissions.SYSTEM_ALL)));

                options.AddPolicy(AuthorizationPolicies.TENANT_MANAGEMENT, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Services.Permissions.TENANT_MANAGE)));

                options.AddPolicy(AuthorizationPolicies.USER_MANAGEMENT, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Services.Permissions.USER_ALL)));

                options.AddPolicy(AuthorizationPolicies.INVENTORY_MANAGEMENT, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Services.Permissions.INVENTORY_ALL)));

                options.AddPolicy(AuthorizationPolicies.REPORTS_ACCESS, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Services.Permissions.REPORTS_READ)));

                options.AddPolicy(AuthorizationPolicies.POS_ACCESS, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Services.Permissions.POS_ALL)));

                options.AddPolicy(AuthorizationPolicies.PRESCRIPTION_MANAGEMENT, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Services.Permissions.PRESCRIPTIONS_ALL)));

                options.AddPolicy(AuthorizationPolicies.PATIENT_MANAGEMENT, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Services.Permissions.PATIENTS_ALL)));

                options.AddPolicy(AuthorizationPolicies.SUBSCRIPTION_MANAGEMENT, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Services.Permissions.SUBSCRIPTIONS_ALL)));

                options.AddPolicy(AuthorizationPolicies.BRANCH_MANAGEMENT, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Services.Permissions.BRANCHES_ALL)));

                options.AddPolicy(AuthorizationPolicies.CROSS_BRANCH_ACCESS, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Services.Permissions.BRANCHES_CROSS_ACCESS)));

                // Combined policies
                options.AddPolicy("AdminOrAbove", policy =>
                    policy.Requirements.Add(new RoleRequirement(Services.Roles.ADMIN, Services.Roles.SUPER_ADMIN)));

                options.AddPolicy("PharmacistOrAbove", policy =>
                    policy.Requirements.Add(new RoleRequirement(
                        Services.Roles.PHARMACIST, 
                        Services.Roles.ADMIN, 
                        Services.Roles.SUPER_ADMIN)));

                options.AddPolicy("CashierOrAbove", policy =>
                    policy.Requirements.Add(new RoleRequirement(
                        Services.Roles.CASHIER,
                        Services.Roles.PHARMACIST,
                        Services.Roles.ADMIN,
                        Services.Roles.SUPER_ADMIN)));
            });

            return services;
        }
    }
}
