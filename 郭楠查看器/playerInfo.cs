using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace 郭楠查看器
{
    public class playerInfo
    {
        public string name { get; set; }
        public string playerId { get; set; }
        public string playerPrColor { get; set; }


        public string clanTag { get; set; }
        public string clanId { get; set; }
        public string clanColor { get; set; }


        private int setIntLevel;
        public string shipId { get; set; }
        public string shipName { get; set; }
        public int shipSort { get; set; }
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
        private void setRomanLevel(int intLevel)//将阿拉伯数字转换成罗马数字
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
        shipLevel_roman = String.Format("{0,-4}", shipLevel_roman);
        }


        public string banMatch { get; set; }
        public string banMatch_fullStr { get; set; }
        public string banColor { get; set; }


        public string battleCount_ship { get; set; }
        public string winRate_ship { get; set; }
        public string battleCount_pvp { get; set; }
        public string winRate_pvp { get; set; }
        public string battleCount_rank { get; set; }
        public string winRate_rank { get; set; }


        public string markMessage { get; set; }
        public string lastMarkMessage { get; set; }
    }
}
