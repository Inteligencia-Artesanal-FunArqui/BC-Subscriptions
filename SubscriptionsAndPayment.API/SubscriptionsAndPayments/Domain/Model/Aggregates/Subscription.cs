using OsitoPolar.Subscriptions.Service.Domain.Model.ValueObjects;
using System.Text.Json;

namespace OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;

/// <summary>
/// Represents a subscription plan for a user or provider.
/// </summary>
public partial class Subscription
{
    public int Id { get; private set; }
    public string PlanName { get; private set; }
    public Price Price { get; private set; }
    public BillingCycle BillingCycle { get; private set; }
    public int? MaxEquipment { get; private set; }
    public int? MaxClients { get; private set; }
    
    // Store features as JSON string in database
    // Needs to be internal or public for EF Core to access it
    internal string? FeaturesJson { get; set; }  // Nullable
    
    // Computed property that converts JSON to/from List<Feature>
    public List<Feature> Features 
    { 
        get => string.IsNullOrEmpty(FeaturesJson) 
            ? new List<Feature>() 
            : JsonSerializer.Deserialize<List<string>>(FeaturesJson)
                ?.Select(f => new Feature(f)).ToList() ?? new List<Feature>();
        private set => FeaturesJson = value?.Any() == true 
            ? JsonSerializer.Serialize(value.Select(f => f.Name).ToList()) 
            : "[]";
    }

    protected Subscription()
    {
        PlanName = string.Empty;
        Price = new Price(0m, "USD");
        BillingCycle = BillingCycle.Monthly;
        FeaturesJson = "[]";
    }

    public Subscription(int id, string planName, decimal price, BillingCycle billingCycle, int? maxEquipment = null, int? maxClients = null, List<string>? featureNames = null) : this()
    {
        Id = id;
        PlanName = planName ?? throw new ArgumentNullException(nameof(planName));
        Price = new Price(price, "USD");
        BillingCycle = billingCycle;
        MaxEquipment = maxEquipment;
        MaxClients = maxClients;
        Features = featureNames?.Select(f => new Feature(f)).ToList() ?? new List<Feature>();
    }

    public void UpdatePlan(string newPlanName, decimal newPrice, BillingCycle newBillingCycle, int? newMaxEquipment = null, int? newMaxClients = null, List<string>? newFeatureNames = null)
    {
        PlanName = newPlanName;
        Price = new Price(newPrice, "USD");
        BillingCycle = newBillingCycle;
        MaxEquipment = newMaxEquipment;
        MaxClients = newMaxClients;
        Features = newFeatureNames?.Select(f => new Feature(f)).ToList() ?? Features;
    }
}