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

namespace MCGalaxy.Commands.World {
    public sealed class CmdRenameLvl : Command {
        public override string name { get { return "renamelvl"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return CommandTypes.World; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public CmdRenameLvl() { }

        public override void Use(Player p, string message) {
            string[] args = message.Split(' ');
            if (args.Length != 2) { Help(p); return; }
            
            Level lvl = LevelInfo.FindMatches(p, args[0]);
            if (lvl == null) return;
            string newName = args[1];
            if (!ValidName(p, newName, "level")) return;
            
            if (LevelInfo.ExistsOffline(newName)) { Player.Message(p, "Level already exists."); return; }
            if (lvl == Server.mainLevel) { Player.Message(p, "Cannot rename the main level."); return; }
            lvl.Unload();
            
            LevelActions.Rename(lvl.name.ToLower(), newName.ToLower());
            try { Command.all.Find("load").Use(p, newName); }
            catch { }
            Player.GlobalMessage("Renamed " + lvl.name + " to " + newName);
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/renamelvl <level> <new name>");
            Player.Message(p, "%HRenames <level> to <new name>");
            Player.Message(p, "%HNote: Portals going to <level> will no longer work.");
        }
    }
}
