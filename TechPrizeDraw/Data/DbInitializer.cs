using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;

namespace TechPrizeDraw.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Apply migrations if using relational database
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        // 1. Seed Roles
        string[] roles = ["SuperAdmin", "Admin", "Support"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Default Admin Users (configurable via environment variables or appsettings)
        string superAdminPassword = configuration["AdminSeed:SuperAdminPassword"]
            ?? Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD")
            ?? "SuperAdmin@2026!";

        string adminPassword = configuration["AdminSeed:AdminPassword"]
            ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
            ?? "Admin@2026!";

        string supportPassword = configuration["AdminSeed:SupportPassword"]
            ?? Environment.GetEnvironmentVariable("SUPPORT_PASSWORD")
            ?? "Support@2026!";

        var defaultUsers = new[]
        {
            new
            {
                Email = configuration["AdminSeed:SuperAdminEmail"] ?? "superadmin@techprizedraw.com",
                UserName = configuration["AdminSeed:SuperAdminEmail"] ?? "superadmin@techprizedraw.com",
                FullName = "Chief Administrator",
                Role = "SuperAdmin",
                Password = superAdminPassword
            },
            new
            {
                Email = configuration["AdminSeed:AdminEmail"] ?? "admin@techprizedraw.com",
                UserName = configuration["AdminSeed:AdminEmail"] ?? "admin@techprizedraw.com",
                FullName = "Campaign Operations Manager",
                Role = "Admin",
                Password = adminPassword
            },
            new
            {
                Email = configuration["AdminSeed:SupportEmail"] ?? "techprizedraw@inbox.eu",
                UserName = configuration["AdminSeed:SupportEmail"] ?? "techprizedraw@inbox.eu",
                FullName = "Support Verification Agent",
                Role = "Support",
                Password = supportPassword
            }
        };

        foreach (var u in defaultUsers)
        {
            var existingUser = await userManager.FindByEmailAsync(u.Email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = u.UserName,
                    Email = u.Email,
                    EmailConfirmed = true,
                    FullName = u.FullName,
                    RoleTitle = u.Role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(user, u.Password);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, u.Role);
                }
            }
        }

        // 3. Seed Campaign if none exists
        if (!await context.Campaigns.AnyAsync())
        {
            var isConfigEnabled = configuration.GetValue<bool>("Campaign:Enabled", false);
            var campaign = new Campaign
            {
                Name = configuration.GetValue<string>("Campaign:Name") ?? "TECH Prize Draw",
                TicketPrefix = configuration.GetValue<string>("Campaign:TicketPrefix") ?? "TECH",
                TicketPrice = configuration.GetValue<decimal?>("Campaign:TicketPrice") ?? 50.00m,
                MaximumTickets = configuration.GetValue<int?>("Campaign:MaximumTickets") ?? 5000,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                DrawDate = DateTime.UtcNow.AddMonths(1).AddDays(1),
                ReservationTimeoutMinutes = configuration.GetValue<int?>("Campaign:ReservationTimeoutMinutes") ?? 60,
                Status = CampaignStatus.Active,
                IsEnabled = isConfigEnabled,
                Description = "Limited 5,000 ticket technology prize draw. Enter for ৳50 BDT via bKash or Nagad.",
                TermsUrl = "/Home/Terms",
                PrivacyPolicyUrl = "/Home/Privacy",
                EligibilityRules = "Open to Bangladesh residents aged 18 and older. Subject to independent regulatory verification.",
                SupportContact = "techprizedraw@inbox.eu",
                CreatedAt = DateTime.UtcNow
            };

            await context.Campaigns.AddAsync(campaign);
            await context.SaveChangesAsync();
        }

        // 4. Seed Payment Methods if none exist
        if (!await context.PaymentMethods.AnyAsync())
        {
            var paymentMethods = new List<PaymentMethod>
            {
                new()
                {
                    Name = "bKash",
                    Provider = PaymentProvider.bKash,
                    AccountNumber = "017XXXXXXXX",
                    Instructions = "Send ৳50 using bKash 'Send Money' to this personal account number. Copy your 10-character Transaction ID (TrxID) and enter it below.",
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "Nagad",
                    Provider = PaymentProvider.Nagad,
                    AccountNumber = "018XXXXXXXX",
                    Instructions = "Send ৳50 using Nagad 'Send Money' to this personal account number. Copy your 8-character Transaction ID (TxnID) and enter it below.",
                    IsActive = true,
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.PaymentMethods.AddRangeAsync(paymentMethods);
            await context.SaveChangesAsync();
        }

        // 5. Seed Prizes if none exist
        if (!await context.Prizes.AnyAsync())
        {
            var prizes = new List<Prize>
            {
                new()
                {
                    Position = 1,
                    Title = "1st Prize",
                    Name = "Next-Gen Pro Laptop",
                    Description = "High-performance workstation laptop featuring cutting-edge processor, 16GB unified RAM, 512GB NVMe SSD, and high-definition Retina display.",
                    Specifications = "15.6-inch IPS / Intel Core i7 / 16GB RAM / 512GB SSD / Thunderbolt 4",
                    ImageUrl = "/images/prizes/laptop.svg",
                    EstimatedValue = 120000m,
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Position = 2,
                    Title = "2nd Prize",
                    Name = "Studio Audio Interface",
                    Description = "Professional 24-bit/192kHz USB audio interface with ultra-low latency, crystal-clear studio-grade preamps, and direct instrument input.",
                    Specifications = "2-In / 2-Out USB-C / 24-bit/192kHz / High-headroom instrument inputs",
                    ImageUrl = "/images/prizes/audio-interface.svg",
                    EstimatedValue = 28000m,
                    IsActive = true,
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Position = 3,
                    Title = "3rd Prize",
                    Name = "Acoustic Guitar",
                    Description = "Handcrafted acoustic guitar with solid spruce top, mahogany back and sides, warm resonant projection, and premium gig bag.",
                    Specifications = "Solid Sitka Spruce / Rosewood fretboard / Chrome die-cast tuners",
                    ImageUrl = "/images/prizes/guitar.svg",
                    EstimatedValue = 18000m,
                    IsActive = true,
                    DisplayOrder = 3,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Prizes.AddRangeAsync(prizes);
            await context.SaveChangesAsync();
        }
    }
}
