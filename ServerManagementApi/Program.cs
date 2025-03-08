using Microsoft.EntityFrameworkCore;
using ServerManagementApi;
using ServerManagementApi.Models.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddTransient<AppSettings>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return configuration.Get<AppSettings>()!;
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var configuration = builder.Configuration;
    options.UseInMemoryDatabase(configuration.GetConnectionString("default")!);
});

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
