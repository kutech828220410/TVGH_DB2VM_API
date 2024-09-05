using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Basic;
using SQLUI;
using HIS_DB_Lib;
using IBM.Data.DB2.Core;
using System.Data;
using System.Text;
using System.Configuration;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DB2VM_API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class DB2 : ControllerBase

    {
        [HttpGet("UDPDPDRG")]
        public string UDPDPDRG()
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            List<medInfoClass> medInfoClasses = new List<medInfoClass>();
            medInfoClass medInfoClass = new medInfoClass
            {
                藥碼 = "04566"
            };
            medInfoClasses.Add(medInfoClass);
            List<medInfoClass> result = ExecuteUDPDPDRG(medInfoClasses);
            returnData returnData = new returnData();
            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = result;
            //returnData.Result = $"取得病床處方共{medCarInfoClasses.Count}筆";
            return returnData.JsonSerializationt(true);
        }
        static string DB2_schema = $"{ConfigurationManager.AppSettings["DB2_schema"]}";

        private DB2Connection GetDB2Connection()
        {
            string DB2_server = $"{ConfigurationManager.AppSettings["DB2_server"]}:{ConfigurationManager.AppSettings["DB2_port"]}";
            string DB2_database = $"{ConfigurationManager.AppSettings["DB2_database"]}";
            string DB2_userid = $"{ConfigurationManager.AppSettings["DB2_user"]}";
            string DB2_password = $"{ConfigurationManager.AppSettings["DB2_password"]}";
            string MyDb2ConnectionString = $"server={DB2_server};database={DB2_database};userid={DB2_userid};password={DB2_password};";
            return new DB2Connection(MyDb2ConnectionString);
        }

        private List<medInfoClass> ExecuteUDPDPDRG(List<medInfoClass> medInfoClasses)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string SP = "UDPDPDRG";
                string procName = $"{DB2_schema}.{SP}";
                foreach (var medInfoClass in medInfoClasses)
                {
                    using (DB2Command cmd = MyDb2Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = procName;
                        cmd.Parameters.Add("@UDDRGNO", DB2Type.VarChar, 5).Value = medInfoClass.藥碼;
                        DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                        DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);
                        Dictionary<string, object> resultDict = new Dictionary<string, object>();

                        using (DB2DataReader reader = cmd.ExecuteReader())
                        {

                            medInfoClass.售價 = reader["UDWCOST"].ToString().Trim();
                            medInfoClass.健保價 = reader["UDPRICE"].ToString().Trim();
                            medInfoClass.頻次代碼 = reader["UDFREQN"].ToString().Trim();
                            medInfoClass.劑量 = reader["UDCMDOSA"].ToString().Trim();

                        }
                    }
                }
                return medInfoClasses;
            }
        }
    }
}
