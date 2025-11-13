using OsitoPolar.Subscriptions.Service.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using OsitoPolar.Subscriptions.Service.Domain.Model.ValueObjects;

namespace OsitoPolar.Subscriptions.Service.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplySubscriptionsConfiguration(this ModelBuilder builder)
    {
        // Subscription configuration
        builder.Entity<Subscription>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).HasColumnName("id").IsRequired().ValueGeneratedOnAdd();
            entity.ToTable("subscriptions");
            
            entity.Property(s => s.PlanName)
                .HasColumnName("plan_name")
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(s => s.Price)
                .HasConversion(
                    p => p.Amount,
                    v => new Price(v, "USD"))
                .HasColumnName("price")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            entity.Property(s => s.BillingCycle)
                .HasConversion(
                    b => b.ToString(), 
                    v => (BillingCycle)Enum.Parse(typeof(BillingCycle), v))
                .HasColumnName("billing_cycle")
                .IsRequired()
                .HasMaxLength(20);
                
            entity.Property(s => s.MaxEquipment).HasColumnName("max_equipment");
            entity.Property(s => s.MaxClients).HasColumnName("max_clients");
            
            entity.Property<string>("FeaturesJson")
                .HasColumnName("features")
                .HasColumnType("json")
                .IsRequired(false);
                
            entity.Ignore(s => s.Features);
            
            entity.Property<DateTimeOffset?>("CreatedDate")
                .HasColumnName("created_date")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property<DateTimeOffset?>("UpdatedDate")
                .HasColumnName("updated_date")
                .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
        });
        
        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasColumnName("id").IsRequired().ValueGeneratedOnAdd();
            entity.ToTable("payments");
            
            entity.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(p => p.SubscriptionId).HasColumnName("subscription_id").IsRequired();
            
            entity.Property(p => p.Amount)
                .HasConversion(
                    a => a.Amount,
                    v => new Price(v, "USD"))
                .HasColumnName("amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();
            
            entity.Property(p => p.StripeSession)
                .HasConversion(
                    s => s.SessionId,
                    v => new StripeSession(v))
                .HasColumnName("stripe_session_id")
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(p => p.CustomerEmail)
                .HasColumnName("customer_email")
                .HasMaxLength(255)
                .IsRequired(false);
                
            entity.Property(p => p.Description)
                .HasColumnName("description")
                .HasMaxLength(500)
                .IsRequired(false);
            
                        
            entity.Property<DateTimeOffset?>("CreatedDate")
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property<DateTimeOffset?>("UpdatedDate")
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
        });

        // ServicePayment configuration
        builder.Entity<ServicePayment>(entity =>
        {
            entity.HasKey(sp => sp.Id);
            entity.Property(sp => sp.Id).HasColumnName("id").IsRequired().ValueGeneratedOnAdd();
            entity.ToTable("service_payments");

            entity.Property(sp => sp.WorkOrderId).HasColumnName("work_order_id").IsRequired();
            entity.Property(sp => sp.ServiceRequestId).HasColumnName("service_request_id").IsRequired();
            entity.Property(sp => sp.OwnerId).HasColumnName("owner_id").IsRequired();
            entity.Property(sp => sp.ProviderId).HasColumnName("provider_id").IsRequired();

            entity.Property(sp => sp.TotalAmount)
                .HasColumnName("total_amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(sp => sp.PlatformFee)
                .HasColumnName("platform_fee")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(sp => sp.ProviderAmount)
                .HasColumnName("provider_amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(sp => sp.StripePaymentIntentId)
                .HasColumnName("stripe_payment_intent_id")
                .HasMaxLength(255)
                .IsRequired(false);

            entity.Property(sp => sp.StripeTransactionId)
                .HasColumnName("stripe_transaction_id")
                .HasMaxLength(255)
                .IsRequired(false);

            entity.Property(sp => sp.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(sp => sp.Description)
                .HasColumnName("description")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(sp => sp.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(sp => sp.CompletedAt)
                .HasColumnName("completed_at")
                .IsRequired(false);

            // Indexes for efficient querying
            entity.HasIndex(sp => sp.WorkOrderId).HasDatabaseName("idx_service_payments_work_order");
            entity.HasIndex(sp => sp.OwnerId).HasDatabaseName("idx_service_payments_owner");
            entity.HasIndex(sp => sp.ProviderId).HasDatabaseName("idx_service_payments_provider");
            entity.HasIndex(sp => sp.Status).HasDatabaseName("idx_service_payments_status");
        });
    }
}