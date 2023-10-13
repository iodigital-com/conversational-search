namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record RedeemResult(RedeemResultStatus Status)
{

    public RedeemResultStatus Status { get; set; } = Status;
}

public enum RedeemResultStatus
{
    SUCCESS,
    ALREADY_REDEEMED,
    FAILURE,
    NOT_FOUND,
    OUTSIDE_EXPIRATION_DATE
}