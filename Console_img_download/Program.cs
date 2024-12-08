using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Basic;
using System.Net;
using HIS_DB_Lib;
using System.Collections.Generic;

namespace Console_img_download
{
    class Program
    {
        static private string API_Server = "http://127.0.0.1:4433";
        static void Main(string[] args)
        {
            //string url = "https://www7.vghtpe.gov.tw/api/find-zero-image-by-udCode?udCode=03923";
            //string url = "https://www7.vghtpe.gov.tw/api/find-first-image-by-udCode?udCode=03923";
            //string filePath = "C:/Users/Administrator/Desktop/MedPic";

            //string pic_base64 = Basic.Net.DownloadImageAsBase64(url);

            try
            {
                MyTimerBasic myTimerBasic = new MyTimerBasic();
                Logger.LogAddLine();
                List<medClass> medClasses = medClass.get_med_cloud(API_Server);
                Logger.Log($"取得藥品資料共<{medClasses.Count}>筆");
                List<medPicClass> medPicClasses = new List<medPicClass>();
                List<object[]> list_medpic = new List<object[]>();
                List<Task> tasks = new List<Task>();
                string log = "";
                int index = 0;
                //foreach(medClass medClass in medClasses)
                //{

                //        string pic_url = medClass.圖片網址;
                //        string pic_base64 = 
                //        tasks.Add(Task.Run(new Action(delegate 
                //        {

                //        })));

                //}


                foreach (medClass medClass in medClasses)
                {
                    List<medPicClass> medPicClasses_buf = new List<medPicClass>();
                   
                    string 藥碼 = medClass.藥品碼;
                    string 藥名 = medClass.藥品名稱;
                    string pic_url = medClass.圖片網址;
                    string pic_base64 = DownloadPic(pic_url);
                    index++;
                    Console.WriteLine($"{index}/{medClasses.Count}");
                    if (pic_base64.StringIsEmpty() == false)
                    {
                        tasks.Add(Task.Run(new Action(delegate
                        {
                            medPicClass medPicClass = new medPicClass();
                            medPicClass.藥碼 = 藥碼;
                            medPicClass.藥名 = 藥名;

                            medPicClass.副檔名 = "jpg";
                            medPicClass.pic_base64 = pic_base64;
                            string losg_temp = $"({藥碼}){藥名}".StringLength(100) + $"取得圖片Base64成功\n";
                            Console.WriteLine(losg_temp);
                            log += losg_temp;
                            medPicClass.add(API_Server, medPicClass);
                            medPicClasses.LockAdd(medPicClass);
                        })));
                    }
                    else
                    {
                        string losg_temp = $"({medClass.藥品碼}){medClass.藥品名稱}".StringLength(100) + $"取得圖片Base64【失敗】\n";
                        Console.WriteLine(losg_temp);
                        log += losg_temp;
                    }
                }
                Task.WhenAll(tasks).Wait();
                Logger.Log($"新增<{medPicClasses.Count}>筆藥品圖片至資料庫");
                Logger.Log($"{log}");
                Logger.LogAddLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading image: {ex.Message}");
            }
        }


        public static string DownloadPic(string url)
        {
            string pic1_base64 = "";
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36 Edg/130.0.0.0");
                    byte[] imageBytes = client.DownloadData(url);
                    pic1_base64 = Convert.ToBase64String(imageBytes);
                    return pic1_base64;
                }
 
            }
            catch
            {
                Console.WriteLine($"發生錯誤 {url}");
                return string.Empty;
            }            
        }
    }
}
