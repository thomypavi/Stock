using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Stock.Models;

namespace Stock
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Leer la cadena de conexión
            var connectionString = builder.Configuration.GetConnectionString("MiConexion")
                ?? throw new Exception("❌ No se encontró la cadena de conexión 'MiConexion' en appsettings.json");

            // Agregar servicios MVC
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("MiConexion")));

            builder.Services.AddControllersWithViews();


            var app = builder.Build();

            // Middleware
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Login}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
