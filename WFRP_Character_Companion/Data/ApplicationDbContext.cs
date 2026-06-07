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

        public DbSet<CharacterDraft> CharacterDrafts { get; set; }
        public DbSet<Origin> Origins { get; set; }

        public DbSet<Campaign> Campaigns => Set<Campaign>();
        public DbSet<CampaignMember> CampaignMembers => Set<CampaignMember>();
        public DbSet<CampaignCharacter> CampaignCharacters => Set<CampaignCharacter>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TalentTestEffect>()
                .HasKey(x => new { x.TalentId, x.SkillName });

            modelBuilder.Entity<CampaignMember>()
                .HasKey(x => new { x.CampaignId, x.UserId });

            modelBuilder.Entity<CampaignCharacter>()
                .HasKey(x => new { x.CampaignId, x.CharacterId });

            modelBuilder.Entity<Campaign>()
                .HasOne(c => c.Owner)
                .WithMany()
                .HasForeignKey(c => c.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CampaignMember>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CampaignMember>()
                .HasOne(m => m.Campaign)
                .WithMany(c => c.Members)
                .HasForeignKey(m => m.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CampaignCharacter>()
                .HasOne(cc => cc.Character)
                .WithMany()
                .HasForeignKey(cc => cc.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CampaignCharacter>()
                .HasOne(cc => cc.Campaign)
                .WithMany(c => c.CampaignCharacters)
                .HasForeignKey(cc => cc.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
