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

// Explicit local bootstrap command; normal server startup does not grant roles.
if (builder.Configuration.GetValue<bool>("seed-admin"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<SecretariatDbContext>();
    var administrator = await AdministratorBootstrap.EnsureCreatedAsync(context);
    app.Logger.LogInformation("Administrator ready: {Name}, {Email}, ID {Id}",
        administrator.DisplayName, administrator.Email, administrator.Id);
    await app.DisposeAsync();
    return;
}


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();


