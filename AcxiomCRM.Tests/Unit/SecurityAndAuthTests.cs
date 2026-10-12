using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Authorization;
using AcxiomCRM.Controllers;

namespace AcxiomCRM.Tests.Unit
{
    public class SecurityAndAuthTests
    {
        [Fact]
        public void UsersController_RequiresAdminRole()
        {
            var authAttribute = typeof(UsersController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authAttribute);
            Assert.Equal(Roles.Admin, authAttribute.Roles);
        }

        [Fact]
        public void AuditLogsController_RequiresAdminRole()
        {
            var authAttribute = typeof(AuditLogsController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authAttribute);
            Assert.Equal(Roles.Admin, authAttribute.Roles);
        }

        [Fact]
        public void ProtectedControllers_RequireAuthentication()
        {
            var controllers = new[]
            {
                typeof(CustomersController),
                typeof(LeadsController),
                typeof(OpportunitiesController),
                typeof(FollowUpsController),
                typeof(ActivitiesController),
                typeof(DashboardController),
                typeof(ReportsController)
            };

            foreach (var controller in controllers)
            {
                var authAttribute = controller.GetCustomAttribute<AuthorizeAttribute>();
                Assert.True(authAttribute != null, $"{controller.Name} must be protected by [Authorize]");
            }
        }

        [Fact]
        public void StateChangingMvcPostActions_MustHaveValidateAntiForgeryToken()
        {
            var controllers = new[]
            {
                typeof(AccountController),
                typeof(UsersController),
                typeof(CustomersController),
                typeof(LeadsController),
                typeof(OpportunitiesController),
                typeof(FollowUpsController),
                typeof(ActivitiesController)
            };

            foreach (var controller in controllers)
            {
                var postMethods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null);

                foreach (var method in postMethods)
                {
                    var antiForgery = method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>();
                    Assert.True(antiForgery != null, $"Method {controller.Name}.{method.Name} must have [ValidateAntiForgeryToken] attribute");
                }
            }
        }

        [Fact]
        public void Roles_ContainsThreeCoreRoles()
        {
            Assert.Contains(Roles.Admin, Roles.AllRoles);
            Assert.Contains(Roles.Manager, Roles.AllRoles);
            Assert.Contains(Roles.SalesExecutive, Roles.AllRoles);
            Assert.Equal(3, Roles.AllRoles.Length);
        }
    }
}
