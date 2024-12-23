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
namespace DB2VM
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestOut : ControllerBase
    {
        static string DB2_server = $"10.30.253.249:{ConfigurationManager.AppSettings["DB2_port"]}";
        string DB2_database = $"{ConfigurationManager.AppSettings["DB2_database"]}";
        string DB2_userid = $"{ConfigurationManager.AppSettings["DB2_user"]}";
        string DB2_password = $"{ConfigurationManager.AppSettings["DB2_password"]}";
        string DB2_schema = $"{ConfigurationManager.AppSettings["DB2_schema"]}";
        // GET api/values
        [HttpGet]
        public string Get()
        {

            string MyDb2ConnectionString_XVGHF3 = $"server=10.30.253.249:51031;database=DBHIS;userid=XVGHF3 ;password=QWER1234;";
            DB2Connection MyDb2Connection = new DB2Connection(MyDb2ConnectionString_XVGHF3);
            try
            {
                MyDb2Connection.Open();
            }
            catch
            {
                return $"DB2 Connecting failed! , {MyDb2ConnectionString_XVGHF3}";
            }
            MyDb2Connection.Close();
            MyDb2Connection.Dispose();

            return $"DB2 Connecting sucess! , {MyDb2ConnectionString_XVGHF3} ";


        }
        [Route("opd")]
        [HttpGet]
        public string Get_opd()
        {


            String MyDb2ConnectionString = $"server=10.30.253.249:51031;database={DB2_database};userid=XVGHF3 ;password=QWER1234;";
            DB2Connection MyDb2Connection = new DB2Connection(MyDb2ConnectionString);
            try
            {
                MyDb2Connection.Open();
                DB2Command cmd = MyDb2Connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM VGHLNXVG.DIMMATRN where DIMDATE >='20240904' with ur;";
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
                    List<batch_inventory_importClass> batch_Inventory_ImportClasses = new List<batch_inventory_importClass>();
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

                        medClasses_cloud_buf = (from temp in medClasses_cloud
                                                where temp.料號 == DIMSTNO
                                                select temp).ToList();
                        string 產出時間 = "";
                        if (DIMDATE.Length == 8 && DIMTIME.Length == 6)
                        {
                            產出時間 = $"{DIMDATE.Substring(0, 4)}-{DIMDATE.Substring(4, 2)}-{DIMDATE.Substring(6, 2)} {DIMTIME.Substring(0, 2)}:{DIMTIME.Substring(2, 2)}:{DIMTIME.Substring(4, 2)}";
                        }
                  
                        if (medClasses_cloud_buf.Count > 0)
                        {
                            batch_inventory_importClass batch_Inventory_ImportClass = new batch_inventory_importClass();
                        }

                    }
                }


            }
            catch(Exception ex)
            {
                return $"DB2 Connecting failed! , {MyDb2ConnectionString}\n Exception : {ex.Message}";
            }
            MyDb2Connection.Close();
            MyDb2Connection.Dispose();
            return $"DB2 Connecting sucess! , {MyDb2ConnectionString}";


        }

    }
}
