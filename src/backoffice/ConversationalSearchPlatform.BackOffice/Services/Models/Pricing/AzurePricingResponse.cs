using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Pricing;

public record AzurePricingResponse
{
    [JsonPropertyName("BillingCurrency")]
    public string BillingCurrency { get; set; }

    [JsonPropertyName("CustomerEntityId")]
    public string CustomerEntityId { get; set; }

    [JsonPropertyName("CustomerEntityType")]
    public string CustomerEntityType { get; set; }

    [JsonPropertyName("NextPageLink")]
    public string NextPageLink { get; set; }

    [JsonPropertyName("Count")]
    public long Count { get; set; }

    [JsonPropertyName("Items")]
    public List<AzurePricingItem> Items { get; set; }
}

public record AzurePricingItem
{
    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; }

    [JsonPropertyName("tierMinimumUnits")]
    public decimal TierMinimumUnits { get; set; }

    [JsonPropertyName("retailPrice")]
    public decimal RetailPrice { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("armRegionName")]
    public string ArmRegionName { get; set; }

    [JsonPropertyName("location")]
    public string Location { get; set; }

    [JsonPropertyName("effectiveStartDate")]
    public DateTimeOffset EffectiveStartDate { get; set; }

    [JsonPropertyName("meterId")]
    public Guid MeterId { get; set; }

    [JsonPropertyName("meterName")]
    public string MeterName { get; set; }

    [JsonPropertyName("productId")]
    public string ProductId { get; set; }

    [JsonPropertyName("skuId")]
    public string SkuId { get; set; }

    [JsonPropertyName("productName")]
    public string ProductName { get; set; }

    [JsonPropertyName("skuName")]
    public string SkuName { get; set; }

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; }

    [JsonPropertyName("serviceId")]
    public string ServiceId { get; set; }

    [JsonPropertyName("serviceFamily")]
    public string ServiceFamily { get; set; }

    [JsonPropertyName("unitOfMeasure")]
    public string UnitOfMeasure { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("isPrimaryMeterRegion")]
    public bool IsPrimaryMeterRegion { get; set; }

    [JsonPropertyName("armSkuName")]
    public string ArmSkuName { get; set; }
}