/*
	Copyright 2011 MCForge
		
	Dual-licensed under the	Educational Community License, Version 2.0 and
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

namespace MCGalaxy.Commands.Building {
    public sealed class CmdPlace : Command {		
        public override string name { get { return "place"; } }
        public override string shortcut { get { return "pl"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
        public CmdPlace() { }

        public override void Use(Player p, string message) {
            int block = -1;
            byte extBlock = 0;
            ushort x = p.pos[0], y = (ushort)(p.pos[1] - 32), z = p.pos[2];

            try {
            	string[] parts = message.Split(' ');
                switch (parts.Length) {
                    case 1: block = message == "" ? Block.rock :
                        DrawCmd.GetBlock(p, parts[0], out extBlock); break;
                    case 3:
                        block = Block.rock;
                        x = (ushort)(Convert.ToUInt16(parts[0]) * 32);
                        y = (ushort)(Convert.ToUInt16(parts[1]) * 32);
                        z = (ushort)(Convert.ToUInt16(parts[2]) * 32);
                        break;
                    case 4:
                        block = DrawCmd.GetBlock(p, parts[0], out extBlock);
                        x = (ushort)(Convert.ToUInt16(parts[1]) * 32);
                        y = (ushort)(Convert.ToUInt16(parts[2]) * 32);
                        z = (ushort)(Convert.ToUInt16(parts[3]) * 32);
                        break;
                    default: Player.Message(p, "Invalid number of parameters"); return;
                }
            } catch { 
            	Player.Message(p, "Invalid parameters"); return; 
            }

            if (block == -1 || block == Block.Zero) return;
            if (!Block.canPlace(p, (byte)block)) { Player.Message(p, "Cannot place that block type."); return; }
            Vec3U16 P = Vec3U16.ClampPos(x, y, z, p.level);
            
            P.X /= 32; P.Y /= 32; P.Z /= 32;
            p.level.UpdateBlock(p, P.X, P.Y, P.Z, (byte)block, extBlock);
            Player.Message(p, "A block was placed at (" + P.X + ", " + P.Y + ", " + P.Z + ").");
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/place [block] <x> <y> <z>");
            Player.Message(p, "%HPlaces block at your feet or <x> <y> <z>");
        }
    }
}
