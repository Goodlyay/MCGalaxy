﻿/*
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

namespace MCGalaxy.BlockPhysics {
    
    public static class ExtraInfoPhysics {
        
        public static bool DoDoorsOnly(Level lvl, ref Check C, Random rand) {
            if (!C.data.HasWait && lvl.blocks[C.b] == Block.air)
            	C.data.ResetTypes();
            if (C.data.Type1 == PhysicsArgs.TntWars) return true;

            bool wait = false, door = C.data.Door;
            int waitTime = 0;
            if (C.data.Type1 == PhysicsArgs.Wait) {
                wait = true; waitTime = C.data.Value1;
            } else if (C.data.Type2 == PhysicsArgs.Wait) {
                wait = true; waitTime = C.data.Value2;
            }
            if (!wait) return false;
            
            if (door && C.data.Data < 2) {
                // TODO: perhaps do proper bounds checking
                Checktdoor(lvl, lvl.IntOffset(C.b, -1, 0, 0));
                Checktdoor(lvl, lvl.IntOffset(C.b, 1, 0, 0));
                Checktdoor(lvl, lvl.IntOffset(C.b, 0, -1, 0));
                Checktdoor(lvl, lvl.IntOffset(C.b, 0, 1, 0));
                Checktdoor(lvl, lvl.IntOffset(C.b, 0, 0, -1));
                Checktdoor(lvl, lvl.IntOffset(C.b, 0, 0, 1));
            }

            if (C.data.Data > waitTime) {
                if (C.data.Type1 == PhysicsArgs.Wait) C.data.Type1 = 0;
                if (C.data.Type2 == PhysicsArgs.Wait) C.data.Type2 = 0;
                return false;
            }
            C.data.Data++;
            return true;
        }
        
        static void Checktdoor(Level lvl, int index) {
            if (index < 0 || index >= lvl.blocks.Length) return;
            byte block = lvl.blocks[index];
            
            if (Block.Props[block].IsTDoor) {
                PhysicsArgs args = default(PhysicsArgs);
                args.Type1 = PhysicsArgs.Wait; args.Value1 = 10;
                args.Type2 = PhysicsArgs.Revert; args.Value2 = block;
                args.Door = true;
                lvl.AddUpdate(index, Block.air, false, args);
            }
        }
        
        public static bool DoComplex(Level lvl, ref Check C) {
            if (!C.data.HasWait && lvl.blocks[C.b] == Block.air)
            	C.data.ResetTypes();
            if (C.data.Type1 == PhysicsArgs.TntWars) return true;
            
            ExtraInfoArgs args = default(ExtraInfoArgs);
            args.Door = C.data.Door;
            ParseType(C.data.Type1, ref args, C.data.Value1);
            ParseType(C.data.Type2, ref args, C.data.Value2);
            
            if (args.Wait) {
                if (args.Door && C.data.Data < 2) {
                    Checktdoor(lvl, lvl.IntOffset(C.b, -1, 0, 0));
                    Checktdoor(lvl, lvl.IntOffset(C.b, 1, 0, 0));
                    Checktdoor(lvl, lvl.IntOffset(C.b, 0, -1, 0));
                    Checktdoor(lvl, lvl.IntOffset(C.b, 0, 1, 0));
                    Checktdoor(lvl, lvl.IntOffset(C.b, 0, 0, -1));
                    Checktdoor(lvl, lvl.IntOffset(C.b, 0, 0, 1));
                }

                if (C.data.Data > args.WaitTime) {
                    if (C.data.Type1 == PhysicsArgs.Wait) C.data.Type1 = 0;
                    if (C.data.Type2 == PhysicsArgs.Wait) C.data.Type2 = 0;
                    DoOther(lvl, ref C, ref args);
                    return false;
                }
                C.data.Data++;
                return true;
            }
            DoOther(lvl, ref C, ref args);
            return false;
        }
        
        static void ParseType(byte type, ref ExtraInfoArgs args, byte value) {
            switch (type) {
                case PhysicsArgs.Wait:
                    args.Wait = true; args.WaitTime = value; break;
                case PhysicsArgs.Drop:
                    args.Drop = true; args.DropNum = value; break;
                case PhysicsArgs.Dissipate:
                    args.Dissipate = true; args.DissipateNum = value; break;
                case PhysicsArgs.Revert:
                    args.Revert = true; args.RevertType = value; break;
                case PhysicsArgs.Explode:
                    args.Explode = true; args.ExplodeNum = value; break;
                case PhysicsArgs.Rainbow:
                    args.Rainbow = true; args.RainbowNum = value; break;
            }
        }
        
        static void DoOther(Level lvl, ref Check C, ref ExtraInfoArgs args) {
            Random rand = lvl.physRandom;            
            if (args.Rainbow) {
            	if (C.data.Data < 4) C.data.Data++;
            	else DoRainbow(lvl, ref C, rand, args.RainbowNum); 
            	return;
            }
            if (args.Revert) {
                lvl.AddUpdate(C.b, args.RevertType);
                C.data.ResetTypes();
            }
            ushort x, y, z;
            lvl.IntToPos(C.b, out x, out y, out z);
            
            // Not setting drop = false can cause occasional leftover blocks, since C.extraInfo is emptied, so
            // drop can generate another block with no dissipate/explode information.
            if (args.Dissipate && rand.Next(1, 100) <= args.DissipateNum) {
                if (!lvl.listUpdateExists.Get(x, y, z)) {
                    lvl.AddUpdate(C.b, Block.air);
                    C.data.ResetTypes();
                    args.Drop = false;
                } else {
                    lvl.AddUpdate(C.b, lvl.blocks[C.b], false, C.data);
                }
            }
            
            if (args.Explode && rand.Next(1, 100) <= args.ExplodeNum) {
                lvl.MakeExplosion(x, y, z, 0);
                C.data.ResetTypes();
                args.Drop = false;
            }
            
            if (args.Drop && rand.Next(1, 100) <= args.DropNum)
                DoDrop(lvl, ref C, rand, args.DropNum, x, y, z);
        }
        
        static void DoRainbow(Level lvl, ref Check C, Random rand, int rainbownum) {          
            if (rainbownum > 2) {
                byte block = lvl.blocks[C.b];
                if (block < Block.red || block > Block.darkpink) {
                    lvl.AddUpdate(C.b, Block.red, false, C.data);
                } else {
                    byte next = block == Block.darkpink ? Block.red : (byte)(block + 1);
                    lvl.AddUpdate(C.b, next);
                }
            } else {
                lvl.AddUpdate(C.b, rand.Next(Block.red, Block.darkpink + 1));
            }
        }
        
        static void DoDrop(Level lvl, ref Check C, Random rand, int dropnum, ushort x, ushort y, ushort z) {
            int index = lvl.PosToInt(x, (ushort)(y - 1), z);
            if (index < 0) return;
            
            byte below = lvl.blocks[index];
            if (!(below == Block.air || below == Block.lava || below == Block.water))
                return;
            
            if (rand.Next(1, 100) < dropnum && lvl.AddUpdate(index, lvl.blocks[C.b], false, C.data)) {
                lvl.AddUpdate(C.b, Block.air);
                C.data.ResetTypes();
            }
        }
        
        struct ExtraInfoArgs {
            public bool Wait, Drop, Dissipate, Revert, Door, Explode, Rainbow;
            public int WaitTime, DropNum, DissipateNum, ExplodeNum, RainbowNum;
            public byte RevertType;
        }
    }
}