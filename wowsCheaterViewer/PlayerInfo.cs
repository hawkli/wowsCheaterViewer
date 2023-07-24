using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace wowsCheaterViewer
{
    public class PlayerInfo : PlayerGameInfoInRep
    {
        private const string errorMessage = "error";
        public long PlayerId { get; set; } = 0;//默认值给0，方便提供给yuyuko
        public string? PlayerPrColor { get; set; } = "Gray";//默认灰色（隐藏战绩或读取失败）
        public bool IsHidden { get; set; }


        public string? ClanTag { get; set; }
        public long ClanId { get; set; } = 0;//默认值给0，方便提供给yuyuko
        public string? ClanColor { get; set; }


        private int _shipLevel_int;
        private string? _shipType;
        public string? ShipName { get; set; }
        public string? ShipType 
        {
            get => _shipType;
            set
            {
                _shipType = value;
                SetShipTypeSort();//设置船种类时，自动设置按种类排序
            }
        }
        public int ShipTypeSort { get; set; }

        public int ShipLevel_int
        {
            get => _shipLevel_int;
            set
            {
                _shipLevel_int = value;
                SetRomanLevel();//设置数字等级时，自动设置罗马等级
            }
        }
        public string? ShipLevel_roman { get; set; }
        private void SetRomanLevel()//赋值阿拉伯数字的同时转换成罗马数字
        {
            int[] nums = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            string[] romans = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            int num = _shipLevel_int;
            for (int i = 0; i < 13; i++)
            {
                while (num >= nums[i])
                {
                    ShipLevel_roman += romans[i];
                    num -= nums[i];
                }
            }
            ShipLevel_roman = string.Format("{0,-4}", ShipLevel_roman);
        }
        private void SetShipTypeSort()//按舰种进行排序
        {
            string[] typeSort = { "Submarine", "Destroyer", "Cruiser", "Battleship", "AirCarrier" };
            ShipTypeSort = Array.IndexOf(typeSort, ShipType);
        }

        public string? BanMatch { get; set; }
        public string? BanMatch_fullStr { get; set; }
        public string? BanColor { get; set; }


        public string? BattleCount_ship { get; set; }
        public string? WinRate_ship { get; set; } = errorMessage;//默认读取失败
        public string? BattleCount_pvp { get; set; }
        public string? WinRate_pvp { get; set; }
        public string? BattleCount_rank { get; set; }
        public string? WinRate_rank { get; set; }


        public string? MarkMessage { get; set; }
        public string? LastMarkMessage { get; set; }

        readonly Config Config = Config.Instance;
        private static readonly object writerLock = new();
        private string? clanEmptyFilePath;
        private readonly List<string> tempFiles = new();
        public void SetBasePlayerInfo(PlayerGameInfoInRep PlayerGameInfoInRep)//根据rep里的内容获取基础信息
        {
            Name = PlayerGameInfoInRep.Name;
            ShipId = PlayerGameInfoInRep.ShipId;
            Relation = PlayerGameInfoInRep.Relation;
        }

        public void GetPlayerId()//获取玩家id
        {
            try
            {
                JObject result_getPlayerId = JObject.Parse(ApiClient.GetPlayerId(Name)!);
                if (JArray.FromObject(result_getPlayerId["data"]!).Count == 0)
                    throw new Exception("未能获取玩家id，可能已改名。");
                PlayerId = (long)result_getPlayerId["data"]?[0]?["spa_id"]!;
                IsHidden = Convert.ToBoolean(result_getPlayerId["data"]?[0]?["hidden"]);
                clanEmptyFilePath = WriteFile(PlayerId + "_clan.txt", "");//写入空文件用于上传军团信息
                tempFiles.Add(clanEmptyFilePath);
            }
            catch (Exception ex) { throw new Exception("获取玩家id失败，" + ex.Message); }
        }

        public void DeleteTempFiles()//删除临时文件
        {
            foreach(string tempFile in tempFiles)
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
        }

        private static string WriteFile(string fileName, string? fileBody)
        {
            string saveFile = Path.Combine(Config.tempFolderPath, fileName);
            lock (writerLock)
            {
                if (!Directory.Exists(Config.tempFolderPath))
                    Directory.CreateDirectory(Config.tempFolderPath);
                StreamWriter sw = new(saveFile);
                sw.Write(fileBody);
                sw.Close();
            }
            return saveFile;
        }

        public void GetBattleInfo_withBattleType(string battleType)//根据战斗类型获取玩家信息
        {
            //battleType=[basic,pve,pvp,pvp_solo,pvp_div2,pvp_div3,rank_old_solo,rank_solo,rank_div2,rank_div3,seasons]
            battleType = battleType.ToLower();
            string? infoFilePath_battleType = null;
            if (!IsHidden)
            {
                try
                {
                    string? PlayersShipsInfo_battleType = ApiClient.GetPalyersShipsInfo_official(PlayerId, battleType);
                    infoFilePath_battleType = WriteFile(PlayerId + "_" + battleType + ".json", PlayersShipsInfo_battleType);
                    string? ParsedPlayerInfoStr_battleType = ApiClient.GetParsedPlayerInfo_yuyuko(PlayerId, ShipId, battleType, infoFilePath_battleType, clanEmptyFilePath!);
                    JObject ParsedPlayerInfoJo_battleType = JObject.Parse(ParsedPlayerInfoStr_battleType!);
                    if (battleType == "pvp")
                    {
                        BattleCount_pvp = ParsedPlayerInfoJo_battleType["userInfo"]?[0]?["shipInfo"]?["battleInfo"]?["battle"]?.ToString();
                        WinRate_pvp = ParsedPlayerInfoJo_battleType["userInfo"]?[0]?["shipInfo"]?["avgInfo"]?["win"]?.ToString() + "%";
                        PlayerPrColor = ParsedPlayerInfoJo_battleType["userInfo"]?[0]?["prInfo"]?["color"]?.ToString();
                        BattleCount_ship = ParsedPlayerInfoJo_battleType["shipInfo"]?[0]?["shipInfo"]?["battleInfo"]?["battle"]?.ToString();
                        WinRate_ship = ParsedPlayerInfoJo_battleType["shipInfo"]?[0]?["shipInfo"]?["avgInfo"]?["win"]?.ToString() + "%";
                    }
                    else if (battleType == "rank_solo")
                    {
                        BattleCount_rank = ParsedPlayerInfoJo_battleType["userInfo"]?[0]?["shipInfo"]?["battleInfo"]?["battle"]?.ToString();
                        WinRate_rank = ParsedPlayerInfoJo_battleType["userInfo"]?[0]?["shipInfo"]?["avgInfo"]?["win"]?.ToString() + "%";
                    }
                }
                catch (Exception ex) { Logger.LogWrite("无法读取玩家" + PlayerId + "的" + battleType + "信息，" + ex.Message); }
                finally { if(!string.IsNullOrEmpty(infoFilePath_battleType)) tempFiles.Add(infoFilePath_battleType); }
            }
            else
            {
                WinRate_ship = "hidden";
            }
        }

        public void GetClanInfo()//获取军团信息
        {
            try
            {
                string? PlayersClanInfoStr = ApiClient.GetPalyersClansInfo_official(PlayerId);
                JObject PlayersClanInfoJo = JObject.Parse(PlayersClanInfoStr!);
                if(!string.IsNullOrEmpty(PlayersClanInfoJo["data"]?["clan_id"]?.ToString()))
                {
                    ClanId = (long)PlayersClanInfoJo["data"]?["clan_id"]!;
                    ClanTag = "[" + PlayersClanInfoJo["data"]?["clan"]?["tag"]?.ToString() + "]";
                    ClanColor = "#" + Convert.ToInt32(PlayersClanInfoJo["data"]?["clan"]?["color"]).ToString("X");
                }
            }
            catch (Exception ex) { Logger.LogWrite("无法读取玩家" + PlayerId + "的军团信息，" + ex.Message); }
        }

        public void GetShipInfo()//获取船信息
        {
            try
            {
                ShipInfo ShipInfo = new();
                if (Config.ShipInfo.ContainsKey(ShipId.ToString()!))//优先获取配置文件中存过的，没存过再调接口拿
                {
                    ShipInfo = Config.ShipInfo[ShipId.ToString()!];
                }
                else
                {
                    string? shipInfoStr = ApiClient.GetShipInfo(ShipId);
                    ShipInfo = JsonConvert.DeserializeObject<ShipInfo>(JObject.Parse(shipInfoStr!)["data"]?.ToString()!)!;
                    Config.AddShipInfo(ShipId, ShipInfo);
                }
                ShipLevel_int = ShipInfo.Level;
                ShipName = ShipInfo.NameCn;
                ShipType = ShipInfo.ShipType;
            }
            catch (Exception ex) { Logger.LogWrite("无法读取玩家" + PlayerId + "的船" + ShipId + "信息，" + ex.Message); }
        }

        public void GetBanInfo()//获取ban信息
        {
            try
            {
                string? getPlayerBanInfoStr = ApiClient.GetPlayerBanInfo_yuyuko(PlayerId);
                JObject getPlayerBanInfoJo = JObject.Parse(getPlayerBanInfoStr!);

                List<BanInfo> banInfoList = JsonConvert.DeserializeObject<List<BanInfo>>(getPlayerBanInfoJo["data"]?["voList"]?.ToString()!)!;
                List<int> banMatch_matchCountList = new();
                List<string> BanMatch_fullStrList = new();
                foreach (BanInfo banInfo in banInfoList) //将每一项的封禁匹配数加到list里
                {
                    banMatch_matchCountList.Add(banInfo.BanNameNamesake);
                    BanMatch_fullStrList.Add(banInfo.GetBanInfoFullStr());
                }
                if (banMatch_matchCountList.Contains(1))//如果有匹配值是1的，标记为红色
                    BanColor = "Red";
                BanMatch = string.Join(",", banMatch_matchCountList);
                if (banInfoList.Count == 0)
                    BanMatch_fullStr = "";
                else
                    BanMatch_fullStr = "可能符合条件的封禁历史记录：" + Environment.NewLine + Environment.NewLine +
                        string.Join(Environment.NewLine, BanMatch_fullStrList);
            }
            catch (Exception ex) { Logger.LogWrite("无法读取玩家" + PlayerId + "的ban信息，" + ex.Message); }
        }

        public void GetMarkInfo()//获取标记信息
        {
            if (Config.Mark.ContainsKey(PlayerId.ToString()))
            {
                //取该玩家标记内容下，按时间排序最晚的那个信息
                MarkInfo MarkInfo = Config.Mark[PlayerId.ToString()].OrderByDescending(i => i.MarkTime).First();
                MarkMessage = MarkInfo.MarkMessage;
                LastMarkMessage = "上次标记时间：" + MarkInfo.MarkTime + Environment.NewLine +
                    "上次标记时的军团：" + MarkInfo.ClanTag + Environment.NewLine +
                    "上次标记时的名称：" + MarkInfo.Name + Environment.NewLine +
                    "上次标记时的内容：" + MarkInfo.MarkMessage;
            }
        }
    
        public void CheckLoadFailed()//如果解析失败，试着直接读取yuyuko里的玩家数据进行补录
        {
            try
            {
                if (WinRate_ship == errorMessage)
                {
                    JObject result_getplayerInfo_yuyuko = JObject.Parse(ApiClient.GetPlayerInfo_yuyuko(PlayerId)!);
                    //解析yuyuko玩家信息
                    if (result_getplayerInfo_yuyuko != null)
                    {
                        try
                        {
                            try
                            {
                                //新版结构
                                BattleCount_pvp = result_getplayerInfo_yuyuko["data"]?["battleTypeInfo"]?["PVP"]?["shipInfo"]?["battleInfo"]?["battle"]?.ToString();
                                WinRate_pvp = result_getplayerInfo_yuyuko["data"]?["battleTypeInfo"]?["PVP"]?["shipInfo"]?["avgInfo"]?["win"]?.ToString() + "%";
                                BattleCount_rank = result_getplayerInfo_yuyuko["data"]?["battleTypeInfo"]?["RANK_SOLO"]?["shipInfo"]?["battleInfo"]?["battle"]?.ToString();
                                WinRate_rank = result_getplayerInfo_yuyuko["data"]?["battleTypeInfo"]?["RANK_SOLO"]?["shipInfo"]?["avgInfo"]?["win"]?.ToString() + "%";
                                PlayerPrColor = result_getplayerInfo_yuyuko["data"]?["prInfo"]?["color"]?.ToString();
                            }
                            catch
                            {
                                //旧版结构
                                BattleCount_pvp = result_getplayerInfo_yuyuko["data"]?["pvp"]?["battles"]?.ToString();
                                WinRate_pvp = result_getplayerInfo_yuyuko["data"]?["pvp"]?["wins"]?.ToString() + "%";
                                BattleCount_rank = result_getplayerInfo_yuyuko["data"]?["rankSolo"]?["battles"]?.ToString();
                                WinRate_rank = result_getplayerInfo_yuyuko["data"]?["rankSolo"]?["wins"]?.ToString() + "%";
                                PlayerPrColor = result_getplayerInfo_yuyuko["data"]?["pr"]?["color"]?.ToString();
                            }
                        }
                        catch (Exception ex) { Logger.LogWrite("无法解析yuyuko玩家信息，" + ex.Message); }
                    }
                    //根据船id获取yuyuko机器人中的船信息和玩家单船信息
                    JObject result_getPlayerShipInfo_yuyuko = JObject.Parse(ApiClient.GetPlayerShipInfo_yuyuko(PlayerId, ShipId)!);
                    //解析玩家单船信息
                    if (result_getPlayerShipInfo_yuyuko != null)
                    {
                        try
                        {
                            try
                            {
                                //新版结构
                                BattleCount_ship = result_getPlayerShipInfo_yuyuko["data"]?["typeInfo"]?["PVP"]?["battleInfo"]?["battleInfo"]?["battle"]?.ToString();
                                WinRate_ship = result_getPlayerShipInfo_yuyuko["data"]?["typeInfo"]?["PVP"]?["battleInfo"]?["avgInfo"]?["win"]?.ToString() + "%";
                            }
                            catch
                            {
                                //旧版结构
                                BattleCount_ship = result_getPlayerShipInfo_yuyuko["data"]?["shipInfo"]?["battles"]?.ToString();
                                WinRate_ship = result_getPlayerShipInfo_yuyuko["data"]?["shipInfo"]?["wins"]?.ToString() + "%";
                            }
                        }
                        catch (Exception ex) { Logger.LogWrite("无法解析yuyuko玩家单船信息，" + ex.Message); }
                    }
                }
            }
            catch (Exception ex) { Logger.LogWrite("备用方案yuyuko数据也无法读取玩家" + PlayerId + "的对局信息，" + ex.Message); }
        }
    }

    public class YuyukoGameInfo//为yuyuko机器人收集信息用的类
    {
        public string? BattleType { get; set; }
        public long Time { get; set; }
        public List<YuyukoPlayerInfo> InfoList { get; set; } = new List<YuyukoPlayerInfo>();


        public void SetTime(string timeStr)//设定对局时间戳
        {
            Time = DateTimeOffset.ParseExact(timeStr, "dd.MM.yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture).ToUnixTimeMilliseconds();
        }

        public void AddYuyukoPlayerInfo(PlayerInfo playerInfo)//添加单个玩家信息
        {
            YuyukoPlayerInfo yuyukoPlayerInfo = new()
            {
                AccountId = playerInfo.PlayerId,
                UserName = playerInfo.Name,
                ShipId = playerInfo.ShipId,
                Hidden = playerInfo.IsHidden,
                ClanId = playerInfo.ClanId,
                Tag = playerInfo.ClanTag,
                Relation = playerInfo.Relation
            };
            InfoList.Add(yuyukoPlayerInfo);
        }

        public void SendInfoToYuyuko()//数据提交给yuyuko
        {
            //首字母小写
            JsonSerializerSettings settings = new()
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
            };
            ApiClient.SendYuyukoGameInfo(JsonConvert.SerializeObject(this, settings).ToString());
        }
    }

    public class YuyukoPlayerInfo//为yuyuko机器人收集信息用的子类
    {
        public string Server { get; set; } = "cn";
        public long AccountId { get; set; }
        public string? UserName { get; set; }
        public long ShipId { get; set; }
        public bool Hidden { get; set; }
        public long ClanId { get; set; }
        public string? Tag { get; set; }
        public int Relation { get; set; }
    }

    public class PlayerGameInfoInRep//从rep文件里获取的玩家信息
    {
        private string? _name;
        public long ShipId { get; set; }
        public int Relation { get; set; }
        public long Id { get; set; }
        public string Name
        {
            get => _name!;
            set 
            {
                if (value != null)
                    _name = System.Text.RegularExpressions.Regex.Unescape(value);//解析Unicode字符串
                else
                    _name = value;
            }
        }
    }

    public class BanInfo//封禁信息子类
    {
        public string? BanName { get; set; }
        public string? UserName { get; set; }
        public string? BanTime { get; set; }
        public string? RecordTime { get; set; }
        public int BanNameNamesake { get; set; }
        public string GetBanInfoFullStr()
        {
            return  $"封禁日期:{BanTime}," + Environment.NewLine +
                    $"官方封禁名:{BanName}," + Environment.NewLine +
                    $"历史用户名:{UserName}," + Environment.NewLine +
                    $"相似用户数:{BanNameNamesake}";
        }
    }
}
