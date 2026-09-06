using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PayDefteri.Api.Tests.Infrastructure;
using PayDefteri.Application.Auth;
using PayDefteri.Application.Common.Interfaces;

namespace PayDefteri.Api.Tests;

[Collection("Api")]
public sealed class EmailVerificationPositiveNegativeTests
{
    private readonly ApiFixture _fixture;

    public EmailVerificationPositiveNegativeTests(ApiFixture fixture) => _fixture = fixture;

    /// <summary>Records the token that would have been mailed to the user.</summary>
    private sealed class RecordingVerificationService : IEmailVerificationService
    {
        public List<EmailVerificationRequest> Sent { get; } = new();

        public Task<EmailVerificationResult> SendVerificationAsync(
            EmailVerificationRequest request,
            CancellationToken cancellationToken = default)
        {
            Sent.Add(request);
            return Task.FromResult(new EmailVerificationResult(Sent: true, Configured: true));
        }
    }

    /// <summary>A transport that reports itself configured, which is what turns verification on.</summary>
    private sealed class ConfiguredEmailSender : IEmailSender
    {
        public bool IsConfigured => true;

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private (TestClient Api, RecordingVerificationService Mail) CreateClient()
    {
        var mail = new RecordingVerificationService();
        var factory = _fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender, ConfiguredEmailSender>();
                services.RemoveAll<IEmailVerificationService>();
                services.AddSingleton<IEmailVerificationService>(mail);
            }));

        return (new TestClient(factory.CreateClient()), mail);
    }

    [Fact]
    public async Task Positive_registration_sends_a_verification_link_and_the_token_confirms_the_address()
    {
        var (api, mail) = CreateClient();
        var email = $"verify_{Guid.NewGuid():N}@example.com";

        await api.RegisterAndLoginAsync(email);

        var (_, unverified) = await api.GetAsync<UserProfileDto>("/api/auth/me");
        unverified!.EmailConfirmed.Should().BeFalse();
        mail.Sent.Should().ContainSingle(x => x.ToEmail == email);

        var confirm = await api.PostAsync<object>("/api/auth/verify-email", new
        {
            userId = mail.Sent[0].UserId,
            token = mail.Sent[0].Token,
        });
        confirm.Response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var (_, verified) = await api.GetAsync<UserProfileDto>("/api/auth/me");
        verified!.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Positive_resend_issues_a_fresh_link_while_the_address_is_unverified()
    {
        var (api, mail) = CreateClient();
        await api.RegisterAndLoginAsync($"resend_{Guid.NewGuid():N}@example.com");

        var (response, _) = await api.PostAsync<object>("/api/auth/verify-email/resend", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        mail.Sent.Should().HaveCount(2);
    }

    [Fact]
    public async Task Negative_a_tampered_token_does_not_confirm_the_address()
    {
        var (api, mail) = CreateClient();
        await api.RegisterAndLoginAsync($"badtoken_{Guid.NewGuid():N}@example.com");

        var (response, _) = await api.PostAsync<object>("/api/auth/verify-email", new
        {
            userId = mail.Sent[0].UserId,
            token = mail.Sent[0].Token + "tampered",
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var (_, profile) = await api.GetAsync<UserProfileDto>("/api/auth/me");
        profile!.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task Negative_an_unverified_owner_cannot_send_plan_invites()
    {
        var (api, _) = CreateClient();
        await api.RegisterAndLoginAsync($"invite_{Guid.NewGuid():N}@example.com");
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api, "Davet planı");

        var (response, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/invites", new
        {
            email = $"davetli_{Guid.NewGuid():N}@example.com",
            partnerId = partners[1].Id,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
