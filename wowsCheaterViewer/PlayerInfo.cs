using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace wowsCheaterViewer
{
    public class playerInfo
    {
        private static string errorMessage = "error";
        public string name { get; set; }
        public string playerId { get; set; } = "0";//默认值给0，方便提供给yuyuko
        public string playerPrColor { get; set; } = "Gray";//默认灰色（隐藏战绩或读取失败）
        public bool isHidden { get; set; }
        public int relation { get; set; }


        public string clanTag { get; set; }
        public string clanId { get; set; } = "0";//默认值给0，方便提供给yuyuko
        public string clanColor { get; set; }


        private int setIntLevel;
        public string shipId { get; set; }
        public string shipName { get; set; }
        public int shipSort { get; set; }
        public string shipType { get; set; }
        public int shipLevel_int
        {
            get => setIntLevel;
            set
            {
                setIntLevel = value;
                setRomanLevel(setIntLevel);
            }
        }
        public string shipLevel_roman { get; set; }
        private void setRomanLevel(int intLevel)//赋值阿拉伯数字的同时转换成罗马数字
        {
            int[] nums = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            string[] romans = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            int num = setIntLevel;
            for (int i = 0; i < 13; i++)
            {
                while (num >= nums[i])
                {
                    shipLevel_roman = shipLevel_roman + romans[i];
                    num -= nums[i];
                }
            }
            shipLevel_roman = string.Format("{0,-4}", shipLevel_roman);
        }
        public void setShipSort()//进行船排序
        {
            //船只排序：舰种(2)+等级(2)+不知道(2)
            string[] typeSort = { "Submarine", "Destroyer", "Cruiser", "Battleship", "AirCarrier" };
            string type = Array.IndexOf(typeSort, shipType).ToString();
            string unknowSort = null;
            string shipSortStr = string.Format("{0}{1}{2}",
                string.Format("{0:D2}", type),
                string.Format("{0:D2}", shipLevel_int),
                string.Format("{0:D2}", unknowSort));
            shipSort = Convert.ToInt32(shipSortStr);
        }

        public string banMatch { get; set; }
        public string banMatch_fullStr { get; set; }
        public string banColor { get; set; }


        public string battleCount_ship { get; set; }
        public string winRate_ship { get; set; } = errorMessage;//默认读取失败
        public string battleCount_pvp { get; set; }
        public string winRate_pvp { get; set; }
        public string battleCount_rank { get; set; }
        public string winRate_rank { get; set; }


        public string markMessage { get; set; }
        public string lastMarkMessage { get; set; }


        
        private static readonly object writerLock = new object();
        apiClient apiClient = new apiClient();
        Logger Logger = Logger.Instance;
        Config Config = Config.Instance;
        private string clanEmptyFilePath;
        private List<string> tempFiles = new List<string>();
        public void SetBasePlayerInfo(PlayerGameInfoInRep PlayerGameInfoInRep)//根据rep里的内容获取基础信息
        {
            name = PlayerGameInfoInRep.name;
            shipId = PlayerGameInfoInRep.shipId;
            relation = PlayerGameInfoInRep.relation;
        }

        public void GetPlayerId()//获取玩家id
        {
            try
            {
                JObject result_getPlayerId = JObject.Parse(apiClient.GetPlayerId(name));
                if (JArray.Parse(result_getPlayerId["data"].ToString()).Count == 0)
                    throw new Exception("未能获取玩家id，可能已改名。");
                playerId = result_getPlayerId["data"][0]["spa_id"].ToString();
                isHidden = Convert.ToBoolean(result_getPlayerId["data"][0]["hidden"]);
            }
            catch (Exception ex) { throw new Exception("获取玩家id失败，" + ex.Message); }
        }


        public void SetEmptyClanFile()//写入空文件用于上传军团信息
        {
            clanEmptyFilePath = writeFile(playerId + "_clan.txt", "");
            tempFiles.Add(clanEmptyFilePath);
        }

        public void DeleteTempFiles()//删除临时文件
        {
            foreach(string tempFile in tempFiles)
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
        }

        private string writeFile(string fileName, string fileBody)
        {
            string saveFile = Path.Combine(Config.tempFolderPath, fileName);
            lock (writerLock)
            {
                if (!Directory.Exists(Config.tempFolderPath))
                    Directory.CreateDirectory(Config.tempFolderPath);
                StreamWriter sw = new StreamWriter(saveFile);
                sw.Write(fileBody);
                sw.Close();
            }
            return saveFile;
        }

        public void GetBattleInfo_withBattleType(string battleType)//根据战斗类型获取玩家信息
        {
            battleType = battleType.ToLower();
            string infoFilePath_battleType = null;
            if (!isHidden)
            {
                string PlayersShipsInfo_battleType = null;
                try
                {
                    PlayersShipsInfo_battleType = apiClient.GetPalyersShipsInfo_official(playerId, battleType);
                    infoFilePath_battleType = writeFile(playerId + "_" + battleType + ".json", PlayersShipsInfo_battleType);
                    string ParsedPlayerInfoStr_battleType = apiClient.GetParsedPlayerInfo_yuyuko(playerId, shipId, battleType, infoFilePath_battleType, clanEmptyFilePath);
                    JObject ParsedPlayerInfoJo_battleType = JObject.Parse(ParsedPlayerInfoStr_battleType);
                    if (battleType == "pvp")
                    {
                        battleCount_pvp = ParsedPlayerInfoJo_battleType["userInfo"][0]["shipInfo"]["battleInfo"]["battle"].ToString();
                        winRate_pvp = ParsedPlayerInfoJo_battleType["userInfo"][0]["shipInfo"]["avgInfo"]["win"].ToString() + "%";
                        playerPrColor = ParsedPlayerInfoJo_battleType["userInfo"][0]["prInfo"]["color"].ToString();
                        battleCount_ship = ParsedPlayerInfoJo_battleType["shipInfo"][0]["shipInfo"]["battleInfo"]["battle"].ToString();
                        winRate_ship = ParsedPlayerInfoJo_battleType["shipInfo"][0]["shipInfo"]["avgInfo"]["win"].ToString() + "%";
                    }
                    else if (battleType == "rank_solo")
                    {
                        battleCount_rank = ParsedPlayerInfoJo_battleType["userInfo"][0]["shipInfo"]["battleInfo"]["battle"].ToString();
                        winRate_rank = ParsedPlayerInfoJo_battleType["userInfo"][0]["shipInfo"]["avgInfo"]["win"].ToString() + "%";
                    }
                }
                catch (Exception ex) { Logger.logWrite("无法读取玩家" + playerId + "的" + battleType + "信息，" + ex.Message); }
                finally { if(!string.IsNullOrEmpty(infoFilePath_battleType)) tempFiles.Add(infoFilePath_battleType); }
            }
            else
            {
                winRate_ship = "hidden";
            }
        }

        public void GetClanInfo()//获取军团信息
        {
            string PlayersClanInfoStr = null;
            try
            {
                PlayersClanInfoStr = apiClient.GetPalyersClansInfo_official(playerId);
                JObject PlayersClanInfoJo = JObject.Parse(PlayersClanInfoStr);
                if(!string.IsNullOrEmpty(PlayersClanInfoJo["data"]["clan_id"].ToString()))
                {
                    clanId = PlayersClanInfoJo["data"]["clan_id"].ToString();
                    clanTag = "[" + PlayersClanInfoJo["data"]["clan"]["tag"].ToString() + "]";
                    clanColor = "#" + Convert.ToInt32(PlayersClanInfoJo["data"]["clan"]["color"]).ToString("X");
                }
            }
            catch (Exception ex) { Logger.logWrite("无法读取玩家" + playerId + "的军团信息，" + ex.Message); }
        }

        public void GetShipInfo()//获取船信息
        {
            string shipInfoStr = null;
            try
            {
                ShipInfo ShipInfo = new ShipInfo();
                if (Config.shipInfo.Keys.Contains(shipId))//优先获取配置文件中存过的，没存过再调接口拿
                {
                    ShipInfo = Config.shipInfo[shipId];
                }
                else
                {
                    shipInfoStr = apiClient.GetShipInfo(shipId);
                    ShipInfo = JsonConvert.DeserializeObject<ShipInfo>(JObject.Parse(shipInfoStr)["data"].ToString());
                    Config.addShipInfo(shipId, ShipInfo);
                }
                shipLevel_int = ShipInfo.level;
                shipName = ShipInfo.nameCn;
                shipType = ShipInfo.shipType;
                setShipSort();
            }
            catch (Exception ex) { Logger.logWrite("无法读取玩家" + playerId + "的船" + shipId + "信息，" + ex.Message); }
        }

        public void GetBanInfo()//获取ban信息
        {
            string getPlayerBanInfoStr = null;
            try
            {
                getPlayerBanInfoStr = apiClient.GetPlayerBanInfo_yuyuko(playerId);
                JObject getPlayerBanInfoJo = JObject.Parse(getPlayerBanInfoStr);
                banMatch_fullStr = getPlayerBanInfoJo["data"]["voList"].ToString();
                List<Int32> banMatch_matchCountList = new List<Int32>();
                foreach (JToken banInfo in getPlayerBanInfoJo["data"]["voList"] as JArray) //将每一项的封禁匹配数加到list里
                    banMatch_matchCountList.Add(Convert.ToInt32(banInfo["banNameNamesake"]));
                if (banMatch_matchCountList.Contains(1))//如果有匹配值是1的，标记为红色
                    banColor = "Red";
                banMatch = string.Join(",", banMatch_matchCountList);
            }
            catch (Exception ex) { Logger.logWrite("无法读取玩家" + playerId + "的ban信息，" + ex.Message); }
        }

        public void GetMarkInfo()//获取标记信息
        {
            if (Config.mark.Keys.Contains(playerId))
            {
                //取该玩家标记内容下，按时间排序最晚的那个信息
                MarkInfo MarkInfo = Config.mark[playerId].OrderByDescending(i => i.markTime).First();
                markMessage = MarkInfo.markMessage;
                lastMarkMessage = "上次标记时间：" + MarkInfo.markTime + Environment.NewLine +
                    "上次标记时的军团：" + MarkInfo.clanTag + Environment.NewLine +
                    "上次标记时的名称：" + MarkInfo.name + Environment.NewLine +
                    "上次标记时的内容：" + MarkInfo.markMessage;
            }
        }
    
        public void CheckLoadFailed()//如果解析失败，试着直接读取yuyuko里的玩家数据进行补录
        {
            try
            {
                if (winRate_ship == "loadFailed")
                {
                    JObject result_getplayerInfo_yuyuko = JObject.Parse(apiClient.GetPlayerInfo_yuyuko(playerId));
                    //解析yuyuko玩家信息
                    if (result_getplayerInfo_yuyuko != null)
                    {
                        try
                        {
                            try
                            {
                                //新版结构
                                battleCount_pvp = result_getplayerInfo_yuyuko["data"]["battleTypeInfo"]["PVP"]["shipInfo"]["battleInfo"]["battle"].ToString();
                                winRate_pvp = result_getplayerInfo_yuyuko["data"]["battleTypeInfo"]["PVP"]["shipInfo"]["avgInfo"]["win"].ToString() + "%";
                                battleCount_rank = result_getplayerInfo_yuyuko["data"]["battleTypeInfo"]["RANK_SOLO"]["shipInfo"]["battleInfo"]["battle"].ToString();
                                winRate_rank = result_getplayerInfo_yuyuko["data"]["battleTypeInfo"]["RANK_SOLO"]["shipInfo"]["avgInfo"]["win"].ToString() + "%";
                                playerPrColor = result_getplayerInfo_yuyuko["data"]["prInfo"]["color"].ToString();
                            }
                            catch
                            {
                                //旧版结构
                                battleCount_pvp = result_getplayerInfo_yuyuko["data"]["pvp"]["battles"].ToString();
                                winRate_pvp = result_getplayerInfo_yuyuko["data"]["pvp"]["wins"].ToString() + "%";
                                battleCount_rank = result_getplayerInfo_yuyuko["data"]["rankSolo"]["battles"].ToString();
                                winRate_rank = result_getplayerInfo_yuyuko["data"]["rankSolo"]["wins"].ToString() + "%";
                                playerPrColor = result_getplayerInfo_yuyuko["data"]["pr"]["color"].ToString();
                            }
                        }
                        catch (Exception ex) { Logger.logWrite("无法解析yuyuko玩家信息，" + ex.Message); }
                    }
                    //根据船id获取yuyuko机器人中的船信息和玩家单船信息
                    JObject result_getPlayerShipInfo_yuyuko = JObject.Parse(apiClient.GetPlayerShipInfo_yuyuko(playerId, shipId));
                    //解析玩家单船信息
                    if (result_getPlayerShipInfo_yuyuko != null)
                    {
                        try
                        {
                            try
                            {
                                //新版结构
                                battleCount_ship = result_getPlayerShipInfo_yuyuko["data"]["typeInfo"]["PVP"]["battleInfo"]["battleInfo"]["battle"].ToString();
                                winRate_ship = result_getPlayerShipInfo_yuyuko["data"]["typeInfo"]["PVP"]["battleInfo"]["avgInfo"]["win"].ToString() + "%";
                            }
                            catch
                            {
                                //旧版结构
                                battleCount_ship = result_getPlayerShipInfo_yuyuko["data"]["shipInfo"]["battles"].ToString();
                                winRate_ship = result_getPlayerShipInfo_yuyuko["data"]["shipInfo"]["wins"].ToString() + "%";
                            }
                        }
                        catch (Exception ex) { Logger.logWrite("无法解析yuyuko玩家单船信息，" + ex.Message); }
                    }
                }
            }
            catch (Exception ex) { Logger.logWrite("备用方案yuyuko数据也无法读取玩家" + playerId + "的对局信息，" + ex.Message); }
        }
    }

    public class yuyukoGameInfo//为yuyuko机器人收集信息用的类
    {
        public string battleType { get; set; }
        public long time { get; set; }
        public List<yuyukoPlayerInfo> infoList { get; set; } = new List<yuyukoPlayerInfo>();


        public void SetTime(string timeStr)//设定对局时间戳
        {
            DateTime dt = DateTime.ParseExact(timeStr, "dd.MM.yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            time = (long)(dt.ToLocalTime() - TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1))).TotalMilliseconds;
        }

        apiClient apiClient = new apiClient();
        public void AddYuyukoPlayerInfo(playerInfo playerInfo)//添加单个玩家信息
        {
            yuyukoPlayerInfo yuyukoPlayerInfo = new yuyukoPlayerInfo();
            yuyukoPlayerInfo.accountId = playerInfo.playerId;
            yuyukoPlayerInfo.userName = playerInfo.name;
            yuyukoPlayerInfo.shipId = playerInfo.shipId;
            yuyukoPlayerInfo.hidden = playerInfo.isHidden;
            yuyukoPlayerInfo.clanId = playerInfo.clanId;
            yuyukoPlayerInfo.tag = playerInfo.clanTag;
            yuyukoPlayerInfo.relation = playerInfo.relation;
            infoList.Add(yuyukoPlayerInfo);
        }

        public void SendInfoToYuyuko()//数据提交给yuyuko
        {
            apiClient.sendYuyukoGameInfo(JsonConvert.SerializeObject(this).ToString());
        }
    }

    public class yuyukoPlayerInfo//为yuyuko机器人收集信息用的子类
    {
        public string server { get; set; } = "cn";
        public string accountId { get; set; }
        public string userName { get; set; }
        public string shipId { get; set; }
        public bool hidden { get; set; }
        public string clanId { get; set; }
        public string tag { get; set; }
        public int relation { get; set; }
    }

    public class PlayerGameInfoInRep//从rep文件里获取的玩家信息
    {
        private string _name;
        public string shipId { get; set; }
        public int relation { get; set; }
        public string id { get; set; }
        public string name
        {
            get => _name;
            set { _name = System.Text.RegularExpressions.Regex.Unescape(value); }
        }
    }
}
