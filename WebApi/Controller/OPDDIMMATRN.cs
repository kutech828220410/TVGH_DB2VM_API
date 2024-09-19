using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using System.Data;
using System.Configuration;
using Basic;
using HIS_DB_Lib;

namespace DB2VM_API
{
    [Route("api/[controller]")]
    [ApiController]
    public class OPDDIMMATRN : ControllerBase
    {
        [Route("get_by_datetime")]
        [HttpPost]
        public string Get_get_by_datetime(returnData returnData)
        {

            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "get_by_datetime";
            //String MyDb2ConnectionString = $"server=10.30.253.249:51031;database=DBHIS;userid=XVGHF3 ;password=QWER1234;";
            String MyDb2ConnectionString = $"server=10.30.253.248:51011;database=DBHIS;userid=XVGHF3 ;password=QWER1234;";
            DB2Connection MyDb2Connection = new DB2Connection(MyDb2ConnectionString);
            List<batch_inventory_importClass> batch_Inventory_ImportClasses = new List<batch_inventory_importClass>();
            try
            {
                if (returnData.ValueAry.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 需為[產出日期]";
                    return returnData.JsonSerializationt();
                }
                if (returnData.ValueAry[0].Check_Date_String() == false)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 需為[產出日期]";
                    return returnData.JsonSerializationt();
                }

                DateTime date = returnData.ValueAry[0].StringToDateTime();

                MyDb2Connection.Open();
                DB2Command cmd = MyDb2Connection.CreateCommand();
                //cmd.CommandText = $"SELECT * FROM VGHLNXVG.DIMMATRN where DIMDATE >='{date.ToDateTinyString()}' with ur;";
                //cmd.CommandText = $"SELECT * FROM VGHLNXVG.DIMAUTRN where DIMDATE >='{date.ToDateTinyString()}' with ur;";
                //cmd.CommandText = $"SELECT * FROM VGHLNXTS.UD_DIMAUTRN where DIMDATE >='{date.ToDateTinyString()}' with ur;";
                cmd.CommandText = $"SELECT * FROM VGHLNXTS.DIMAUTRNUDNO where DIMDATE >='{date.ToDateTinyString()}' with ur;";



                using (DB2DataReader reader = cmd.ExecuteReader())
                {
                    // 取得欄位數量
                    int fieldCount = reader.FieldCount;

                    // 用於存放欄位名稱的列表
                    List<string> columnNames = new List<string>();

                    // 迭代取得欄位名稱
                    for (int i = 0; i < fieldCount; i++)
                    {
                        columnNames.Add(reader.GetName(i)); // 取得欄位名稱
                    }

                    // 列印欄位名稱
                    foreach (var name in columnNames)
                    {
                        Console.WriteLine(name);
                    }
                    List<medClass> medClasses_cloud = medClass.get_med_cloud("http://127.0.0.1:4433");
                    List<medClass> medClasses_cloud_buf = new List<medClass>();
                 
                    while (reader.Read())
                    {
                        string DIMSTNO = reader["DIMSTNO"].ToString().Trim();
                        string DIMNAME = reader["DIMNAME"].ToString().Trim();
                        string DIMDATE = reader["DIMDATE"].ToString().Trim();
                        string DIMTIME = reader["DIMTIME"].ToString().Trim();
                        string DIMPLACE = reader["DIMPLACE"].ToString().Trim();
                        string DIMFLOC = reader["DIMFLOC"].ToString().Trim();
                        string DIMTLOC = reader["DIMTLOC"].ToString().Trim();
                        string DIMUNIT = reader["DIMUNIT"].ToString().Trim();
                        string DIMQUTY = reader["DIMQUTY"].ToString().Trim();
                        string DIMFLAG = reader["DIMFLAG"].ToString().Trim();
                        string DIMRQUTY = reader["DIMRQUTY"].ToString().Trim();
                        if (DIMNAME.StartsWith("Foliromin"))
                        {

                        }

                        medClasses_cloud_buf = (from temp in medClasses_cloud
                                                where temp.料號 == DIMSTNO
                                                select temp).ToList();
                        string 建表時間 = "";
                        if (DIMDATE.Length == 8 && DIMTIME.Length == 6)
                        {
                            建表時間 = $"{DIMDATE.Substring(0, 4)}-{DIMDATE.Substring(4, 2)}-{DIMDATE.Substring(6, 2)} {DIMTIME.Substring(0, 2)}:{DIMTIME.Substring(2, 2)}:{DIMTIME.Substring(4, 2)}";
                        }
                        int 數量 = DIMQUTY.StringToInt32();
                        if (數量 < 0) continue;
                        if (medClasses_cloud_buf.Count > 0)
                        {
                            batch_inventory_importClass batch_Inventory_ImportClass = new batch_inventory_importClass();
                            batch_Inventory_ImportClass.藥碼 = medClasses_cloud_buf[0].藥品碼;
                            batch_Inventory_ImportClass.藥名 = medClasses_cloud_buf[0].藥品名稱;
                            batch_Inventory_ImportClass.單位 = medClasses_cloud_buf[0].包裝單位;
                            batch_Inventory_ImportClass.建表人員 = "系統";
                            batch_Inventory_ImportClass.建表時間 = 建表時間;
                            batch_Inventory_ImportClass.效期 = DateTime.Now.ToDateString();
                            batch_Inventory_ImportClass.批號 = "無";
                            batch_Inventory_ImportClass.數量 = 數量.ToString();
                            batch_Inventory_ImportClass.庫別 = returnData.Value;
                            batch_Inventory_ImportClass.備註 = $"HIS新增時間[{建表時間}]";
                            batch_Inventory_ImportClasses.Add(batch_Inventory_ImportClass);
                        }

                    }
                }


            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"取得資料共<{batch_Inventory_ImportClasses.Count}>筆,DB2 Connecting failed! , {MyDb2ConnectionString}\n Exception : {ex.Message}";
                return returnData.JsonSerializationt(true);
            }
            MyDb2Connection.Close();
            MyDb2Connection.Dispose();

            returnData.Code = 200;
            returnData.Data = batch_Inventory_ImportClasses;
            returnData.Result = $"取得資料共<{batch_Inventory_ImportClasses.Count}>筆,DB2 Connecting sucess! , {MyDb2ConnectionString}";
            return returnData.JsonSerializationt(true);



        }
    }
}
