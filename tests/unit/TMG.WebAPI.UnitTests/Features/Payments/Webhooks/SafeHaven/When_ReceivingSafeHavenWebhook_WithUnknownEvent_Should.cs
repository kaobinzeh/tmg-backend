using System.Text.Json;
using TMG.WebAPI.Features.Payments.Webhooks.SafeHaven;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Payments.Webhooks.SafeHaven;

public sealed class When_ReceivingSafeHavenWebhook_WithUnknownEvent_Should
{
    [Fact]
    public async Task ThrowJsonException()
    {
        var context = new PaymentsControllerTestContext();
        var sut = new SafeHavenWebhooksController(
            context.CreateSafeHavenAccountCreditWebhookHandler(),
            context.CreateSafeHavenAccountDebitWebhookHandler(),
            context.CreateSafeHavenVirtualAccountTransferWebhookHandler());

        await Should.ThrowAsync<JsonException>(() =>
            sut.Handle(
                new SafeHavenWebhookRequest("unsupported.event", JsonSerializer.SerializeToElement(new { foo = "bar" })),
                CancellationToken.None));
    }
}
