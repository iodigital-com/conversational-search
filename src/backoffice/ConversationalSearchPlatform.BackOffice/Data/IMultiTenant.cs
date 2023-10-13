namespace ConversationalSearchPlatform.BackOffice.Data;

public interface IMultiTenant
{
    public string TenantId { get; set; }
}