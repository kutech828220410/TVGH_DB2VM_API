using System;
using Basic;
using HIS_DB_Lib;
using System.Threading;


namespace ConsoleApp_medCartData
{
    class Program
    {
        static void Main(string[] args)
        {
            bool isNewInstance;
            Mutex mutex = new Mutex(true, "ConsoleApp_medCartData", out isNewInstance);
            try
            {
                if (!isNewInstance)
                {
                    Console.ReadKey();
                    Console.WriteLine("程式已經在運行中...");
                    return;
                }
                while (true)
                {
                    DateTime now = DateTime.Now;
                    if (now.TimeOfDay > new TimeSpan(15, 0, 0)) break;
                    Console.WriteLine($"{DateTime.Now.ToString()}-取得病床資訊、處方開始");
                    string url = "https://localhost:44392/api/med_cart/get_all_db";
                    //string url = "http://10.107.3.147:443/api/med_cart/get_all_db";
                    string json = Basic.Net.WEBApiGet(url);
                    //returnData returnData_get_all = json.JsonDeserializet<returnData>();
                    //Console.WriteLine($"{returnData_get_all.JsonSerializationt(true)}");
                    Console.WriteLine($"{DateTime.Now.ToString()}-取得病床資訊、處方結束");
                    Console.WriteLine("----------------------------------------");

                    Console.WriteLine($"{DateTime.Now.ToString()}-取得處方異動開始");
                    url = "http://10.107.3.147:443/api/med_cart/get_medChange";
                    json = Basic.Net.WEBApiGet(url);
                    //returnData returnData_get_medChange = json.JsonDeserializet<returnData>();
                    //Console.WriteLine($"{returnData_get_medChange.JsonSerializationt(true)}");
                    Console.WriteLine($"{DateTime.Now.ToString()}-取得處方異動結束");
                    Console.WriteLine("----------------------------------------");

                    Console.WriteLine($"{DateTime.Now.ToString()}-藥品資訊更新開始");
                    url = "http://10.107.3.147:443/api/med_cart/get_medInfo";
                    json = Basic.Net.WEBApiGet(url);
                    //returnData returnData_get_medInfo = json.JsonDeserializet<returnData>();
                    //Console.WriteLine($"{returnData_get_medInfo.JsonSerializationt(true)}");
                    Console.WriteLine($"{DateTime.Now.ToString()}-藥品資訊更新結束");
                    Console.WriteLine("----------------------------------------");

                    Console.WriteLine($"{DateTime.Now.ToString()}-病床異動更新開始");
                    url = "http://10.107.3.147:443/api/med_cart/get_bedStatus";
                    json = Basic.Net.WEBApiGet(url);
                    //returnData returnData_get_medInfo = json.JsonDeserializet<returnData>();
                    //Console.WriteLine($"{returnData_get_medInfo.JsonSerializationt(true)}");
                    Console.WriteLine($"{DateTime.Now.ToString()}-病床異動更新結束");
                    Console.WriteLine("----------------------------------------");
                    System.Threading.Thread.Sleep(120000);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception:{ex}");
                Console.ReadKey();
            }
            
        }
    }
}
