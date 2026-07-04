namespace TMG.Domain.Common.Notifications;

/// <summary>Renders a tenancy agreement as a self-contained HTML document for archival + delivery.</summary>
public interface ITenancyAgreementRenderer
{
    string Render(TenancyAgreementModel model);
}
