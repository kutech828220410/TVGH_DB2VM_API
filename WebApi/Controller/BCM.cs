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

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DB2VM_API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class BCM : ControllerBase
    {
        static private string API_Server = "http://127.0.0.1:4433/api/serversetting";
        static string DB2_schema = $"{ConfigurationManager.AppSettings["DB2_schema"]}";
        [HttpPost("get_atc")]
        public string get_atc([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            List<ServerSettingClass> serverSettingClasses = ServerSettingClassMethod.WebApiGet($"{API_Server}");
            serverSettingClasses = serverSettingClasses.MyFind("Main", "網頁", "API01");
            string API = serverSettingClasses[0].Server;
            List<medInterClass> medInterClasses = ExecuteUDSDBBCM();
            medInterClass.update_med_inter(API, medInterClasses);
            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = medInterClasses;
            returnData.Result = $"取得ATC";
            return returnData.JsonSerializationt(true);

        }
        private DB2Connection GetDB2Connection()
        {
            string DB2_server = $"{ConfigurationManager.AppSettings["DB2_server"]}:{ConfigurationManager.AppSettings["DB2_port"]}";
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
    }
}

