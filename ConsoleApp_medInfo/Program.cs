using System;
using Basic;
using HIS_DB_Lib;

namespace ConsoleApp_medInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("藥品資訊更新開始");
            string url = "http://10.107.3.147:443/api/med_cart/get_medInfo";
            string json = Basic.Net.WEBApiGet(url);
            //returnData returnData = json.JsonDeserializet<returnData>();
            //Console.WriteLine($"code:{returnData.Code} result:{returnData.Result}");
            Console.WriteLine("藥品資訊更新結束");
        }
    }
}
