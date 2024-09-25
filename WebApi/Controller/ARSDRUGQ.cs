using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using Basic;
using HIS_DB_Lib;
using System.Data;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DB2VM_API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class ARSDRUGQ : ControllerBase
    {
        [HttpGet]
        public string GET(string? BarCode)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData returnData = new returnData();
            try
            {
                if (BarCode.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "Barcode空白";
                    return returnData.JsonSerializationt(true);
                }
                List<OrderClass> orderClasses = ExecuteARSDRUGQ(BarCode);
                returnData.Code = 200;
                returnData.Data = orderClasses;
                //returnData.Result = $"取得醫令成功,共<{orderClasses.Count}>筆,新增<{list_醫囑資料_add.Count}>筆";
                returnData.TimeTaken = myTimerBasic.ToString();
                string jsonString = returnData.JsonSerializationt(true);
                return jsonString;
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception:{e.Message}";
                return returnData.JsonSerializationt(true);
            }
            

        }
        private DB2Connection GetDB2Connection()
        {           
            string MyDb2ConnectionString = $"server=10.30.253.249:51031;database=DBHIS;userid=XVGHF3 ;password=QWER1234;;";
            return new DB2Connection(MyDb2ConnectionString);
        }
        private List<OrderClass> ExecuteARSDRUGQ(string BarCode)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string procName = "VGHLNXVG.ARSDRUGQ";
                List<OrderClass> orderClasses = new List<OrderClass>();
                using (DB2Command cmd = MyDb2Connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = procName;
                    cmd.Parameters.Add("@BARCODE", DB2Type.VarChar, 12).Value = BarCode;
                    DB2Parameter RET = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar);
                    DB2Parameter SQLERRCD = cmd.Parameters.Add("@SQLERRCD", DB2Type.Integer);

                    using (DB2DataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            OrderClass OrderClass = new OrderClass
                            {
                                藥局代碼 = "OPD",
                                藥品碼 = reader["UDDRGNO"].ToString().Trim(),
                                領藥號 = reader["ARNDSPCN"].ToString().Trim(),
                                批序 = reader["ARSBDNO"].ToString().Trim(),
                                藥袋條碼 = BarCode,
                                藥品名稱 = reader["ARNHNAMB"].ToString().Trim(),
                                病歷號 = reader["ARNHIST"].ToString().Trim(),
                                //劑量單位 = reader[""].ToString().Trim(),
                                單次劑量 = reader["ARNHDOSE"].ToString().Trim(),
                                頻次 = reader["ARNHFQCY"].ToString().Trim(),
                                //途徑 = reader[""].ToString().Trim(),
                                交易量 = reader["ARNHDQTY"].ToString().Trim(),
                                PRI_KEY = BarCode,
                                狀態 = "ARNHDSTA",
                                開方日期 = reader["ARSBDATE"].ToString().Trim()
                            };
                            orderClasses.Add(OrderClass);
                        }
                    }
                }
                return orderClasses;
            }
        }
    }
}
