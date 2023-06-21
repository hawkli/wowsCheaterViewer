﻿using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Web;
using System.Windows;
using System.Threading.Tasks;

namespace 郭楠查看器
{
    public class apiClient
    {
        Logger Logger = Logger.Instance;
        string address_official = "https://vortex.wowsgame.cn";
        string address_yuyukoWowsApi_域名1 = "https://api.wows.shinoaki.com";//即将弃用
        string address_yuyukoWowsApi_域名2 = "https://api.wows.yuyuko.dev";//即将弃用
        string address_yuyukoWowsApi_域名3 = "https://v3-api.wows.shinoaki.com";
        string address_yuyuko战舰世界API平台接口处理与反向代理 = "https://dev-proxy.wows.shinoaki.com:7700";

        //battleType=[basic,pve,pvp,pvp_solo,pvp_div2,pvp_div3,rank_old_solo,rank_solo,rank_div2,rank_div3,seasons]

        public async Task<String> GetClientAsync(string url)//调用get接口，需要url
        {
            string apiResult_str = null;
            int code = 0;
            try
            {
                HttpClient HttpClient = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await HttpClient.SendAsync(request);
                apiResult_str = response.Content.ReadAsStringAsync().Result;
                code = Convert.ToInt32(response.StatusCode);
                checkApiResult(code, apiResult_str);
            }
            catch (Exception ex) { Logger.logWrite(String.Format("get调用失败，url:{0};code:{1};result:{2};reason:{3}", url, code, apiResult_str, ex.Message)); }

            return apiResult_str;
        }
        public async Task<String> PostClientAsync(string url, Dictionary<string, string>? bodyDictionary, Dictionary<string, string>? filePaths)//调用post接口，需要url，jsonBody和file选填
        {
            string apiResult_str = null;
            int code = 0;
            try
            {
                HttpClient HttpClient = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                if (bodyDictionary != null)
                {
                    //处理bodyJson
                    StringContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(bodyDictionary), Encoding.UTF8, "application/json");
                    request.Content = content;
                }
                else if (filePaths != null)
                {
                    //处理文件
                    MultipartFormDataContent content = new MultipartFormDataContent();
                    foreach (KeyValuePair<string, string> filePath in filePaths)
                        content.Add(new StreamContent(File.OpenRead(filePath.Value)), filePath.Key, filePath.Value);
                    request.Content = content;
                }
                HttpResponseMessage response = await HttpClient.SendAsync(request);
                apiResult_str = response.Content.ReadAsStringAsync().Result;
                code = Convert.ToInt32(response.StatusCode);
                checkApiResult(code, apiResult_str);
            }
            catch (Exception ex) { Logger.logWrite(String.Format("get调用失败，url:{0};code:{1};result:{2};reason:{3}", url, code, apiResult_str, ex.Message)); }

            return apiResult_str;
        }
        private void checkApiResult(int code, string apiResult_str)
        {
            JObject apiResult = JObject.Parse(apiResult_str);
            Boolean success = true;

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

        public String GetPlayerId(string playerName)//通过玩家名称获取玩家id
        {
            string url = address_official + "/api/accounts/search/autocomplete/" + Uri.EscapeDataString(playerName);
            return GetClientAsync(url).Result;
        }
        public String GetPlayerInfo_official(string playerId)//通过玩家id获取官方的玩家信息
        {
            string url = address_official + "/api/accounts/" + playerId;
            return GetClientAsync(url).Result;
        }
        public String GetPalyersShipsInfo_official(string playerId, string battleType)//通过玩家id获取官方的玩家船信息
        {
            string url = address_official + "/api/accounts/" + playerId + "/ships/" + battleType.ToLower();
            return GetClientAsync(url).Result;
        }
        public String GetPalyersClansInfo_official(string playerId)//通过玩家id获取官方的玩家军团信息
        {
            string url = address_official + "/api/accounts/" + playerId + "/clans";
            return GetClientAsync(url).Result;
        }
        public String GetPlayerInfo_yuyuko(string playerId)//通过玩家id获取yuyuko(old)的玩家信息
        {
            string url = address_yuyukoWowsApi_域名3 + "/public/wows/account/user/info?server=cn&accountId=" + playerId;
            return GetClientAsync(url).Result;
        }
        public String GetPlayerShipInfo_yuyuko(string playerId, string shipId)//通过玩家id和船id获取yuyuko(old)的玩家单船信息
        {
            string url = address_yuyukoWowsApi_域名3 + "/public/wows/account/ship/info?accountId=" + playerId + "&server=cn&shipId=" + shipId;
            return GetClientAsync(url).Result;
        }
        public String GetPlayerBanInfo_yuyuko(string playerId)//通过玩家id获取yuyuko的ban信息
        {
            string url = address_yuyukoWowsApi_域名2 + "/public/wows/ban/cn/user";
            Dictionary<string, string> contantDictionary = new Dictionary<string, string>();
            contantDictionary["accountId"] = playerId;
            return PostClientAsync(url, contantDictionary, null).Result;
        }
        public String GetShipInfo(string shipId)//通过船id获取yuyuko的船信息
        {
            string url = address_yuyukoWowsApi_域名2 + "/public/wows/encyclopedia/ship/info?shipId=" + shipId;
            return GetClientAsync(url).Result;
        }
        public String GetPlayerShipRankSort(string playerId, string shipId)//通过玩家id和船id获取yuyuko的排行，顺便帮雨季收集玩家信息
        {
            string url = address_yuyuko战舰世界API平台接口处理与反向代理 + "/wows/rank/cn/sort/" + playerId + "/" + shipId;
            return GetClientAsync(url).Result;
        }

        public String GetParsedPlayerInfo_yuyuko(string playerId, string shipId, string battleType, string uploadFilePath, string clanFilePath)//用官网返回的信息写入文件，让yuyuko解析
        {
            string url = address_yuyuko战舰世界API平台接口处理与反向代理 + "/process/wows/user/info/cn/upload/vortex/data/" + battleType.ToUpper() + "/battle/" + playerId + "/query/" + shipId;
            return PostClientAsync(url, null, new Dictionary<string, string> { { "files", uploadFilePath }, { "clan", clanFilePath } }).Result;
        }
    }
}
