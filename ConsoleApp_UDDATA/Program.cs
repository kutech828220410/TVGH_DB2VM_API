using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Basic;
using System.Threading;
using HIS_DB_Lib;

namespace ConsoleApp_UDDATA
{
    class Program
    {
        static void Main(string[] args)
        {
            bool isNewInstance;
            Mutex mutex = new Mutex(true, "ConsoleApp_get_UDData", out isNewInstance);
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
                    string url = "http://10.107.3.147:443/api/med_cart/get_all_db";
                    string json = Basic.Net.WEBApiGet(url);
                    returnData returnData_UDData = json.JsonDeserializet<returnData>();
                    Console.WriteLine($"{DateTime.Now.ToString()}-取得病床資訊、處方結束\n{returnData_UDData.JsonSerializationt(true)}");
                    Console.WriteLine("----------------------------------------");

                    Console.WriteLine($"{DateTime.Now.ToString()}-取得處方異動");
                    url = "http://10.107.3.147:443/api/med_cart/get_medChange";
                    json = Basic.Net.WEBApiGet(url);
                    returnData returnData_medChange = json.JsonDeserializet<returnData>();
                    Console.WriteLine($"{DateTime.Now.ToString()}-取得處方異動結束");
                    Console.WriteLine("----------------------------------------");

                    Console.WriteLine($"{DateTime.Now.ToString()}-取得病床異動");
                    url = "http://10.107.3.147:443/api/med_cart/get_bedStatus";
                    json = Basic.Net.WEBApiGet(url);
                    returnData returnData_bedChange = json.JsonDeserializet<returnData>();
                    Console.WriteLine($"{DateTime.Now.ToString()}-取得病床異動結束");
                    Console.WriteLine("----------------------------------------");

                    Console.WriteLine($"{DateTime.Now.ToString()}-取得藥品資訊");
                    url = "http://10.107.3.147:443/api/med_cart/get_medInfo";
                    json = Basic.Net.WEBApiGet(url);
                    returnData returnData_medInfo = json.JsonDeserializet<returnData>();
                    Console.WriteLine($"{DateTime.Now.ToString()}-取得藥品資訊結束");
                    Console.WriteLine("----------------------------------------");
                    System.Threading.Thread.Sleep(60000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception:{ex}");
                Console.ReadKey();
            }
        }
    }
}
