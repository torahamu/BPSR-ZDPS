using Dapper;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Text;

namespace BPSR_ZDPS.Database.Migrations
{
    public class NamespaceMigration_1 : BaseMigration
    {
        public NamespaceMigration_1()
        {
            Name = "Namespace Migration";
            Description = "Migrate from old namespace to the new";
            MinVersion = 1.0f;
            NewVersion = 1.1f;
            Progress = -1f;
        }

        public override bool RunMigration(DbConnection dbConn, SqliteTransaction tx)
        {
            var encounters = DB.LoadEncounterSummaries();

            foreach (var encounter in encounters)
            {
                var entityBlob = dbConn.QuerySingleOrDefault<EntityBlobTable>(DBSchema.Entities.SelectByEncounterId, new { EncounterId = encounter.EncounterId }, tx);
                using (var memStream = new MemoryStream(entityBlob.Data))
                {
                    using (var decompStream = new ZstdSharp.DecompressionStream(memStream))
                    {
                        using (var streamReader = new StreamReader(decompStream))
                        {
                            var txt = streamReader.ReadToEnd();
                            var sb = new StringBuilder(txt);
                            sb.Replace("BPSR-DeepsLib", "BPSR-ZDPSLib");
                            sb.Replace("BPSR_ZDPS.CombatStats2, BPSR-ZDPS", "BPSR_ZDPS.CombatStats, BPSR-ZDPS");

                            using (var memoryStream = new MemoryStream())
                            {
                                using (var compStream = new ZstdSharp.CompressionStream(memoryStream))
                                {
                                    using (var streamWriter = new StreamWriter(compStream, Encoding.UTF8, 1024, true))
                                    {
                                        streamWriter.Write(sb.ToString());
                                        streamWriter.Flush();
                                    }
                                    compStream.Flush();
                                }
                                memoryStream.Flush();

                                var blob = new EntityBlobTable();
                                blob.EncounterId = encounter.EncounterId;
                                blob.Data = memoryStream.ToArray();
                                var result = dbConn.Execute("UPDATE Entities SET Data = @Data WHERE EncounterId = @EncounterId", blob, tx);
                            }
                        }
                    }
                }
            }

            dbConn.Execute(DBSchema.DbData.Delete, tx);
            dbConn.Execute("INSERT INTO DbData (Version) SELECT (1.1)", tx);


            return true;
        }
    }
}
