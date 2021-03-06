/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
    Dual-licensed under the    Educational Community License, Version 2.0 and
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
namespace MCGalaxy.Commands {
    public sealed class CmdBlockSet : Command {
        public override string name { get { return "blockset"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public CmdBlockSet() { }

        public override void Use(Player p, string message) {
            string[] args = message.Split(' ');
            if (args.Length < 2) { Help(p); return; }

            byte block = Block.Byte(args[0]);
            if (block == Block.Zero) { Player.Message(p, "Could not find block entered"); return; }
            Group grp = Group.FindMatches(p, args[1]);
            if (grp == null) return;
            
            if (p != null && grp.Permission > p.Rank) { Player.Message(p, "Cannot set to a rank higher than yourself."); return; }
            if (p != null && !Block.canPlace(p, block)) { Player.Message(p, "Cannot modify a block set for a higher rank"); return; }

            Block.BlockList[block].lowestRank = grp.Permission;
            Block.SaveBlocks(Block.BlockList);
            Block.ResendBlockPermissions(block);
            // TODO: custom blocks permissions

            Player.GlobalMessage("&d" + Block.Name(block) + "%S's permission was changed to " + grp.ColoredName);
            if (p == null)
                Player.Message(p, Block.Name(block) + "'s permission was changed to " + grp.ColoredName);
        }
        
        public override void Help(Player p) {
            Player.Message(p, "/blockset [block] [rank] - Changes [block] rank to [rank]");
            Player.Message(p, "Only blocks you can use can be modified");
            Player.Message(p, "Available ranks: " + Group.concatList());
        }
    }
}
