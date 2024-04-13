using Deployf.Botf;
using RunBot2024.Models;
using RunBot2024.Services;
using SQLite;
//using Microsoft.Data.Sqlite;
//using SQLitePCL;

namespace RunBot2024
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
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
            });

            app.Run();
        }
    }
}
