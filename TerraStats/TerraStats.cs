using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace TerraStats
{
    [ApiVersion(1, 25)]
    public class TerraStats : TerrariaPlugin
    {
        #region Info
        public override string Name { get { return "TerraStats"; } }
        public override string Author { get { return "Ryozuki"; } }
        public override string Description { get { return "A stats gatherer plugin"; } }
        public override Version Version { get { return new Version(1, 0, 0, 25); } }
        #endregion

        public static IDbConnection Db { get; private set; }
        public static DBManager DbManager { get; private set; }

        public TerraStats(Main game) : base(game)
        {

        }

        #region Initialize
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.NetGetData.Register(this, onGetData);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.ServerLeave.Deregister(this, onLeave);
            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += OnPlayerLogin;
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.NetGetData.Deregister(this, onGetData);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
                ServerApi.Hooks.ServerLeave.Deregister(this, onLeave);
                TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= OnPlayerLogin;
            }
        }

        private void onGetData(GetDataEventArgs e)
        {
            if (e.Handled)
                return;

            TSPlayer sender = TShock.Players[e.Msg.whoAmI];

            if (sender == null)
                return;

            switch (e.MsgID)
            {
                case PacketTypes.PlayerKillMe:
                    using (var reader = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length)))
                    {
                        byte victimid = reader.ReadByte();
                        byte hitdirr = reader.ReadByte();
                        short damagee = reader.ReadInt16();
                        bool pvp = reader.ReadBoolean();
                        string deathtexte = reader.ReadString();

                        TSPlayer victim = TShock.Players[victimid];

                        foreach(TUser user in DbManager.Users)
                        {
                            if(user.UserID == victim.User.ID)
                            {
                                if (victim.IsLoggedIn)
                                {
                                    user.Deaths += 1;
                                    if (pvp)
                                    {
                                        foreach(TUser usr in DbManager.Users)
                                        {
                                            if(usr.UserID == user.Killer.User.ID)
                                            {
                                                usr.PvPKills += 1;
                                                DbManager.updateUser(usr);
                                            }
                                        }
                                    }
                                    DbManager.updateUser(user);
                                }
                            }
                        } 
                    }
                    break;
                case PacketTypes.PlayerDamage:
                    using (var reader = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length)))
                    {
                        byte victimid = reader.ReadByte();
                        byte hitdirr = reader.ReadByte();
                        short damage = reader.ReadInt16();
                        string deathtexte = reader.ReadString();
                        byte flag = reader.ReadByte();

                        TSPlayer victim = TShock.Players[victimid];

                        foreach (TUser user in DbManager.Users)
                        {
                            if (user.UserID == victim.User.ID)
                            {
                                if (victim.IsLoggedIn)
                                {
                                    user.DamageRecieved += damage;
                                    user.Killer = sender;
                                    DbManager.updateUser(user);
                                }
                            }

                            if (user.UserID == sender.User.ID)
                            {
                                if (sender.IsLoggedIn)
                                {
                                    user.DamageGiven += damage;
                                    DbManager.updateUser(user);
                                }
                            }
                        }
                    }
                    break;
                case PacketTypes.NpcStrike:
                    using (var reader = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length)))
                    {
                        short npcid = reader.ReadInt16();
                        short damage = reader.ReadInt16();
                        float knockback = reader.ReadSingle();
                        byte dir = reader.ReadByte();
                        bool crit = reader.ReadBoolean();

                        NPC npc = TShock.Utils.GetNPCById(npcid);

                        foreach(TUser user in DbManager.Users)
                        {
                            if (user.UserID == sender.User.ID)
                            {
                                if (sender.IsLoggedIn)
                                {
                                    if (damage != -1)
                                    {
                                        // Add damage
                                        user.DamageGiven += damage;
                                    }

                                    if((npc.life - damage) < 0)
                                    {
                                        // Add mobkill
                                        user.MobKills += 1;
                                    }

                                    DbManager.updateUser(user);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        void OnPlayerLogin(TShockAPI.Hooks.PlayerPostLoginEventArgs args)
        {
            TUser ply = new TUser(args.Player.Name, args.Player.User.ID, 0, 0, 0, 0, 0);
            DbManager.addUser(ply, args.Player);
        }

        private void onLeave(LeaveEventArgs args)
        {
            // Delete them from user list in db
            throw new NotImplementedException();
        }

        void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("terrastats.use", getStats, "stats")
            {
                HelpText = "Usage: /stats [player] // Player is optional"
            });

            Db = new SqliteConnection("uri=file://" + Path.Combine(TShock.SavePath, "TerraStats.sqlite") + ",Version=3");
        }

        private void getStats(CommandArgs args)
        {
            TSPlayer ply = args.Player;

            if (ply == null)
                return;

            if (!ply.IsLoggedIn)
                return;

            if(args.Parameters.Count < 1)
            {
                foreach (TUser user in DbManager.Users)
                {
                    if (user.UserID == ply.User.ID)
                    {
                        ply.SendMessage("[TerraStats] Stats for: " + user.Name, new Color(30, 225, 212));
                        ply.SendMessage("MobKills: " + user.MobKills, new Color(30, 225, 212));
                        ply.SendMessage("PvPKills: " + user.PvPKills, new Color(30, 225, 212));
                        ply.SendMessage("Deaths: " + user.Deaths, new Color(30, 225, 212));
                        ply.SendMessage("Damage Given: " + user.DamageGiven, new Color(30, 225, 212));
                        ply.SendMessage("Damage Taken: " + user.DamageRecieved, new Color(30, 225, 212));
                    }
                }
            }
            else
            {
                List<TSPlayer> fplayer = TShock.Utils.FindPlayer(args.Parameters[0]);

                bool isinDatabase = false;
                
                if (fplayer.Count == 0)
                {
                    foreach (TUser user in DbManager.Users)
                    {
                        if (user.Name == args.Parameters[0])
                        {
                            isinDatabase = true;
                        }
                    }

                    if (!isinDatabase)
                    {
                        ply.SendErrorMessage("[TerraStats] Player not found.");
                        return;
                    }
                }

                foreach (TUser user in DbManager.Users)
                {
                    if (!isinDatabase)
                    {
                        if (user.UserID == fplayer[0].User.ID)
                        {
                            ply.SendMessage("[TerraStats] Stats for: " + user.Name, new Color(30, 225, 212));
                            ply.SendMessage("MobKills: " + user.MobKills, new Color(30, 225, 212));
                            ply.SendMessage("PvPKills: " + user.PvPKills, new Color(30, 225, 212));
                            ply.SendMessage("Deaths: " + user.Deaths, new Color(30, 225, 212));
                            ply.SendMessage("Damage Given: " + user.DamageGiven, new Color(30, 225, 212));
                            ply.SendMessage("Damage Taken: " + user.DamageRecieved, new Color(30, 225, 212));
                            break;
                        }
                    }

                    if (isinDatabase)
                    {
                        if (user.Name == args.Parameters[0])
                        {
                            ply.SendMessage("[TerraStats] Stats for: " + user.Name, new Color(79, 14, 102));
                            ply.SendMessage("MobKills: " + user.MobKills, new Color(93, 18, 121));
                            ply.SendMessage("PvPKills: " + user.PvPKills, new Color(104, 28, 131));
                            ply.SendMessage("Deaths: " + user.Deaths, new Color(116, 35, 145));
                            ply.SendMessage("Damage Given: " + user.DamageGiven, new Color(124, 32, 158));
                            ply.SendMessage("Damage Taken: " + user.DamageRecieved, new Color(137, 33, 175));
                            break;
                        }
                    }
                }
            }
            
        }

        private void OnPostInitialize(EventArgs args)
        {
            DbManager = new DBManager(Db);
        }
    }
}