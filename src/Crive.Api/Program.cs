using Crive.Api.Hubs;
using Crive.Api.Infrastructure;
using Crive.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource)
        .UseSnakeCaseNamingConvention());

// Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

// Tenant & Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MachineService>();
builder.Services.AddScoped<RuleCompilationService>();

// SignalR
builder.Services.AddSignalR()
    .AddMessagePackProtocol()
    .AddStackExchangeRedis(redisConnection);

// Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "super_secret_key_that_should_be_long_enough_for_hmacsha256";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Crive",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Crive",
            ValidateLifetime = true
        };

        // Support SignalR Auth
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.AllowAnyOrigin() // Adjust in prod
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("DefaultPolicy");

app.UseAuthentication();

// Tenant middleware
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.MapHub<AgentHub>("/hubs/agent");
app.MapHub<DashboardHub>("/hubs/dashboard");

app.Run();
