using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Web;
using System.Windows;

namespace 郭楠查看器
{
    public partial class apiClient
    {
        Logger Logger = Logger.Instance;
        string ip_official = "https://vortex.wowsgame.cn";
        string ip_yuyuko = "https://api.wows.shinoaki.com";
        string ip_yuyuko_dev = "https://api.wows.yuyuko.dev";
        string ip_yuyuko_new = "https://dev-proxy.wows.shinoaki.com:7700";


        public JObject GetClient(string url)//调用get接口，需要url
        {
            HttpClient HttpClient = new HttpClient();
            JObject apiResult = null;
            try
            {
                HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                var response = HttpClient.GetAsync(new Uri(url)).Result;
                int code = Convert.ToInt32(response.StatusCode);
                var apiResult_str = response.Content.ReadAsStringAsync().Result;
                apiResult = (JObject)JsonConvert.DeserializeObject<JObject>(apiResult_str);

                if (code != 200)
                    throw new Exception("Api Connection Failed. Code:" + code.ToString() + ";Result:" + apiResult_str);

                Boolean success = true;
                if (apiResult.ContainsKey("code"))
                    if (Convert.ToInt32(apiResult["code"]) != 200)
                        success = false;
                if (apiResult.ContainsKey("status"))
                    if (apiResult["status"].ToString() != "ok")
                        success = false;

                if (!success)
                    throw new Exception("Api Connection Failed. Code:" + code.ToString() + ";Result:" + apiResult_str);
            }
            catch (Exception ex) { Logger.logWrite(String.Format("get调用失败，url:{0}；失败原因：{1}", url, ex.Message)); }

            return apiResult;
        }
        public JObject PostClient(string url, Dictionary<string, string> contantDictionary)//调用post接口，需要url和jsonBody
        {
            HttpClient HttpClient = new HttpClient();
            JObject apiResult = null;
            try
            {
                var response = HttpClient.PostAsync(url, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(contantDictionary), Encoding.UTF8, "application/json")).Result;
                int code = Convert.ToInt32(response.StatusCode);
                string apiResult_str = response.Content.ReadAsStringAsync().Result;
                apiResult = (JObject)JsonConvert.DeserializeObject<JObject>(apiResult_str);

                if (code != 200)
                    throw new Exception("Api Connection Failed. Code:" + code.ToString() + ";Result:" + apiResult.ToString());

                Boolean success = true;
                if (apiResult.ContainsKey("code"))
                    if (Convert.ToInt32(apiResult["code"]) != 200)
                        success = false;
                if (apiResult.ContainsKey("status"))
                    if (apiResult["status"].ToString() != "ok")
                        success = false;

                if (!success)
                    throw new Exception("Api Connection Failed. Code:" + code.ToString() + ";Result:" + apiResult_str);
            }
            catch (Exception ex) { Logger.logWrite(String.Format("post调用失败，url:{0}；失败原因：{1}",url,ex.Message)); }

            return apiResult;
        }

        public JObject GetPlayerId(string playerName)//通过玩家名称获取玩家id
        {
            string url = ip_official + "/api/accounts/search/autocomplete/" + Uri.EscapeDataString(playerName);
            return GetClient(url);
        }
        public JObject GetPlayerInfo_official(string playerId)//通过玩家id获取官方的玩家信息
        {
            string url = ip_official + "/api/accounts/" + playerId;
            return GetClient(url);
        }
        public JObject GetPlayerInfo_yuyuko(string playerId)//通过玩家id获取yuyuko(old)的玩家信息
        {
            string url = ip_yuyuko + "/public/wows/account/user/info?server=cn&accountId=" + playerId;
            return GetClient(url);
        }
        public JObject GetPlayerShipInfo_yuyuko(string playerId, string shipId)//通过玩家id和船id获取yuyuko(old)的玩家单船信息
        {
            string url = ip_yuyuko + "/public/wows/account/ship/info?accountId=" + playerId + "&server=cn&shipId=" + shipId;
            return GetClient(url);
        }

        public JObject GetPlayerInfo_yuyuko_new(string playerId, string shipId)//通过玩家id和船id获取yuyuko的玩家单船信息
        {
            string url = ip_yuyuko + "/process/wows/user/info/cn/"+ playerId + "/query/" + shipId;
            return GetClient(url);
        }

        public JObject GetPlayerBanInfo_yuyuko(string playerId)//通过玩家id获取yuyuko的ban信息
        {
            string url = ip_yuyuko_dev + "/public/wows/ban/cn/user";
            Dictionary<string, string> contantDictionary = new Dictionary<string, string>();
            contantDictionary["accountId"] = playerId;
            return PostClient(url, contantDictionary);
        }
        public JObject GetShipInfo(string shipId)//通过船id获取yuyuko的船信息
        {
            string url = ip_yuyuko_dev + "/public/wows/encyclopedia/ship/info?shipId=" + shipId;
            return GetClient(url);
        }
        public JObject GetPlayerShipRankSort(string playerId,string shipId)//通过玩家id和船id获取yuyuko的排行，顺便帮雨季收集玩家信息
        {
            string url = ip_yuyuko_new + "/wows/rank/cn/sort/" + playerId + "/" + shipId;
            return GetClient(url);
        }
    }
}
