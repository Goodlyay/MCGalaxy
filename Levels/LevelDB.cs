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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MCGalaxy.SQL;
using MCGalaxy.SQL.Native;

namespace MCGalaxy {
    public static class LevelDB {
        
        public unsafe static void SaveBlockDB(Level lvl) {
            if (lvl.blockCache.Count == 0) return;
            if (!lvl.UseBlockDB) { lvl.blockCache.Clear(); return; }
            List<Level.BlockPos> tempCache = lvl.blockCache;
            string date = new String('-', 19); //yyyy-mm-dd hh:mm:ss
            
            fixed (char* ptr = date) {
                ptr[4] = '-'; ptr[7] = '-'; ptr[10] = ' '; ptr[13] = ':'; ptr[16] = ':';
                using (BulkTransaction bulk = BulkTransaction.CreateNative())
                    DoSaveChanges(tempCache, ptr, lvl, date, bulk);
            }
            tempCache.Clear();
            lvl.blockCache = new List<Level.BlockPos>();
            Server.s.Log("Saved BlockDB changes for:" + lvl.name, true);
        }
        
        unsafe static bool DoSaveChanges(List<Level.BlockPos> tempCache, char* ptr, 
                                         Level lvl, string date, BulkTransaction transaction) {
            string template = "INSERT INTO `Block" + lvl.name +
                "` (Username, TimePerformed, X, Y, Z, type, deleted) VALUES (@Name, @Time, @X, @Y, @Z, @Tile, @Del)";
            ushort x, y, z;
            
            IDbCommand cmd = BulkTransaction.CreateCommand(template, transaction);
            if (cmd == null) return false;
            
            IDataParameter nameP = transaction.CreateParam("@Name", DbType.AnsiStringFixedLength); cmd.Parameters.Add(nameP);
            IDataParameter timeP = transaction.CreateParam("@Time", DbType.AnsiStringFixedLength); cmd.Parameters.Add(timeP);
            IDataParameter xP = transaction.CreateParam("@X", DbType.UInt16); cmd.Parameters.Add(xP);
            IDataParameter yP = transaction.CreateParam("@Y", DbType.UInt16); cmd.Parameters.Add(yP);
            IDataParameter zP = transaction.CreateParam("@Z", DbType.UInt16); cmd.Parameters.Add(zP);
            IDataParameter tileP = transaction.CreateParam("@Tile", DbType.Byte); cmd.Parameters.Add(tileP);
            IDataParameter delP = transaction.CreateParam("@Del", DbType.Boolean); cmd.Parameters.Add(delP);
            bool isNative = transaction is NativeBulkTransaction;
            
            for (int i = 0; i < tempCache.Count; i++) {
                Level.BlockPos bP = tempCache[i];
                lvl.IntToPos(bP.index, out x, out y, out z);
                DateTime time = Server.StartTimeLocal.AddTicks((bP.flags >> 2) * TimeSpan.TicksPerSecond);
                MakeInt(time.Year, 4, 0, ptr); MakeInt(time.Month, 2, 5, ptr); MakeInt(time.Day, 2, 8, ptr);
                MakeInt(time.Hour, 2, 11, ptr); MakeInt(time.Minute, 2, 14, ptr); MakeInt(time.Second, 2, 17, ptr);
                
                // For NativeParameter, we make the optimisation of avoiding boxing primitive types.
                if (!isNative) {
                    nameP.Value = bP.name;
                    timeP.Value = date;
                    xP.Value = x; yP.Value = y; zP.Value = z;
                    tileP.Value = (bP.flags & 2) != 0 ? Block.custom_block : bP.rawType;
                    delP.Value = (bP.flags & 1) != 0;
                } else {
                    ((NativeParameter)nameP).SetString(bP.name);
                    ((NativeParameter)timeP).SetString(date);
                    ((NativeParameter)xP).U16Value = x;
                    ((NativeParameter)yP).U16Value = y;
                    ((NativeParameter)zP).U16Value = z;
                    ((NativeParameter)tileP).U8Value = (bP.flags & 2) != 0 ? Block.custom_block : bP.rawType;
                    ((NativeParameter)delP).BoolValue = (bP.flags & 1) != 0;
                }

                if (!BulkTransaction.Execute(template, cmd)) {
                    cmd.Dispose();
                    cmd.Parameters.Clear();
                    transaction.Rollback(); return false;
                }
            }
            cmd.Dispose();
            cmd.Parameters.Clear();
            transaction.Commit();
            return true;
        }
        
        unsafe static void MakeInt(int value, int chars, int offset, char* ptr) {
            for (int i = 0; i < chars; i++, value /= 10) {
                char c = (char)('0' + (value % 10));
                ptr[offset + (chars - 1 - i)] = c;
            }
        }
        
        public static void CreateTables(string givenName) {
            Database.executeQuery(String.Format(createBlock, givenName, Server.useMySQL ? "BOOL" : "INT"));
            Database.executeQuery(String.Format(createPortals, givenName));
            Database.executeQuery(String.Format(createMessages, givenName));
            Database.executeQuery(String.Format(createZones, givenName));
        }
        
        internal static void LoadZones(Level level, string name) {
            using (DataTable table = Database.fillData("SELECT * FROM `Zone" + name + "`")) {
                Level.Zone Zn;
                foreach (DataRow row in table.Rows) {
                    Zn.smallX = ushort.Parse(row["SmallX"].ToString());
                    Zn.smallY = ushort.Parse(row["SmallY"].ToString());
                    Zn.smallZ = ushort.Parse(row["SmallZ"].ToString());
                    Zn.bigX = ushort.Parse(row["BigX"].ToString());
                    Zn.bigY = ushort.Parse(row["BigY"].ToString());
                    Zn.bigZ = ushort.Parse(row["BigZ"].ToString());
                    Zn.Owner = row["Owner"].ToString();
                    level.ZoneList.Add(Zn);
                }
            }
        }
        
        internal static void LoadMetadata(Level level, string name) {
            using (DataTable table = Database.fillData("SELECT * FROM `Portals" + name + "`")) {
                foreach (DataRow row in table.Rows) {
                    byte tile = level.GetTile(ushort.Parse(row["EntryX"].ToString()),
                                              ushort.Parse(row["EntryY"].ToString()),
                                              ushort.Parse(row["EntryZ"].ToString()));
                    if (Block.portal(tile)) continue;
                    
                    Database.executeQuery("DELETE FROM `Portals" + name + "` WHERE EntryX=" + row["EntryX"]
                                          + " AND EntryY=" + row["EntryY"] + " AND EntryZ=" + row["EntryZ"]);
                }
            }
            
            using (DataTable table = Database.fillData("SELECT * FROM `Messages" + name + "`")) {
                foreach (DataRow row in table.Rows) {
                    byte tile = level.GetTile(ushort.Parse(row["X"].ToString()),
                                              ushort.Parse(row["Y"].ToString()),
                                              ushort.Parse(row["Z"].ToString()));
                    if (Block.mb(tile)) continue;
                    
                    //givenName is safe against SQL injections, it gets checked in CmdLoad.cs
                    Database.executeQuery("DELETE FROM `Messages" + name + "` WHERE X=" +
                                          row["X"] + " AND Y=" + row["Y"] + " AND Z=" + row["Z"]);
                }
            }
        }
		
		public static void DeleteZone(string level, Level.Zone zn) {
			ParameterisedQuery query = ParameterisedQuery.Create();
			query.AddParam("@Owner", zn.Owner);
			Database.executeQuery(query, "DELETE FROM `Zone" + level + "` WHERE Owner=@Owner" +
			                      " AND SmallX='" + zn.smallX + "' AND SMALLY='" +
			                      zn.smallY + "' AND SMALLZ='" + zn.smallZ + "' AND BIGX='" +
			                      zn.bigX + "' AND BIGY='" + zn.bigY + "' AND BIGZ='" + zn.bigZ + "'");
		}
		
		public static void CreateZone(string level, Level.Zone zn) {
			ParameterisedQuery query = ParameterisedQuery.Create();
			query.AddParam("@Owner", zn.Owner);
			Database.executeQuery(query, "INSERT INTO `Zone" + level + 
			                      "` (SmallX, SmallY, SmallZ, BigX, BigY, BigZ, Owner) VALUES ("
			                      + zn.smallX + ", " + zn.smallY + ", " + zn.smallZ + ", " 
			                      + zn.bigX + ", " + zn.bigY + ", " + zn.bigZ + ", @Owner)");
		}
		
        
        const string createBlock =
            @"CREATE TABLE if not exists `Block{0}` (
Username      CHAR(20),
TimePerformed DATETIME,
X             SMALLINT  UNSIGNED,
Y             SMALLINT UNSIGNED,
Z             SMALLINT UNSIGNED,
Type          TINYINT UNSIGNED,
Deleted       {1})";
        
        const string createPortals =
            @"CREATE TABLE if not exists `Portals{0}` (
EntryX  SMALLINT UNSIGNED,
EntryY  SMALLINT UNSIGNED,
EntryZ  SMALLINT UNSIGNED,
ExitMap CHAR(20),
ExitX   SMALLINT UNSIGNED,
ExitY   SMALLINT UNSIGNED,
ExitZ   SMALLINT UNSIGNED)";
        
        const string createMessages =
            @"CREATE TABLE if not exists `Messages{0}` (
X       SMALLINT UNSIGNED,
Y       SMALLINT UNSIGNED,
Z       SMALLINT UNSIGNED,
Message CHAR(255))";
        
        const string createZones =
            @"CREATE TABLE if not exists `Zone{0}` (
SmallX SMALLINT UNSIGNED,
SmallY SMALLINT UNSIGNED,
SmallZ SMALLINT UNSIGNED,
BigX   SMALLINT UNSIGNED,
BigY   SMALLINT UNSIGNED,
BigZ   SMALLINT UNSIGNED,
Owner  VARCHAR(20))";
    }
}