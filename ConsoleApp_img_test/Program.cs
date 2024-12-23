using System;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using HIS_DB_Lib;
using Basic;

namespace ConsoleApp_img_test
{
    class Program
    {
        static private string API_Server = "http://127.0.0.1:4433";
        static void Main(string[] args)
        {
            try
            {
                //List<medClass> medClasses = medClass.get_med_cloud(API_Server);
                //Console.WriteLine($"取的藥檔共{medClasses.Count}筆");
                string code = "00534";
                string url = $"https://www7.vghtpe.gov.tw/api/find-zero-image-by-udCode?udCode={code}";
                string pic_base64 = DownloadPic(url);
                if(pic_base64.StringIsEmpty() == false)
                {
                    Console.WriteLine($"{pic_base64}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading image: {ex.Message}");
            }

        }
        public static string DownloadPic(string url)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
                byte[] imageBytes = client.DownloadData(url);
                string pic1_base64 = Convert.ToBase64String(imageBytes);
                return pic1_base64;
            }
        }
    }
}
