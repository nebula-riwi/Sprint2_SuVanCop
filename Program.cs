using Microsoft.EntityFrameworkCore;
using SuVanCop.Data;
using SuVanCop.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configurar Kestrel para IPv4 y HTTPS


// Servicios
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PostgresDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSignalR();

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// Rutas MVC y SignalR
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<TurnHub>("/turnHub");

app.Run();