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

namespace MCGalaxy.Commands.Building {    
    public sealed class CmdSPlace : Command {       
        public override string name { get { return "splace"; } }
        public override string shortcut { get { return "set"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Builder; } }
        public CmdSPlace() { }

        public override void Use(Player p, string message) {
            ushort distance = 0, interval = 0;
            if (message == "") { Help(p); return; }
            if (Player.IsSuper(p)) { MessageInGameOnly(p); return; }
            
            string[] parts = message.Split(' ');
            if (!ushort.TryParse(parts[0], out distance)) {
                Player.Message(p, "Distance must be a number less than 65536."); return;
            }
            if (parts.Length > 1 && !ushort.TryParse(parts[1], out interval)) {
                Player.Message(p, "Interval must be a number less than 65536."); return;
            }

            if (distance < 1) {
                Player.Message(p, "Enter a distance greater than 0."); return;
            }
            if (interval >= distance) {
                Player.Message(p, "The Interval cannot be greater than the distance."); return;
            }

            DrawArgs dArgs = default(DrawArgs);
            dArgs.distance = distance; dArgs.interval = interval;
            Player.Message(p, "Place two blocks to determine direction.");
            p.MakeSelection(2, dArgs, DoSPlace);
        }
        
        bool DoSPlace(Player p, Vec3S32[] m, object state, byte type, byte extType) {
            DrawArgs dArgs = (DrawArgs)state;
            ushort distance = dArgs.distance, interval = dArgs.interval;
            if (m[0] == m[1]) { Player.Message(p, "No direction was selected"); return false; }
            
            int dirX = 0, dirY = 0, dirZ = 0;
            int dx = Math.Abs(m[1].X - m[0].X), dy = Math.Abs(m[1].Y - m[0].Y), dz = Math.Abs(m[1].Z - m[0].Z);
            if (dy > dx && dy > dz) 
                dirY = m[1].Y > m[0].Y ? 1 : -1;
            else if (dx > dz) 
                dirX = m[1].X > m[0].X ? 1 : -1;
            else 
                dirZ = m[1].Z > m[0].Z ? 1 : -1;
            
            ushort endX = (ushort)(m[0].X + dirX * distance);
            ushort endY = (ushort)(m[0].Y + dirY * distance);
            ushort endZ = (ushort)(m[0].Z + dirZ * distance);
            p.level.UpdateBlock(p, endX, endY, endZ, Block.rock, 0);   
            
            if (interval > 0) {
                int x = m[0].X, y = m[0].Y, z = m[0].Z;
                int delta = 0;
                while (x >= 0 && y >= 0 && z >= 0 && x < p.level.Width && y < p.level.Height && z < p.level.Length && delta < distance) {
                    p.level.UpdateBlock(p, (ushort)x, (ushort)y, (ushort)z, Block.rock, 0);
                    x += dirX * interval; y += dirY * interval; z += dirZ * interval;
                    delta = Math.Abs(x - m[0].X) + Math.Abs(y - m[0].Y) + Math.Abs(z - m[0].Z);
                }
            } else {
                p.level.UpdateBlock(p, (ushort)m[0].X, (ushort)m[0].Y, (ushort)m[0].Z, Block.rock, 0);
            }

            Player.Message(p, "Placed stone blocks {0} apart.", interval > 0 ? interval : distance);
            return true;
        }
        
        struct DrawArgs { public ushort distance, interval; }

        public override void Help(Player p) {
            Player.Message(p, "%T/splace [distance] [interval]");
            Player.Message(p, "%HMeasures a set [distance] and places a stone block at each end.");
            Player.Message(p, "%HOptionally place a block at set [interval] between them.");
        }
    }
}
