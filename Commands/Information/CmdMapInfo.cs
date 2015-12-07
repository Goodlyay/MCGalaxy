/*
	Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
	
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
using System.IO;
namespace MCGalaxy.Commands
{
    public sealed class CmdMapInfo : Command
    {
        public override string name { get { return "mapinfo"; } }
        public override string shortcut { get { return "status"; } }
        public override string type { get { return CommandTypes.Information; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }
        public CmdMapInfo() { }

        public override void Use(Player p, string message)
        {
            Level foundLevel;

            if (message == "") { foundLevel = p.level; }
            else foundLevel = Level.Find(message);

            if (foundLevel == null) { Player.SendMessage(p, "Could not find specified level."); return; }

            Player.SendMessage(p, "&b" + foundLevel.name + Server.DefaultColor + ": Width=" + foundLevel.Width.ToString() + " Height=" + foundLevel.Height.ToString() + " Depth=" + foundLevel.Length.ToString());

            switch (foundLevel.physics)
            {
                case 0: Player.SendMessage(p, "Physics are &cOFF" + Server.DefaultColor + " on &b" + foundLevel.name); break;
                case 1: Player.SendMessage(p, "Physics are &aNormal" + Server.DefaultColor + " on &b" + foundLevel.name); break;
                case 2: Player.SendMessage(p, "Physics are &aAdvanced" + Server.DefaultColor + " on &b" + foundLevel.name); break;
                case 3: Player.SendMessage(p, "Physics are &aHardcore" + Server.DefaultColor + " on &b" + foundLevel.name); break;
                case 4: Player.SendMessage(p, "Physics are &aInstant" + Server.DefaultColor + " on &b" + foundLevel.name); break;
            }

            try
            {
                Player.SendMessage(p, "Build rank = " + Group.findPerm(foundLevel.permissionbuild).color + Group.findPerm(foundLevel.permissionbuild).trueName + Server.DefaultColor + " : Visit rank = " + Group.findPerm(foundLevel.permissionvisit).color + Group.findPerm(foundLevel.permissionvisit).trueName);
            } catch (Exception e) { Server.ErrorLog(e); }

            Player.SendMessage(p, "BuildMax Rank = " + Group.findPerm(foundLevel.perbuildmax).color + Group.findPerm(foundLevel.perbuildmax).trueName + Server.DefaultColor + " : VisitMax Rank = " + Group.findPerm(foundLevel.pervisitmax).color + Group.findPerm(foundLevel.pervisitmax).trueName);

            if (foundLevel.guns)
            {
                Player.SendMessage(p, "&cGuns &eare &aonline &eon " + foundLevel.name + ".");
            }
            else
            {
                Player.SendMessage(p, "&cGuns &eare &coffline &eon " + foundLevel.name + ".");
            }

            if (Directory.Exists(@Server.backupLocation + "/" + foundLevel.name))
            {
                int latestBackup = Directory.GetDirectories(@Server.backupLocation + "/" + foundLevel.name).Length;
                Player.SendMessage(p, "Latest backup: &a" + latestBackup + Server.DefaultColor + " at &a" + Directory.GetCreationTime(@Server.backupLocation + "/" + foundLevel.name + "/" + latestBackup).ToString("yyyy-MM-dd HH:mm:ss")); // + Directory.GetCreationTime(@Server.backupLocation + "/" + latestBackup + "/").ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                Player.SendMessage(p, "No backups for this map exist yet.");
            }
            if (foundLevel.textureUrl != "")
            {
                Player.SendMessage(p, "TexturePack: %b" + foundLevel.textureUrl);
            }
            else if (foundLevel == Server.mainLevel && Server.defaultTextureUrl != "")
            {
                Player.SendMessage(p, "TexturePack: " + Server.defaultTextureUrl);
            }
            else
            {
                Player.SendMessage(p, "No textures for this map exist yet.");
            }
            if (foundLevel.FogColor != null) { Player.SendMessage(p, "Fog Color: %b" + foundLevel.FogColor.ToString()); }
            else { Player.SendMessage(p, "Fog Color: %bnone");  }
            if (foundLevel.CloudColor != null) { Player.SendMessage(p, "Cloud Color: %b" + foundLevel.CloudColor.ToString()); }
            else { Player.SendMessage(p, "Cloud Color: %bnone"); }
            if (foundLevel.SkyColor != null) { Player.SendMessage(p, "Sky Color: %b" + foundLevel.SkyColor.ToString()); }
            else { Player.SendMessage(p, "Sky Color: %bnone"); }
            if (foundLevel.ShadowColor != null) { Player.SendMessage(p, "Shadow Color: %b" + foundLevel.ShadowColor.ToString()); }
            else { Player.SendMessage(p, "Shadow Color: %bnone"); }
            if (foundLevel.LightColor != null) { Player.SendMessage(p, "Sunlight Color: %b" + foundLevel.LightColor.ToString()); }
            else { Player.SendMessage(p, "Sunlight Color: %bnone"); }
            if (foundLevel.EdgeLevel != -1) { Player.SendMessage(p, "Water Level: %b" + foundLevel.EdgeLevel.ToString()); }
            else { Player.SendMessage(p, "Water Level: %bdefault"); }
            if (foundLevel.EdgeBlock != Block.blackrock) { Player.SendMessage(p, "Edge Block: %b" + foundLevel.EdgeBlock.ToString()); }
            else { Player.SendMessage(p, "Edge Block: %bdefault"); }
            if (foundLevel.HorizonBlock != Block.water) { Player.SendMessage(p, "Horizon Block: %b" + foundLevel.HorizonBlock.ToString()); }
            else { Player.SendMessage(p, "Horizon Block: %bdefault"); }
        }
        public override void Help(Player p)
        {
            Player.SendMessage(p, "/mapinfo <map> - Display details of <map>");
        }
    }
}