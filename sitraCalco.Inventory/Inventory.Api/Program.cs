using Microsoft.AspNetCore.Authentication.JwtBearer;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.IdentityModel.Tokens;
using Google.Cloud.SecretManager.V1;
using Inventory.Domain.Options;
using Inventory.Application;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var projectId = builder.Configuration["SecretManager:ProjectId"] ?? throw new InvalidOperationException("No se configuró SecretManager:ProjectId.");
    var secretId = builder.Configuration["SecretManager:SecretId"] ?? throw new InvalidOperationException("No se configuró SecretManager:SecretId.");
    var client = SecretManagerServiceClient.Create();
    var secretVersion = new SecretVersionName(projectId, secretId, "latest");
    var response = client.AccessSecretVersion(secretVersion);

    using var stream = new MemoryStream(response.Payload.Data.ToByteArray());

    builder.Configuration.AddJsonStream(stream);
    builder.Configuration.AddEnvironmentVariables();
}

const string CorsPolicy = "SitraCalcoCorsPolicy";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApplication();
builder.Services.AddOptions<GoogleCloudStorageOptions>()
    .BindConfiguration(GoogleCloudStorageOptions.SectionName)
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.BucketName)
            && !options.BucketName.Contains('/'),
        "GoogleCloudStorage:BucketName debe contener solamente el nombre del bucket.")
    .Validate(
        options => options.ProductImagePrefix is
            "sitracalco_inventory_dev/image/products"
            or "sitracalco_inventory_qa/image/products"
            or "sitracalco_inventory/image/products",
        "El prefijo de imágenes debe corresponder a DEV, QA o producción.")
    .Validate(
        options => options.SignedUrlExpirationMinutes is > 0 and <= 10080,
        "La duración de la Signed URL debe estar entre 1 minuto y 7 días.")
    .Validate(
        options => options.MaxImageSizeMb > 0,
        "El tamaño máximo de imagen debe ser mayor a cero.")
    .ValidateOnStart();
builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddSwaggerGen(options =>
{

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {

        Name = "Authorization",

        Type = SecuritySchemeType.Http,

        Scheme = "bearer",

        BearerFormat = "JWT",

        In = ParameterLocation.Header,

        Description = "Ingrese el token JWT."
    });


    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {

        [new OpenApiSecuritySchemeReference("Bearer", document)] = []

    });

});

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("No se encontró Jwt:Secret."); 
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("No se encontró Jwt:Issuer."); 
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("No se encontró Jwt:Audience."); 
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services

    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

    .AddJwtBearer(options =>

    {

        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {

            ValidateIssuer = true,

            ValidIssuer = jwtIssuer,

            ValidateAudience = true,

            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,

            IssuerSigningKey = jwtKey,

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero,

            NameClaimType = "userLogin",

            RoleClaimType = "nameRole"

        };

    });

builder.Services.AddAuthorization();


var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
