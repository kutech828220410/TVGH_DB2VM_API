using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using Basic;
using SQLUI;
using HIS_DB_Lib;
using IBM.Data.DB2.Core;
using System.Data;
using System.Collections.Concurrent;

namespace DB2VM_API
{
    [Route("api/[controller]")]
    [ApiController]
    public class BBCM : ControllerBase
    {
        static string DB2_schema = $"{ConfigurationManager.AppSettings["DB2_schema"]}";
        static private string API01 = "http://127.0.0.1:4433";

        [HttpGet]
        public string Get(string? code)
        {
            returnData returnData = new returnData();
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
      
                List<medClass> medClasses = ExecuteUDSDBBCM(code);
                List<Task> tasks = new List<Task>();
                returnData returnData_med_cloud = new returnData();
                returnData returnData_med_price = new returnData();
                tasks.Add(Task.Run(new Action(delegate
                {
                    List<medClass> medClass_1 = ExecuteUDPDPHLP(medClasses);
                    List<medClass> medClass_2 = ExecuteUDPDPDRG(medClass_1);
                    List<medClass> medClass_3 = ExecuteDRUGSPEC(medClass_2);
                    returnData_med_cloud = medClass.add_med_clouds(API01, medClass_3);
                })));
                tasks.Add(Task.Run(new Action(delegate
                {
                    List<medPriceClass> medPriceClasses = ExecuteUDPDPDRGPrice(medClasses);
                    returnData_med_price = medPriceClass.update(API01, medPriceClasses);
                })));
                Task.WhenAll(tasks).Wait();
                if (returnData_med_cloud.Code != 200)
                {
                    return returnData_med_cloud.JsonSerializationt(true);
                }
                if (returnData_med_cloud.Code != 200)
                {
                    return returnData_med_price.JsonSerializationt(true);
                }

                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = "";
                returnData.Result = $"更新藥品資訊成功";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception:{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }


        private DB2Connection GetDB2Connection()
        {
            string DB2_server = $"10.30.253.249:{ConfigurationManager.AppSettings["DB2_port"]}";
            string DB2_database = $"{ConfigurationManager.AppSettings["DB2_database"]}";
            string DB2_userid = $"{ConfigurationManager.AppSettings["DB2_user"]}";
            string DB2_password = $"{ConfigurationManager.AppSettings["DB2_password"]}";
            string MyDb2ConnectionString = $"server={DB2_server};database={DB2_database};userid={DB2_userid};password={DB2_password};";
            return new DB2Connection(MyDb2ConnectionString);
        }
        private List<medClass> ExecuteUDSDBBCM(string code)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string SP = "UDSDBBCM";
                string procName = $"{DB2_schema}.{SP}";
                using (DB2Command cmd = MyDb2Connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = procName;
                    cmd.Parameters.Add("@TSYSD", DB2Type.VarChar, 5).Value = "ADMUD";
                    cmd.Parameters.Add("@TUDDRGNO", DB2Type.VarChar, 5).Value = $"{code}";
                    DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                    DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);
                    using (DB2DataReader reader = cmd.ExecuteReader())
                    {
                        List<medClass> medClasses = new List<medClass>();
                        while (reader.Read())
                        {
                            medClass medClass = new medClass()
                            {
                                ATC = reader["UDATC"].ToString().Trim(),
                                藥品碼 = reader["UDDRGNO"].ToString().Trim(),
                                藥品名稱 = reader["UDARNAME"].ToString().Trim(),
                                料號 = reader["UDSTOKNO"].ToString().Trim(),
                                藥品條碼1 = reader["UDBARCD1"].ToString().Trim()
                            };
                            if (medClass.藥品名稱.ToLower().Contains(" cap ") || medClass.藥品名稱.ToLower().Contains(" tab "))
                            {
                                medClass.圖片網址 = $"https://www7.vghtpe.gov.tw/api/find-image-by-udCode-page?udCode={medClass.藥品碼}&page=2";
                            }
                            else
                            {
                                medClass.圖片網址 = $"https://www7.vghtpe.gov.tw/api/find-image-by-udCode-page?udCode={medClass.藥品碼}&page=1";
                            }
                            string 藥品條碼1 = "";
                            string 藥品條碼2 = "";
                            string 藥品條碼3 = "";
                            string 藥品條碼4 = "";
                            string 藥品條碼5 = "";
                            藥品條碼1 = reader["UDBARCD1"].ToString().Trim();
                            藥品條碼2 = reader["UDBARCD2"].ToString().Trim();
                            藥品條碼3 = reader["UDBARCD3"].ToString().Trim();
                            藥品條碼4 = reader["UDBARCD4"].ToString().Trim();
                            藥品條碼5 = reader["UDBARCD5"].ToString().Trim();
                            if (藥品條碼1.StringIsEmpty() == false) medClass.Add_BarCode(藥品條碼1);
                            if (藥品條碼2.StringIsEmpty() == false) medClass.Add_BarCode(藥品條碼2);
                            if (藥品條碼3.StringIsEmpty() == false) medClass.Add_BarCode(藥品條碼3);
                            if (藥品條碼4.StringIsEmpty() == false) medClass.Add_BarCode(藥品條碼4);
                            if (藥品條碼5.StringIsEmpty() == false) medClass.Add_BarCode(藥品條碼5);
                            medClasses.Add(medClass);
                        }
                        return medClasses;
                    }
                }
            }
        }
        private List<medClass> ExecuteUDPDPHLP(List<medClass> medClasses)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string SP = "UDPDPHLP";
                string procName = $"{DB2_schema}.{SP}";
                List<medInfoClass> medInfoClasses = new List<medInfoClass>();
                foreach (var medclass in medClasses)
                {
                    using (DB2Command cmd = MyDb2Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = procName;
                        cmd.Parameters.Add("@UDDRGNO", DB2Type.VarChar, 5).Value = medclass.藥品碼;
                        DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                        DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);
                        using (DB2DataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string update_time = reader["DHUPDT"].ToString().Trim();
                                DateTime dateTime = DateTime.Parse(update_time);
                                medclass.藥理分類序號 = reader["DHSEQNO"].ToString().Trim(); //序號
                                medclass.藥理分類代碼 = reader["DHCHARNO"].ToString().Trim(); //分類號
                                medclass.藥理分類名 = reader["DHCHARNM"].ToString().Trim(); //類別名
                                //medclass.藥品名稱 = reader["DHGNAME"].ToString().Trim(); //藥品通名
                                medclass.藥品學名 = reader["DHTNAME"].ToString().Trim(); //藥品商品名
                                medclass.類別 = reader["DHRXCLAS"].ToString().Trim(); //藥品分類
                                medclass.治療分類名 = reader["DHTXCLAS"].ToString().Trim(); //藥品治療分類
                                medclass.適應症 = reader["DHINDICA"].ToString().Trim(); //適應症
                                medclass.使用說明 = reader["DHADMIN"].ToString().Trim(); //用法劑量
                                medclass.備註 = reader["DHNOTE"].ToString().Trim(); //備註
                                //藥碼 = medClasses[i].藥品碼;
                                medclass.仿單網址 = $"https://www7.vghtpe.gov.tw/api/find-package-insert-by-udCode?udCode={medclass.藥品碼}";
                                //更新時間 = dateTime.ToDateTimeString();

                                //medInfoClasses.Add(medInfoClass);
                            }
                        }
                    };
                }
                return medClasses;
            }
        }
        private List<medClass> ExecuteUDPDPDRG(List<medClass> medClasses)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string SP = "UDPDPDRG";
                string procName = $"{DB2_schema}.{SP}";
                foreach (var medclass in medClasses)
                {
                    using (DB2Command cmd = MyDb2Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = procName;
                        cmd.Parameters.Add("@TDRGNO", DB2Type.VarChar, 5).Value = medclass.藥品碼;
                        DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                        DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);
                        using (DB2DataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medclass.藥品名稱 = reader["UDARNAME"].ToString().Trim();
                                //medInfoClass.售價 = reader["UDWCOST"].ToString().Trim();
                                //medInfoClass.健保價 = reader["UDPRICE"].ToString().Trim();
                                medclass.建議頻次 = reader["UDFREQN"].ToString().Trim();
                                medclass.建議劑量 = reader["UDCMDOSA"].ToString().Trim();

                            }
                        }
                    }
                }

                return medClasses;
            }
        }
        private List<medClass> ExecuteDRUGSPEC(List<medClass> medClasses)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string SP = "VGHLNXVG.DRUGSPEC";
                string procName = $"{SP}";
                string today = DateTime.Today.ToDateString();
                foreach (var medclass in medClasses)
                {
                    string SPEC = "";
                    using (DB2Command cmd = MyDb2Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = procName;
                        cmd.Parameters.Add("@DRUGNO", DB2Type.VarChar, 5).Value = medclass.藥品碼;
                        cmd.Parameters.Add("@SDATE", DB2Type.Date).Value = today;
                        DB2Parameter ARNAME = cmd.Parameters.Add("@ARNAME", DB2Type.VarChar, 60);
                        DB2Parameter RDATE = cmd.Parameters.Add("@RDATE", DB2Type.DateTime);
                        DB2Parameter SQLERRCD = cmd.Parameters.Add("@SQLERRCD", DB2Type.Integer);
                        DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);
                        using (DB2DataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                SPEC += $"{reader["SPRSN"].ToString().Trim()}\n";
                            }
                        }
                    }
                    medclass.健保規範 = SPEC;
                }
                return medClasses;
            }
        }

        private List<medPriceClass> ExecuteUDPDPDRGPrice(List<medClass> medClasses)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string SP = "UDPDPDRG";
                string procName = $"{DB2_schema}.{SP}";
                List<medPriceClass> medPriceClasses = new List<medPriceClass>();
                foreach (var medclass in medClasses)
                {
                    using (DB2Command cmd = MyDb2Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = procName;
                        cmd.Parameters.Add("@TDRGNO", DB2Type.VarChar, 5).Value = medclass.藥品碼;
                        DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                        DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);
                        using (DB2DataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medPriceClass medPriceClass = new medPriceClass()
                                {
                                    藥品碼 = medclass.藥品碼,
                                    售價 = reader["UDWCOST"].ToString().Trim(),
                                    健保價 = reader["UDPRICE"].ToString().Trim(),
                                    ATC = medclass.ATC
                                };
                                medPriceClasses.Add(medPriceClass);
                            }
                        }
                    }
                }
                return medPriceClasses;
            }
        }


    }
}
