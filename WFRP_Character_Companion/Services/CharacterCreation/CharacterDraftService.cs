using Microsoft.EntityFrameworkCore;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Services.CharacterCreation
{
    public class CharacterDraftService(ApplicationDbContext db)
    {
        public async Task<CharacterDraft?> GetActiveDraftAsync(string userId)
        {
            return await db.CharacterDrafts
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<CharacterDraft> GetOrCreateActiveDraftAsync(string userId)
        {
            var draft = await GetActiveDraftAsync(userId);
            if (draft != null)
                return draft;

            draft = new CharacterDraft
            {
                UserId = userId,
                Step = CharacterCreationStep.Race,
                Experience = 0
            };
            db.CharacterDrafts.Add(draft);
            await db.SaveChangesAsync();
            return draft;
        }

        /// <summary>
        /// Usuwa stare drafty i tworzy świeży — wywoływane przy starcie nowej postaci.
        /// </summary>
        public async Task<CharacterDraft> StartNewDraftAsync(string userId)
        {
            var existing = await db.CharacterDrafts.Where(x => x.UserId == userId).ToListAsync();
            if (existing.Count > 0)
                db.CharacterDrafts.RemoveRange(existing);

            var draft = new CharacterDraft
            {
                UserId = userId,
                Step = CharacterCreationStep.Race,
                Experience = 0
            };
            db.CharacterDrafts.Add(draft);
            await db.SaveChangesAsync();
            return draft;
        }
    }
}
