﻿/*
    Copyright 2015 MCGalaxy
        
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
using System.Collections.Generic;
using MCGalaxy.Drawing.Brushes;

namespace MCGalaxy.Drawing.Ops {

    public abstract class PyramidDrawOp : DrawOp {
        protected DrawOp baseOp;
        protected int yDir;
        
        public PyramidDrawOp(DrawOp baseOp, int yDir) {
            this.baseOp = baseOp;
            this.yDir = yDir;
        }
        
        public override long GetBlocksAffected(Level lvl, Vec3S32[] marks) {
            Vec3S32 oMin = Min, oMax = Max;
            baseOp.Min = oMin; baseOp.Max = oMax;
            Vec3S32 p1 = Min, p2 = Max;
            long total = 0;
            
            while (true) {
                total += baseOp.GetBlocksAffected(lvl, marks);
                if (p1.Y >= lvl.Height || Math.Abs(p2.X - p1.X) <= 1 || Math.Abs(p2.Z - p1.Z) <= 1)
                    break;
                p1.X++; p2.X--;
                p1.Z++; p2.Z--;
                p1.Y = (ushort)(p1.Y + yDir); p2.Y = p1.Y;
                baseOp.Min = p1; baseOp.Max = p2;
            }
            baseOp.Min = oMin; baseOp.Max = oMax;
            return total;
        }
        
        public override IEnumerable<DrawOpBlock> Perform(Vec3S32[] marks, Player p, Level lvl, Brush brush) {
            Vec3S32 p1 = Min, p2 = Max;
            baseOp.Level = Level;
            
            while (true) {
                foreach (var block in baseOp.Perform(marks, p, lvl, brush))
                    yield return block;
                if (p1.Y >= lvl.Height || Math.Abs(p2.X - p1.X) <= 1 || Math.Abs(p2.Z - p1.Z) <= 1)
                    yield break;
                
                p1.X++; p2.X--;
                p1.Z++; p2.Z--;
                p1.Y = (ushort)(p1.Y + yDir); p2.Y = p1.Y;
                baseOp.Min = p1; baseOp.Max = p2;
            }
        }
    }
    
    public class PyramidSolidDrawOp : PyramidDrawOp {

        public PyramidSolidDrawOp() : base(new CuboidDrawOp(), 1) {
        }
        
        public override string Name { get { return "Pyramid solid"; } }
    }
    
    public class PyramidHollowDrawOp : PyramidDrawOp {      

        public PyramidHollowDrawOp() : base(new CuboidWallsDrawOp(), 1) {
        }
        
        public override string Name { get { return "Pyramid hollow"; } }
    }
    
    public class PyramidReverseDrawOp : PyramidDrawOp {

        DrawOp wallOp;
        Brush airBrush;
        public PyramidReverseDrawOp() : base(new CuboidDrawOp(), -1) {
            wallOp = new CuboidWallsDrawOp();
            airBrush = new SolidBrush(Block.air, 0);
        }
        
        public override string Name { get { return "Pyramid reverse"; } }
        
        public override IEnumerable<DrawOpBlock> Perform(Vec3S32[] marks, Player p, Level lvl, Brush brush) {
            Vec3U16 p1 = Clamp(Min), p2 = Clamp(Max);
            wallOp.Min = Min; wallOp.Max = Max;
            baseOp.Min = Min; baseOp.Max = Max;
            wallOp.Level = Level; baseOp.Level = Level;
            
            while (true) {
                foreach (var block in wallOp.Perform(marks, p, lvl, brush))
                    yield return block;
                if (p1.Y >= lvl.Height || Math.Abs(p2.X - p1.X) <= 1 || Math.Abs(p2.Z - p1.Z) <= 1)
                    yield break;
                
                p1.X++; p2.X--;
                p1.Z++; p2.Z--;
                wallOp.Min = p1; wallOp.Max = p2;
                baseOp.Min = p1; baseOp.Max = p2;
                
                foreach (var block in baseOp.Perform(marks, p, lvl, airBrush))
                    yield return block;
                p1.Y = (ushort)(p1.Y + yDir); p2.Y = p1.Y;
                wallOp.Min = p1; wallOp.Max = p2;
                baseOp.Min = p1; baseOp.Max = p2;
            }
        }
    }
}
