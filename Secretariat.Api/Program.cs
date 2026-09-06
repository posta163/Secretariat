using Microsoft.EntityFrameworkCore;
using Secretariat.Api.CurrentUser;
using Secretariat.Api.Data;
using Secretariat.Api.Storage;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SecretariatDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));



builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, LocalCurrentUserService>();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


