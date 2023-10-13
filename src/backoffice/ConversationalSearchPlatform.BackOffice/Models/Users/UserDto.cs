namespace ConversationalSearchPlatform.BackOffice.Models.Users;

public record UserDto(Guid? Id, string Name, string Email, string TenantId, string TenantName);