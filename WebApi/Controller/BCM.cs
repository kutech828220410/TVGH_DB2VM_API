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


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DB2VM_API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class BCM : ControllerBase
    {
        static private string API_Server = "http://127.0.0.1:4433/api/serversetting";
        static string DB2_schema = $"{ConfigurationManager.AppSettings["DB2_schema"]}";
        [HttpGet]
        public string get_pic()
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData returnData = new returnData();
            try
            {
                string API = "http://127.0.0.1:4433";
                List<medClass> medClasses = medClass.get_med_cloud(API);
                ConcurrentBag<medClass> localList = new ConcurrentBag<medClass>();
                List<medClass> medclass_replace = new List<medClass>();
                Parallel.ForEach(medClasses, new ParallelOptions { MaxDegreeOfParallelism = 4 }, medclass =>
                {
                    medclass.圖片網址 = $"https://www7.vghtpe.gov.tw/api/find-zero-image-by-udCode?udCode={medclass.藥品碼}";
                    localList.Add(medclass);
                });
                lock (medclass_replace)
                {
                    medclass_replace.AddRange(localList);
                }
                //medCarInfoClass.update_med_page_cloud(API, medClasses);
                returnData.Code = 200;
                returnData.Result = $"取得藥品資料共{medClasses.Count}筆";
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = medClasses;
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }
        [HttpPost("get_atc")]
        public string get_atc([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            List<sys_serverSettingClass> serverSettingClasses = sys_serverSettingClassMethod.WebApiGet($"{API_Server}");
            sys_serverSettingClass serverSettingClass = serverSettingClasses.MyFind("Main", "網頁", "API01").FirstOrDefault();           
            string API = serverSettingClasses[0].Server;
            List<medInterClass> medInterClasses = ExecuteUDSDBBCM();
            medInterClass.update_med_inter(API, medInterClasses);
            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = medInterClasses;
            returnData.Result = $"取得ATC";
            return returnData.JsonSerializationt(true);

        }
        [HttpPost("UDSDBBCM")]
        public string get_UDSDBBCM([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            List<Dictionary<string, object>> result = UDSDBBCM();
            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = result;
            returnData.Result = $"";
            return returnData.JsonSerializationt(true);
        }
        [HttpGet("BBCM")]
        public string BBCM(string ?code)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            List<Dictionary<string, object>> result = UDSDBBCM(code);
            returnData returnData = new returnData();
            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = result;
            returnData.Result = $"";
            return returnData.JsonSerializationt(true);
        }
        [HttpPost("DRUGSPEC")]
        public string get_DRUGSPEC([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            List<Dictionary<string, object>> result = DRUGSPEC();
            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = result;
            returnData.Result = $"";
            return returnData.JsonSerializationt(true);
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
        private List<medInterClass> ExecuteUDSDBBCM()
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
                    cmd.Parameters.Add("@TUDDRGNO", DB2Type.VarChar, 5).Value = $"";
                    DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                    DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);
                    using (DB2DataReader reader = cmd.ExecuteReader())
                    {
                        List<medInterClass> medInterClasses = new List<medInterClass>();
                        while (reader.Read())
                        {
                            medInterClass medInterClass = new medInterClass
                            {
                                ATC = reader["UDATC"].ToString().Trim(),
                            };
                            medInterClasses.Add(medInterClass);
                        }
                        return medInterClasses;
                    }
                }
            }
        }
        private List<Dictionary<string, object>> UDSDBBCM()
        {
            string code = "";
            return UDSDBBCM(code);
        }
        private List<Dictionary<string, object>> UDSDBBCM(string code)
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
                        List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                        while (reader.Read())
                        {
                            Dictionary<string, object> row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                row.Add(columnName, value);
                            }
                            result.Add(row);
                        }
                        return result;
                    }
                }
            }

        }
        private List<Dictionary<string, object>> DRUGSPEC()
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string SP = "VGHLNXVG.DRUGSPEC";
                string procName = $"{SP}";
                using (DB2Command cmd = MyDb2Connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = procName;
                    cmd.Parameters.Add("@DRUGNO", DB2Type.VarChar, 5).Value = "06120";
                    cmd.Parameters.Add("@SDATE", DB2Type.Date).Value = "2024-10-11".ToDateString();
                    DB2Parameter ARNAME = cmd.Parameters.Add("@ARNAME", DB2Type.VarChar, 60);
                    DB2Parameter RDATE = cmd.Parameters.Add("@RDATE", DB2Type.DateTime);
                    DB2Parameter SQLERRCD = cmd.Parameters.Add("@SQLERRCD", DB2Type.Integer);
                    DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);


                    using (DB2DataReader reader = cmd.ExecuteReader())
                    {
                        List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                        while (reader.Read())
                        {
                            Dictionary<string, object> row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                row.Add(columnName, value);
                            }
                            result.Add(row);
                        }
                        return result;
                    }
                }
            }

        }
    }
}

