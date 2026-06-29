namespace TMG.Domain.Authentication.Services;

public interface IUserAgentParserService
{
    UserAgentInfo Parse(string userAgent);
}
