/*
    Copyright 2011 MCForge
    
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
using System.Globalization;
namespace MCGalaxy.Commands
{
    public sealed class CmdGive : Command
    {
        public override string name { get { return "give"; } }
        public override string shortcut { get { return "gib"; } }
        public override string type { get { return CommandTypes.Economy; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override CommandEnable Enabled { get { return CommandEnable.Economy; } }        
        public CmdGive() { }

        public override void Use(Player p, string message) {
            string[] args = message.Split(' ');
            if (args.Length != 2) { Help(p); return; }
            string giver = null, giverRaw = null;
            if (p == null) { giverRaw = "(console)"; giver = "(console)"; } 
            else { giverRaw = p.color + p.name; giver = p.ColoredName; }

            int amount;
            if (!int.TryParse(args[1], out amount)) {
                Player.Message(p, "Amount must be an integer."); return;
            }
            if (amount < 0) { Player.Message(p, "Cannot give negative %3" + Server.moneys); return; }
            
            int matches = 1;
            Player who = PlayerInfo.FindMatches(p, args[0], out matches);
            if (matches > 1) return;
            if (p != null && p == who) { Player.Message(p, "You cannot give yourself %3" + Server.moneys); return; }
            Economy.EcoStats ecos;

            if (who == null) {
                OfflinePlayer off = PlayerInfo.FindOfflineMatches(p, args[0]);
                if (off == null) return;
                ecos = Economy.RetrieveEcoStats(off.name);
                if (ReachedMax(p, ecos.money, amount)) return;
                Player.GlobalMessage(giver + " %Sgave %f" + ecos.playerName + "%S(offline)" + " %f" + amount + " %3" + Server.moneys);
            } else {
                if (ReachedMax(p, who.money, amount)) return;
                ecos.money = who.money;
                who.money += amount;
                who.OnMoneyChanged();
                ecos = Economy.RetrieveEcoStats(who.name);
                Player.GlobalMessage(giver + " %Sgave " + who.ColoredName + " %f" + amount + " %3" + Server.moneys);
            }
            
            ecos.money += amount;
            ecos.salary = "%f" + amount + "%3 " + Server.moneys + " by " +
                giverRaw + "%3 on %f" + DateTime.Now.ToString(CultureInfo.InvariantCulture);
            Economy.UpdateEcoStats(ecos);
        }
        
        static bool ReachedMax(Player p, int current, int amount) {
            if (current + amount > 16777215) {
                Player.Message(p, "%cPlayers cannot have over %316,777,215 %3" + Server.moneys); return true;
            }
            return false;
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/give [player] <amount>");
            Player.Message(p, "%HGives [player] <amount> %3" + Server.moneys);
        }
    }
}
