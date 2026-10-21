using PropertyHub.DataAccess.Context;
using PropertyHub.DataAccess.Entities;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Interfaces.IService;
using PropertyHub.Managers;
using PropertyHub.Services;
using PropertyHub.Services.Validators;
using PropertyHub.Models.JwtTokenService;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
// Configure JWT settings
var jwtSettings=builder.Configuration.GetSection("Jwt");
var tokenConfiguration=jwtSettings.Get<TokenConfiguration>();
builder.Services.AddSingleton(tokenConfiguration!);

// Add JWT Authentication (after AddIdentity so Bearer becomes the default scheme)
builder.Services
    .AddAuthentication(options =>
    {
        // AddIdentity sets the cookie scheme for authenticate/challenge, which a plain
        // AddAuthentication(defaultScheme) does not override, so set them explicitly.
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["AccessTokenSecret"]!)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Register services
builder.Services.AddScoped<IAppUserService, AppUserService>();
builder.Services.AddScoped<IAppUserManager, AppUserManager>();
builder.Services.AddScoped<RegisterValidator>();
builder.Services.AddScoped<LoginValidator>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IPropertyManager, PropertyManager>();
builder.Services.AddScoped<PropertyValidator>();
builder.Services.AddScoped<IFileUploadManager,FileUploadManager>();
builder.Services.AddScoped<IFavoriteManager,FavoriteManager>();
builder.Services.AddScoped<IFavoriteService,FavoriteService>();
builder.Services.AddScoped<IReviewManager,ReviewManager>();
builder.Services.AddScoped<IReviewService,ReviewService>();
builder.Services.AddScoped<ReviewValidator>();
builder.Services.AddScoped<IInquiryManager, InquiryManager>();
builder.Services.AddScoped<IInquiryService, InquiryService>();
builder.Services.AddScoped<CreateInquiryValidator>();
builder.Services.AddScoped<ResponseInquiryValidator>();
// Add Controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PropertyHub API",
        Version = "v1",
        Description = "API for Property Hub Application"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token from /api/auth/login (no 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PropertyHub API v1");
        c.RoutePrefix = string.Empty; // Makes Swagger the default page
    });
}
app.UseStaticFiles();
var uploadPath=Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","uploads","properties");
if (!Directory.Exists(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapControllers();

app.Run();