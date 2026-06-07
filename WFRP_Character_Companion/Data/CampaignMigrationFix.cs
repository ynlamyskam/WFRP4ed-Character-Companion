using Microsoft.EntityFrameworkCore;

namespace WFRP_Character_Companion.Data
{
    /// <summary>
    /// Ensures campaign tables exist (e.g. after an empty AddCampaigns migration was applied).
    /// </summary>
    public static class CampaignMigrationFix
    {
        private const string CampaignsMigrationId = "20260604181449_AddCampaigns";

        public static void ApplyIfNeeded(ApplicationDbContext db)
        {
            if (TableExists(db, "Campaigns"))
                return;

            if (db.Database.GetAppliedMigrations().Contains(CampaignsMigrationId))
            {
                db.Database.ExecuteSqlRaw(
                    "DELETE FROM __EFMigrationsHistory WHERE MigrationId = {0}",
                    CampaignsMigrationId);
            }

            db.Database.Migrate();
        }

        private static bool TableExists(ApplicationDbContext db, string tableName)
        {
            var connection = db.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;
            if (shouldClose)
                connection.Open();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
                var param = command.CreateParameter();
                param.ParameterName = "$name";
                param.Value = tableName;
                command.Parameters.Add(param);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
            finally
            {
                if (shouldClose)
                    connection.Close();
            }
        }
    }
}
