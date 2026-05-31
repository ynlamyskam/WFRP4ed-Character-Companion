using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Data.Seed;
using WFRP_Character_Companion.Data.Seed.Importers;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services;
using WFRP_Character_Companion.Services.ContentParsing;

namespace WFRP_Character_Companion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddRazorPages();
            builder.Services.AddScoped<TalentImporter>();
            builder.Services.AddScoped<TalentJsonParser>();
            builder.Services.AddScoped<TalentRulesService>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                db.Database.Migrate();

                var importer = scope.ServiceProvider.GetRequiredService<TalentImporter>();

                importer.Import("Data/Seed/Content/talents.json");

                SkillSeed.SeedSkills(db);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
