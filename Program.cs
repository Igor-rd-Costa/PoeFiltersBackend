using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.Configure<AuthConfig>(
    builder.Configuration.GetSection("Auth"));

builder.Services.AddCors(c =>
{
    c.AddPolicy("Dev", conf =>
    {
        conf
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
    c.AddPolicy("Prod", conf =>
    {
        conf.WithOrigins("")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.AddHttpClient();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
builder.Services.AddAuthentication()
.AddCookie("Identity.Application", o =>
{
    o.Events.OnSigningIn = context =>
    {
        context.Properties.ExpiresUtc = DateTime.UtcNow.AddHours(8);
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
builder.Services.AddIdentityCore<User>()
.AddSignInManager<SignInManager<User>>()
.AddUserManager<UserManager<User>>()
.AddUserStore<UserStore>();

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddSingleton<FiltersService>();
builder.Services.AddSingleton<ItemsService>();
builder.Services.AddSingleton<WikiService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("Dev");
}
else
{
    app.UseCors("Prod");
}

app.UseHttpsRedirection();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
