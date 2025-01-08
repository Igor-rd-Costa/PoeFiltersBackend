var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.Configure<MongoDbConfig>(
    builder.Configuration.GetSection("PoEFiltersMongoDB"));

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
builder.Services.AddSingleton<FiltersService>();
builder.Services.AddSingleton<ItemsService>();
builder.Services.AddSingleton<WikiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors("Dev");
}
else
{
    app.UseCors("Prod");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
