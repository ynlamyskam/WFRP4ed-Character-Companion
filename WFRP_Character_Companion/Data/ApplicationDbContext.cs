using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Character> Characters => Set<Character>();
        public DbSet<Skill> Skills { get; set; }
        public DbSet<Talent> Talents { get; set; }
        public DbSet<CharacterSkill> CharacterSkills { get; set; }
        public DbSet<CharacterTalent> CharacterTalents { get; set; }
        public DbSet<TalentTestEffect> TalentTestEffects { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TalentTestEffect>()
                .HasKey(x => new { x.TalentId, x.SkillName });
        }
    }
}
