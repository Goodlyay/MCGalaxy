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
using System.Globalization;
using System.IO;

namespace MCGalaxy.Eco {
    
    public sealed class RankItem : Item {
        
        public RankItem() {
            Aliases = new [] { "rank", "ranks", "rankup" };
        }
        
        public override string Name { get { return "Rank"; } }
        
        public override string ShopName { get { return "Rankup"; } }
        
        public string MaxRank = Group.findPerm(LevelPermission.AdvBuilder).name;
        
        public List<Rank> RanksList = new List<Rank>();
        public class Rank {
            public Group group;
            public int price = 1000;
        }
        
        public override void Parse(string line, string[] split) {
            if (split[1] == "price") {
                Rank rnk = FindRank(split[2]);
                if (rnk == null) {
                    rnk = new Rank();
                    rnk.group = Group.Find(split[2]);
                    rnk.price = int.Parse(split[3]);
                    RanksList.Add(rnk);
                } else {
                    rnk.price = int.Parse(split[3]);
                }
            } else if (split[1] == "maxrank") {
                if (Group.Exists(split[2])) MaxRank = split[2];
            } else if (split[1] == "enabled") {
                Enabled = split[2].CaselessEq("true");
            }
        }
        
        public override void Serialise(StreamWriter writer) {
            writer.WriteLine("rank:enabled:" + Enabled);
            writer.WriteLine("rank:maxrank:" + MaxRank);
            foreach (Rank rnk in RanksList) {
                writer.WriteLine("rank:price:" + rnk.group.name + ":" + rnk.price);
                if (rnk.group.name == MaxRank) break;
            }
        }
        
        protected internal override void OnBuyCommand(Command cmd, Player p, 
                                                      string message, string[] args) {
            if (args.Length >= 2) {
                Player.Message(p, "%cYou cannot provide a rank name, use %a/buy rank %cto buy the NEXT rank."); return;
            }
            Group maxrank = Group.Find(MaxRank);
            if (p.Rank >= maxrank.Permission) {
                Player.Message(p, "%cYou cannot buy anymore ranks, because you passed the max buyable rank: " + maxrank.color + maxrank.name);
                return;
            }
            if (p.money < NextRank(p).price) {
                Player.Message(p, "%cYou don't have enough %3" + Server.moneys + "%c to buy the next rank"); return;
            }
            
            Command.all.Find("setrank").Use(null, "+up " + p.name);
            Player.Message(p, "%aYou've successfully bought the rank " + p.group.ColoredName);
            Economy.MakePurchase(p, FindRank(p.group.name).price, "%3Rank: " + p.group.ColoredName);
        }
        
        protected internal override void OnSetupCommand(Player p, string[] args) {
            switch (args[1].ToLower()) {
                case "enable":
                    Player.Message(p, "%aThe " + Name + " item is now enabled.");
                    Enabled = true; break;
                case "disable":
                    Player.Message(p, "%aThe " + Name + " item is now disabled.");
                    Enabled = false; break;
                case "price":
                    Rank rnk = FindRank(args[2]);
                    if (rnk == null) {
                        Player.Message(p, "%cThat wasn't a rank or it's past the max rank (maxrank is: " + Group.Find(MaxRank).color + MaxRank + "%c)"); return; }
                    int cost;
                    if (!int.TryParse(args[3], out cost)) {
                        Player.Message(p, "\"" + args[3] + "\" is not a valid integer."); return;
                    }
                    Player.Message(p, "%aSuccesfully changed the rank price for " + rnk.group.color + rnk.group.name + " to: %f" + cost + " %3" + Server.moneys);
                    rnk.price = cost; break;

                case "maxrank":
                case "max":
                case "maximum":
                case "maximumrank":
                    Group grp = Group.Find(args[2]);
                    if (grp == null) { Player.Message(p, "%cThat wasn't a rank!"); return; }
                    if (p != null && p.Rank < grp.Permission) { Player.Message(p, "%cCan't set a maxrank that is higher than yours!"); return; }
                    MaxRank = args[2].ToLower();
                    Player.Message(p, "%aSuccessfully set max rank to: " + grp.ColoredName);
                    UpdatePrices();
                    break;
                default:
                    Player.Message(p, "Supported actions: enable, disable, price [rank] [pcost], maxrank [rank]"); break;
            }
        }

        protected internal override void OnStoreOverview(Player p) {
            Group maxrank = Group.Find(MaxRank);
            if (p == null || p.Rank >= maxrank.Permission) {
                Player.Message(p, "Rankup - &calready at max rank."); return;
            }
            Rank rnk = NextRank(p);
            Player.Message(p, "Rankup to " + rnk.group.color + rnk.group.name + " %S- costs &f" + rnk.price + " &3" + Server.moneys);
        }
        
        protected internal override void OnStoreCommand(Player p) {
            Group maxrank = Group.Find(MaxRank);
            Player.Message(p, "Syntax: %T/buy rankup");            
            Player.Message(p, "%fThe max buyable rank is: " + maxrank.ColoredName);
            Player.Message(p, "%cYou can only buy ranks one at a time, in sequential order.");
            
            foreach (Rank rnk in RanksList) {
                Player.Message(p, rnk.group.color + rnk.group.name + " costs &f" + rnk.price + " &3" + Server.moneys);
                if (rnk.group.name.CaselessEq(maxrank.name)) break;
            }
        }
        
        public Rank FindRank(string name) {
            foreach (Rank rank in RanksList) {
                if (rank.group.name != null && rank.group.name.CaselessEq(name))
                    return rank;
            }
            return null;
        }

        public Rank NextRank(Player p) {
            int index = Group.GroupList.IndexOf(p.group);
            if (index < Group.GroupList.Count - 1)
                return FindRank(Group.GroupList[index + 1].name);
            return null;
        }
        
        public void UpdatePrices() {
            int lasttrueprice = 0;
            foreach (Group group in Group.GroupList) {
                if (group.Permission > Group.Find(MaxRank).Permission) break;
                if (group.Permission <= Group.Find(Server.defaultRank).Permission) continue;
                
                Rank rank = FindRank(group.name);
                if (rank == null) {
                    rank = new Rank();
                    rank.group = group;
                    if (lasttrueprice == 0) { rank.price = 1000; }
                    else { rank.price = lasttrueprice + 250; }
                    RanksList.Add(rank);
                } else {
                    lasttrueprice = rank.price;
                }
            }
        }
    }
}
