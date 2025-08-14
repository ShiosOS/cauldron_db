using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cauldron.Server.Data;
using Cauldron.Server.Dtos;
using Cauldron.Server.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var pg = $"Host={Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost"};" +
         $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"};" +
         $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres"};" +
         $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres"};" +
         $"Database={Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "cauldron"}";

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(pg));
builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.IdentityModel", LogLevel.Debug);

var issuer  = Environment.GetEnvironmentVariable("JWT_ISSUER")   ?? "cauldron-server";
var audience= Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "cauldron-api";
var secret  = Environment.GetEnvironmentVariable("JWT_SECRET")   ?? "CHANGE_ME_LONG";
var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

builder.Services
    .AddIdentityCore<AppUser>(o =>
    {
        o.SignIn.RequireConfirmedEmail = false;
        o.Password.RequiredLength = 8;
        o.Password.RequireDigit = true;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // set true in prod behind HTTPS
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = issuer,
            ValidateAudience         = true,
            ValidAudience            = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = key,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => new { ok = true, service = "cauldron-api" });

app.MapPost("/auth/register",
    async (RegisterRequest req, [FromServices] UserManager<AppUser> users) =>
    {
        var user = new AppUser { UserName = req.Email, Email = req.Email };
        var result = await users.CreateAsync(user, req.Password);
        return !result.Succeeded
            ? Results.BadRequest(result.Errors.Select(e => e.Description))
            : Results.Ok();
    });

app.MapPost("/auth/login",
    async (LoginRequest req,
        [FromServices] SignInManager<AppUser> signIn,
        [FromServices] UserManager<AppUser> users) =>
{
    var user = await users.FindByEmailAsync(req.Email);
    if (user is null) return Results.Unauthorized();

    var check = await signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
    if (!check.Succeeded) return Results.Unauthorized();

    var claims = new[]
    {
        new Claim("sub", user.Id.ToString()),
        new Claim("email", user.Email ?? ""),
    };

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        notBefore: DateTime.UtcNow,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { access_token = jwt, token_type = "Bearer", expires_in = 8 * 3600 });
});

app.MapGet("/whoami", (ClaimsPrincipal user) =>
{
    var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value; // mapped from "sub"
    var email = user.FindFirst(ClaimTypes.Email)?.Value;        // mapped from "email"
    return new { sub, email };
}).RequireAuthorization();


var port = int.Parse(Environment.GetEnvironmentVariable("CAULDRON_SERVER_PORT") ?? "5180");
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
