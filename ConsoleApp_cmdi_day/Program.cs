using System;
using HIS_DB_Lib;
using SQLUI;
namespace ConsoleApp_cmdi_day
{
    class Program
    {
        private static string API_Server = "http://127.0.0.1:4433";
        static void Main(string[] args)
        {
            Table table = transactionsClass.Init(API_Server, "門診高管櫃", "調劑台");
        }
    }
}
