using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OsitoPolar.Subscriptions.Service.Domain.Model.Queries;
using OsitoPolar.Subscriptions.Service.Domain.Services;
using OsitoPolar.Subscriptions.Service.Interfaces.REST.Resources;
using OsitoPolar.Subscriptions.Service.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;
//using OsitoPolar.Subscriptions.Service.Domain.Model.Commands; 
namespace OsitoPolar.Subscriptions.Service.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Subscription Management Endpoints")]
public class SubscriptionsController(
    ISubscriptionCommandService subscriptionCommandService,
    ISubscriptionQueryService subscriptionQueryService) : ControllerBase
{
    /// <summary>
    /// Gets a subscription by its unique identifier.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription to retrieve.</param>
    /// <returns>A SubscriptionResource if found, or 404 Not Found if not found.</returns>
    [HttpGet("{subscriptionId:int}")]
    [SwaggerOperation(
        Summary = "Get Subscription by Id",
        Description = "Returns a subscription by its unique identifier.",
        OperationId = "GetSubscriptionById")]
    [SwaggerResponse(StatusCodes.Status200OK, "Subscription found", typeof(SubscriptionResource))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Subscription not found")]
    public async Task<IActionResult> GetSubscriptionById(int subscriptionId)
    {
        var query = new GetSubscriptionByIdQuery(subscriptionId);
        var subscription = await subscriptionQueryService.Handle(query);
        if (subscription is null) return NotFound();
        var resource = SubscriptionResourceFromEntityAssembler.ToResourceFromEntity(subscription);
        return Ok(resource);
    }

    /// <summary>
    /// Gets all subscriptions for a specific user type.
    /// </summary>
    /// <param name="userType">The type of user (e.g., 'user' or 'provider').</param>
    /// <returns>A list of subscriptions as resources.</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get All Subscriptions",
        Description = "Returns a list of all subscriptions for a specific user type.",
        OperationId = "GetAllSubscriptions")]
    [SwaggerResponse(StatusCodes.Status200OK, "List of subscriptions", typeof(IEnumerable<SubscriptionResource>))]
    public async Task<IActionResult> GetAllSubscriptions([FromQuery] string userType)
    {
        var query = new GetPlansQuery(userType);
        var subscriptions = await subscriptionQueryService.Handle(query);
        var resources = subscriptions.Select(SubscriptionResourceFromEntityAssembler.ToResourceFromEntity).ToList();
        return Ok(resources);
    }

    /// <summary>
    /// Upgrades a subscription to a new plan.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription to upgrade.</param>
    /// <param name="resource">The resource containing the upgrade details.</param>
    /// <returns>The updated subscription resource with a 201 Created status code, or 400 Bad Request if upgrade failed.</returns>
    [HttpPatch("{subscriptionId:int}")]
    [SwaggerOperation(
        Summary = "Upgrade Subscription",
        Description = "Upgrades a subscription to a new plan.",
        OperationId = "UpgradeSubscription")]
    [SwaggerResponse(StatusCodes.Status201Created, "Subscription upgraded", typeof(SubscriptionResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The subscription could not be upgraded")]
    public async Task<IActionResult> UpgradeSubscription(int subscriptionId, [FromBody] UpgradeSubscriptionResource resource)
    {
        try
        {
            var command = UpgradeSubscriptionCommandFromResourceAssembler.ToCommandFromResource(resource with { UserId = subscriptionId });
            var subscription = await subscriptionCommandService.Handle(command);
            if (subscription is null) return BadRequest("Subscription could not be upgraded.");
            var createdResource = SubscriptionResourceFromEntityAssembler.ToResourceFromEntity(subscription);
            return CreatedAtAction(nameof(GetSubscriptionById), new { subscriptionId = createdResource.Id }, createdResource);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets full subscription plan data for IAM registration flow.
    /// This endpoint is used by IAM Service to validate plans during registration.
    /// </summary>
    /// <param name="planId">The plan ID</param>
    /// <returns>Full subscription plan data including price, currency, maxEquipment, and maxClients</returns>
    [HttpGet("plans/{planId:int}")]
    [SwaggerOperation(
        Summary = "Get Subscription Plan for Registration",
        Description = "Returns full subscription plan data for IAM Service registration flow.",
        OperationId = "GetSubscriptionPlan")]
    [SwaggerResponse(StatusCodes.Status200OK, "Subscription plan found", typeof(SubscriptionPlanResource))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Subscription plan not found")]
    public async Task<IActionResult> GetSubscriptionPlan(int planId)
    {
        var query = new GetSubscriptionByIdQuery(planId);
        var subscription = await subscriptionQueryService.Handle(query);
        if (subscription is null) return NotFound();

        var resource = new SubscriptionPlanResource(
            Id: subscription.Id,
            PlanName: subscription.PlanName,
            Price: subscription.Price.Amount,
            Currency: subscription.Price.Currency,
            MaxEquipment: subscription.MaxEquipment,
            MaxClients: subscription.MaxClients
        );

        return Ok(resource);
    }

    /// <summary>
    /// Gets subscription data for IAM registration flow.
    /// </summary>
    /// <param name="planId">The plan ID</param>
    /// <returns>Subscription data including planId, planName, price, and maxClients</returns>
    [HttpGet("{planId:int}/data")]
    [SwaggerOperation(
        Summary = "Get Subscription Data",
        Description = "Returns subscription data for IAM Service registration flow.",
        OperationId = "GetSubscriptionData")]
    [SwaggerResponse(StatusCodes.Status200OK, "Subscription data found", typeof(SubscriptionDataResource))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Subscription not found")]
    public async Task<IActionResult> GetSubscriptionData(int planId)
    {
        var query = new GetSubscriptionByIdQuery(planId);
        var subscription = await subscriptionQueryService.Handle(query);
        if (subscription is null) return NotFound();

        var resource = new SubscriptionDataResource(
            PlanId: subscription.Id,
            PlanName: subscription.PlanName,
            Price: subscription.Price.Amount,
            MaxClients: subscription.MaxClients ?? 0
        );

        return Ok(resource);
    }

    /// <summary>
    /// Gets subscription limits for registration validation.
    /// </summary>
    /// <param name="planId">The plan ID</param>
    /// <returns>Subscription limits including maxEquipment and maxClients</returns>
    [HttpGet("{planId:int}/limits")]
    [SwaggerOperation(
        Summary = "Get Subscription Limits",
        Description = "Returns subscription limits for IAM Service registration flow.",
        OperationId = "GetSubscriptionLimits")]
    [SwaggerResponse(StatusCodes.Status200OK, "Subscription limits found", typeof(SubscriptionLimitsResource))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Subscription not found")]
    public async Task<IActionResult> GetSubscriptionLimits(int planId)
    {
        var query = new GetSubscriptionByIdQuery(planId);
        var subscription = await subscriptionQueryService.Handle(query);
        if (subscription is null) return NotFound();

        var resource = new SubscriptionLimitsResource(
            MaxEquipment: subscription.MaxEquipment ?? 0,
            MaxClients: subscription.MaxClients ?? 0
        );

        return Ok(resource);
    }
}

public record UpgradeSubscriptionResource(int UserId, int PlanId);

/// <summary>
/// Lightweight resource for subscription data used in IAM registration
/// </summary>
public record SubscriptionDataResource(int PlanId, string PlanName, decimal Price, int MaxClients);

/// <summary>
/// Resource for subscription limits used in IAM registration validation
/// </summary>
public record SubscriptionLimitsResource(int MaxEquipment, int MaxClients);

/// <summary>
/// Full subscription plan resource for IAM registration validation
/// Includes all fields needed by IAM Service to validate and create checkout sessions
/// </summary>
public record SubscriptionPlanResource(
    int Id,
    string PlanName,
    decimal Price,
    string Currency,
    int? MaxEquipment,
    int? MaxClients);