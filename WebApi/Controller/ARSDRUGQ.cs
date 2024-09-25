using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using Basic;
using HIS_DB_Lib;
using System.Data;
using SQLUI;
using System.Configuration;
using MySql.Data.MySqlClient;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DB2VM_API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class ARSDRUGQ : ControllerBase
    {
        static private MySqlSslMode SSLMode = MySqlSslMode.None;
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

                if (orderClasses.Count == 0)
                {
                    if (BarCode.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "找無藥單資料";
                        return returnData.JsonSerializationt(true);
                    }
                }
                var (Server, DB, UserName, Password, Port) = GetServerInfo();
                Table table = OrderClass.init("http://127.0.0.1:4433");
                SQLControl sQLControl_醫囑資料 = new SQLControl(Server, DB, table.TableName, UserName, Password, Port, SSLMode);
                List<object[]> list_醫囑資料 = sQLControl_醫囑資料.GetRowsByDefult(null, enum_醫囑資料.PRI_KEY.GetEnumName(), BarCode);
                List<object[]> list_醫囑資料_add = new List<object[]>();

                if (list_醫囑資料.Count != orderClasses.Count)
                {
                    for (int i = 0; i < orderClasses.Count; i++)
                    {
                        object[] value = orderClasses[i].ClassToSQL<OrderClass, enum_醫囑資料>();

                        value[(int)enum_醫囑資料.GUID] = Guid.NewGuid().ToString();
                        value[(int)enum_醫囑資料.藥局代碼] = orderClasses[i].藥局代碼;
                        value[(int)enum_醫囑資料.藥品碼] = orderClasses[i].藥品碼;
                        value[(int)enum_醫囑資料.藥品名稱] = orderClasses[i].藥品名稱;
                        value[(int)enum_醫囑資料.病歷號] = orderClasses[i].病歷號;
                        value[(int)enum_醫囑資料.藥袋條碼] = orderClasses[i].藥袋條碼;
                        value[(int)enum_醫囑資料.PRI_KEY] = orderClasses[i].PRI_KEY;
                        value[(int)enum_醫囑資料.交易量] = orderClasses[i].交易量;
                        value[(int)enum_醫囑資料.途徑] = orderClasses[i].途徑;
                        value[(int)enum_醫囑資料.頻次] = orderClasses[i].頻次;
                        value[(int)enum_醫囑資料.單次劑量] = orderClasses[i].單次劑量;
                        value[(int)enum_醫囑資料.產出時間] = DateTime.Now.ToDateTimeString_6();
                        value[(int)enum_醫囑資料.過帳時間] = DateTime.MinValue.ToDateTimeString_6();
                        value[(int)enum_醫囑資料.展藥時間] = DateTime.MinValue.ToDateTimeString_6();
                        value[(int)enum_醫囑資料.開方日期] = orderClasses[i].開方日期;
                        value[(int)enum_醫囑資料.狀態] = "未過帳";
                        list_醫囑資料_add.Add(value);
                    }

                }

                sQLControl_醫囑資料.AddRows(null, list_醫囑資料_add);

                returnData.Code = 200;
                returnData.Data = orderClasses;
                returnData.Result = $"取得醫令成功,共<{orderClasses.Count}>筆,新增<{list_醫囑資料_add.Count}>筆";
                returnData.TimeTaken = myTimerBasic.ToString();
                return returnData.JsonSerializationt(true);
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
                            string 處方狀態 = reader["ARNHDSTA"].ToString().Trim();
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
                                交易量 = (reader["ARNHDQTY"].ToString().Trim().StringToInt32() * -1).ToString(),
                                PRI_KEY = BarCode,
                                狀態 = "已過帳",
                                開方日期 = reader["ARSBDATE"].ToString().Trim()
                            };
                            if(處方狀態 == "30") orderClasses.Add(OrderClass);
                        }
                    }
                }
                return orderClasses;
            }
        }
        private (string Server, string DB, string UserName, string Password, uint Port) GetServerInfo()
        {
            string Server = $"{ConfigurationManager.AppSettings["MySQL_server"]}";
            string DB = $"{ConfigurationManager.AppSettings["MySQL_database"]}";
            string UserName = $"{ConfigurationManager.AppSettings["MySQL_user"]}";
            string Password = $"{ConfigurationManager.AppSettings["MySQL_password"]}";
            uint Port = ($"{ConfigurationManager.AppSettings["MySQL_port"]}").StringToUInt32();
            return (Server, DB, UserName, Password, Port);
        }
        
        
    }
}
