using System;
using HIS_DB_Lib;
using SQLUI;
using Basic;
using System.Collections.Generic;
using System.Linq;
namespace ConsoleApp_撥補帳自動滾單
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                returnData returnData = new returnData();
                returnData.Value = "門診高管櫃";
                returnData.ValueAry.Add($"{DateTime.Now.ToDateString()}");
                string json_in = returnData.JsonSerializationt();
                Logger.LogAddLine();
                Logger.Log($"[{returnData.Value}-{returnData.ValueAry[0]}]開始取得撥補資料");
                string json_out = Basic.Net.WEBApiPostJson("http://127.0.0.1:444/api/OPDDIMMATRN/get_by_datetime", json_in);
                returnData returnData_out = json_out.JsonDeserializet<returnData>();
                List<batch_inventory_importClass> batch_inventory_importClasses = returnData_out.Data.ObjToClass<List<batch_inventory_importClass>>();
                Logger.Log($"[{returnData.Value}-{returnData.ValueAry[0]}]取得HIS撥補資料,共<{batch_inventory_importClasses.Count}>筆");
                List<batch_inventory_importClass> batch_inventory_importClasse_buf = new List<batch_inventory_importClass>();
                List<batch_inventory_importClass> batch_inventory_importClasse_add = new List<batch_inventory_importClass>();
                List<batch_inventory_importClass> batch_inventory_importClasses_today = batch_inventory_importClass.get_by_CT_TIME("http://127.0.0.1:4433", DateTime.Now.GetStartDate(), DateTime.Now.GetEndDate());
                Logger.Log($"[{returnData.Value}-{returnData.ValueAry[0]}]取得Server撥補資料,共<{batch_inventory_importClasses_today.Count}>筆");


                for (int i = 0; i < batch_inventory_importClasses.Count; i++)
                {
                    string 藥碼 = batch_inventory_importClasses[i].藥碼;
                    string 數量 = batch_inventory_importClasses[i].數量;
                    string 備註 = batch_inventory_importClasses[i].備註;

                    batch_inventory_importClasse_buf = (from temp in batch_inventory_importClasses_today
                                                        where temp.藥碼 == 藥碼
                                                        where temp.數量.StringToInt32() == 數量.StringToInt32()
                                                        where temp.備註.StringToDateTime() == 備註.StringToDateTime()
                                                        select temp).ToList();
                    if(batch_inventory_importClasse_buf.Count > 0)
                    {

                    }
                    else
                    {
                        batch_inventory_importClasse_add.Add(batch_inventory_importClasses[i]);
                    }
                }
                Logger.Log($"[{returnData.Value}-{returnData.ValueAry[0]}]共需新增<{batch_inventory_importClasse_add.Count}>筆");

                batch_inventory_importClass.add("http://127.0.0.1:4433", batch_inventory_importClasse_add, "系統");

            }
            catch (Exception ex)
            {
                Logger.Log($"Exception : {ex.Message}");
            }
            finally
            {
                Logger.LogAddLine();
            }
            
        }
    }
}
