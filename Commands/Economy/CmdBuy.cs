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
using MCGalaxy.Eco;

namespace MCGalaxy.Commands {
    
    /// <summary> Economy Beta v1.0 QuantumHive </summary>
    public sealed class CmdBuy : Command {
        public override string name { get { return "buy"; } }
        public override string shortcut { get { return "purchase"; } }
        public override string type { get { return CommandTypes.Economy; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
        public override CommandEnable Enabled { get { return CommandEnable.Economy; } }
        
        public override void Use(Player p, string message) {
            if (Player.IsSuper(p)) { MessageInGameOnly(p); return; }
            string[] parts = message.Split(' ');

            foreach (Item item in Economy.Items)
                foreach (string alias in item.Aliases)
            {
                if (parts[0].CaselessEq(alias)) {
                    if (!item.Enabled) {
                        Player.Message(p, "%cThe " + item.Name + " item is not currently buyable."); return;
                    }
                    item.OnBuyCommand(this, p, message, parts); 
                    return;
                }
            }
            Help(p);
        }
        
        public override void Help(Player p) {            
            Player.Message(p, "%T/buy [item] [value] <map name>");
            Player.Message(p, "%Hmap name is only used for %T/buy map%H.");
            Player.Message(p, "%HUse %T/store <type> %Hto see the information for an item.");
            Player.Message(p, "%H  Available items: %f" + Economy.GetItemNames(", "));
        }
    }
}
