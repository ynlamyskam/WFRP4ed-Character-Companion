using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Models.Import;
using WFRP_Character_Companion.Services;
using WFRP_Character_Companion.Services.CharacterCreation;
using WFRP_Character_Companion.Services.Content;

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
            builder.Services.AddScoped<TalentRulesService>();
            builder.Services.AddScoped<CharacterDraftService>();
            builder.Services.AddScoped<CreationContentService>();
            builder.Services.AddScoped<IContentImporter<Talent>>(sp =>
            {
                var db = sp.GetRequiredService<ApplicationDbContext>();

                var parser = new JsonContentParser<TalentImportDto, Talent>(dto =>
                {
                    return new Talent
                    {
                        Name = dto.Name,
                        Description = dto.Description,
                        MaxLevelType = dto.MaxLevelType,
                        FixedMaxLevel = dto.FixedMaxLevel,
                        MaxLevelAttributes = dto.MaxLevelAttributes ?? [],
                        TestEffects = dto.Tests?.Select(x => new TalentTestEffect
                        {
                            SkillName = x.Skill,
                            Condition = x.Condition,
                            BonusPerLevelAbove1 = x.BonusPerLevelAbove1
                        }).ToList() ?? []
                    };
                });

                return new ContentImporter<TalentImportDto, Talent>(db, parser);
            });

            builder.Services.AddScoped<IContentImporter<Skill>>(sp =>
            {
                var db = sp.GetRequiredService<ApplicationDbContext>();

                var parser = new JsonContentParser<SkillImportDto, Skill>(dto =>
                {
                    return new Skill
                    {
                        Name = dto.Name,
                        IsAdvanced = dto.IsAdvanced,
                        HasSpecialization = dto.HasSpecialization,
                        GoverningAttribute = dto.GoverningAttribute
                    };
                });

                return new ContentImporter<SkillImportDto, Skill>(db, parser);
            });

            builder.Services.AddScoped<IContentImporter<Origin>>(sp =>
            {
                var db = sp.GetRequiredService<ApplicationDbContext>();
                var parser = new JsonContentParser<OriginImportDto, Origin>(dto =>
                {
                    var package = new OriginPackage
                    {
                        Skills = dto.Package.Skills?.Select(s =>
                        {
                            var type = s.Type ?? (s.Skill != null ? GrantType.Fixed : GrantType.Choice);
                            return new SkillGrant
                            {

                                Type = type,
                                Choose = s.Choose ?? 0,
                                Skill = s.Skill == null ? null : new SkillRef { Name = s.Skill.Name, Specialization = s.Skill.Specialization },
                                Options = s.Options?.Select(o => new SkillRef { Name = o.Name, Specialization = o.Specialization }).ToList() ?? []
                            };
                        }).ToList() ?? [],
                        Talents = dto.Package.Talents?.Select(t =>
                        {
                            var type = t.Type ?? (t.Talent != null ? TalentGrantType.Fixed : TalentGrantType.Choice);
                            return new TalentGrant
                            {
                                Type = type,
                                Choose = t.Choose ?? 0,
                                Count = t.Count ?? 0,
                                Talent = t.Talent == null ? null : new TalentRef { Name = t.Talent.Name, Specialization = t.Talent.Specialization },
                                Options = t.Options?.Select(o => new TalentRef { Name = o.Name, Specialization = o.Specialization }).ToList() ?? []
                            };
                        }).ToList() ?? []
                    };
                    
                    return new Origin
                    {
                        Race = dto.Race,
                        Name = dto.Name,
                        PackageJson = JsonSerializer.Serialize(package, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = {new JsonStringEnumConverter()}})
                    };
                });
                return new ContentImporter<OriginImportDto, Origin>(db, parser);
            });
       
            builder.Services.AddScoped<IContentImporter<Models.Profession>>(sp =>
            {
                var db = sp.GetRequiredService<ApplicationDbContext>();

                var parser = new JsonContentParser<object, Models.Profession>(dto =>
                {
                    return null!; 
                });

                return new ContentImporter<object, Models.Profession>(db, parser);
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                db.Database.Migrate();
                CampaignMigrationFix.ApplyIfNeeded(db);

                var talentImporter = scope.ServiceProvider.GetRequiredService<IContentImporter<Talent>>();
                talentImporter.Import("Data/Seed/Content/talents.json");

                var skillImporter = scope.ServiceProvider.GetRequiredService<IContentImporter<Skill>>();
                skillImporter.Import("Data/Seed/Content/skills.json");

                var originImporter = scope.ServiceProvider.GetRequiredService<IContentImporter<Origin>>();
                //originImporter.Import("Data/Seed/Content/origins.json");

                var contentDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Seed", "Content");
                var originFiles = Directory.GetFiles(contentDir, "origins*.json")
                    .Where(f => !f.EndsWith("origins.combined.json", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f)
                    .ToArray();

                var combined = new List<OriginImportDto>();
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                jsonOptions.Converters.Add(new JsonStringEnumConverter());

                foreach (var f in originFiles)
                {
                    var txt = File.ReadAllText(f);
                    var list = JsonSerializer.Deserialize<List<OriginImportDto>>(txt, jsonOptions);
                    if (list != null)
                        combined.AddRange(list);
                }


                var deduped = combined
                    .GroupBy(o => (Race: o.Race ?? string.Empty, Name: o.Name ?? string.Empty))
                    .Select(g => g.First())
                    .ToList();

                var combinedPath = Path.Combine(contentDir, "origins.combined.json");
                File.WriteAllText(combinedPath, JsonSerializer.Serialize(deduped, new JsonSerializerOptions { WriteIndented = true }));

                originImporter.Import(combinedPath);

                try { File.Delete(combinedPath); } catch { /* ignore */ }
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
