using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace GearBuffs
{
    public class Database
    {
        private IDbConnection _db;

        public Database(IDbConnection db)
        {
            _db = db;
            var sqlCreator = new SqlTableCreator(_db,
                _db.GetSqlType() == SqlType.Sqlite
                    ? (IQueryBuilder)new SqliteQueryCreator()
                    : new MysqlQueryCreator());
            var table = new SqlTable("GearBuffs",
                new SqlColumn("ID", MySqlDbType.Int32) { AutoIncrement = true, Primary = true },
                new SqlColumn("Item", MySqlDbType.Int32),
                new SqlColumn("Buff", MySqlDbType.Int32),
                new SqlColumn("Duration", MySqlDbType.Int32),
                new SqlColumn("Held", MySqlDbType.String),
                new SqlColumn("Aura", MySqlDbType.String),
                new SqlColumn("Range", MySqlDbType.Int32));
            sqlCreator.EnsureTableStructure(table);
        }
        public static Database InitDb(string name)
        {
            IDbConnection db;
            if (TShock.Config.StorageType.ToLower() == "sqlite")
                db =
                    new SqliteConnection(string.Format("uri=file://{0},Version=3",
                        Path.Combine(TShock.SavePath, name + ".sqlite")));
            else if (TShock.Config.StorageType.ToLower() == "mysql")
            {
                try
                {
                    var host = TShock.Config.MySqlHost.Split(':');
                    db = new MySqlConnection
                    {
                        ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
                            host[0],
                            host.Length == 1 ? "3306" : host[1],
                            TShock.Config.MySqlDbName,
                            TShock.Config.MySqlUsername,
                            TShock.Config.MySqlPassword
                            )
                    };
                }
                catch (MySqlException x)
                {
                    TShock.Log.Error(x.ToString());
                    throw new Exception("MySQL not setup correctly.");
                }
            }
            else
                throw new Exception("Invalid storage type.");
            var database = new Database(db);
            return database;
        }
        public QueryResult QueryReader(string query, params object[] args)
        {
            return _db.QueryReader(query, args);
        }

        public int Query(string query, params object[] args)
        {
            return _db.Query(query, args);
        }

        public int AddGearBuff(GearBuff copy)
        {
            Query("INSERT INTO GearBuffs (Item, Buff, Duration, Held, Aura, Range) VALUES (@0, @1, @2, @3, @4, @5)",
                copy.item, copy.buff, copy.duration, copy.held, copy.aura, copy.range);
            using (var reader = QueryReader("SELECT max(ID) FROM GearBuffs"))
            {
                if (reader.Read())
                {
                    var id = reader.Get<int>("max(ID)");
                    return id;
                }
            }
            return -1;
        }
        public void DeleteGearBuff(int itemID)
        {
            Query("DELETE FROM GearBuffs WHERE ITEM = @0", itemID);
        }

        public void UpdateGearBuff(GearBuff update)
        {
            var query =
                string.Format(
                    "UPDATE JumpPads SET Item = {0}, Buff = {1}, Duration = {2}, Held = {3}, Aura = {4}, Range = {5}",
                    update.item, update.buff, update.duration, update.held, update.aura, update.range);
        }

        public void LoadGearBuffs(ref List<GearBuff> list)
        {
            using (var reader = QueryReader("SELECT * FROM GearBuffs"))
            {
                while (reader.Read())
                {
                    var id = reader.Get<int>("ID");
                    var item = reader.Get<int>("Item");
                    var buff = reader.Get<int>("Buff");
                    var duration = reader.Get<int>("Duration");
                    var held = reader.Get<string>("Held");
                    var aura = reader.Get<string>("Aura");
                    var range = reader.Get<int>("Range");

                    var gearBuff = new GearBuff(item, buff, duration, held, aura, range) {Id = id};
                    list.Add(gearBuff);
                }
            }
        }
    }
}
