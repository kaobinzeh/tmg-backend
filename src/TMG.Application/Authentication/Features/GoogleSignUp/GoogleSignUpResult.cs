namespace TMG.Application.Authentication.Features.GoogleSignUp;

public sealed record GoogleSignUpResult(
    GoogleSignUpStatus Status,
    string? Email = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public enum GoogleSignUpStatus
{
    Accepted = 1,
    InvalidGoogleToken = 2,
    DuplicateEmail = 3,
    DuplicateGoogleAccount = 4,
    ValidationFailed = 5
}
