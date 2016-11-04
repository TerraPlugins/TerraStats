using TShockAPI;

namespace TerraStats
{
    public class TUser
    {
        public string Name { get; set; }
        public int UserID { get; set; }
        public int Deaths { get; set; }
        public int MobKills { get; set; }
        public int PvPKills { get; set; }
        public int DamageGiven { get; set; }
        public int DamageRecieved { get; set; }
        public TSPlayer Killer { get; set; }

        public TUser(string name, int userid, int deaths, int mobkills, int pvpkills, int dmggiven, int dmgtaken)
        {
            Name = name;
            UserID = userid;
            Deaths = deaths;
            MobKills = mobkills;
            PvPKills = pvpkills;
            DamageGiven = dmggiven;
            DamageRecieved = dmgtaken;
        }
    }
}
