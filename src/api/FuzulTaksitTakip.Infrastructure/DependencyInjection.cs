using System.Security.Claims;
using System.Text;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Infrastructure.Auth;
using FuzulTaksitTakip.Infrastructure.Background;
using FuzulTaksitTakip.Infrastructure.Documents;
using FuzulTaksitTakip.Infrastructure.Email;
using FuzulTaksitTakip.Infrastructure.Identity;
using FuzulTaksitTakip.Infrastructure.Persistence;
using FuzulTaksitTakip.Infrastructure.Services;
using FuzulTaksitTakip.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FuzulTaksitTakip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.Configure<MobileSessionOptions>(configuration.GetSection(MobileSessionOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services
            .AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
        }

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token)
                            && context.Request.Cookies.TryGetValue("paydefteri_session", out var token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IMobileRefreshTokenService, MobileRefreshTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddScoped<IPlanAuthorization, PlanAuthorizationService>();
        services.AddHttpClient<IGeminiExpenseReceiptAnalyzer, GeminiExpenseReceiptAnalyzer>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GeminiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(45);
        });
        services.AddHttpClient<IOpenAiExpenseReceiptAnalyzer, OpenAiExpenseReceiptAnalyzer>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(45);
        });
        services.AddScoped<IExpenseReceiptAnalyzer, FallbackExpenseReceiptAnalyzer>();
        services.Configure<ReceiptStorageOptions>(configuration.GetSection(ReceiptStorageOptions.SectionName));
        services.AddSingleton<IReceiptStorage, LocalReceiptStorage>();
        services.AddSingleton<IPlanDocumentParser, PlanDocumentParser>();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));

        var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
        if (emailOptions.Enabled
            && !string.IsNullOrWhiteSpace(emailOptions.Smtp.Host)
            && !string.IsNullOrWhiteSpace(emailOptions.FromAddress))
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }

        services.AddSingleton<IInviteEmailService, InviteEmailService>();
        services.AddSingleton<IReminderEmailService, ReminderEmailService>();

        services.Configure<ReminderOptions>(configuration.GetSection(ReminderOptions.SectionName));
        services.AddHostedService<DailyReminderHostedService>();
        services.AddHostedService<ExpenseRecurrenceHostedService>();

        return services;
    }
}
