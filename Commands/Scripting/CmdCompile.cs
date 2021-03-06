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
using System.IO;
namespace MCGalaxy.Commands {
    public sealed class CmdCompile : Command {
        
        public override string name { get { return "compile"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return CommandTypes.Other; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public CmdCompile() { }

        public override void Use(Player p, string message) {
            if (message == "") { Help(p); return; }
            string[] args = message.Split(' ');
            
            Scripting engine = null;
            if (args.Length == 1) {
                engine = Scripting.CS;
            } else if (args[1] == "vb") {
                engine = Scripting.VB;
            } else {
                Help(p); return;
            }

            if (!File.Exists(Scripting.SourceDir + "Cmd" + args[0] + engine.Ext)) {
            	Player.Message(p, "File &9Cmd" + args[0] + engine.Ext + " %Snot found!"); return;
            }
            bool success = false;
            try {
            	success = engine.Compile(args[0]);
            } catch (Exception e) {
                Server.ErrorLog(e);
                Player.Message(p, "An exception was thrown during compilation.");
                return;
            }
            
            if (success)
                Player.Message(p, "Compiled successfully.");
            else
                Player.Message(p, "Compilation error. Please check " + Scripting.ErrorPath + " for more information.");
        }

        public override void Help(Player p) {
            Player.Message(p, "%T/compile <class name>");
            Player.Message(p, "%HCompiles a command class file into a DLL.");
            Player.Message(p, "%T/compile <class name> vb");
            Player.Message(p, "%HCompiles a command class (that was written in visual basic) file into a DLL.");
            Player.Message(p, "%H  class name: &9Cmd&e<class name>&9.cs");
        }
    }
}
