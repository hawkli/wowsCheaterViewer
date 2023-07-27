using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace wowsCheaterViewer
{
    public class Config
    {
        //用Lazy实现单实例
        private static readonly Lazy<Config> _instance = new(() => new Config());
        public static Config Instance => _instance.Value;

        //config，会写入文件的属性
        public string? IgnoreVersionTag { get; set; }
        private string? _replayPath;
        public string? ReplayPath
        {
            get => _replayPath;
            set
            {
                _replayPath = value;
                CheckRootPath();//每当路径初始化或变更时，检查是否可用
            }
        }
        public Dictionary<string, List<MarkInfo>> Mark { get; set; } = new Dictionary<string, List<MarkInfo>>();
        public Dictionary<string, ShipInfo> ShipInfo { get; set; } = new Dictionary<string, ShipInfo>();
        public Dictionary<long, PlayerInfo> DIYPlayerInfo { get; set; } = new Dictionary<long, PlayerInfo>();

        //config，不写入文件的静态变量和常量
        public const string versionTag = "2023.07.17";
        public const string updateFolderPath = ".update";
        public const string tempFolderPath = ".temp";
        private const string configPath = @"config.json";
        public static bool watchFlag = false;
        public static string watchMessage = "未设定游戏路径";
        private static readonly object writerLock = new();

        //方法
        public void Init()//初始化
        {
            try
            {
                if (File.Exists(configPath))
                    JsonConvert.PopulateObject(File.ReadAllText(configPath), this);
                else
                    Update(); //不存在时说明首次运行，此时新建配置文件
                
                if (!DIYPlayerInfo.ContainsKey(-1))//为待编辑玩家加一个样例
                {
                    DIYPlayerInfo[-1] = new PlayerInfo();
                    Update();
                }
            }
            catch(Exception ex)
            {
                Update();//读取失败时重建配置文件
                Logger.LogWrite("读取配置文件失败，已重建，"+ex.Message);
            }
            CheckRootPath();
        }
        public void Update()//更新配置文件
        {
            lock (writerLock)
            {
                StreamWriter sw = new(configPath);
                sw.Write(JsonConvert.SerializeObject(this).ToString());
                sw.Close();
            }
        }
        public void ResetRootPath()//重设路径
        {
            FolderBrowserDialog dlg = new();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ReplayPath = dlg.SelectedPath;
                CheckRootPath();
            }
        }
        private void CheckRootPath()//检查游戏路径
        {
            watchFlag = false;
            //只有填了值且路径存在的时候才开启监控
            if (!string.IsNullOrEmpty(ReplayPath))
                if(Directory.Exists(ReplayPath))
                    watchFlag = true;

            if (watchFlag)
            {
                bool parentDirectoryContainsWowsExe = File.Exists(Path.Combine(Directory.GetParent(ReplayPath)?.FullName!, "WorldOfWarships.exe"));
                bool directoryNameContainsReplays = ReplayPath.Split('\\').Last().ToLower().Contains("replays");
                if (directoryNameContainsReplays && parentDirectoryContainsWowsExe)
                    watchMessage = "路径设置成功：";
                else if (directoryNameContainsReplays && !parentDirectoryContainsWowsExe)
                    watchMessage = "路径设置成功，但它似乎不在游戏根目录下，请确保对局自动生成的[tempArenaInfo.json]文件在此文件夹中：";
                else
                    watchMessage = "路径设置成功，但它似乎不是rep文件夹，请确保对局自动生成的[tempArenaInfo.json]文件在此文件夹中：";
                Update();
            }
            else
            {
                watchMessage = "未设定回放文件路径或路径有误，无法监控对局：";
            }
            Logger.LogWrite(watchMessage);
        }

        public void AddMarkInfo(PlayerInfo playerInfo)//新增标记
        {
            MarkInfo MarkInfo = new()
            {
                ClanTag = playerInfo.ClanTag,
                Name = playerInfo.Name,
                MarkMessage = playerInfo.MarkMessage
            };

            //标记记录玩家的id，如果id未获取到，设为玩家名称
            string markKey;
            if (playerInfo.PlayerId == 0)
                markKey = playerInfo.Name;
            else
                markKey = playerInfo.PlayerId.ToString();

            //已有玩家信息就增加，没有就新建
            if (Mark.ContainsKey(markKey!))
                Mark[markKey].Add(MarkInfo);
            else
                Mark[markKey] = new List<MarkInfo> { MarkInfo };

            Logger.LogWrite($"已更新标记玩家：{markKey}，标记内容：{playerInfo.MarkMessage}");
            Update();
        }

        public void AddShipInfo(long shipId, ShipInfo ShipInfo)//新增船信息
        {
            if (!this.ShipInfo.ContainsKey(shipId.ToString()))
                this.ShipInfo[shipId.ToString()] = ShipInfo;
            Logger.LogWrite("已新增船id：" + shipId );
            Update();
        }
    }


    public class MarkInfo
    {
        public string? MarkTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string? ClanTag { get; set; }
        public string? Name { get; set; }
        public string? MarkMessage { get; set; }

    }

    public class ShipInfo
    {
        public string? NameCn { get; set; }
        public string? NameEnglish { get; set; }
        public int Level { get; set; }
        public string? ShipType { get; set; }
        public string? Country { get; set; }
        public string? ShipIndex { get; set; }
        public string? GroupType { get; set; }
    }
}