using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Automation.Peers;
using System.IO;
using System.Threading;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using System.Web;
using System.Diagnostics;
using System.Windows.Threading;
using System.Runtime.CompilerServices;
using Microsoft.WindowsAPICodePack.Shell.Interop;
using System.Reflection;
using System.Net;
using System.Security.Policy;
using System.IO.Compression;
using Path = System.IO.Path;
using System.Reflection.Emit;
using Microsoft.VisualBasic;
using System.Net.Mail;

namespace 郭楠查看器
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Config Config = Config.Instance;
        Logger Logger = Logger.Instance;
        private FileSystemWatcher watcher = new FileSystemWatcher();
        private apiClient apiClient = new apiClient();
        private string visionTag = "2023.06.08";
        private string updateFolderPath = ".update";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)//窗口加载完成后，初始化并监控rep文件夹
        {
            Config.init();
            rootPath.Text = Config.wowsRootPath;
            //checkUpdate();
            watchRepFolder();
        }
        private void checkUpdate()//检测客户端升级
        {
            try
            {
                if (Directory.Exists(updateFolderPath))//每次检测时删除更新文件夹，保证没有脏文件
                    Directory.Delete(updateFolderPath, true);
                string releaseCheckUrl = "https://gitee.com/api/v5/repos/bbaoqaq/wowsCheaterViewer/releases/latest";
                string releaseCheckResurnStr = apiClient.GetClientAsync(releaseCheckUrl).Result;
                JObject releaseCheckResurnJson = JObject.Parse(releaseCheckResurnStr);

                if (releaseCheckResurnJson["tag_name"].ToString() == visionTag)
                {
                    Logger.logWrite("无需更新");
                }
                else
                {
                    Logger.logWrite("需要更新");
                    string updatalog = releaseCheckResurnJson["body"].ToString();

                    Boolean updataFlag = false;
                    updataFlag = MessageBox.Show("检查到新版本，是否进行更新？"+Environment.NewLine+"更新内容：" + Environment.NewLine + updatalog, "更新提示", MessageBoxButton.OKCancel) == MessageBoxResult.OK;
                    if (updataFlag)
                    {
                        Config.watchFlag = false;
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            logShow("确认更新");
                            //确认能够转换GBK编码
                            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                            string updateZipFilePath = Path.Combine(updateFolderPath, releaseCheckResurnJson["assets"][0]["name"].ToString());
                            string downloadUrl = releaseCheckResurnJson["assets"][0]["browser_download_url"].ToString();
                            Directory.CreateDirectory(updateFolderPath);
                            //下载
                            bool downloadFlag = false;
                            using (var web = new WebClient())
                            {
                                web.DownloadProgressChanged += (s, e) =>
                                {
                                    string.Format("正在下载文件：{0}%  ({1}/{2})",
                                        string.Format("{0:D2}", e.ProgressPercentage),
                                        e.BytesReceived,
                                        e.TotalBytesToReceive);
                                    logShow("正在下载："+e.ProgressPercentage.ToString()+"%");
                                };
                                web.DownloadFileCompleted += (s, e) =>
                                {
                                    downloadFlag = true;
                                };
                                web.DownloadFileAsync(new Uri(downloadUrl), updateZipFilePath);
                            }
                            while (!downloadFlag) ;
                            Logger.logWrite("下载完成");
                            //解压
                            ZipFile.ExtractToDirectory(updateZipFilePath, updateFolderPath, Encoding.GetEncoding("GBK"));
                            File.Delete(updateZipFilePath);
                            logShow("解压完成");
                            //生成更新批处理脚本
                            string updateBatPath = Path.Combine(updateFolderPath, "update.bat");
                            string copyFromFolderPath = Directory.GetDirectories(updateFolderPath).First();//取解压后的根文件夹
                            string copyToFolderPath = Environment.CurrentDirectory;//取当前文件夹
                            string processName = Assembly.GetExecutingAssembly().GetName().Name;//取项目名称，也是进程名称
                            string batStr = @"chcp 65001" + Environment.NewLine +//用中文编码
                                "taskkill /f /im " + processName + ".exe " + Environment.NewLine +//结束项目进程
                                "xcopy " + copyFromFolderPath.Replace(" ", @""" """) + " " + copyToFolderPath.Replace(" ", @""" """) + " /e /y " + Environment.NewLine +//覆盖所有需要更新的文件
                                "start " + Path.Combine(copyToFolderPath, processName + ".exe").Replace(" ", @""" """);//重启进程
                            File.Delete(updateBatPath);
                            StreamWriter sw = new StreamWriter(updateBatPath);
                            sw.Write(batStr, Encoding.GetEncoding("GBK"));
                            sw.Close();
                            logShow("即将更新");
                            //启动批处理脚本
                            Process Process = new Process();
                            Process.StartInfo.WorkingDirectory = copyToFolderPath;
                            Process.StartInfo.FileName = updateBatPath;
                            Process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            Process.Start();
                            Process.WaitForExit();
                            //如果没有正常启动则报错
                            throw new Exception("批处理脚本启动失败");
                        });
                    }
                    else
                    {
                        logShow("用户取消更新");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.logWrite("更新失败，" + ex.Message);
                MessageBox.Show("更新失败，" + ex.Message);
            }
        }
        private void logShow(string message)//显示日志
        {
            Dispatcher.Invoke(() => logText.Text = message);
            Logger.logWrite(message);
        }
        
        
        private void readmeEvent(object sender, RoutedEventArgs e)//使用与免责声明
        {
            ReadMeWindow window = new ReadMeWindow();
            window.Show();
        }
        private void resetRootPathEvent(object sender, RoutedEventArgs e)//重设路径事件
        {
            Config.resetRootPath();
            if (Config.watchFlag)
                logShow("路径设置成功");
            else
                logShow("路径有误，请选择游戏根路径");
            rootPath.Text = Config.wowsRootPath;
        }
        private void reflashEvent(object sender, RoutedEventArgs e)//手动刷新事件
        {
            if (Config.watchFlag)
            {
                logShow("刷新当前对局数据");
                teamView(readTempJson());
            }
            else
            {
                logShow("未设定游戏路径，无法读取。请设定后重试");
            }
        }
        private void readRepEvent(object sender, RoutedEventArgs e)//读取指定rep文件
        {
            string defaultPath = @"C:\";
            if (Config.watchFlag)
                    defaultPath = Path.Combine(Config.wowsRootPath, "replays"); 

            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.EnsureReadOnly = true;
            dlg.InitialDirectory = defaultPath;
            dlg.Filters.Add(new CommonFileDialogFilter("战舰世界回放文件", "*.wowsreplay"));
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                teamView(readRepJson(dlg.FileName));
        }
        private void markEnemyEvent(object sender, RoutedEventArgs e)//标记所有敌方
        {
            List<playerInfo> updateTeam2 = new List<playerInfo>();
            string markMessage = Interaction.InputBox("标记内容：", "提示");
            if (!string.IsNullOrEmpty(markMessage))
            {
                try
                {
                    foreach (playerInfo playerInfo in this.team2.Items)
                    {
                        playerInfo.markMessage = markMessage;
                        Config.addMarkInfo(playerInfo);
                        updateTeam2.Add(playerInfo);
                    }
                    Dispatcher.Invoke(() =>
                    {
                        team2.ItemsSource = updateTeam2;
                    });
                    logShow(markMessage + "标记成功");
                }
                catch (Exception ex)
                {
                    logShow(markMessage + "标记失败，" + ex.Message);
                }
            }
        }
        private void debugPlayerEvent(object sender, RoutedEventArgs e)//单个玩家调试
        {
            string playerStr = Interaction.InputBox("调试文本："+ Environment.NewLine + "（作者用来调试的，想用这个功能的话请参考说明文档）", "提示");
            if(!string.IsNullOrEmpty(playerStr)) 
            {
                try
                {
                    PlayerGameInfoInRep PlayerGameInfoInRep = new PlayerGameInfoInRep();
                    try { PlayerGameInfoInRep = JsonConvert.DeserializeObject<PlayerGameInfoInRep>(playerStr); }
                    catch (Exception ex) { throw new Exception("解析rep中的玩家信息失败" + ex.Message); }

                    playerInfo playerInfo = parsePlayerJson(new playerInfo(), PlayerGameInfoInRep);
                    PropertyInfo[] properties = playerInfo.GetType().GetProperties();
                    string outputStr = null;
                    for (int i = 0; i < properties.Count(); i++)
                        outputStr = outputStr + string.Format("{0,-20}", properties[i].Name) + ":" + properties[i].GetValue(playerInfo) + Environment.NewLine;

                    Logger.logWrite(outputStr);
                    MessageBox.Show(outputStr);
                }
                catch(Exception ex) { MessageBox.Show("解析失败，"+ex.Message); }
            }
        }
        private void markMessageChangedEvent(object sender, RoutedEventArgs e)//标记变更时更新配置
        {
            playerInfo currentPlayerInfo = new playerInfo();
            if (this.team1.SelectedIndex >= 0)
                currentPlayerInfo = (playerInfo)this.team1.Items[this.team1.SelectedIndex];
            else if (this.team2.SelectedIndex >= 0)
                currentPlayerInfo = (playerInfo)this.team2.Items[this.team2.SelectedIndex];
            else
                logShow("更新玩家标记失败，未能定位到玩家所在队伍");

            if (currentPlayerInfo != null)
                Config.addMarkInfo(currentPlayerInfo);
        }

        private void watchRepFolder()//监控rep文件夹
        {
            if(Config.watchFlag)
            {
                watcher.EnableRaisingEvents = false;//先停止监控
                watcher.Path = System.IO.Path.Combine(Config.wowsRootPath, "replays");
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = "tempArenaInfo.json";
                watcher.Created += (s, e) => 
                {
                    logShow("检测到对局开始，正在读取");
                    teamView(readTempJson());
                };
                watcher.Deleted += (s, e) => 
                {
                    logShow("检测到对局结束，正在监控rep文件夹");
                };
                watcher.EnableRaisingEvents = true;
                logShow("正在监控rep文件夹");
            }
            else
            {
                logShow("请检查路径是否设置");
            }
        }
        private JObject readTempJson()//读取对局中生成的临时文件
        {
            Thread.Sleep(1000);
            JObject infoJson;
            try
            {
                string infoStr = File.ReadAllText(Path.Combine(Config.wowsRootPath, "replays", "tempArenaInfo.json"));
                infoJson = JObject.Parse(infoStr);
            }
            catch (Exception ex)
            {
                infoJson = null;
                logShow("未能成功读取对局文件，" + ex.Message);
            }
            return infoJson;
        }
        private JObject readRepJson(string path)//读取指定的rep文件
        {
            JObject infoJson;
            string infoStr;
            try
            {
                StreamReader sr = new StreamReader(path);
                infoStr = sr.ReadLine();
                sr.Close();

                infoStr = @"{""matchGroup""" +
                    System.Text.RegularExpressions.Regex.Matches(infoStr, @"(?<={""matchGroup"").*(?=""mapBorder"")").First().ToString() +
                    @"""mapBorder"": null}";
                infoJson = Newtonsoft.Json.Linq.JObject.Parse(infoStr);

                logShow("正在读取rep文件，" + path);
            }
            catch (Exception ex)
            {
                infoJson = null;
                logShow("未能成功读取rep文件，" + ex.Message);
            }
            return infoJson;
        }
        private void teamView(JObject infoJson)//解析对局json，把队伍信息绑定到前台表格中
        {
            if (infoJson != null) 
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                string exceptionMessage = null;
                System.Threading.Tasks.Task.Run(() =>{
                    try
                    {
                        //每次读取时禁用刷新按钮
                        Dispatcher.Invoke(() =>
                        {
                            reflashBtn.IsEnabled = false;
                            readRepBtn.IsEnabled = false;
                            markEnemyBtn.IsEnabled = false;
                            watcher.EnableRaisingEvents = false;//先停止监控
                        });
                        
                        List<playerInfo> playerInfo_team1 = new List<playerInfo>();
                        List<playerInfo> playerInfo_team2 = new List<playerInfo>();
                        int readCount = 0;
                        int failedList = 0;
                        List<PlayerGameInfoInRep> PlayerGameInfoList = JsonConvert.DeserializeObject<List<PlayerGameInfoInRep>>(infoJson["vehicles"].ToString());
                        
                        yuyukoGameInfo yuyukoGameInfo = new yuyukoGameInfo();
                        yuyukoGameInfo.SetTime(infoJson["dateTime"].ToString());
                        yuyukoGameInfo.battleType = infoJson["matchGroup"].ToString();

                        //并行执行
                        ParallelLoopResult parallelResult = Parallel.For(0, PlayerGameInfoList.Count(), i =>
                        {
                            playerInfo playerInfo = new playerInfo();
                            try
                            {
                                playerInfo = parsePlayerJson(playerInfo, PlayerGameInfoList[i]);
                            }
                            catch(Exception ex)
                            {
                                failedList++;
                                Logger.logWrite("玩家信息读取失败，" + ex.Message + Environment.NewLine + 
                                    JsonConvert.SerializeObject(PlayerGameInfoList[i]).ToString());
                            }
                            finally
                            {
                                readCount++;
                                yuyukoGameInfo.AddYuyukoPlayerInfo(playerInfo);
                                
                                if (playerInfo.relation == 1 || playerInfo.relation == 0)//0是用户，1是己方，2是敌方
                                    playerInfo_team1.Add(playerInfo);
                                else
                                    playerInfo_team2.Add(playerInfo);

                                logShow(string.Format("正在读取对局信息({0}/{1})",
                                    readCount.ToString(),
                                    PlayerGameInfoList.Count()));
                            }
                        });
                        //绑定给前台
                        Dispatcher.Invoke(() =>
                        {
                            team1.ItemsSource = playerInfo_team1.OrderByDescending(i => i.shipSort);
                            team2.ItemsSource = playerInfo_team2.OrderByDescending(i => i.shipSort);
                        });
                        yuyukoGameInfo.SendInfoToYuyuko();

                        if (failedList > 0)
                            exceptionMessage = "有" + failedList.ToString() + "个玩家读取失败";
                    }
                    catch (Exception ex)
                    {
                        exceptionMessage = ex.Message;
                    }
                    finally
                    {
                        sw.Stop();
                        logShow("已读取对局文件，耗时" + (sw.ElapsedMilliseconds / 1000).ToString() + "秒" + (string.IsNullOrEmpty(exceptionMessage) ? "" : "，" + exceptionMessage));

                        //每次读取完成后启用刷新按钮
                        Dispatcher.Invoke(() =>
                        {
                            reflashBtn.IsEnabled = true;
                            readRepBtn.IsEnabled = true;
                            markEnemyBtn.IsEnabled = true;
                            watcher.EnableRaisingEvents = Config.watchFlag;//恢复监视
                        });
                    }
                });
            };
        }
        private playerInfo parsePlayerJson(playerInfo playerInfo, PlayerGameInfoInRep PlayerGameInfoInRep)//解析单个玩家的json数据
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            playerInfo.SetBasePlayerInfo(PlayerGameInfoInRep);//从rep里的文本获取基础信息
            playerInfo.GetPlayerId();//取玩家id
            playerInfo.SetEmptyClanFile();//写入空文件用于上传军团信息
            playerInfo.GetBattleInfo_withBattleType("pvp");//获取pvp数据
            playerInfo.GetBattleInfo_withBattleType("rank_solo");//获取rank数据
            playerInfo.DeleteTempFiles();//删除临时文件
            playerInfo.GetClanInfo();//获取军团数据
            playerInfo.GetShipInfo();//获取船数据
            playerInfo.GetBanInfo();//获取ban信息
            playerInfo.GetMarkInfo();//获取标记信息

            /*
            //并行线程太多，反而比不并行来的慢
            Parallel.Invoke(
                () =>//获取pvp数据
                {
                    Stopwatch sw_step = new Stopwatch();
                    sw_step.Start();
                    logShow("玩家" + playerInfo.playerId + "pvp查询开始");
                    playerInfo.GetBattleInfo_withBattleType("pvp");
                    sw_step.Stop();
                    logShow("玩家" + playerInfo.playerId + "pvp查询完成，耗时" + (sw_step.ElapsedMilliseconds / 1000).ToString() + "秒");
                },
                () =>//获取rank数据
                {
                    Stopwatch sw_step = new Stopwatch();
                    sw_step.Start();
                    logShow("玩家" + playerInfo.playerId + "rank查询开始");
                    playerInfo.GetBattleInfo_withBattleType("rank_solo");
                    sw_step.Stop();
                    logShow("玩家" + playerInfo.playerId + "rank查询完成，耗时" + (sw_step.ElapsedMilliseconds / 1000).ToString() + "秒");
                },
                () =>//获取军团数据
                {
                    Stopwatch sw_step = new Stopwatch();
                    sw_step.Start();
                    logShow("玩家" + playerInfo.playerId + "军团查询开始");
                    playerInfo.GetClanInfo();
                    sw_step.Stop();
                    logShow("玩家" + playerInfo.playerId + "军团查询完成，耗时" + (sw_step.ElapsedMilliseconds / 1000).ToString() + "秒");
                },
                () =>//获取船数据
                {
                    Stopwatch sw_step = new Stopwatch();
                    sw_step.Start();
                    logShow("玩家" + playerInfo.playerId + "船查询开始");
                    playerInfo.GetShipInfo();
                    sw_step.Stop();
                    logShow("玩家" + playerInfo.playerId + "船查询完成，耗时" + (sw_step.ElapsedMilliseconds / 1000).ToString() + "秒");
                },
                () =>//获取ban信息
                {
                    Stopwatch sw_step = new Stopwatch();
                    sw_step.Start();
                    logShow("玩家" + playerInfo.playerId + "ban查询开始");
                    playerInfo.GetBanInfo();
                    sw_step.Stop();
                    logShow("玩家" + playerInfo.playerId + "ban查询完成，耗时" + (sw_step.ElapsedMilliseconds / 1000).ToString() + "秒");
                },
                () =>//获取标记信息
                {
                    Stopwatch sw_step = new Stopwatch();
                    sw_step.Start();
                    logShow("玩家" + playerInfo.playerId + "mark查询开始");
                    playerInfo.GetMarkInfo();
                    sw_step.Stop();
                    logShow("玩家" + playerInfo.playerId + "mark查询完成，耗时" + (sw_step.ElapsedMilliseconds / 1000).ToString() + "秒");
                }
            );
            */
            sw.Stop();
            Logger.logWrite("玩家"+playerInfo.playerId+"查询完成，耗时" + (sw.ElapsedMilliseconds / 1000).ToString() + "秒");
            return playerInfo;
        }
    }
}