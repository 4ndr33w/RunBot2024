using Deployf.Botf;
using Microsoft.EntityFrameworkCore;
using RunBot2024.DbContexts;
using RunBot2024.Models;
using RunBot2024.Services;
using RunBot2024.Services.Interfaces;
using SQLite;

namespace RunBot2024
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("NpgConnection");

            builder.Services.AddDbContext<NpgDbContext>(op => op.UseNpgsql(connectionString));

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddLogging();

            var app = builder.Build();

            //Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            BotfProgram.StartBot(args, onConfigure: (svc, cfg) =>
            {
                var sqLiteConnectionString = builder.Configuration.GetConnectionString("SqliteConnection");
                var db = new SQLiteConnection(sqLiteConnectionString);

                db.CreateTable<User>();

                svc.AddSingleton(db.Table<User>());
                svc.AddSingleton(db);

                svc.AddSingleton<IBotUserService, UserService>();

                ///////////////////////////////

                svc.AddLogging();
                svc.AddScoped<IRivalService, RivalService>(provider => new RivalService(builder.Configuration));
                svc.AddSingleton<ICompanyService, CompanyService>(provider => new CompanyService(builder.Configuration));
                svc.AddScoped<ILogService, LogService>(provider => new LogService(builder.Configuration));
            });

            app.Run();
        }
    }
}
