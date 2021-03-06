/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
*/
using System;
using System.IO;

namespace MCGalaxy.Commands {   
    public sealed class CmdRankInfo : Command {        
        public override string name { get { return "rankinfo"; } }
        public override string shortcut { get { return "ri"; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.AdvBuilder; } }
        public CmdRankInfo() { }

        public override void Use(Player p, string message) {
            if (message == "") { Help(p); return; }
            Player who = PlayerInfo.Find(message);
            string target = who == null ? message : who.name;

            Player.Message(p, "&1Rank information for " + target);
            bool found = false;
            DateTime now = DateTime.Now;
            
            foreach (string line in Server.RankInfo.Find(target)) {
                string[] parts = line.Split(' ');                
                Group newRank = Group.Find(parts[7]), oldRank = Group.Find(parts[8]);
                string newRankName = newRank == null ? parts[7] : newRank.ColoredName;
                string oldRankName = oldRank == null ? parts[8] : oldRank.ColoredName;
                
                int minutes = Convert.ToInt32(parts[2]), hours = Convert.ToInt32(parts[3]);
                int days = Convert.ToInt32(parts[4]), months = Convert.ToInt32(parts[5]);
                int years = Convert.ToInt32(parts[6]);
                DateTime timeRanked = new DateTime(years, months, days, hours, minutes, 0);
                
                string reason = parts.Length <= 9 ? "(no reason given)" :
                    CP437Reader.ConvertToRaw(parts[9].Replace("%20", " "));
                TimeSpan delta = now - timeRanked;
               
                Player.Message(p, "&aFrom {0} &ato {1} &a{2} ago", 
                               oldRankName, newRankName, delta.Shorten(true, false));
                Player.Message(p, "&aBy %S{0}&a, reason: %S{1}", parts[1], reason);
                found = true;
            }
            if (!found)
                Player.Message(p, "&cPlayer &a" + target + "&c has not been ranked yet.");
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/rankinfo [player]");
            Player.Message(p, "%HReturns details about that person's rankings.");
        }
    }
}
