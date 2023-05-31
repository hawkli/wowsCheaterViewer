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

        private HttpClient HttpClient = new HttpClient();
        private JObject GetClient(string url)//调用get接口，需要url
        {
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            var response = HttpClient.GetAsync(new Uri(url)).Result;
            int code = Convert.ToInt32(response.StatusCode);
            var apiResult_str = response.Content.ReadAsStringAsync().Result;
            JObject apiResult = (JObject)JsonConvert.DeserializeObject<JObject>(apiResult_str);

            if (code != 200)
                throw new Exception("Api Connection Failed. Code:" + code.ToString() + ";Result:" + apiResult_str);

            Boolean success = false;
            if (apiResult.ContainsKey("code"))
                if (Convert.ToInt32(apiResult["code"]) == 200)
                    success = true;
            if (apiResult.ContainsKey("status"))
                if (apiResult["status"].ToString() == "ok")
                    success = true;

            if (!success)
                throw new Exception("Api Connection Failed. Code:" + code.ToString() + ";Result:" + apiResult_str);

            return apiResult;
        }
        private JObject PostClient(string url, Dictionary<string, string> contantDictionary)//调用post接口，需要url和jsonBody
        {
            var response = HttpClient.PostAsync(url, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(contantDictionary),Encoding.UTF8,"application/json")).Result;
            int code = Convert.ToInt32(response.StatusCode);
            string apiResult_str = response.Content.ReadAsStringAsync().Result;
            JObject apiResult = (JObject)JsonConvert.DeserializeObject<JObject>(apiResult_str);
            
            if (code != 200)
                throw new Exception("Connection Failed. Code:" + code.ToString() + ";Result:" + apiResult.ToString());

            Boolean success = false;
            if (apiResult.ContainsKey("code"))
                if (Convert.ToInt32(apiResult["code"]) == 200)
                    success = true;
            if (apiResult.ContainsKey("status"))
                if (apiResult["status"].ToString() == "ok")
                    success = true;
            if (!success)
                throw new Exception("Api Connection Failed. Code:" + code.ToString() + ";Result:" + apiResult.ToString());

            return apiResult;
        }

        public JObject GetPlayerId(string playerName)//通过玩家名称获取玩家id
        {
            string url = "https://vortex.wowsgame.cn/api/accounts/search/autocomplete/" + HttpUtility.UrlEncode(playerName).Replace("+"," ");
            try
            {
                return GetClient(url);
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public JObject GetPlayerInfo_official(string playerId)//通过玩家id获取官方的玩家信息
        {
            string url = "https://vortex.wowsgame.cn/api/accounts/" + playerId;
            try
            {
                return GetClient(url);
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public JObject GetPlayerInfo_yuyuko(string playerId)//通过玩家id获取yuyuko的玩家信息
        {
            string url = "https://api.wows.shinoaki.com/public/wows/account/user/info?server=cn&accountId=" + playerId;
            try
            {
                return GetClient(url);
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public JObject GetPlayerShipInfo_yuyuko(string playerId, string shipId)//通过玩家id和船id获取yuyuko的玩家单船信息
        {
            string url = "https://api.wows.shinoaki.com/public/wows/account/ship/info?accountId=" + playerId + "&server=cn&shipId=" + shipId;
            try
            {
                return GetClient(url);
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public JObject GetPlayerBanInfo_yuyuko(string playerId)//通过玩家id获取yuyuko的ban信息
        {
            string url = "https://api.wows.yuyuko.dev/public/wows/ban/cn/user";
            Dictionary<string, string> contantDictionary = new Dictionary<string, string>();
            contantDictionary["accountId"] = playerId;
            try
            {
                return PostClient(url, contantDictionary);
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public JObject GetShipInfo(string shipId)//通过船id获取yuyuko的船信息
        {
            string url = "https://api.wows.yuyuko.dev/public/wows/encyclopedia/ship/info?shipId=" + shipId;
            try
            {
                return GetClient(url);
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public JObject GetPlayerShipRankSort(string playerId,string shipId)//通过玩家id和船id获取yuyuko的排行，顺便帮雨季收集玩家信息
        {
            string url = "https://dev-proxy.wows.shinoaki.com:7700/wows/rank/cn/sort/" + playerId + "/" + shipId;
            try
            {
                return GetClient(url);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
