namespace TMG.Domain.Common.Notifications;

/// <summary>Renders a formal rent-due notice letter as a self-contained HTML document for archival + delivery.</summary>
public interface INoticeLetterRenderer
{
    string Render(NoticeLetterModel model);
}
