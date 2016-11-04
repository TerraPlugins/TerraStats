using System;
using System.Collections.Generic;
using System.Data;
using TShockAPI.DB;
using MySql.Data.MySqlClient;
using TShockAPI;

namespace TerraStats
{
    public class DBManager
    {
        private IDbConnection db;

        public List<TUser> Users = new List<TUser>();

        public DBManager(IDbConnection db)
        {
            this.db = db;

            var sqlCreator = new SqlTableCreator(db, (IQueryBuilder)new SqliteQueryCreator());
            sqlCreator.EnsureTableStructure(new SqlTable("Users",
                new SqlColumn("ID", MySqlDbType.Int32) { AutoIncrement = true, Primary = true },
                new SqlColumn("UserID", MySqlDbType.Int32),
                new SqlColumn("Name", MySqlDbType.Text),
                new SqlColumn("Deaths", MySqlDbType.Int32),
                new SqlColumn("MobKills", MySqlDbType.Int32),
                new SqlColumn("PvPKills", MySqlDbType.Int32),
                new SqlColumn("DmgGiven", MySqlDbType.Int32),
                new SqlColumn("DmgTaken", MySqlDbType.Int32)
                ));

            using (QueryResult result = db.QueryReader("SELECT * FROM Users"))
            {
                while (result.Read())
                {
                    Users.Add(new TUser(
                        result.Get<string>("Name"),
                        result.Get<int>("UserID"),
                        result.Get<int>("Deaths"),
                        result.Get<int>("MobKills"),
                        result.Get<int>("PvPKills"),
                        result.Get<int>("DmgGiven"),
                        result.Get<int>("DmgTaken")
                        ));
                }
            }
        }

        public void addUser(TUser user, TSPlayer ply)
        {
            try
            {
                bool exists = false;
                foreach (TUser usr in Users)
                {
                    if (usr.UserID == user.UserID)
                    {
                        // Player is already in database
                        exists = true;
                    }
                }

                if(exists == false)
                {
                    // Add it to database
                    db.Query("INSERT INTO Users (UserID, Name, Deaths, MobKills, PvPKills, DmgGiven, DmgTaken) VALUES (@0, @1, @2, @3, @4, @5, @6)",
                    user.UserID,
                    user.Name,
                    user.Deaths,
                    user.MobKills,
                    user.PvPKills,
                    user.DamageGiven,
                    user.DamageRecieved
                    );
                    // Add it to local list
                    Users.Add(user);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void updateUser(TUser user)
        {
            foreach(TUser usr in Users)
            {
                if(usr.UserID == user.UserID)
                {
                    try
                    {
                        db.Query("UPDATE Users SET Deaths=@0, MobKills=@1, PvPKills=@2, DmgGiven=@3, DmgTaken=@4 WHERE UserID=@5",
                        user.Deaths,
                        user.MobKills,
                        user.PvPKills,
                        user.DamageGiven,
                        user.DamageRecieved,
                        user.UserID
                        );
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    
                }
            }
        }
    }
}
