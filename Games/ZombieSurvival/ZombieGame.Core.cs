﻿/*
    Copyright 2010 MCLawl Team - 
    Created by Snowl (David D.) and Cazzar (Cayde D.)

    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.osedu.org/licenses/ECL-2.0
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;

namespace MCGalaxy {
    
    public sealed partial class ZombieGame {
        
        void MainLoop() {
            if (gameStatus == 0) return;
            if (!initialChangeLevel) {
                ChangeLevel();
                initialChangeLevel = true;
            }

            while (true) {
                zombieRound = false;
                amountOfRounds++;
                
                if (gameStatus == 0) { return; }
                else if (gameStatus == 1) { DoRound(); if (ChangeLevels) ChangeLevel();}
                else if (gameStatus == 2) { DoRound(); if (ChangeLevels) ChangeLevel(); gameStatus = 0; return; }
                else if (gameStatus == 3)
                {
                    if (limitRounds == amountOfRounds) { ResetState(); return; }
                    else { DoRound(); if (ChangeLevels) ChangeLevel(); }
                }
                else if (gameStatus == 4) { ResetState(); return; }
            }
        }

        void DoRound()
        {
            if (gameStatus == 0) return;
    GoBack: Player.GlobalMessage("%4Round Start:%f 2:00");
            Thread.Sleep(60000); if (!Server.ZombieModeOn) return;
            Player.GlobalMessage("%4Round Start:%f 1:00");
            Thread.Sleep(55000); if (!Server.ZombieModeOn) return;
            Server.s.Log(Convert.ToString(ChangeLevels) + " " + Convert.ToString(Server.ZombieOnlyServer) + " " + Convert.ToString(UseLevelList) + " " + string.Join(",", LevelList.ToArray()));
            Player.GlobalMessage("%4Round Start:%f 5...");
            Thread.Sleep(1000); if (!Server.ZombieModeOn) return;
            Player.GlobalMessage("%4Round Start:%f 4...");
            Thread.Sleep(1000); if (!Server.ZombieModeOn) return;
            Player.GlobalMessage("%4Round Start:%f 3...");
            Thread.Sleep(1000); if (!Server.ZombieModeOn) return;
            Player.GlobalMessage("%4Round Start:%f 2...");
            Thread.Sleep(1000); if (!Server.ZombieModeOn) return;
            Player.GlobalMessage("%4Round Start:%f 1...");
            Thread.Sleep(1000); if (!Server.ZombieModeOn) return;
            zombieRound = true;
            int playerscountminusref = 0; List<Player> players = new List<Player>();
            
            Player[] online = PlayerInfo.Online; 
            foreach (Player p in online) {
                if (p.referee) {
                    p.color = p.group.color;
                } else if (p.level.name == currentLevelName) {
                    p.color = p.group.color;
                    players.Add(p);
                    playerscountminusref++;
                }
            }
            if (playerscountminusref < 2) {
                Player.GlobalMessage(Colors.red + "ERROR: Need more than 2 players to play");
                goto GoBack;
            }

            theEnd:
            Random random = new Random();
            int firstinfect = random.Next(players.Count());
            Player player = null;
            if (queZombie)
                player = PlayerInfo.Find(nextZombie);
            else
                player = players[firstinfect];

            if (player.level.name != currentLevelName) goto theEnd;

            Player.GlobalMessage(player.color + player.name + Server.DefaultColor + " started the infection!");
            player.infected = true;
            player.color = Colors.red;
            Player.GlobalDespawn(player, false);
            Player.GlobalSpawn(player, player.pos[0], player.pos[1], player.pos[2], player.rot[0], player.rot[1], false);

            zombieRound = true;
            int roundMins = random.Next(5, 12);
            Player.GlobalMessage("The round will last for " + roundMins + " minutes!");
            timer = new System.Timers.Timer(roundMins * 60 * 1000);
            timer.Elapsed += new ElapsedEventHandler(EndRound);
            timer.Enabled = true;

            online = PlayerInfo.Online;
            foreach (Player p in online)
            {
                if(p != player)
                alive.Add(p);
            }

            infectd.Clear();
            if (queZombie)
            infectd.Add(PlayerInfo.Find(nextZombie));
            else
            infectd.Add(player);
            aliveCount = alive.Count;

            while (aliveCount > 0)
            {
                aliveCount = alive.Count;
                infectd.ForEach(delegate(Player pKiller)
                {
                    if (pKiller.color != Colors.red)
                    {
                        pKiller.color = Colors.red;
                        Player.GlobalDespawn(pKiller, false);
                        Player.GlobalSpawn(pKiller, pKiller.pos[0], pKiller.pos[1], pKiller.pos[2], pKiller.rot[0], pKiller.rot[1], false);
                    }
                    alive.ForEach(delegate(Player pAlive)
                    {
                        if (pAlive.color != pAlive.group.color)
                        {
                            pAlive.color = pAlive.group.color;
                            Player.GlobalDespawn(pAlive, false);
                            Player.GlobalSpawn(pAlive, pAlive.pos[0], pAlive.pos[1], pAlive.pos[2], pAlive.rot[0], pAlive.rot[1], false);
                        }
                        if (Math.Abs(pAlive.pos[0] / 32 - pKiller.pos[0] / 32) <= 1 && Math.Abs(pAlive.pos[1] / 32 - pKiller.pos[1] / 32) <= 1
                            && Math.Abs(pAlive.pos[2] / 32 - pKiller.pos[2] / 32) <= 1) {
                            if (!pAlive.infected && pKiller.infected && !pAlive.referee && !pKiller.referee && pKiller != pAlive && pKiller.level.name == currentLevelName && pAlive.level.name == currentLevelName)
                            {
                                pAlive.infected = true;
                                infectd.Add(pAlive);
                                alive.Remove(pAlive);
                                players.Remove(pAlive);
                                pAlive.blockCount = 25;
                                if (lastPlayerToInfect == pKiller.name)
                                {
                                    infectCombo++;
                                    if (infectCombo >= 2)
                                    {
                                        pKiller.SendMessage("You gained " + (4 - infectCombo) + " " + Server.moneys);
                                        pKiller.money = pKiller.money + 4 - infectCombo;
                                        Player.GlobalMessage(pKiller.color + pKiller.name + " is on a rampage! " + (infectCombo + 1) + " infections in a row!");
                                    }
                                }
                                else
                                {
                                    infectCombo = 0;
                                }
                                lastPlayerToInfect = pKiller.name;
                                pKiller.infectThisRound++;
                                int cazzar = random.Next(0, infectMessages.Length);
                                if (infectMessages2[cazzar] == "")
                                {
                                    Player.GlobalMessage(Colors.red + pKiller.name + Colors.yellow + infectMessages[cazzar] + Colors.red + pAlive.name);
                                }
                                else if (infectMessages[cazzar] == "")
                                {
                                    Player.GlobalMessage(Colors.red + pAlive.name + Colors.yellow + infectMessages2[cazzar]);
                                }
                                else
                                {
                                    Player.GlobalMessage(Colors.red + pKiller.name + Colors.yellow + infectMessages[cazzar] + Colors.red + pAlive.name + Colors.yellow + infectMessages2[cazzar]);
                                }
                                pAlive.color = Colors.red;
                                pKiller.playersInfected = pKiller.playersInfected++;
                                Player.GlobalDespawn(pAlive, false);
                                Player.GlobalSpawn(pAlive, pAlive.pos[0], pAlive.pos[1], pAlive.pos[2], pAlive.rot[0], pAlive.rot[1], false);
                                Thread.Sleep(500);
                            }
                        }
                    });
                });
                Thread.Sleep(500);
            }
            if (gameStatus == 0)
            {
                gameStatus = 4;
                return;
            }
            else
            {
                HandOutRewards();
            }
        }

        public void EndRound(object sender, ElapsedEventArgs e)
        {
            if (gameStatus == 0) return;
            Player.GlobalMessage("%4Round End:%f 5"); Thread.Sleep(1000);
            Player.GlobalMessage("%4Round End:%f 4"); Thread.Sleep(1000);
            Player.GlobalMessage("%4Round End:%f 3"); Thread.Sleep(1000);
            Player.GlobalMessage("%4Round End:%f 2"); Thread.Sleep(1000);
            Player.GlobalMessage("%4Round End:%f 1"); Thread.Sleep(1000);
            HandOutRewards();
        }

        public void HandOutRewards()
        {
            zombieRound = false;
            if (gameStatus == 0) return;
            Player.GlobalMessage(Colors.lime + "The game has ended!");
            if(aliveCount == 0)
                Player.GlobalMessage(Colors.maroon + "Zombies have won this round.");
            else
                Player.GlobalMessage(Colors.green + "Congratulations to our survivor(s)");
            timer.Enabled = false;
            string playersString = "";
            Player[] online = null;
            if (aliveCount == 0)
            {
                online = PlayerInfo.Online;
                foreach (Player winners in online)
                {
                    if (winners.level.name == currentLevelName)
                    {
                        winners.blockCount = 50;
                        winners.infected = false;
                        winners.infectThisRound = 0;
                        if (winners.level.name == currentLevelName)
                        {
                            winners.color = winners.group.color;
                            playersString += winners.group.color + winners.name + Colors.white + ", ";
                        }
                    }
                }
            }
            else
            {
                alive.ForEach(delegate(Player winners)
                {
                        winners.blockCount = 50;
                        winners.infected = false;
                        winners.infectThisRound = 0;
                    if (winners.level.name == currentLevelName)
                    {
                        winners.color = winners.group.color;
                        playersString += winners.group.color + winners.name + Colors.white + ", ";
                    }
                });
            }
            Player.GlobalMessage(playersString);
            online = PlayerInfo.Online;
            foreach (Player winners in online)
            {
                if (!winners.CheckIfInsideBlock() && aliveCount == 0 && winners.level.name == currentLevelName)
                {
                    Player.GlobalDespawn(winners, false);
                    Player.GlobalSpawn(winners, winners.pos[0], winners.pos[1], winners.pos[2], winners.rot[0], winners.rot[1], false);
                    Random random2 = new Random();
                    int randomInt = 0;
                    if (winners.playersInfected > 5)
                    {
                        randomInt = random2.Next(1, winners.playersInfected);
                    }
                    else
                    {
                        randomInt = random2.Next(1, 5);
                    }
                    Player.SendMessage(winners, Colors.gold + "You gained " + randomInt + " " + Server.moneys);
                    winners.blockCount = 50;
                    winners.playersInfected = 0;
                    winners.money = winners.money + randomInt;
                }
                else if (!winners.CheckIfInsideBlock() && (aliveCount == 1 && !winners.infected) && winners.level.name == currentLevelName)
                {
                    Player.GlobalDespawn(winners, false);
                    Player.GlobalSpawn(winners, winners.pos[0], winners.pos[1], winners.pos[2], winners.rot[0], winners.rot[1], false);
                    Random random2 = new Random();
                    int randomInt = 0;
                    randomInt = random2.Next(1, 15);
                    Player.SendMessage(winners, Colors.gold + "You gained " + randomInt + " " + Server.moneys);
                    winners.blockCount = 50;
                    winners.playersInfected = 0;
                    winners.money = winners.money + randomInt;
                }
                else if (winners.level.name == currentLevelName)
                {
                    winners.SendMessage("You may not hide inside a block! No " + Server.moneys + " for you!");
                }
            }
            try {alive.Clear(); infectd.Clear(); } catch{ }
            online = PlayerInfo.Online; 
            foreach (Player player in online)
            {
                player.infected = false;
                player.color = player.group.color;
                Player.GlobalDespawn(player, false);
                Player.GlobalSpawn(player, player.pos[0], player.pos[1], player.pos[2], player.rot[0], player.rot[1], false);
                if (player.level.name == currentLevelName)
                {
                    if (player.referee)
                    {
                        player.SendMessage("You gained one " + Server.moneys + " because you're a ref. Would you like a medal as well?");
                        player.money++;
                    }
                }
            }
            return;
        }

        public void ChangeLevel()
        {
            if (queLevel)
            {
                ChangeLevel(nextLevel, Server.ZombieOnlyServer);
            }
            try
            {
                if (ChangeLevels)
                {
                    ArrayList al = new ArrayList();
                    DirectoryInfo di = new DirectoryInfo("levels/");
                    FileInfo[] fi = di.GetFiles("*.lvl");
                    foreach (FileInfo fil in fi)
                    {
                        al.Add(fil.Name.Split('.')[0]);
                    }

                    if (al.Count <= 2 && !UseLevelList) { Server.s.Log("You must have more than 2 levels to change levels in Zombie Survival"); return; }

                    if (LevelList.Count < 2 && UseLevelList) { Server.s.Log("You must have more than 2 levels in your level list to change levels in Zombie Survival"); return; }

                    string selectedLevel1 = "";
                    string selectedLevel2 = "";

                LevelChoice:
                    Random r = new Random();
                    int x = 0;
                    int x2 = 1;
                    string level = ""; string level2 = "";
                    if (!UseLevelList)
                    {
                        x = r.Next(0, al.Count);
                        x2 = r.Next(0, al.Count);
                        level = al[x].ToString();
                        level2 = al[x2].ToString();
                    }
                    else
                    {
                        x = r.Next(0, LevelList.Count());
                        x2 = r.Next(0, LevelList.Count());
                        level = LevelList[x].ToString();
                        level2 = LevelList[x2].ToString();
                    }
                    Level current = Server.mainLevel;

                    if (lastLevelVote1 == level || lastLevelVote2 == level2 || lastLevelVote1 == level2 || lastLevelVote2 == level || current == LevelInfo.Find(level) || currentZombieLevel == level || current == LevelInfo.Find(level2) || currentZombieLevel == level2)
                        goto LevelChoice;
                    else if (selectedLevel1 == "") { selectedLevel1 = level; goto LevelChoice; }
                    else
                        selectedLevel2 = level2;

                    Level1Vote = 0; Level2Vote = 0; Level3Vote = 0;
                    lastLevelVote1 = selectedLevel1; lastLevelVote2 = selectedLevel2;

                    if (gameStatus == 4 || gameStatus == 0) { return; }

                    if (initialChangeLevel)
                    {
                        Server.votingforlevel = true;
                        Player.GlobalMessage(" " + Colors.black + "Level Vote: %S" + selectedLevel1 + ", " + selectedLevel2 + 
                                             " or random " + "(" + Colors.lime + "1%S/" + Colors.red + "2%S/" + Colors.blue + "3%S)");
                        System.Threading.Thread.Sleep(15000);
                        Server.votingforlevel = false;
                    }
                    else { Level1Vote = 1; Level2Vote = 0; Level3Vote = 0; }

                    if (gameStatus == 4 || gameStatus == 0) { return; }

                    if (Level1Vote >= Level2Vote)
                    {
                        if (Level3Vote > Level1Vote && Level3Vote > Level2Vote)
                        {
                            r = new Random();
                            int x3 = r.Next(0, al.Count);
                            ChangeLevel(al[x3].ToString(), Server.ZombieOnlyServer);
                        }
                        ChangeLevel(selectedLevel1, Server.ZombieOnlyServer);
                    }
                    else
                    {
                        if (Level3Vote > Level1Vote && Level3Vote > Level2Vote)
                        {
                            r = new Random();
                            int x4 = r.Next(0, al.Count);
                            ChangeLevel(al[x4].ToString(), Server.ZombieOnlyServer);
                        }
                        ChangeLevel(selectedLevel2, Server.ZombieOnlyServer);
                    }
                    Player[] online = PlayerInfo.Online; 
                    foreach (Player winners in online) {
                        winners.voted = false;
                    }
                }
            }
            catch { }
        }
    }
}