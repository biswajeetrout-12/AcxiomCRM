using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, bool seedDemoData = true)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContext>>();

            // Ensure database is created and migrations applied
            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            // 1. Seed Roles (always required for role-based authorization to function)
            foreach (var roleName in Roles.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // If demo seeding is disabled (e.g., standard production deployment), stop here.
            if (!seedDemoData)
            {
                logger?.LogInformation("Demo data seeding is disabled. Skipping creation of default development demo accounts.");
                return;
            }

            // 2. Seed Users
            var adminEmail = "admin@acxiomcrm.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-30)
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                }
            }

            var managerEmail = "manager@acxiomcrm.com";
            var managerUser = await userManager.FindByEmailAsync(managerEmail);
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    FullName = "Sarah Jenkins (Sales Manager)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-30)
                };
                var result = await userManager.CreateAsync(managerUser, "Manager@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, Roles.Manager);
                }
            }

            var salesEmail = "sales@acxiomcrm.com";
            var salesUser = await userManager.FindByEmailAsync(salesEmail);
            if (salesUser == null)
            {
                salesUser = new ApplicationUser
                {
                    UserName = salesEmail,
                    Email = salesEmail,
                    FullName = "Alex Rivera (Sales Executive)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-30)
                };
                var result = await userManager.CreateAsync(salesUser, "Sales@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser, Roles.SalesExecutive);
                }
            }

            var sales2Email = "sales2@acxiomcrm.com";
            var sales2User = await userManager.FindByEmailAsync(sales2Email);
            if (sales2User == null)
            {
                sales2User = new ApplicationUser
                {
                    UserName = sales2Email,
                    Email = sales2Email,
                    FullName = "Maria Chen (Sales Executive)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-25)
                };
                var result = await userManager.CreateAsync(sales2User, "Sales@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(sales2User, Roles.SalesExecutive);
                }
            }

            // 3. Seed Customers
            if (!context.Customers.Any())
            {
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        CustomerCode = "CUST-1001",
                        CustomerName = "Apex Global Technologies",
                        Email = "contact@apextech.com",
                        Phone = "9876543210",
                        CompanyName = "Apex Technologies Inc.",
                        Address = "100 Innovation Blvd, Suite 400",
                        City = "San Jose",
                        State = "CA",
                        Status = "Active",
                        OwnerId = salesUser.Id,
                        CreatedBy = "System Seeder",
                        CreatedDate = DateTime.UtcNow.AddDays(-20)
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1002",
                        CustomerName = "Beacon Health Solutions",
                        Email = "info@beaconhealth.org",
                        Phone = "9876543211",
                        CompanyName = "Beacon Health Group",
                        Address = "450 Medical Center Pkwy",
                        City = "Boston",
                        State = "MA",
                        Status = "Active",
                        OwnerId = salesUser.Id,
                        CreatedBy = "System Seeder",
                        CreatedDate = DateTime.UtcNow.AddDays(-15)
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1003",
                        CustomerName = "Vanguard Financial Services",
                        Email = "clientcare@vanguardfs.com",
                        Phone = "9876543212",
                        CompanyName = "Vanguard Financial Corp",
                        Address = "200 Wall Street",
                        City = "New York",
                        State = "NY",
                        Status = "Active",
                        OwnerId = sales2User.Id,
                        CreatedBy = "System Seeder",
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    }
                };

                await context.Customers.AddRangeAsync(customers);
                await context.SaveChangesAsync();
            }

            // 4. Seed Leads
            if (!context.Leads.Any())
            {
                var leads = new List<Lead>
                {
                    new Lead
                    {
                        LeadCode = "LEAD-1001",
                        LeadName = "David Miller",
                        Email = "david.miller@zenithmedia.com",
                        Phone = "9876543213",
                        CompanyName = "Zenith Media Works",
                        Source = "Website",
                        Status = "Qualified",
                        Priority = "High",
                        ExpectedValue = 45000m,
                        Notes = "Interested in enterprise CRM migration and data synchronization.",
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-12)
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1002",
                        LeadName = "Elena Rostova",
                        Email = "elena.r@quantumlogistics.io",
                        Phone = "9876543214",
                        CompanyName = "Quantum Logistics",
                        Source = "Referral",
                        Status = "Contacted",
                        Priority = "Urgent",
                        ExpectedValue = 75000m,
                        Notes = "Needs supply chain CRM integration for 150 drivers and managers.",
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-7)
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1003",
                        LeadName = "Marcus Brody",
                        Email = "marcus@cloudscale.net",
                        Phone = "9876543215",
                        CompanyName = "CloudScale Networks",
                        Source = "Advertisement",
                        Status = "New",
                        Priority = "Medium",
                        ExpectedValue = 28000m,
                        Notes = "Downloaded product whitepaper. Follow-up needed.",
                        AssignedTo = sales2User.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-3)
                    }
                };

                await context.Leads.AddRangeAsync(leads);
                await context.SaveChangesAsync();
            }

            // 5. Seed Opportunities
            if (!context.Opportunities.Any())
            {
                var apexCust = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
                var beaconCust = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1002");
                var vanguardCust = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1003");

                var opportunities = new List<Opportunity>();

                if (apexCust != null)
                {
                    opportunities.Add(new Opportunity
                    {
                        OpportunityName = "Apex Cloud Migration Enterprise",
                        CustomerId = apexCust.CustomerId,
                        Amount = 60000m,
                        Stage = "Negotiation",
                        Probability = 80,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
                        Status = "Open",
                        AssignedTo = salesUser.Id,
                        Notes = "Final contract terms under legal review. Expected signature next week.",
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    });
                }

                if (beaconCust != null)
                {
                    opportunities.Add(new Opportunity
                    {
                        OpportunityName = "Beacon Health CRM Portal",
                        CustomerId = beaconCust.CustomerId,
                        Amount = 85000m,
                        Stage = "Proposal",
                        Probability = 60,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                        Status = "Open",
                        AssignedTo = salesUser.Id,
                        Notes = "Demo completed successfully. Formal proposal submitted.",
                        CreatedDate = DateTime.UtcNow.AddDays(-8)
                    });
                }

                if (vanguardCust != null)
                {
                    opportunities.Add(new Opportunity
                    {
                        OpportunityName = "Vanguard Portfolio Security Suite",
                        CustomerId = vanguardCust.CustomerId,
                        Amount = 120000m,
                        Stage = "Won",
                        Probability = 100,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-2),
                        Status = "Won",
                        AssignedTo = sales2User.Id,
                        Notes = "Deal closed and signed. Implementation scheduled for next month.",
                        CreatedDate = DateTime.UtcNow.AddDays(-20)
                    });
                }

                if (opportunities.Any())
                {
                    await context.Opportunities.AddRangeAsync(opportunities);
                    await context.SaveChangesAsync();
                }
            }

            // 6. Seed FollowUps
            if (!context.FollowUps.Any())
            {
                var apexCust = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
                var lead1 = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-1001");

                var followUps = new List<FollowUp>
                {
                    new FollowUp
                    {
                        CustomerId = apexCust?.CustomerId,
                        RelatedType = "Customer",
                        FollowUpDate = DateTime.UtcNow.AddDays(2),
                        FollowUpType = "Meeting",
                        Subject = "Contract review meeting with VP of IT",
                        Remarks = "Discuss SLA adjustments and licensing terms.",
                        Status = "Planned",
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-2)
                    },
                    new FollowUp
                    {
                        LeadId = lead1?.LeadId,
                        RelatedType = "Lead",
                        FollowUpDate = DateTime.UtcNow.AddDays(4),
                        FollowUpType = "Call",
                        Subject = "Discovery call for enterprise requirements",
                        Remarks = "Walkthrough architecture and timeline.",
                        Status = "Planned",
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    }
                };

                await context.FollowUps.AddRangeAsync(followUps);
                await context.SaveChangesAsync();
            }

            // 7. Seed Activities
            if (!context.Activities.Any())
            {
                var apexCust = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
                var activities = new List<Activity>
                {
                    new Activity
                    {
                        ActivityType = "Meeting",
                        Subject = "Initial Executive Pitch",
                        Description = "Presented AcxiomCRM product roadmap to C-suite stakeholders.",
                        ActivityDate = DateTime.UtcNow.AddDays(-14),
                        CustomerId = apexCust?.CustomerId,
                        AssignedTo = salesUser.Id,
                        Status = "Completed",
                        CreatedDate = DateTime.UtcNow.AddDays(-14)
                    },
                    new Activity
                    {
                        ActivityType = "Email",
                        Subject = "Sent Proposal & Pricing Tier Matrix",
                        Description = "Emailed detailed proposal documentation including custom tier quotes.",
                        ActivityDate = DateTime.UtcNow.AddDays(-6),
                        CustomerId = apexCust?.CustomerId,
                        AssignedTo = salesUser.Id,
                        Status = "Completed",
                        CreatedDate = DateTime.UtcNow.AddDays(-6)
                    }
                };

                await context.Activities.AddRangeAsync(activities);
                await context.SaveChangesAsync();
            }

            // 8. Seed Initial Audit Log
            if (!context.AuditLogs.Any())
            {
                var auditLog = new AuditLog
                {
                    UserId = adminUser.Id,
                    Action = "System Initialized",
                    EntityName = "Database",
                    RecordId = "0",
                    OldValue = null,
                    NewValue = "Initial Seed with roles, default accounts, and baseline CRM data.",
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = "127.0.0.1",
                    Result = "Success",
                    Details = "System successfully seeded with Admin, Manager, and Sales Executive accounts."
                };

                await context.AuditLogs.AddAsync(auditLog);
                await context.SaveChangesAsync();
            }
        }
    }
}
