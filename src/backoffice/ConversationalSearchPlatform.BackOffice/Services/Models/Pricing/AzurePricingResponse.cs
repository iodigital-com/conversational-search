using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Pricing;

public record AzurePricingResponse
{
    [JsonPropertyName("BillingCurrency")]
    public string BillingCurrency { get; set; } = default!;

    [JsonPropertyName("CustomerEntityId")]
    public string CustomerEntityId { get; set; } = default!;

    [JsonPropertyName("CustomerEntityType")]
    public string CustomerEntityType { get; set; } = default!;

    [JsonPropertyName("NextPageLink")]
    public string NextPageLink { get; set; } = default!;

    [JsonPropertyName("Count")]
    public long Count { get; set; }

    [JsonPropertyName("Items")]
    public List<AzurePricingItem> Items { get; set; } = default!;
}

public record AzurePricingItem
{
    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = default!;

    [JsonPropertyName("tierMinimumUnits")]
    public decimal TierMinimumUnits { get; set; }

    [JsonPropertyName("retailPrice")]
    public decimal RetailPrice { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("armRegionName")]
    public string ArmRegionName { get; set; } = default!;

    [JsonPropertyName("location")]
    public string Location { get; set; } = default!;

    [JsonPropertyName("effectiveStartDate")]
    public DateTimeOffset EffectiveStartDate { get; set; }

    [JsonPropertyName("meterId")]
    public Guid MeterId { get; set; }

    [JsonPropertyName("meterName")]
    public string MeterName { get; set; } = default!;

    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = default!;

    [JsonPropertyName("skuId")]
    public string SkuId { get; set; } = default!;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = default!;

    [JsonPropertyName("skuName")]
    public string SkuName { get; set; } = default!;

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = default!;

    [JsonPropertyName("serviceId")]
    public string ServiceId { get; set; } = default!;

    [JsonPropertyName("serviceFamily")]
    public string ServiceFamily { get; set; } = default!;

    [JsonPropertyName("unitOfMeasure")]
    public string UnitOfMeasure { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("isPrimaryMeterRegion")]
    public bool IsPrimaryMeterRegion { get; set; }

    [JsonPropertyName("armSkuName")]
    public string ArmSkuName { get; set; } = default!;
}