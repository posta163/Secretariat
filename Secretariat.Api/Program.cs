using Microsoft.EntityFrameworkCore;
using Secretariat.Api.Data;
using Secretariat.Api.Services.Storage;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SecretariatDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));



builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddScoped<IFileStorage, LocalFileStorage>();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


