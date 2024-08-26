using System;
using Basic;
namespace ConsoleApp_test_db2vm
{
    class Program
    {
        private static System.Threading.Mutex mutex;
        static void Main(string[] args)
        {
            mutex = new System.Threading.Mutex(true, "OnlyRun");
            if (mutex.WaitOne(0, false))
            {
                while (true)
                {
                    Console.WriteLine($"{DateTime.Now.ToDateTimeString()}-測試通訊....");
                    string json = Basic.Net.WEBApiGet("http://10.53.10.153:443/api/test");
                    Console.WriteLine($"{DateTime.Now.ToDateTimeString()}-{json} ok....");
                    System.Threading.Thread.Sleep(5000);
                }
            }
            else
            {

            }



        }
    }
}
