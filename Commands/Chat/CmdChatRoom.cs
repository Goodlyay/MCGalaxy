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
namespace MCGalaxy.Commands {
    
    public sealed class CmdChatRoom : Command {
        
        public override string name { get { return "chatroom"; } }
        public override string shortcut { get { return "cr"; } }
        public override string type { get { return CommandTypes.Chat; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }
        public override CommandPerm[] ExtraPerms {
            get { return new[] { 
                    new CommandPerm(LevelPermission.AdvBuilder, "+ can create chatrooms"),
                    new CommandPerm(LevelPermission.AdvBuilder, "+ can delete an empty chatroom"),
                    new CommandPerm(LevelPermission.Operator, "+ can delete a chatroom"),
                    new CommandPerm(LevelPermission.Operator, "+ can spy on a chatroom"),
                    new CommandPerm(LevelPermission.Operator, "+ can force a player to join a chatroom"),
                    new CommandPerm(LevelPermission.Operator, "+ can kick a player from a chatroom"),
                    new CommandPerm(LevelPermission.Operator, "+ can send a global message to a chatroom (without any delay)"),
                }; }
        }
        
        public override void Use(Player p, string message) {
            if (Player.IsSuper(p)) { MessageInGameOnly(p); return; }
            string[] parts = message.ToLower().Split(' ');
            
            if (parts.Length == 0) {
                if (Server.Chatrooms.Count == 0) {
                    Player.Message(p, "There are currently no rooms");
                } else {
                    Player.Message(p, "The current rooms are:");
                    foreach (string room in Server.Chatrooms)
                        Player.Message(p, room);
                }
                return;
            }
            
            switch (parts[0]) {
                case "join":
                    HandleJoin(p, parts); break;
                case "leave":
                    HandleLeave(p); break;
                case "make":
                case "create":
                    HandleCreate(p, parts); break;
                case "delete":
                case "remove":
                    HandleDelete(p, parts); break;
                case "spy":
                case "watch":
                    HandleSpy(p, parts); break;
                case "forcejoin":
                    HandleForceJoin(p, parts); break;
                case "kick":
                case "forceleave":
                    HandleKick(p, parts); break;
                case "globalmessage":
                case "global":
                case "all":
                    HandleAll(p, parts, message); break;
                default:
                    HandleOther(p, parts); break;
            }
        }
        
        void HandleJoin(Player p, string[] parts) {
            if (parts.Length > 1 && Server.Chatrooms.Contains(parts[1])) {
                string room = parts[1];
                if (p.spyChatRooms.Contains(room)) {
                    Player.Message(p, "The chat room '{0}' has been removed " +
                                      "from your spying list because you are joining the room.", room);
                    p.spyChatRooms.Remove(room);
                }
                
                Player.Message(p, "You joined the chat room '" + room + "'");
                Chat.ChatRoom(p, p.color + p.name + " %Shas joined your chat room", false, room);
                p.Chatroom = room;
            } else {
                Player.Message(p, "There is no chat room with that name");
            }
        }
        
        void HandleLeave(Player p) {
            Player.Message(p, "You left the chat room '" + p.Chatroom + "'");
            Chat.ChatRoom(p, p.color + p.name + " %Shas left the chat room", false, p.Chatroom);
            Player.GlobalMessage(p.color + p.name + " %Shas left their chat room " + p.Chatroom);
            p.Chatroom = null;
        }
        
        void HandleCreate(Player p, string[] parts) {
            if (!CheckExtraPerm(p, 1)) { MessageNeedExtra(p, "can create a chatroom.", 1); return; }
            if (parts.Length <= 1) {
                Player.Message(p, "You need to provide a new chatroom name.");
                return;
            }
            
            string room = parts[1];
            if (Server.Chatrooms.Contains(parts[1])) {
                Player.Message(p, "The chatoom '{0}' already exists", room);
            } else {
                Server.Chatrooms.Add(room);
                Player.GlobalMessage("A new chat room '" + room + "' has been created");
            }
        }
        
        void HandleDelete(Player p, string[] parts) {
            if (parts.Length <= 1) {
                Player.Message(p, "You need to provide a chatroom name to delete.");
                return;
            }
            string room = parts[1];
            bool canDeleteForce = CheckExtraPerm(p, 3);
            bool canDelete = CheckExtraPerm(p, 2);
            if (!canDelete && !canDeleteForce) {
                Player.Message(p, "You aren't a high enough rank to delete a chatroon.");
                return;
            }

            if (!Server.Chatrooms.Contains(room)) {
                Player.Message(p, "There is no chatroom with the name '{0}'", room); return;
            }
            
            if (!canDeleteForce) {
                Player[] players = PlayerInfo.Online.Items; 
                foreach (Player pl in players) {
                    if (pl != p && pl.Chatroom == room) {
                        Player.Message(p, "Sorry, someone else is in the chatroom"); return;
                    }
                }
            }
            
            Player.GlobalMessage(room + " is being deleted");
            if (p.Chatroom == room)
                HandleLeave(p);
            Server.Chatrooms.Remove(room);
            
            Player[] online = PlayerInfo.Online.Items;
            foreach (Player pl in online) {
                if (pl.Chatroom == room) {
                    pl.Chatroom = null;
                    Player.Message(pl, "You left the chatroom '{0}' because it is being deleted", room);
                }
                
                if (pl.spyChatRooms.Contains(room)) {
                    pl.spyChatRooms.Remove(room);
                    pl.SendMessage("Stopped spying on chatroom '" + room + 
                                   "' because it was deleted by: " + p.color + p.name);
                }
            }
            Player.GlobalMessage("The chatroom '" + room + "' has been deleted");
        }
        
        void HandleSpy(Player p, string[] parts) {
            if (!CheckExtraPerm(p, 4)) { MessageNeedExtra(p, "can spy on a chatroom.", 4); return; }
            if (parts.Length <= 1) {
                Player.Message(p, "You need to provide a chatroom name to spy on."); return;
            }
            
            string room = parts[1];
            if (Server.Chatrooms.Contains(room)) {
                if (p.Chatroom == room) {
                    Player.Message(p, "You cannot spy on your own room"); return;
                }
                
                if (p.spyChatRooms.Contains(room)) {
                    Player.Message(p, "'{0}' is already in your spying list.", room);
                } else {
                    p.spyChatRooms.Add(room);
                    Player.Message(p, "'{0}' has been added to your chat room spying list", room);
                }
            } else {
                Player.Message(p, "There is no chatroom with the name '{0}'", room);
            }
        }
        
        void HandleForceJoin(Player p, string[] parts) {
            if (!CheckExtraPerm(p, 5)) { MessageNeedExtra(p, "can force players to join a chatroom.", 5); return; }
            if (parts.Length <= 2) {
                Player.Message(p, "You need to provide a player name, then a chatroom name."); return;
            }
            
            string name = parts[1], room = parts[2];
            Player pl = PlayerInfo.FindMatches(p, name);
            if (pl == null) return;
            if (!Server.Chatrooms.Contains(room)) {
                Player.Message(p, "There is no chatroom with the name '{0}'", room); return;
            }
            if (pl.Rank >= p.Rank) { MessageTooHighRank(p, "force-join", false); return;}
            
            if (pl.spyChatRooms.Contains(room)) {
            	Player.Message(pl, "The chat room '{0}' has been removed from your spying list " +
                                   "because you are force joining the room '{0}'", room);
                pl.spyChatRooms.Remove(room);
            }
            
            Player.Message(pl, "You've been forced to join the chat room '{0}'", room);
            Chat.ChatRoom(pl, pl.ColoredName + " %Shas force joined your chat room", false, room);
            pl.Chatroom = room;
            Player.Message(p, pl.ColoredName + " %Swas forced to join the chatroom '{0}' by you", room);
        }
        
        void HandleKick(Player p, string[] parts) {
            if (!CheckExtraPerm(p, 6)) { MessageNeedExtra(p, "can kick players from a chatroom.", 6); return; }
            if (parts.Length <= 1) {
                Player.Message(p, "You need to provide a player name.");
                return;
            }
            
            string name = parts[1];
            Player pl = PlayerInfo.FindMatches(p, name);
            if (pl == null) return;
            if (pl.Rank >= p.Rank) {
            	MessageTooHighRank(p, "kick from a chatroom", false); return;
            }
            
            Player.Message(pl, "You were kicked from the chat room '" + pl.Chatroom + "'");
            Player.Message(p, pl.color + pl.name + " %Swas kicked from the chat room '" + pl.Chatroom + "'");
            Chat.ChatRoom(pl, pl.color + pl.name + " %Swas kicked from your chat room", false, pl.Chatroom);
            pl.Chatroom = null;
        }
        
        void HandleAll(Player p, string[] parts, string message) {
            int length = parts.Length > 1 ? parts[0].Length + 1 : parts[0].Length;
            message = message.Substring( length );
            if (CheckExtraPerm(p, 7)) {
                Chat.GlobalChatRoom(p, message, true);
                return;
            }
            
            if (p.lastchatroomglobal.AddSeconds(30) < DateTime.UtcNow) {
                Chat.GlobalChatRoom(p, message, true);
                p.lastchatroomglobal = DateTime.UtcNow;
            } else {
                Player.Message(p, "Sorry, you must wait 30 seconds in between each global chatroom message!!");
            }
        }
        
        void HandleOther(Player p, string[] parts) {
            string room = parts[0];
            if (Server.Chatrooms.Contains(room)) {
                Player.Message(p, "Players in room '" + room + "' :");
                Player[] players = PlayerInfo.Online.Items;
                foreach (Player pl in players) {
                    if (pl.Chatroom == room)
                        Player.Message(p, pl.color + pl.name);
                }
            } else {
                Player.Message(p, "There is no command with the type '" + room + "'," +
                                   "nor is there a chat room with that name.");
                Help(p);
            }
        }
        
        public override void Help(Player p) {
            Player.Message(p, "/chatroom - gets a list of all the current rooms");
            Player.Message(p, "/chatroom [room] - gives you details about the room");
            Player.Message(p, "/chatroom join [room] - joins a room");
            Player.Message(p, "/chatroom leave [room] - leaves a room");
            
            if (CheckExtraPerm(p, 1))
                Player.Message(p, "/chatroom create [room] - creates a new room");
            if (CheckExtraPerm(p, 3))
                Player.Message(p, "/chatroom delete [room] - deletes a room");
            else if (CheckExtraPerm(p, 2))
                Player.Message(p, "/chatroom delete [room] - deletes a room only if all people have left");
            
            if (CheckExtraPerm(p, 4))
                Player.Message(p, "/chatroom spy [room] - spy on a chatroom");
            if (CheckExtraPerm(p, 5))
                Player.Message(p, "/chatroom forcejoin [player] [room] - forces a player to join a room");
            if (CheckExtraPerm(p, 6))
                Player.Message(p, "/chatroom kick [player] - kicks the player from their current room");
            
            if (CheckExtraPerm(p, 7))
                Player.Message(p, "/chatroom all [message] - sends a global message to all rooms");
            else
                Player.Message(p, "/chatroom all [message] - sends a global message to all rooms " +
                                   "(limited to 1 every 30 seconds)");
        }
    }
}
