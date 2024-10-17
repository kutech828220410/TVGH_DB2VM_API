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
                string log = "";
                int index = 0;
                foreach (medClass medClass in medClasses)
                {
                    
                    medPicClass medPicClass = new medPicClass();
                    medPicClass.藥碼 = medClass.藥品碼;
                    medPicClass.藥名 = medClass.藥品名稱;
                    List<medPicClass> medPicClasses_buf = new List<medPicClass>();
                    bool flag_pic0_OK = false;
                    bool flag_pic1_OK = false;
                    if (medClass.圖片網址.StringIsEmpty() == false)
                    {
                        string pic_base64 = DownloadPic(medClass.圖片網址);

                        if (pic_base64.StringIsEmpty() == false)
                        {
                            medPicClass.副檔名 = "jpg";
                            medPicClass.pic_base64 = pic_base64;
                            string losg_temp = $"({medClass.藥品碼}){medClass.藥品名稱}".StringLength(150) + $"取得圖片(0)Base64成功\n";
                            log += losg_temp;
                            flag_pic0_OK = true;
                        }
                        else
                        {
                            string losg_temp = $"({medClass.藥品碼}){medClass.藥品名稱}".StringLength(150) + $"取得圖片(0)Base64【失敗】\n";
                            log += losg_temp;
                        }
                    }
                    if (medClass.圖片網址1.StringIsEmpty() == false)
                    {
                        string pic1_base64 = DownloadPic(medClass.圖片網址1);                          

                        if (pic1_base64.StringIsEmpty() == false)
                        {
                            medPicClass.副檔名1 = "jpg";
                            medPicClass.pic1_base64 = pic1_base64;
                            string losg_temp = $"({medClass.藥品碼}){medClass.藥品名稱}".StringLength(150) + $"取得圖片(1)Base64成功\n";
                            log += losg_temp;
                            flag_pic1_OK = true;
                        }
                        else
                        {
                            string losg_temp = $"({medClass.藥品碼}){medClass.藥品名稱}".StringLength(150) + $"取得圖片(1)Base64【失敗】\n";
                            log += losg_temp;
                        }
                    }
                    index++;
                    Console.WriteLine($"{index}/{medClasses.Count}");

                    if (flag_pic0_OK || flag_pic1_OK) medPicClasses.Add(medPicClass);
                    
                }

               
                medPicClass.add(API_Server, medPicClasses);
                
                Logger.Log($"新增<{medPicClasses.Count}>筆藥品圖片至資料庫");

                Logger.LogAddLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading image: {ex.Message}");
            }
        }  
        public static string DownloadPic(string url)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
                    byte[] imageBytes = client.DownloadData(url);
                    string pic1_base64 = Convert.ToBase64String(imageBytes);
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
