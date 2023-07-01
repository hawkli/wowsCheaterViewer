using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace wowsCheaterViewer
{
    public class apiClient
    {
        static string address_official = "https://vortex.wowsgame.cn";
        static string address_yuyukoWowsApi_域名1 = "https://api.wows.shinoaki.com";//即将弃用
        static string address_yuyukoWowsApi_域名2 = "https://api.wows.yuyuko.dev";//即将弃用
        static string address_yuyukoWowsApi_域名3 = "https://v3-api.wows.shinoaki.com";
        static string address_yuyuko战舰世界API平台接口处理与反向代理 = "https://dev-proxy.wows.shinoaki.com:7700";

        //battleType=[basic,pve,pvp,pvp_solo,pvp_div2,pvp_div3,rank_old_solo,rank_solo,rank_div2,rank_div3,seasons]

        public static async Task<string> GetClientAsync(string url)//调用get接口，需要url
        {
            string apiResult_str = null;
            int code = 0;
            try
            {
                HttpClient HttpClient = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                if(url.Contains(address_yuyukoWowsApi_域名3))//yuyuko新域名接口需要加一个headers，老的则不用
                    request.Headers.Add("Yuyuko-Client-Type", "WEB;01");
                HttpResponseMessage response = await HttpClient.SendAsync(request);
                apiResult_str = response.Content.ReadAsStringAsync().Result;
                code = Convert.ToInt32(response.StatusCode);
                checkApiResult(code, apiResult_str);
            }
            catch (Exception ex) 
            { 
                Logger.logWrite($"get调用失败，url:{url};code:{code};result:{apiResult_str};reason:{ex.Message}" );
                if (url.Contains(address_yuyukoWowsApi_域名3))
                {
                    Logger.logWrite("更换域名重试yuyuko接口");
                    apiResult_str = GetClientAsync(url.Replace(address_yuyukoWowsApi_域名3, address_yuyukoWowsApi_域名2)).Result;
                }
            }

            return apiResult_str;
        }
        public static async Task<string> PostClientAsync(string url, string? bodyString, Dictionary<string, string>? filePaths)//调用post接口，需要url，jsonBody和file选填
        {
            string apiResult_str = null;
            int code = 0;
            try
            {
                HttpClient HttpClient = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                if (!string.IsNullOrEmpty(bodyString))
                {
                    //处理bodyJson
                    StringContent content = new StringContent(bodyString, Encoding.UTF8, "application/json");
                    request.Content = content;
                }
                else if (filePaths != null)
                {
                    //处理文件
                    MultipartFormDataContent content = new MultipartFormDataContent();
                    foreach (KeyValuePair<string, string> filePath in filePaths)
                    {
                        Stream fs = new FileStream(filePath.Value, FileMode.Open, FileAccess.Read);
                        byte[] data = new byte[fs.Length];
                        fs.Read(data, 0, data.Length);
                        fs.Close();
                        content.Add(new ByteArrayContent(data), filePath.Key, filePath.Value);
                    }
                    request.Content = content;
                }
                if (url.Contains(address_yuyukoWowsApi_域名3))//yuyuko新域名接口需要加一个headers，老的则不用
                    request.Headers.Add("Yuyuko-Client-Type", "WEB;01");
                HttpResponseMessage response = await HttpClient.SendAsync(request);
                apiResult_str = response.Content.ReadAsStringAsync().Result;
                code = Convert.ToInt32(response.StatusCode);
                checkApiResult(code, apiResult_str);
            }
            catch (Exception ex)
            {
                Logger.logWrite($"get调用失败，url:{url};code:{code};result:{apiResult_str};reason:{ex.Message}");
                if (url.Contains(address_yuyukoWowsApi_域名3))
                {
                    Logger.logWrite("更换域名重试yuyuko接口");
                    apiResult_str = PostClientAsync(url.Replace(address_yuyukoWowsApi_域名3, address_yuyukoWowsApi_域名2), bodyString, filePaths).Result;
                }
            }

            return apiResult_str;
        }
        private static void checkApiResult(int code, string apiResult_str)
        {
            JObject apiResult = JObject.Parse(apiResult_str);
            bool success = true;

            if (code != 200)
                success = false;
            if (apiResult.ContainsKey("code"))
                if (Convert.ToInt32(apiResult["code"]) != 200)
                    success = false;
            if (apiResult.ContainsKey("status"))
                if (apiResult["status"].ToString() != "ok")
                    success = false;

            if (!success)
                throw new Exception("Api Connection Failed. Code:" + code.ToString());
        }

        public static string GetPlayerId(string playerName)//通过玩家名称获取玩家id
        {
            string url = address_official + "/api/accounts/search/autocomplete/" + Uri.EscapeDataString(playerName);
            return GetClientAsync(url).Result;
        }
        public static string GetPlayerInfo_official(string playerId)//通过玩家id获取官方的玩家信息
        {
            string url = address_official + "/api/accounts/" + playerId;
            return GetClientAsync(url).Result;
        }
        public static string GetPalyersShipsInfo_official(string playerId, string battleType)//通过玩家id获取官方的玩家船信息
        {
            string url = address_official + "/api/accounts/" + playerId + "/ships/" + battleType.ToLower();
            return GetClientAsync(url).Result;
        }
        public static string GetPalyersClansInfo_official(string playerId)//通过玩家id获取官方的玩家军团信息
        {
            string url = address_official + "/api/accounts/" + playerId + "/clans";
            return GetClientAsync(url).Result;
        }
        public static string GetPlayerInfo_yuyuko(string playerId)//通过玩家id获取yuyuko的玩家信息
        {
            string url = address_yuyukoWowsApi_域名3 + "/public/wows/account/user/info?server=cn&accountId=" + playerId;
            return GetClientAsync(url).Result;
        }
        public static string GetPlayerShipInfo_yuyuko(string playerId, string shipId)//通过玩家id和船id获取yuyuko(old)的玩家单船信息
        {
            string url = address_yuyukoWowsApi_域名3 + "/public/wows/account/ship/info?accountId=" + playerId + "&server=cn&shipId=" + shipId;
            return GetClientAsync(url).Result;
        }
        public static string GetPlayerBanInfo_yuyuko(string playerId)//通过玩家id获取yuyuko的ban信息
        {
            string url = address_yuyukoWowsApi_域名3 + "/public/wows/ban/cn/user";
            Dictionary<string, string> contantDictionary = new Dictionary<string, string>();
            contantDictionary["accountId"] = playerId;
            return PostClientAsync(url, JsonConvert.SerializeObject(contantDictionary), null).Result;
        }
        public static string GetShipInfo(string shipId)//通过船id获取yuyuko的船信息
        {
            string url = address_yuyukoWowsApi_域名3 + "/public/wows/encyclopedia/ship/info?shipId=" + shipId;
            return GetClientAsync(url).Result;
        }
        public static string GetPlayerShipRankSort(string playerId, string shipId)//通过玩家id和船id获取yuyuko的排行，顺便帮雨季收集玩家信息
        {
            string url = address_yuyuko战舰世界API平台接口处理与反向代理 + "/wows/rank/cn/sort/" + playerId + "/" + shipId;
            return GetClientAsync(url).Result;
        }
        public static string GetParsedPlayerInfo_yuyuko(string playerId, string shipId, string battleType, string uploadFilePath, string clanFilePath)//用官网返回的信息写入文件，让yuyuko解析
        {
            string url = address_yuyuko战舰世界API平台接口处理与反向代理 + "/process/wows/user/info/cn/upload/vortex/data/" + battleType.ToUpper() + "/battle/" + playerId + "/query/" + shipId;
            return PostClientAsync(url, null, new Dictionary<string, string> { { "files", uploadFilePath }, { "clan", clanFilePath } }).Result;
        }
        public static void sendYuyukoGameInfo(string yuyukoGameInfoStr)//数据提交给yuyuko
        {
            string url = address_yuyuko战舰世界API平台接口处理与反向代理 + "/upload/wows/game/player";
            _ = PostClientAsync(url, yuyukoGameInfoStr, null);
        }
    }
}
