using Capitec.Dispute.Application.Interfaces;
using Microsoft.OpenApi.Models;
using Capitec.Dispute.Application.Validators;
using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Infrastructure.Data;
using Capitec.Dispute.Infrastructure.Services;
using Capitec.Dispute.API.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text.Json;
using System.Text;
using System.Threading.RateLimiting;
using Capitec.Dispute.Domain.Entities;

// Bootstrap logger for startup errors before host is built
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

const string fileTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

// Replace default logging with Serilog
builder.Host.UseSerilog((context, services, cfg) => cfg
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()

    // Console — everything (useful during development)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

    // System folder — infrastructure logs (no UserType tag) + all errors
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            !e.Properties.ContainsKey("UserType") ||
            e.Level >= LogEventLevel.Error)
        .WriteTo.File(
            path: "logs/system/app-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: fileTemplate)
        .WriteTo.File(
            formatter: new CompactJsonFormatter(),
            path: "logs/system/app-json-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30))

);

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("DefaultConnection is not configured. Please set the connection string in appsettings.json or environment variables.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(60);
    })
);

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString,
        name: "sql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql" },
        timeout: TimeSpan.FromSeconds(10));

// Add Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ── JWT Authentication ──────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKeyString = jwtSettings["SecretKey"];

// Fail fast in production if the default insecure key is still in use
if (builder.Environment.IsProduction() &&
    (string.IsNullOrEmpty(secretKeyString) ||
     secretKeyString.Contains("your-") ||
     secretKeyString.Length < 32))
{
    throw new InvalidOperationException(
        "A secure JWT SecretKey must be set via the Jwt__SecretKey environment variable in production.");
}

var secretKey = Encoding.ASCII.GetBytes(secretKeyString ?? "your-secret-key-here-make-it-long");
var tokenExpirationMinutes = int.Parse(jwtSettings["TokenExpirationMinutes"] ?? "60");
var issuer = jwtSettings["Issuer"] ?? "Capitec";
var audience = jwtSettings["Audience"] ?? "DisputePortal";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ── Rate limiting (secondary layer — BFF is the primary) ───────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(15);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            JsonSerializer.Serialize(new { message = "Too many requests, please try again later." }),
            cancellationToken);
    };
});

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();
builder.Services.AddScoped<IValidator<RegisterUserRequestDto>, RegisterUserValidator>();
builder.Services.AddScoped<IValidator<LoginRequestDto>, LoginValidator>();
builder.Services.AddScoped<IValidator<CreateDisputeRequestDto>, CreateDisputeValidator>();

// Add AutoMapper
var assemblies = AppDomain.CurrentDomain.GetAssemblies();
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(assemblies);
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Capitec Dispute Portal API",
        Version = "v1",
        Description = "Backend API for the Capitec Transaction Dispute Portal. Handles customer authentication, dispute submissions, employee management, and transaction lookups."
    });

    // Include XML doc comments (summaries on controller actions)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add JWT bearer auth to the Swagger UI so protected endpoints can be tested
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token. You can obtain one from POST /api/auth/login or POST /api/employee/login."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS — locked to the BFF origin only ───────────────────────────────────
var bffOrigin = builder.Configuration["BffOrigin"] ?? "http://localhost:4000";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBff", corsPolicyBuilder =>
    {
        corsPolicyBuilder
            .WithOrigins(bffOrigin)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add application services
builder.Services.AddSingleton<IActivityLogger, ActivityLogger>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmployeeAuthService, EmployeeAuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDisputeService, DisputeService>();
builder.Services.AddHttpClient<ITranslationService, TranslationService>();

var app = builder.Build();

// Apply migrations and seed roles
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Customer", "Employee" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed default employees for testing
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    var defaultEmployees = new[]
    {
        new { Email = "employee1@capitec.co.za", Password = "Employee@123!", FirstName = "Jane",  LastName = "Smith",   Phone = "0110000001", Department = "Disputes", EmployeeCode = "EMP-000001" },
        new { Email = "employee2@capitec.co.za", Password = "Employee@456!", FirstName = "Johan", LastName = "DuToit",  Phone = "0110000002", Department = "Disputes", EmployeeCode = "EMP-000002" },
    };

    foreach (var emp in defaultEmployees)
    {
        if (await userManager.FindByEmailAsync(emp.Email) != null)
            continue;

        var identityUser = new User
        {
            Email        = emp.Email,
            UserName     = emp.Email,
            FirstName    = emp.FirstName,
            LastName     = emp.LastName,
            PhoneNumber  = emp.Phone,
            AccountNumber = Random.Shared.NextInt64(1000000000L, 9999999999L).ToString()
        };

        var result = await userManager.CreateAsync(identityUser, emp.Password);
        if (!result.Succeeded)
        {
            Log.Warning("Seed employee {Email} could not be created: {Errors}",
                emp.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            continue;
        }

        await userManager.AddToRoleAsync(identityUser, "Employee");

        db.Employees.Add(new Employee
        {
            Email        = emp.Email,
            FirstName    = emp.FirstName,
            LastName     = emp.LastName,
            Phone        = emp.Phone,
            Department   = emp.Department,
            PasswordHash = identityUser.PasswordHash ?? string.Empty,
            EmployeeCode = emp.EmployeeCode
        });
    }

    await db.SaveChangesAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Capitec Dispute Portal API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Capitec Dispute Portal API";
    });
}
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
        ex != null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Debug
                : LogEventLevel.Information;
});
app.UseHttpsRedirection();
app.UseCors("AllowBff");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = 200,
        [HealthStatus.Degraded] = 200,
        [HealthStatus.Unhealthy] = 503
    },
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.ToString()
            }),
            totalDuration = report.TotalDuration.ToString()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

try
{
    Log.Information("Starting Capitec Dispute Portal API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
