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
using SQLUI;
namespace DB2VM
{
    [Route("api/[controller]")]
    [ApiController]
    public class UDSDBBCMController : ControllerBase
    {
        public enum enum_雲端藥檔
        {
            GUID,
            藥品碼,
            中文名稱,
            藥品名稱,
            藥品學名,
            健保碼,
            包裝單位,
            包裝數量,
            最小包裝單位,
            最小包裝數量,
            藥品條碼1,
            藥品條碼2,
            警訊藥品,
            管制級別,
            類別,
        }
        static string MySQL_server = $"{ConfigurationManager.AppSettings["MySQL_server"]}";
        static string MySQL_database = $"{ConfigurationManager.AppSettings["MySQL_database"]}";
        static string MySQL_userid = $"{ConfigurationManager.AppSettings["MySQL_user"]}";
        static string MySQL_password = $"{ConfigurationManager.AppSettings["MySQL_password"]}";
        static string MySQL_port = $"{ConfigurationManager.AppSettings["MySQL_port"]}";

        private SQLControl sQLControl_藥檔資料 = new SQLControl(MySQL_server, MySQL_database, "medicine_page_cloud", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);


        string DB2_server = $"{ConfigurationManager.AppSettings["DB2_server"]}:{ConfigurationManager.AppSettings["DB2_port"]}";
        string DB2_database = $"{ConfigurationManager.AppSettings["DB2_database"]}";
        string DB2_userid = $"{ConfigurationManager.AppSettings["DB2_user"]}";
        string DB2_password = $"{ConfigurationManager.AppSettings["DB2_password"]}";
        string DB2_schema = $"{ConfigurationManager.AppSettings["DB2_schema"]}";
        [HttpGet]
        public string Get(string? Code)
        {
            if (Code.StringIsEmpty()) Code = "";
            String MyDb2ConnectionString = $"server={DB2_server};database={DB2_database};userid={DB2_userid};password={DB2_password};";
            DB2Connection MyDb2Connection = new DB2Connection(MyDb2ConnectionString);

            MyDb2Connection.Open();

            String procName = $"{DB2_schema}.UDSDBBCM";
            DB2Command cmd = new DB2Command();
            cmd.Connection = MyDb2Connection;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = procName;
            //cmd.Transaction = trans;
            cmd.Parameters.Add("@TSYSD", DB2Type.VarChar, 5).Value = "ADMUD";
            cmd.Parameters.Add("@TUDDRGNO", DB2Type.VarChar, 5).Value = $"{Code}";

            DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
            RET.Direction = ParameterDirection.Output;

            DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 200);
            RETMSG.Direction = ParameterDirection.Output;
            var reader = cmd.ExecuteReader();
            List<MedClass> medClasses = new List<MedClass>();
            while (reader.Read())
            {
                MedClass medClass = new MedClass();
                medClass.藥品碼 = reader["UDDRGNO"].ToString().Trim();
                medClass.藥品名稱 = reader["UDARNAME"].ToString().Trim();
                medClass.料號 = reader["UDSTOKNO"].ToString().Trim();
                medClass.ATC主碼 = reader["UDATC"].ToString().Trim();
                medClass.藥品條碼1 = reader["UDBARCD1"].ToString().Trim();
                medClass.藥品條碼2 = reader["UDBARCD2"].ToString().Trim();
          
                medClasses.Add(medClass);
            }

            MyDb2Connection.Close();


            List<object[]> list_藥檔資料 = sQLControl_藥檔資料.GetAllRows(null);
            List<object[]> list_藥檔資料_buf = new List<object[]>();
            List<object[]> list_藥檔資料_add = new List<object[]>();
            List<object[]> list_藥檔資料_replace = new List<object[]>();
            for (int i = 0; i < medClasses.Count; i++)
            {
                list_藥檔資料_buf = list_藥檔資料.GetRows((int)enum_雲端藥檔.藥品碼, medClasses[i].藥品碼);
                if (list_藥檔資料_buf.Count == 0)
                {
                    object[] value = new object[new enum_雲端藥檔().GetLength()];
                    value[(int)enum_雲端藥檔.GUID] = Guid.NewGuid().ToString();
                    value[(int)enum_雲端藥檔.藥品碼] = medClasses[i].藥品碼;
                    value[(int)enum_雲端藥檔.藥品名稱] = medClasses[i].藥品名稱;
                    value[(int)enum_雲端藥檔.藥品學名] = medClasses[i].藥品學名;
                    value[(int)enum_雲端藥檔.警訊藥品] = medClasses[i].警訊藥品;
                    value[(int)enum_雲端藥檔.管制級別] = medClasses[i].管制級別;
                    list_藥檔資料_add.Add(value);
                }
                else
                {
                    object[] value = list_藥檔資料[0];
                    value[(int)enum_雲端藥檔.藥品碼] = medClasses[i].藥品碼;
                    value[(int)enum_雲端藥檔.藥品名稱] = medClasses[i].藥品名稱;
                    value[(int)enum_雲端藥檔.藥品學名] = medClasses[i].藥品學名;
                    value[(int)enum_雲端藥檔.警訊藥品] = medClasses[i].警訊藥品;
                    value[(int)enum_雲端藥檔.管制級別] = medClasses[i].管制級別;
                    List<object[]> list = new List<object[]>();
                    list.Add(value);
                    list_藥檔資料_replace.Add(value);
                }
            }
            if (list_藥檔資料_add.Count > 0) sQLControl_藥檔資料.AddRows(null, list_藥檔資料_add);
            if(list_藥檔資料_replace.Count > 0) sQLControl_藥檔資料.UpdateByDefulteExtra(null, list_藥檔資料_replace);

            if (medClasses.Count == 0) return "[]";
            string jsonString = medClasses.JsonSerializationt();
            return jsonString;
        }
    }
}
