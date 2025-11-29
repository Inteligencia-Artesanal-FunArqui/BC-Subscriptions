using Microsoft.EntityFrameworkCore;
using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using OsitoPolar.Subscriptions.Service.Domain.Model.ValueObjects;
using OsitoPolar.Subscriptions.Service.Infrastructure.Persistence.EFC.Configuration;

namespace OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Persistence.EFC.Configuration;

/// <summary>
/// Database seeder for initial data population
/// Seeds subscription plans on first run
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seed subscription plans if they don't exist
    /// </summary>
    public static async Task SeedSubscriptionPlans(SubscriptionsDbContext context)
    {
        // Check if any plans already exist
        var existingPlansCount = await context.Set<Subscription>().CountAsync();

        if (existingPlansCount > 0)
        {
            Console.WriteLine($"✅ Subscription plans already exist ({existingPlansCount} plans found). Skipping seeding.");
            return;
        }

        Console.WriteLine("🌱 Seeding subscription plans...");

        var plans = new List<Subscription>
        {
            // Owner Plans (IDs 1-3) - Based on MaxEquipment
            new Subscription(
                id: 1,
                planName: "Basic (Polar Bear)",
                price: 18.99m,
                billingCycle: BillingCycle.Monthly,
                maxEquipment: 6,
                maxClients: null,
                featureNames: new List<string>
                {
                    "Up to 6 units",
                    "Real-time temperature monitoring",
                    "Critical-fault email alerts",
                    "Remote on/off control",
                    "Maintenance history log",
                    "Email support"
                }
            ),
            new Subscription(
                id: 2,
                planName: "Standard (Snow Bear)",
                price: 35.13m,
                billingCycle: BillingCycle.Monthly,
                maxEquipment: 12,
                maxClients: null,
                featureNames: new List<string>
                {
                    "Up to 12 units",
                    "Everything in Basic",
                    "Advanced monitoring (energy, usage)",
                    "Remote temperature adjustment",
                    "Monthly energy reports",
                    "Scheduled maintenance"
                }
            ),
            new Subscription(
                id: 3,
                planName: "Premium (Glacial Bear)",
                price: 67.56m,
                billingCycle: BillingCycle.Monthly,
                maxEquipment: 24,
                maxClients: null,
                featureNames: new List<string>
                {
                    "Up to 24 units",
                    "Everything in Standard",
                    "Full monitoring (temp, energy, run-time)",
                    "Auto-scheduled preventive maintenance",
                    "Exclusive analytics dashboard"
                }
            ),

            // Provider Plans (IDs 4-6) - Based on MaxClients
            new Subscription(
                id: 4,
                planName: "Small Company",
                price: 40.51m,
                billingCycle: BillingCycle.Monthly,
                maxEquipment: null,
                maxClients: 10,
                featureNames: new List<string>
                {
                    "Manage up to 10 clients",
                    "Client & unit management",
                    "Technician visit scheduling",
                    "Basic technical reports",
                    "Client-fault notifications",
                    "Service history log",
                    "Email support"
                }
            ),
            new Subscription(
                id: 5,
                planName: "Medium Company",
                price: 81.08m,
                billingCycle: BillingCycle.Monthly,
                maxEquipment: null,
                maxClients: 30,
                featureNames: new List<string>
                {
                    "Up to 30 clients",
                    "Everything in Small Company",
                    "Detailed technical reports",
                    "Tech performance metrics",
                    "Client feedback surveys",
                    "Service dashboard",
                    "Priority support"
                }
            ),
            new Subscription(
                id: 6,
                planName: "Enterprise Premium",
                price: 162.16m,
                billingCycle: BillingCycle.Monthly,
                maxEquipment: null,
                maxClients: null, // Unlimited
                featureNames: new List<string>
                {
                    "Unlimited clients",
                    "Everything in Medium Company",
                    "Advanced admin dashboard",
                    "Predictive maintenance alerts",
                    "Historical data exports",
                    "Custom reporting & branding"
                }
            )
        };

        await context.Set<Subscription>().AddRangeAsync(plans);
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ Successfully seeded {plans.Count} subscription plans!");
        Console.WriteLine("   - Owner Plans (IDs 1-3): Basic (Polar Bear), Standard (Snow Bear), Premium (Glacial Bear)");
        Console.WriteLine("   - Provider Plans (IDs 4-6): Small Company, Medium Company, Enterprise Premium");
    }

    /// <summary>
    /// Run all database seeding operations
    /// </summary>
    public static async Task SeedDatabase(SubscriptionsDbContext context)
    {
        Console.WriteLine("🚀 Starting database seeding...");

        await SeedSubscriptionPlans(context);

        Console.WriteLine("🎉 Database seeding completed!");
    }
}
