using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.Reflection;
using System.IO.Compression;
using Microsoft.VisualBasic;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using System.Net.Http;
using System.Windows.Controls;

namespace wowsCheaterViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Config Config = Config.Instance;
        private FileSystemWatcher watcher = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)//窗口加载完成后，初始化并监控rep文件夹
        {
            Config.Init();
            WatchMessage.Text = Config.watchMessage;
            ReplayPath.Text = Config.ReplayPath;
            CheckUpdate();
            WatchRepFolder();
        }
        private void CheckUpdate()//检测客户端升级
        {
            try
            {
                string releaseCheckUrl = "https://gitee.com/api/v5/repos/bbaoqaq/wowsCheaterViewer/releases/latest";
                string alternateDownloadUrl = "https://amt.one/bz";
                ApiClient.GetClientAsync(releaseCheckUrl).ContinueWith(async t =>
                {
                    if (Directory.Exists(Config.updateFolderPath))//每次检测时删除更新文件夹，保证没有脏文件
                        Directory.Delete(Config.updateFolderPath, true);
                    JObject releaseCheckResurnJson = JObject.Parse(t.Result!);
                    string lastVersionTag = releaseCheckResurnJson["tag_name"]?.ToString()!;
                    if (lastVersionTag == Config.versionTag || lastVersionTag == Config.IgnoreVersionTag)
                    {
                        //如果tag和当前版本或忽略版本相同，则视为无需更新
                        Logger.LogWrite("无需更新");
                    }
                    else
                    {
                        Logger.LogWrite("需要更新");
                        string updatalog = releaseCheckResurnJson["body"]?.ToString()!;

                        MessageBoxResult updataFlag = MessageBox.Show(
                            "检查到新版本，是否进行更新？（点击取消将忽略此次更新）" + Environment.NewLine + "更新内容：" + Environment.NewLine + updatalog,
                            "更新提示",
                            MessageBoxButton.YesNoCancel);
                        switch(updataFlag)
                        {
                            case MessageBoxResult.No:
                                LogShow("取消更新");
                                break;

                            case MessageBoxResult.Cancel:
                                LogShow("忽略本次更新");
                                Config.IgnoreVersionTag = lastVersionTag;
                                Config.Update();
                                break;

                            case MessageBoxResult.Yes:
                                LogShow("确认更新");
                                //确认能够转换GBK编码
                                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                                string updateZipFileName = releaseCheckResurnJson["assets"]?[0]?["name"]?.ToString()!;
                                string updateZipFilePath = Path.Combine(Config.updateFolderPath, updateZipFileName);
                                string downloadUrl = releaseCheckResurnJson["assets"]?[0]?["browser_download_url"]?.ToString()!;
                                Directory.CreateDirectory(Config.updateFolderPath);
                                try
                                {
                                    Logger.LogWrite("从gitee下载");
                                    await DownloadFile(downloadUrl, updateZipFilePath);
                                }
                                catch
                                {
                                    Logger.LogWrite("从备用地址下载");
                                    downloadUrl = alternateDownloadUrl + "/" + updateZipFileName;
                                    await DownloadFile(downloadUrl, updateZipFilePath);
                                }
                                //解压
                                ZipFile.ExtractToDirectory(updateZipFilePath, Config.updateFolderPath, Encoding.GetEncoding("GBK"));
                                File.Delete(updateZipFilePath);
                                LogShow("解压完成");
                                //生成更新批处理脚本
                                string updateBatPath = Path.Combine(Config.updateFolderPath, "update.bat");
                                string copyFromFolderPath = Directory.GetDirectories(Config.updateFolderPath).First();//取解压后的根文件夹
                                string copyToFolderPath = Environment.CurrentDirectory;//取当前文件夹
                                string processName = Assembly.GetExecutingAssembly().GetName().Name!;//取项目名称，也是进程名称
                                string batStr = @"chcp 65001" + Environment.NewLine +//用中文编码
                                    "taskkill /f /im " + processName + ".exe " + Environment.NewLine +//结束项目进程
                                    "xcopy " + copyFromFolderPath.Replace(" ", @""" """) + " " + copyToFolderPath.Replace(" ", @""" """) + " /e /y " + Environment.NewLine +//覆盖所有需要更新的文件
                                    "start " + Path.Combine(copyToFolderPath, processName + ".exe").Replace(" ", @""" """);//重启进程
                                File.Delete(updateBatPath);
                                StreamWriter sw = new(updateBatPath);
                                sw.Write(batStr, Encoding.GetEncoding("GBK"));
                                sw.Close();
                                LogShow("即将更新");
                                //启动批处理脚本
                                Process Process = new();
                                Process.StartInfo.WorkingDirectory = copyToFolderPath;
                                Process.StartInfo.FileName = updateBatPath;
                                Process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                Process.Start();
                                Process.WaitForExit();
                                //如果没有正常启动则报错
                                throw new Exception("批处理脚本启动失败");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogWrite("更新失败，" + ex.Message);
                MessageBox.Show("更新失败，" + ex.Message);
            }
        }
        private async Task DownloadFile(string url, string path)//下载
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                using FileStream filestream = File.Create(path);
                HttpClient client = new();
                //创建一个缓冲区，大小为20mb
                byte[] buffer = new byte[1024 * 1024 * 20];
                HttpRequestMessage request = new(HttpMethod.Get, new Uri(url));
                HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                //获取响应内容长度
                long fileSize = response.Content.Headers.ContentLength ?? -1;
                long readSize = 0;
                using Stream stream = await response.Content.ReadAsStreamAsync();
                int len;
                //循环异步读取响应流的内容，直到读取完毕
                while ((len = await stream.ReadAsync(buffer)) > 0)
                {
                    await filestream.WriteAsync(buffer.AsMemory(0, len));
                    readSize += len;
                    LogShow(((double)readSize / fileSize).ToString("P"));
                }
                //销毁fs缓冲区
                filestream.Flush();
                Logger.LogWrite("下载完成");
            }
            catch(Exception ex)
            {
                Logger.LogWrite("下载文件失败，url：" + url + "，失败原因：" + ex.Message);
                throw;
            }
        }
        private void LogShow(string message)//显示日志
        {
            Dispatcher.Invoke(() => logText.Text = message);
            Logger.LogWrite(message);
        }
        private void CopyBan_Click(object sender, RoutedEventArgs e)//复制封禁信息
        {
            PlayerInfo currentPlayerInfo = (PlayerInfo)((MenuItem)e.OriginalSource).DataContext;
            Logger.LogWrite($"即将复制封禁信息的玩家:{currentPlayerInfo.PlayerId}");
            System.Windows.Clipboard.SetDataObject($"玩家{currentPlayerInfo.Name}{currentPlayerInfo.BanMatch_fullStr}");
        }

        private void ReadmeEvent(object sender, RoutedEventArgs e)//使用与免责声明
        {
            ReadMeWindow window = new();
            window.Show();
        }
        private void ResetRootPathEvent(object sender, RoutedEventArgs e)//重设路径事件
        {
            Config.ResetRootPath();
            WatchMessage.Text = Config.watchMessage;
            ReplayPath.Text = Config.ReplayPath;
            WatchRepFolder();
        }
        private void ReflashEvent(object sender, RoutedEventArgs e)//手动刷新事件
        {
            if (Config.watchFlag)
            {
                LogShow("刷新当前对局数据");
                ParseGame(ReadTempJson());
            }
            else
            {
                LogShow("未设定游戏路径，无法读取。请设定后重试");
            }
        }
        private void ReadRepEvent(object sender, RoutedEventArgs e)//读取指定rep文件
        {
            OpenFileDialog dlg = new()
            {
                Filter = "wows回放文件|*.wowsreplay",
                InitialDirectory = Config.watchFlag ? Path.Combine(Config.ReplayPath!, "replays") : @"C:\"
            };
            dlg.ShowDialog();
            string repPath = dlg.FileName;
            if (!string.IsNullOrEmpty(repPath))
                ParseGame(ReadRepJson(repPath));
        }
        private void MarkEnemyEvent(object sender, RoutedEventArgs e)//标记所有敌方
        {
            List<PlayerInfo> updateTeam2 = new();
            string markMessage = Interaction.InputBox("标记内容：", "提示");
            if (!string.IsNullOrEmpty(markMessage))
            {
                try
                {
                    foreach (PlayerInfo playerInfo in this.team2.Items)
                    {
                        playerInfo.MarkMessage = markMessage;
                        Config.AddMarkInfo(playerInfo);
                        updateTeam2.Add(playerInfo);
                    }
                    Dispatcher.Invoke(() =>
                    {
                        team2.ItemsSource = updateTeam2;
                    });
                    LogShow(markMessage + "标记成功");
                }
                catch (Exception ex)
                {
                    LogShow(markMessage + "标记失败，" + ex.Message);
                }
            }
        }
        private void DebugPlayerEvent(object sender, RoutedEventArgs e)//单个玩家调试
        {
            string debugStr = Interaction.InputBox("调试文本："+ Environment.NewLine + "（作者用来调试的，想用这个功能的话请参考说明文档）", "提示");
            string? outputStr = null;
            if (!string.IsNullOrEmpty(debugStr)) 
            {
                Task.Run(() =>
                {
                    try
                    {
                        //解析输入文本
                        PlayerGameInfoInRep PlayerGameInfoInRep = new();
                        try { PlayerGameInfoInRep = JsonConvert.DeserializeObject<PlayerGameInfoInRep>(debugStr)!; }
                        catch (Exception ex) { throw new Exception("输入文本有误：" + ex.Message); }
                        //解析玩家信息
                        PlayerInfo playerInfo = ParsePlayer(new PlayerInfo(), PlayerGameInfoInRep);
                        PropertyInfo[] properties = playerInfo.GetType().GetProperties();
                        for (int i = 0; i < properties.Length; i++)
                            outputStr = outputStr + string.Format("{0,-20}", properties[i].Name) + ":" + properties[i].GetValue(playerInfo) + Environment.NewLine;
                    }
                    catch (Exception ex) { outputStr = "解析失败，" + ex.Message; }
                    finally
                    {
                        Logger.LogWrite(outputStr);
                        MessageBox.Show(outputStr);
                    }
                });
            }
        }
        private void MarkMessageChangedEvent(object sender, RoutedEventArgs e)//标记变更时更新配置
        {
            PlayerInfo currentPlayerInfo = (PlayerInfo)((System.Windows.Controls.TextBox)e.OriginalSource).DataContext;
            Logger.LogWrite($"即将标记的玩家:{currentPlayerInfo.PlayerId}");
            Config.AddMarkInfo(currentPlayerInfo);
        }

        private void WatchRepFolder()//监控rep文件夹
        {
            if(Config.watchFlag)
            {
                watcher.EnableRaisingEvents = false;//先停止监控
                watcher.Path = Config.ReplayPath!;
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = "tempArenaInfo.json";
                watcher.Created += (s, e) => 
                {
                    LogShow("检测到对局开始，正在读取");
                    ParseGame(ReadTempJson());
                };
                watcher.Deleted += (s, e) => 
                {
                    LogShow("检测到对局结束，正在监控rep文件夹");
                };
                watcher.EnableRaisingEvents = true;
                LogShow("正在监控rep文件夹");
            }
            else
            {
                LogShow("请检查路径是否设置");
            }
        }
        private JObject? ReadTempJson()//读取对局中生成的临时文件
        {
            Thread.Sleep(1000);
            try
            {
                string infoStr = File.ReadAllText(Path.Combine(Config.ReplayPath!, "tempArenaInfo.json"));
                return JObject.Parse(infoStr);
            }
            catch (Exception ex)
            {
                LogShow("未能成功读取对局文件，" + ex.Message);
                return null;
            }
        }
        private JObject? ReadRepJson(string path)//读取指定的rep文件
        {
            JObject? infoJson;
            string? infoStr;
            try
            {
                StreamReader sr = new(path);
                infoStr = sr.ReadLine();
                sr.Close();

                //读取rep文件中有效的对局信息，截取matchGroup开头，mapBorder结尾的字符串
                infoStr = @"{""matchGroup""" +
                    MatchInfoJsonInRep().Match(infoStr!).ToString() +
                    @"""mapBorder"": null}";
                infoJson = JObject.Parse(infoStr);

                LogShow("正在读取rep文件，" + path);
            }
            catch (Exception ex)
            {
                infoJson = null;
                LogShow("未能成功读取rep文件，" + ex.Message);
            }
            return infoJson;
        }
        private void ParseGame(JObject? infoJson)//解析对局json，把队伍信息绑定到前台表格中
        {
            if (infoJson != null) 
            {
                Stopwatch sw = new();
                sw.Start();
                string? exceptionMessage = null;
                Task.Run(() =>{
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
                        
                        List<PlayerInfo> playerInfo_team1 = new();
                        List<PlayerInfo> playerInfo_team2 = new();
                        int readCount = 0;
                        List<string> failedList = new();
                        List<PlayerGameInfoInRep> PlayerGameInfoList = JsonConvert.DeserializeObject<List<PlayerGameInfoInRep>>(infoJson["vehicles"]?.ToString()!)!;
                        
                        //建立反馈信息的类，并补充时间和战斗类型
                        YuyukoGameInfo yuyukoGameInfo = new();
                        yuyukoGameInfo.SetTime(infoJson["dateTime"]?.ToString()!);
                        yuyukoGameInfo.BattleType = infoJson["matchGroup"]?.ToString()!;

                        //并行执行
                        ParallelLoopResult parallelResult = Parallel.For(0, PlayerGameInfoList.Count, i =>
                        {
                            PlayerInfo playerInfo = new();
                            try
                            {
                                Thread.Sleep(300 * i);//并行调用之间延迟300毫秒，避免360接口提示调用过多的问题
                                playerInfo = ParsePlayer(playerInfo,PlayerGameInfoList[i]);
                            }
                            catch(Exception ex)
                            {
                                failedList.Add(PlayerGameInfoList[i].Name);
                                Logger.LogWrite("玩家信息读取失败，" + ex.Message + Environment.NewLine + 
                                    JsonConvert.SerializeObject(PlayerGameInfoList[i]).ToString());
                            }
                            finally
                            {
                                readCount++;
                                yuyukoGameInfo.AddYuyukoPlayerInfo(playerInfo);//添加到反馈信息

                                //修改群友信息，不影响yuyuko反馈
                                try
                                {
                                    if (Config.DIYPlayerInfo.ContainsKey(playerInfo.PlayerId))
                                    {
                                        PlayerInfo playerInfoInConfig = Config.DIYPlayerInfo[playerInfo.PlayerId];
                                        foreach (PropertyInfo prop in typeof(PlayerInfo).GetProperties())
                                        {
                                            object? defaultValue = prop.GetValue(new PlayerInfo());
                                            object? editValue = prop.GetValue(playerInfoInConfig);
                                            if (!string.IsNullOrEmpty(editValue!.ToString()))
                                                if (editValue != defaultValue)//排除空和默认值
                                                    prop.SetValue(playerInfo, editValue);
                                        }
                                    }
                                }
                                catch(Exception ex)
                                {
                                    Logger.LogWrite($"编辑群友信息失败，id:{playerInfo.Id},异常:{ex.Message}");
                                }
                                

                                if (playerInfo.Relation == 1 || playerInfo.Relation == 0)//0是用户，1是己方，2是敌方
                                    playerInfo_team1.Add(playerInfo);
                                else
                                    playerInfo_team2.Add(playerInfo);

                                LogShow($"正在读取对局信息({readCount}/{PlayerGameInfoList.Count})");
                            }
                        });
                        //绑定给前台
                        Dispatcher.Invoke(() =>
                        {
                            //输出前进行排序，先按舰种次按等级排序
                            team1.ItemsSource = playerInfo_team1.OrderByDescending(i => i.ShipTypeSort).ThenByDescending(i => i.ShipLevel_int);
                            team2.ItemsSource = playerInfo_team2.OrderByDescending(i => i.ShipTypeSort).ThenByDescending(i => i.ShipLevel_int);
                        });
                        yuyukoGameInfo.SendInfoToYuyuko();//发送反馈信息

                        if (failedList.Count > 0)
                            exceptionMessage = $"玩家{string.Join(",",failedList)}读取失败";
                    }
                    catch (Exception ex)
                    {
                        exceptionMessage = ex.Message;
                    }
                    finally
                    {
                        sw.Stop();
                        if (!string.IsNullOrEmpty(exceptionMessage))
                            exceptionMessage = "，发生异常：" + exceptionMessage;
                        LogShow($"已读取对局文件，耗时{(sw.ElapsedMilliseconds / 1000)}秒{exceptionMessage}");

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
        private static PlayerInfo ParsePlayer(PlayerInfo playerInfo, PlayerGameInfoInRep PlayerGameInfoInRep)//解析单个玩家的json数据
        {
            Stopwatch sw = new();
            sw.Start();

            playerInfo.SetBasePlayerInfo(PlayerGameInfoInRep);
            playerInfo.GetPlayerId();//取玩家id
            playerInfo.GetBattleInfo_withBattleType("pvp");//获取pvp数据
            playerInfo.GetBattleInfo_withBattleType("rank_solo");//获取rank数据
            playerInfo.DeleteTempFiles();//删除临时文件
            playerInfo.GetClanInfo();//获取军团数据
            playerInfo.GetShipInfo();//获取船数据
            playerInfo.GetBanInfo();//获取ban信息
            playerInfo.GetMarkInfo();//获取标记信息
            playerInfo.CheckLoadFailed();//检查数据是否抓取成功，失败则换一个方案抓取数据

                /*
                //并行线程太多，反而比不并行来的慢
                Parallel.Invoke(
                    () =>//获取pvp数据
                    {
                        playerInfo.GetBattleInfo_withBattleType("pvp");
                    },
                    () =>//获取rank数据
                    {
                        playerInfo.GetBattleInfo_withBattleType("rank_solo");
                    },
                    () =>//获取军团数据
                    {
                        playerInfo.GetClanInfo();
                    },
                    () =>//获取船数据
                    {
                        playerInfo.GetShipInfo();
                    },
                    () =>//获取ban信息
                    {
                        playerInfo.GetBanInfo();
                    },
                    () =>//获取标记信息
                    {
                        playerInfo.GetMarkInfo();
                    }
                );
                */
                sw.Stop();
            Logger.LogWrite("玩家"+playerInfo.PlayerId+"查询完成，耗时" + (sw.ElapsedMilliseconds / 1000).ToString() + "秒");
            return playerInfo;
        }

        [System.Text.RegularExpressions.GeneratedRegex("(?<={\"matchGroup\").*(?=\"mapBorder\")")]
        private static partial System.Text.RegularExpressions.Regex MatchInfoJsonInRep();

    }
}