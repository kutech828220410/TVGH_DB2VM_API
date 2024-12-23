using System;
using Basic;
namespace ConsoleApp_connect_testIn
{
    class Program
    {
        private static System.Threading.Mutex mutex;
        static void Main(string[] args)
        {
            mutex = new System.Threading.Mutex(true, "OnlyRun");
            while (true)
            {
                Console.WriteLine($"{DateTime.Now.ToDateTimeString()}-測試通訊....");
                string json = Basic.Net.WEBApiGet("http://10.107.3.147:443/api/testin");
                Console.WriteLine($"{DateTime.Now.ToDateTimeString()}-{json} ok....");
                System.Threading.Thread.Sleep(3000);
            }
            
        }
    }
}
