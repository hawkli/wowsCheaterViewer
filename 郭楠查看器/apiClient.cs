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

        public void logWrite(string message)//写入日志文件
        {
            using (FileStream fs = new FileStream("log.log", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
            {
                fs.Seek(0, SeekOrigin.End);
                fs.Write(Encoding.Default.GetBytes((string.Format("{0} {1}" + Environment.NewLine, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message))));
            }
        }
        private JObject GetClient(string url)//调用get接口，需要url
        {
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

                Boolean success = false;
                if (apiResult.ContainsKey("code"))
                    if (Convert.ToInt32(apiResult["code"]) == 200)
                        success = true;
                if (apiResult.ContainsKey("status"))
                    if (apiResult["status"].ToString() == "ok")
                        success = true;

                if (!success)
                    throw new Exception("Api Connection Failed. Code:" + code.ToString() + ";Result:" + apiResult_str);
            }
            catch (Exception ex) { logWrite(String.Format("get调用失败，url:{0}；失败原因：{1}", url, ex.Message)); }

            return apiResult;
        }
        private JObject PostClient(string url, Dictionary<string, string> contantDictionary)//调用post接口，需要url和jsonBody
        {
            JObject apiResult = null;
            try
            {
                var response = HttpClient.PostAsync(url, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(contantDictionary), Encoding.UTF8, "application/json")).Result;
                int code = Convert.ToInt32(response.StatusCode);
                string apiResult_str = response.Content.ReadAsStringAsync().Result;
                apiResult = (JObject)JsonConvert.DeserializeObject<JObject>(apiResult_str);

                if (code != 200)
                    throw new Exception("Api Connection Failed. Code:" + code.ToString() + ";Result:" + apiResult.ToString());

                Boolean success = false;
                if (apiResult.ContainsKey("code"))
                    if (Convert.ToInt32(apiResult["code"]) == 200)
                        success = true;
                if (apiResult.ContainsKey("status"))
                    if (apiResult["status"].ToString() == "ok")
                        success = true;
                if (!success)
                    throw new Exception("Api Connection Failed. Code:" + code.ToString() + ";Result:" + apiResult.ToString());
            }
            catch (Exception ex) { logWrite(String.Format("post调用失败，url:{0}；失败原因：{1}",url,ex.Message)); }

            return apiResult;
        }

        public JObject GetPlayerId(string playerName)//通过玩家名称获取玩家id
        {
            string url = "https://vortex.wowsgame.cn/api/accounts/search/autocomplete/" + HttpUtility.UrlEncode(playerName).Replace("+"," ");
            return GetClient(url);
        }
        public JObject GetPlayerInfo_official(string playerId)//通过玩家id获取官方的玩家信息
        {
            string url = "https://vortex.wowsgame.cn/api/accounts/" + playerId;
            return GetClient(url);
        }
        public JObject GetPlayerInfo_yuyuko(string playerId)//通过玩家id获取yuyuko的玩家信息
        {
            string url = "https://api.wows.shinoaki.com/public/wows/account/user/info?server=cn&accountId=" + playerId;
            return GetClient(url);
        }
        public JObject GetPlayerShipInfo_yuyuko(string playerId, string shipId)//通过玩家id和船id获取yuyuko的玩家单船信息
        {
            string url = "https://api.wows.shinoaki.com/public/wows/account/ship/info?accountId=" + playerId + "&server=cn&shipId=" + shipId;
            return GetClient(url);
        }
        public JObject GetPlayerBanInfo_yuyuko(string playerId)//通过玩家id获取yuyuko的ban信息
        {
            string url = "https://api.wows.yuyuko.dev/public/wows/ban/cn/user";
            Dictionary<string, string> contantDictionary = new Dictionary<string, string>();
            contantDictionary["accountId"] = playerId;
            return PostClient(url, contantDictionary);
        }
        public JObject GetShipInfo(string shipId)//通过船id获取yuyuko的船信息
        {
            string url = "https://api.wows.yuyuko.dev/public/wows/encyclopedia/ship/info?shipId=" + shipId;
            return GetClient(url);
        }
        public JObject GetPlayerShipRankSort(string playerId,string shipId)//通过玩家id和船id获取yuyuko的排行，顺便帮雨季收集玩家信息
        {
            string url = "https://dev-proxy.wows.shinoaki.com:7700/wows/rank/cn/sort/" + playerId + "/" + shipId;
            return GetClient(url);
        }
    }
}
