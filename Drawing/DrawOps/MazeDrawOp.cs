﻿/*
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
using System.Collections.Generic;
using System.Security.Cryptography;
using MCGalaxy.Drawing.Brushes;

namespace MCGalaxy.Drawing.Ops {
    
    public class MazeDrawOp : CuboidHollowsDrawOp {
        
        public override string Name { get { return "Maze"; } }
        
        internal int randomizer = 0;
        bool[,] wall;
        RNGCryptoServiceProvider rng1;
        Random rng2;
        byte[] r = new byte[1];
        int width, length;
        
        public override long GetBlocksAffected(Level lvl, Vec3S32[] marks) {
            int lenX = (Math.Abs(Max.X - Min.X) + 1) / 2;
            int lenZ = (Math.Abs(Max.Z - Min.Z) + 1) / 2;
            return lenX * lenZ * 3;
        }
        
        public override IEnumerable<DrawOpBlock> Perform(Vec3S32[] marks, Player p, Level lvl, Brush brush) {
            width = Max.X - Min.X;
            if (width % 2 != 0) { width++; Min.X--; }
            width -= 2;
            length = Max.Z - Min.Z;
            if (length % 2 != 0) { length++; Min.Z--; }
            length -= 2;
            
            if (width <= 0 || length <= 0) {
                Player.Message(p, "The corners of the maze need to be further apart."); yield break;
            }
            Player.Message(p, "Generating maze... this could take a while");
            //substract 2 cause we will just make the inner. the outer wall is made seperately
            wall = new bool[width + 1, length + 1];//+1 cause we begin at 0 so we need one object more
            for (int w = 0; w <= width; w++)
                for (int h = 0; h <= length; h++)
            {
                wall[w, h] = true;
            }
            rng1 = new RNGCryptoServiceProvider();
            rng2 = new Random();
            
            Stack<GridNode> stack = new Stack<GridNode>(width * length);
            stack.Push(new GridNode(0, 0));
            wall[0, 0] = false;
            while (true) {
                GridNode P = stack.Peek();
                if (TurnsPossible(P)) {
                    GridNode P1, P2;
                    MoveRandomDir(P, out P1, out P2);
                    wall[P1.X, P1.Y] = false;
                    wall[P2.X, P2.Y] = false;
                    stack.Push(P2);
                    //we get the next two nodes
                    //the first is a middle node from which there shouldnt start a new corridor
                    //the second is added to the stack. next try will be with this node
                    //i hope this will work this time...
                } else {
                    stack.Pop();//if this node is a dead and it will be removed
                }
                if (stack.Count < 1) break;//if no nodes are free anymore we will end the generation here
            }
            
            Player.Message(p, "Generated maze, now drawing.");
            Vec3U16 min = Clamp(Min), max = Clamp(Max);
            ushort y = min.Y;
            for (ushort xx = 0; xx <= width; xx++)
                for (ushort zz = 0; zz <= length; zz++)
                    if (wall[xx, zz])
            {
                yield return Place((ushort)(xx + min.X + 1), y, (ushort)(zz + min.Z + 1), Block.staircasefull, 0);
                yield return Place((ushort)(xx + min.X + 1), (ushort)(y + 1), (ushort)(zz + min.Z + 1), Block.leaf, 0);
                yield return Place((ushort)(xx + min.X + 1), (ushort)(y + 2), (ushort)(zz + min.Z + 1), Block.leaf, 0);
            }
            
            brush = new SolidBrush(Block.staircasefull, 0);
            foreach (var block in QuadX(min.X, y, min.Z, y, max.Z, brush))
                yield return block;
            foreach (var block in QuadX(max.X, y, min.Z, y, max.Z, brush))
                yield return block;
            foreach (var block in QuadZ(min.Z, y, min.X, y, max.X, brush))
                yield return block;
            foreach (var block in QuadZ(max.Z, y, min.X, y, max.X, brush))
                yield return block;
            
            brush = new SolidBrush(Block.leaf, 0);
            foreach (var block in QuadX(min.X, (ushort)(y + 1), min.Z, (ushort)(y + 2), max.Z, brush))
                yield return block;
            foreach (var block in QuadX(max.X, (ushort)(y + 1), min.Z, (ushort)(y + 2), max.Z, brush))
                yield return block;
            foreach (var block in QuadZ(min.Z, (ushort)(y + 1), min.X, (ushort)(y + 2), max.X, brush))
                yield return block;
            foreach (var block in QuadZ(max.Z, (ushort)(y + 1), min.X, (ushort)(y + 2), max.X, brush))
                yield return block;
            
            Player.Message(p, "Maze painted. Build the entrance and exit yourself");
            randomizer = 0;
        }
        
        void MoveRandomDir(GridNode P, out GridNode P1, out GridNode P2) {        
            while (true) {
                r[0] = 0;
                if (randomizer == 0) {
                    rng1.GetBytes(r);
                    r[0] /= (255 / 4);
                } else {
                    r[0] = (byte)rng2.Next(4);
                }

                switch (r[0]) {
                    case 0: //go up
                        if (IsWall(P.X, P.Y + 2)) {
                            P1 = new GridNode(P.X, (ushort)(P.Y + 1));
                            P2 = new GridNode(P.X, (ushort)(P.Y + 2));
                            return;
                        } break;
                    case 1: //go down
                        if (IsWall(P.X, P.Y - 2)) {
                            P1 = new GridNode(P.X, (ushort)(P.Y - 1));
                            P2 = new GridNode(P.X, (ushort)(P.Y - 2));
                            return;
                        } break;
                    case 2: //go right
                        if (IsWall(P.X + 2, P.Y)) {
                            P1 = new GridNode((ushort)(P.X + 1), P.Y);
                            P2 = new GridNode((ushort)(P.X + 2), P.Y);
                            return;
                        } break;
                    case 3: //go left
                        if (IsWall(P.X - 2, P.Y)) {
                            P1 = new GridNode((ushort)(P.X - 1), P.Y);
                            P2 = new GridNode((ushort)(P.X - 2), P.Y);
                            return;
                        } break;
                }
            }
        }
        
        bool TurnsPossible(GridNode P) {
            return IsWall(P.X, P.Y + 2) || IsWall(P.X, P.Y - 2)
                || IsWall(P.X + 2, P.Y) || IsWall(P.X - 2, P.Y);
        }

        bool IsWall(int x, int y) {
        	if (x < 0 || y < 0 || x > width || y > length) return false;
        	return wall[x, y];
        }
        
        struct GridNode {
            public ushort X, Y;         
            public GridNode(ushort x, ushort y) { X = x; Y = y; }
        }
    }
}
