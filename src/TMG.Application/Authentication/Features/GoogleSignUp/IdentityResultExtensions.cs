using Microsoft.AspNetCore.Identity;

namespace TMG.Application.Authentication.Features.GoogleSignUp;

internal static class IdentityResultExtensions
{
    public static IReadOnlyDictionary<string, string[]> ToValidationDictionary(this IdentityResult result) =>
        result.Errors
            .GroupBy(GetPropertyName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray(), StringComparer.Ordinal);

    private static string GetPropertyName(IdentityError error) =>
        error.Code switch
        {
            nameof(IdentityErrorDescriber.DuplicateEmail) => nameof(GoogleSignUpCommand.IdToken),
            nameof(IdentityErrorDescriber.DuplicateUserName) => nameof(GoogleSignUpCommand.IdToken),
            nameof(IdentityErrorDescriber.InvalidEmail) => nameof(GoogleSignUpCommand.IdToken),
            nameof(IdentityErrorDescriber.InvalidUserName) => nameof(GoogleSignUpCommand.IdToken),
            nameof(IdentityErrorDescriber.LoginAlreadyAssociated) => nameof(GoogleSignUpCommand.IdToken),
            _ => string.Empty
        };
}
