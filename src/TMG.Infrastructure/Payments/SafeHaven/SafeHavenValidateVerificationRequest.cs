namespace TMG.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenValidateVerificationRequest(
    string IdentityId,
    string Type,
    string Otp);
