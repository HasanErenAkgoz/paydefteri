using System.Text.RegularExpressions;
using PayDefteri.Application.SpendingAnalysis;

namespace PayDefteri.Api.Tests;

/// <summary>
/// Üretimde istekler önce nginx'ten geçiyor. Proxy sınırı API'nin altına düşerse
/// kullanıcıya vaat edilen dosya boyutu API'ye hiç ulaşmadan ham 413 ile kesilir —
/// bu testler katmanların birbirinden kaymasını engeller.
/// </summary>
public sealed class UploadLimitConsistencyTests
{
    private static readonly Regex NginxBodyLimit = new(
        @"client_max_body_size\s+(\d+)m\s*;", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex RequestSizeLimitAttribute = new(
        @"\[RequestSizeLimit\((\d+)\s*\*\s*1024\s*\*\s*1024\)\]", RegexOptions.CultureInvariant);

    [Fact]
    public void Nginx_body_limit_stays_above_every_api_request_size_limit()
    {
        var nginxMegabytes = ReadNginxBodyLimitMegabytes();
        var apiLimits = ReadControllerRequestSizeLimits();

        apiLimits.Should().NotBeEmpty("controller'larda [RequestSizeLimit] bekleniyor");

        var largestApiLimit = apiLimits.Max(x => x.Megabytes);
        nginxMegabytes.Should().BeGreaterThan(
            largestApiLimit,
            "nginx client_max_body_size, en yüksek [RequestSizeLimit] değerinin üzerinde kalmalı; "
                + $"aksi halde {largestApiLimit} MB kabul ettiğini söyleyen uç noktalar proxy'de kesilir");
    }

    [Fact]
    public void Statement_upload_endpoints_accept_the_size_their_validator_advertises()
    {
        var apiLimits = ReadControllerRequestSizeLimits();
        var validatorMegabytes = UploadSpendingStatementCommandValidator.MaxFileSize / (1024 * 1024);

        foreach (var endpoint in new[] { "SpendingAnalysisController", "ExpensesController" })
        {
            var limit = apiLimits
                .Where(x => x.Controller == endpoint)
                .Select(x => x.Megabytes)
                .DefaultIfEmpty(0)
                .Max();

            limit.Should().BeGreaterThan(
                validatorMegabytes,
                $"{endpoint} multipart ek yükü için doğrulayıcı sınırının ({validatorMegabytes} MB) üzerinde "
                    + "bir istek sınırı tanımlamalı, yoksa tam sınırdaki dosya doğrulayıcıya hiç ulaşmaz");
        }
    }

    private static int ReadNginxBodyLimitMegabytes()
    {
        var path = Path.Combine(FindRepositoryRoot(), "src", "web", "deploy", "nginx.conf");
        File.Exists(path).Should().BeTrue($"nginx yapılandırması bulunmalı: {path}");

        var match = NginxBodyLimit.Match(File.ReadAllText(path));
        match.Success.Should().BeTrue("nginx.conf içinde client_max_body_size tanımlı olmalı");
        return int.Parse(match.Groups[1].Value);
    }

    private static List<(string Controller, int Megabytes)> ReadControllerRequestSizeLimits()
    {
        var controllersPath = Path.Combine(
            FindRepositoryRoot(), "src", "api", "PayDefteri.Api", "Controllers");

        return Directory.EnumerateFiles(controllersPath, "*.cs")
            .SelectMany(file => RequestSizeLimitAttribute
                .Matches(File.ReadAllText(file))
                .Select(match => (
                    Controller: Path.GetFileNameWithoutExtension(file),
                    Megabytes: int.Parse(match.Groups[1].Value))))
            .ToList();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PayDefteri.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("testler repo kökünü bulabilmeli (PayDefteri.sln)");
        return directory!.FullName;
    }
}
