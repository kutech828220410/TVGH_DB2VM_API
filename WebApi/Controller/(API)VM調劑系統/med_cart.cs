﻿using Microsoft.AspNetCore.Mvc;
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
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DB2VM_API.Controller._API_VM調劑系統
{
    [Route("api/[controller]")]
    [ApiController]
    public class med_cart : ControllerBase
    {
        static private string API01 = "http://127.0.0.1:4433";
        static string DB2_schema = $"{ConfigurationManager.AppSettings["DB2_schema"]}";
        /// <summary>
        ///以藥局和護理站取得占床資料
        /// </summary>
        /// <remarks>
        /// 以下為JSON範例
        /// <code>
        ///     {
        ///         "ValueAry":[藥局, 護理站]
        ///     }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns>
        [HttpPost("get_bed_list_by_cart")]
        public string get_bed_list_by_cart([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 2)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[藥局, 護理站]";
                    return returnData.JsonSerializationt(true);
                }

               
                string 藥局 = returnData.ValueAry[0];
                string 護理站 = returnData.ValueAry[1];
                List<medCarInfoClass> bedList = ExecuteUDPDPPF1(藥局, 護理站);
                List<medCarInfoClass> bedListInfo = ExecuteUDPDPPF0(bedList);
                List<medCarInfoClass> out_medCarInfoClass = medCarInfoClass.update_med_carinfo(API01, bedListInfo);
                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = out_medCarInfoClass;
                returnData.Result = $"取得住院{藥局} {護理站} 病床資訊共{bedList.Count}筆";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception:{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        ///以GUID取得病人詳細資料
        /// </summary>
        /// <remarks>
        /// 以下為JSON範例
        /// <code>
        ///     {
        ///         "ValueAry":[GUID]
        ///     }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns>
        [HttpPost("get_patient_by_GUID")]
        public string get_patient_by_GUID([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
                if (returnData.ValueAry == null || returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[\"GUID\"]";
                    return returnData.JsonSerializationt(true);
                }              
                medCarInfoClass targetPatient = medCarInfoClass.get_patient_by_GUID_brief(API01, returnData.ValueAry);
                if (targetPatient == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "無對應的病人資料";
                    return returnData.JsonSerializationt(true);
                }
                string 藥局 = targetPatient.藥局;
                string 護理站 = targetPatient.護理站;
                string 床號 = targetPatient.床號;
                List<medCarInfoClass> medCarInfoClasses = new List<medCarInfoClass> { targetPatient };
                List<medCpoeClass> bedListCpoe = ExecuteUDPDPDSP(medCarInfoClasses);
                if (bedListCpoe.Count == 0) 
                {
                    medCarInfoClasses[0].調劑狀態 = "Y";
                    medCarInfoClass.update_med_carinfo(API01, medCarInfoClasses);
                }
                else
                {
                    medCpoeClass.update_med_cpoe(API01, bedListCpoe);
                }

                medCarInfoClass out_medCarInfoClass = medCarInfoClass.get_patient_by_GUID(API01,returnData.Value, returnData.ValueAry);

                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = out_medCarInfoClass;
                returnData.Result = $"取得{藥局} {護理站} 第{床號}病床資訊";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception:{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        ///以護理站取得藥品總量
        /// </summary>
        /// <remarks>
        /// 以下為JSON範例
        /// <code>
        ///     {
        ///         "Value":"調劑台"
        ///         "ValueAry":[藥局, 護理站]
        ///     }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns>
        [HttpPost("get_med_qty")]
        public string get_med_qty([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
                if (returnData.ValueAry == null || returnData.ValueAry.Count != 2)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[藥局, 護理站]";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.Value == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.Value 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }

                string 藥局 = returnData.ValueAry[0];
                string 護理站 = returnData.ValueAry[1];

                List<medCarInfoClass> bedList = ExecuteUDPDPPF1(藥局, 護理站);
                List<medCarInfoClass> bedListInfo = ExecuteUDPDPPF0(bedList);
                medCarInfoClass.update_med_carinfo(API01, bedListInfo);
                List<medCarInfoClass> out_medCarInfoClass = medCarInfoClass.get_bed_list_by_cart(API01, returnData.ValueAry);
                List<medCpoeClass> bedListCpoe = ExecuteUDPDPDSP(out_medCarInfoClass);
                medCpoeClass.update_med_cpoe(API01, bedListCpoe);

                //List<medCarInfoClass> update = new List<medCarInfoClass>();
                //foreach (var medCarInfoClass in out_medCarInfoClass)
                //{
                //    List<medCpoeClass> medCpoeClasses = bedListCpoe
                //        .Where(temp => temp.Master_GUID == medCarInfoClass.GUID)
                //        .ToList();
                //    if (medCpoeClasses.Count == 0 && medCarInfoClass.占床狀態 == "已佔床")
                //    {
                //        medCarInfoClass.調劑狀態 = "Y";
                //        update.Add(medCarInfoClass);
                //    }
                //}
                //if (update.Count != 0) medCarInfoClass.update_med_carinfo(API01, update);

                List<medQtyClass> get_med_qty = medCpoeClass.get_med_qty(API01, returnData.Value, returnData.ValueAry);
                if (get_med_qty == null)
                {
                    returnData.Code = 200;
                    returnData.Result = $"無藥品處方資料";
                    return returnData.JsonSerializationt(true);
                }
                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = get_med_qty;
                returnData.Result = $"{藥局} {護理站} 的藥品清單";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        ///以病床GUID取得處方異動資料
        /// </summary>
        /// <remarks>
        /// 以下為JSON範例
        /// <code>
        ///     {
        ///         "ValueAry":[GUID]
        ///     }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns>
        [HttpPost("get_medChange_by_GUID")]
        public string get_medChange_by_GUID([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
                if (returnData.ValueAry == null || returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[GUID]";
                    return returnData.JsonSerializationt(true);
                }

                List<medCarInfoClass> bedList = new List<medCarInfoClass> { medCarInfoClass.get_patient_by_GUID_brief(API01, returnData.ValueAry) };
                List<medCpoeRecClass> medCpoe_change = ExecuteUDPDPORD(bedList);
                medCpoeRecClass.update_med_CpoeRec(API01, medCpoe_change);
                List<medCarInfoClass> get_patient = medCpoeRecClass.get_medChange_by_GUID(API01, returnData.ValueAry);

                string 藥局 = bedList[0].藥局;
                string 護理站 = bedList[0].護理站;
                string 床號 = bedList[0].床號;

                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = get_patient;
                returnData.Result = $"取得{藥局} {護理站} 第{床號}床 處方異動資料共{get_patient[0].處方異動.Count}筆";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception:{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        ///以藥局、護理站確認是否可交車
        /// </summary>
        /// <remarks>
        /// 以下為JSON範例
        /// <code>
        ///     {
        ///         "ValueAry":[藥局, 護理站]
        ///     }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns>
        [HttpPost("handover")]
        public string handover([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
                DateTime now = DateTime.Now;
                if (now.TimeOfDay < new TimeSpan(15, 0, 0))
                {
                    returnData.Code = -200;
                    returnData.Result = "執行失敗：目前時間尚未超過下午三點。";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry == null || returnData.ValueAry.Count != 2)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[藥局, 護理站]";
                    return returnData.JsonSerializationt(true);
                }
                string 藥局 = returnData.ValueAry[0];
                string 護理站 = returnData.ValueAry[1];

                List<medCarInfoClass> bedList = ExecuteUDPDPPF1(藥局, 護理站);
                List<medCarInfoClass> bedListInfo = ExecuteUDPDPPF0(bedList);
                List<medCarInfoClass> update_medCarInfoClass = medCarInfoClass.update_med_carinfo(API01, bedListInfo);
                List<medCpoeClass> bedListCpoe = ExecuteUDPDPDSP(update_medCarInfoClass);
                medCpoeClass.update_med_cpoe(API01, bedListCpoe);

                HIS_DB_Lib.returnData returnData_handover = medCpoeClass.handover(API01, returnData.ValueAry);

                returnData_handover.Code = 200;
                returnData_handover.TimeTaken = $"{myTimerBasic}";
                return returnData_handover.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception:{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        [HttpPost("get_all_by_cart")]
        public string get_all_by_cart([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
                if (returnData.ValueAry == null || returnData.ValueAry.Count != 2)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[藥局, 護理站]";
                    return returnData.JsonSerializationt(true);
                }              
                string 藥局 = returnData.ValueAry[0];
                string 護理站 = returnData.ValueAry[1];
                List<medCarInfoClass> bedList = ExecuteUDPDPPF1(藥局, 護理站);
                List<medCarInfoClass> bedListInfo = ExecuteUDPDPPF0(bedList);
                List<medCarInfoClass> update_medCarInfoClass = medCarInfoClass.update_med_carinfo(API01, bedListInfo);
                List<medCpoeClass> bedListCpoe = ExecuteUDPDPDSP(update_medCarInfoClass);
                List<medCpoeClass> update_medCpoeClass = medCpoeClass.update_med_cpoe(API01, bedListCpoe);

                //List<medCarInfoClass> update = new List<medCarInfoClass>();
                //foreach (var medCarInfoClass in update_medCarInfoClass)
                //{
                //    List<medCpoeClass> medCpoeClasses = bedListCpoe
                //        .Where(temp => temp.Master_GUID == medCarInfoClass.GUID)
                //        .ToList();
                //    if (medCpoeClasses.Count == 0 && medCarInfoClass.占床狀態 == "已佔床")
                //    {
                //        medCarInfoClass.調劑狀態 = "Y";
                //        update.Add(medCarInfoClass);
                //    }
                //}
                //if (update.Count != 0) medCarInfoClass.update_med_carinfo(API01, update);


                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = update_medCpoeClass;
                returnData.Result = $"取得 {護理站} 病床資訊共{bedList.Count}筆";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception:{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        [HttpPost("get_medChange_by_cart")]
        public string get_medChange_by_cart([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
                if (returnData.ValueAry == null || returnData.ValueAry.Count != 2)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[藥局, 護理站]";
                    return returnData.JsonSerializationt(true);
                }              
                List<medCarInfoClass> bedList = medCarInfoClass.get_bed_list_by_cart(API01, returnData.ValueAry);
                List<medCpoeRecClass> medCpoe_change = ExecuteUDPDPORD(bedList);
                List<medCpoeRecClass> update_medCpoe_change = medCpoeRecClass.update_med_CpoeRec(API01, medCpoe_change);

                string 藥局 = returnData.ValueAry[0];
                string 護理站 = returnData.ValueAry[1];

                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = update_medCpoe_change;
                returnData.Result = $"取得{藥局} {護理站} 處方異動資料共{update_medCpoe_change.Count}筆";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception:{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        [HttpPost("get_medInfo_by_cart")]
        public string get_medInfo_by_cart([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
               
                string 藥局 = "UC02";

                string[] list = { "C039", "C049", "C059", "C069", "C079", "C089", "C099", "C109", "C119", "C129", "BMT" };
                List<string> nurst_list = list.ToList();
                //List<string> code = new List<string>();
                for (int i = 0; i < nurst_list.Count;i++)
                {
                    List<medCarInfoClass> bedList = ExecuteUDPDPPF1(藥局, nurst_list[i]);
                    List<medCpoeClass> bedListCpoe = ExecuteUDPDPDSP(bedList);
                    List<string> code = bedListCpoe.GroupBy(temp => temp.藥碼).Select(group => group.Key).ToList();
                    List<string> result = code.SelectMany(code => code.Split(",")).ToList();
                    List<medInfoClass> medInfoClass_1 = ExecuteUDPDPHLP(result);
                    List<medInfoClass> medInfoClass_2 = ExecuteUDPDPDRG(medInfoClass_1);
                    medInfoClass.update_med_info(API01, medInfoClass_2);
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
        [HttpPost("get_medInfo")]
        public string get_medInfo([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {              
                List<medClass> medClasses = medClass.get_med_cloud(API01);
                //List<medClass> select = medClasses.Where(temp => temp.藥品碼 == "04514" || temp.藥品碼 == "05412" || temp.藥品碼 == "04974").ToList();
                List<string> code = medClasses.Select(temp => temp.藥品碼).ToList();
                //List<string> result = code.SelectMany(code => code.Split(",")).ToList();
                List<medInfoClass> medInfoClass_1 = ExecuteUDPDPHLP(code);
                List<medInfoClass> medInfoClass_2 = ExecuteUDPDPDRG(medInfoClass_1);
                List<medInfoClass> medInfoClass_3 = ExecuteDRUGSPEC(medInfoClass_2);
                medInfoClass.update_med_info(API01, medInfoClass_3);             
              
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
        [HttpGet("UDPDPPF1")]
        public string UDPDPPF1()
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData returnData = new returnData();
            string 藥局 = "UC02";
            string 護理站 = "C039";
            string Server = "";
            List<medCarInfoClass> medCarInfoClasses = ExecuteUDPDPPF1(藥局, 護理站);
            //List<medCarInfoClass> output_medCarInfoClass = medCarInfoClass.update_bed_list(Server, medCarInfoClasses);
            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = medCarInfoClasses;
            returnData.Result = $"取得 {護理站} 的病床資訊共{medCarInfoClasses.Count}筆";
            return returnData.JsonSerializationt(true);
        }
        [HttpGet("UDPDPPF0")]
        public string UDPDPPF0()
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData returnData = new returnData();
            List<medCarInfoClass> medCarInfoClassList = new List<medCarInfoClass>();
            medCarInfoClass v1 = new medCarInfoClass
            {
                病歷號 = "9394632",
                住院號 = "31620090"
            };
            medCarInfoClassList.Add(v1);
            List<medCarInfoClass> medCarInfoClasses = ExecuteUDPDPPF0(medCarInfoClassList);

            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = medCarInfoClasses;
            returnData.Result = $"取得病人資料";
            return returnData.JsonSerializationt(true);
        }
        [HttpGet("UDPDPDSP")]
        public string UDPDPDSP()
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData returnData = new returnData();
            List<medCarInfoClass> medCarInfoClassList = new List<medCarInfoClass>();
            medCarInfoClass v1 = new medCarInfoClass
            {
                藥局 = "UC02",
                護理站 = "C079",
                住院號 = "31695645"
            };
            medCarInfoClassList.Add(v1);
            List<medCpoeClass> medCarInfoClasses = ExecuteUDPDPDSP(medCarInfoClassList);

            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = medCarInfoClasses;
            returnData.Result = $"取得病床處方共{medCarInfoClasses.Count}筆";
            return returnData.JsonSerializationt(true);
        }
        [HttpGet("UDPDPORD")]
        public string UDPDPORD()
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData returnData = new returnData();
            List<medCarInfoClass> medCarInfoClassList = new List<medCarInfoClass>();
            medCarInfoClass v1 = new medCarInfoClass
            {
                藥局 = "UC02",
                護理站 = "C079",
                住院號 = "31641549"
            };
            medCarInfoClassList.Add(v1);
            List<medCpoeRecClass> medCarInfoClasses = ExecuteUDPDPORD(medCarInfoClassList);
            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = medCarInfoClasses;
            returnData.Result = $"取得病床處方共{medCarInfoClasses.Count}筆";
            return returnData.JsonSerializationt(true);
        }
        [HttpPost("UDPDPORD")]
        public string Post_UDPDPORD([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            List<medCarInfoClass> medCarInfoClassList = returnData.Data.ObjToClass<List<medCarInfoClass>>();
            List<medCpoeRecClass> medCarInfoClasses = ExecuteUDPDPORD(medCarInfoClassList);
            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = medCarInfoClasses;
            returnData.Result = $"取得病床處方共{medCarInfoClasses.Count}筆";
            return returnData.JsonSerializationt(true);
        }
        [HttpGet("UDPDPHLP")]
        public string UDPDPHLP()
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            string code = "04514";
            List<string> codes = new List<string>() { code };
            List<medInfoClass> result = ExecuteUDPDPHLP(codes);
            returnData returnData = new returnData();
            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Data = result;
            //returnData.Result = $"取得病床處方共{medCarInfoClasses.Count}筆";
            return returnData.JsonSerializationt(true);
        }
        [HttpGet("UDPDPDRG")]
        public string UDPDPDRG()
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            List<medInfoClass> medInfoClasses = new List<medInfoClass>();
            medInfoClass medInfoClass = new medInfoClass
            {
                藥碼 = "04514"
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
        private DB2Connection GetDB2Connection()
        {
            string DB2_server = $"10.30.253.249:{ConfigurationManager.AppSettings["DB2_port"]}";
            string DB2_database = $"{ConfigurationManager.AppSettings["DB2_database"]}";
            string DB2_userid = $"{ConfigurationManager.AppSettings["DB2_user"]}";
            string DB2_password = $"{ConfigurationManager.AppSettings["DB2_password"]}";
            string MyDb2ConnectionString = $"server={DB2_server};database={DB2_database};userid={DB2_userid};password={DB2_password};";
            return new DB2Connection(MyDb2ConnectionString);
        }
        private List<medCarInfoClass> ExecuteUDPDPPF1(string phar, string hnursta)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string SP = "UDPDPPF1";
                string procName = $"{DB2_schema}.{SP}";
                using (DB2Command cmd = MyDb2Connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = procName;
                    cmd.Parameters.Add("@TNURSTA", DB2Type.VarChar, 4).Value = hnursta;
                    DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                    DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);
                    using (DB2DataReader reader = cmd.ExecuteReader())
                    {
                        List<medCarInfoClass> medCarInfoClasses = new List<medCarInfoClass>();
                        while (reader.Read())
                        {


                            medCarInfoClass medCarInfoClass = new medCarInfoClass
                            {
                                藥局 = phar,
                                更新時間 = DateTime.Now.ToDateTimeString(),
                                護理站 = reader["HNURSTA"].ToString().Trim(),
                                床號 = reader["HBEDNO"].ToString().Trim(),
                                病歷號 = reader["HISTNUM"].ToString().Trim(),
                                住院號 = reader["PCASENO"].ToString().Trim(),
                                姓名 = ReplaceInvalidCharacters(reader["PNAMEC"].ToString()).Trim()
                            };
                            if (!string.IsNullOrWhiteSpace(medCarInfoClass.姓名)) medCarInfoClass.占床狀態 = "已佔床";


                            medCarInfoClasses.Add(medCarInfoClass);
                        }
                        return medCarInfoClasses;
                    }
                }
            }
        }
        private List<medCarInfoClass> ExecuteUDPDPPF0(List<medCarInfoClass> medCarInfoClasses)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string SP = "UDPDPPF0";
                string procName = $"{DB2_schema}.{SP}";
                foreach (var medCarInfoClass in medCarInfoClasses)
                {
                    if (medCarInfoClass.住院號.StringIsEmpty()) continue;
                    using (DB2Command cmd = MyDb2Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = procName;
                        cmd.Parameters.Add("@THISTNO", DB2Type.VarChar, 10).Value = medCarInfoClass.病歷號;
                        cmd.Parameters.Add("@TCASENO", DB2Type.VarChar, 8).Value = medCarInfoClass.住院號;
                        DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                        DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);
                        using (DB2DataReader reader = cmd.ExecuteReader())
                        {
                            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();
                            while (reader.Read())
                            {
                                Dictionary<string, object> row = new Dictionary<string, object>();
                                for (int j = 0; j < reader.FieldCount; j++)
                                {
                                    row[reader.GetName(j)] = reader.IsDBNull(j) ? null : reader.GetValue(j);
                                }
                                results.Add(row);
                            }


                            diseaseClass diseaseClass = new diseaseClass();
                            foreach (var row in results)
                            {
                                if (row.ContainsKey("UDPDPSY") && row.ContainsKey("UDPDPVL"))
                                {
                                    string key = row["UDPDPSY"].ToString().Trim();
                                    string value = row["UDPDPVL"].ToString().Trim();
                                    if (key == "HSEXC") medCarInfoClass.性別 = value;
                                    if (key == "PBIRTH8") medCarInfoClass.出生日期 = value;
                                    if (key == "PSECTC") medCarInfoClass.科別 = value;
                                    if (key == "PFINC") medCarInfoClass.財務 = value;
                                    if (key == "PADMDT") medCarInfoClass.入院日期 = value;
                                    if (key == "PVSDNO") medCarInfoClass.主治醫師代碼 = value;
                                    if (key == "PRDNO") medCarInfoClass.住院醫師代碼 = value;
                                    if (key == "PVSNAM") medCarInfoClass.主治醫師 = value;
                                    if (key == "PRNAM") medCarInfoClass.住院醫師 = value;
                                    if (key == "PBHIGHT") medCarInfoClass.身高 = value;
                                    if (key == "PBWEIGHT") medCarInfoClass.體重 = value;
                                    if (key == "PBBSA") medCarInfoClass.體表面積 = value;

                                    if (key == "NGTUBE") medCarInfoClass.鼻胃管使用狀況 = value;
                                    if (key == "TUBE") medCarInfoClass.其他管路使用狀況 = value;
                                    if (key == "HAllERGY") medCarInfoClass.過敏史 = value;
                                    if (key == "RTALB") medCarInfoClass.白蛋白 = value;
                                    if (key == "RTCREA") medCarInfoClass.肌酸酐 = value;
                                    if (key == "RTEGFRM") medCarInfoClass.估算腎小球過濾率 = value;
                                    if (key == "RTALT") medCarInfoClass.丙氨酸氨基轉移酶 = value;
                                    if (key == "RTK") medCarInfoClass.鉀離子 = value;
                                    if (key == "RTCA") medCarInfoClass.鈣離子 = value;
                                    if (key == "RTTB") medCarInfoClass.總膽紅素 = value;
                                    if (key == "RTNA") medCarInfoClass.鈉離子 = value;
                                    if (key == "RTWBC") medCarInfoClass.白血球 = value;
                                    if (key == "RTHGB") medCarInfoClass.血紅素 = value;
                                    if (key == "RTPLT") medCarInfoClass.血小板 = value;
                                    if (key == "RTINR") medCarInfoClass.國際標準化比率 = value;
                                    if (key == "PBIRTH8")
                                    {
                                        if (!string.IsNullOrWhiteSpace(value)) medCarInfoClass.年齡 = age(value);
                                    }

                                    if (key == "HICD1") diseaseClass.國際疾病分類代碼1 = value;
                                    if (key == "HICDTX1") diseaseClass.疾病說明1 = value;
                                    if (key == "HICD2") diseaseClass.國際疾病分類代碼2 = value;
                                    if (key == "HICDTX2") diseaseClass.疾病說明2 = value;
                                    if (key == "HICD3") diseaseClass.國際疾病分類代碼3 = value;
                                    if (key == "HICDTX3") diseaseClass.疾病說明3 = value;
                                    if (key == "HICD4") diseaseClass.國際疾病分類代碼4 = value;
                                    if (key == "HICDTX4") diseaseClass.疾病說明4 = value;
                                }
                            }
                            (string 疾病代碼, string 疾病說明) = disease(diseaseClass);
                            medCarInfoClass.疾病代碼 = 疾病代碼;
                            medCarInfoClass.疾病說明 = 疾病說明;
                            abnormal(medCarInfoClass);
                        }
                    }
                }
                return medCarInfoClasses;
            }
        }
        private List<medCpoeClass> ExecuteUDPDPDSP(List<medCarInfoClass> medCarInfoClasses)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string procName = $"{DB2_schema}.UDPDPDSP";
                List<medCpoeClass> prescription = new List<medCpoeClass>();
                DateTime now = DateTime.Now;
                string time = now.ToString("HHmm");
                string updateTime = now.ToDateTimeString();
                foreach (var medCarInfoClass in medCarInfoClasses)
                {
                    if (medCarInfoClass.住院號.StringIsEmpty()) continue;

                    using (DB2Command cmd = MyDb2Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = procName;
                        cmd.Parameters.Add("@TCASENO", DB2Type.VarChar, 8).Value = medCarInfoClass.住院號;
                        cmd.Parameters.Add("@TNURSTA", DB2Type.VarChar, 4).Value = medCarInfoClass.護理站;
                        cmd.Parameters.Add("@TTIME", DB2Type.VarChar, 4).Value = time;
                        DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                        using (DB2DataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string 開始日期 = reader["UDBGNDT2"].ToString().Trim();
                                string 開始時間 = reader["UDBGNTM"].ToString().Trim();
                                string 日期時間 = $"{開始日期} {開始時間.Substring(0, 2)}:{開始時間.Substring(2, 2)}:00";
                                DateTime 開始日期時間 = DateTime.ParseExact(日期時間, "yyyy-MM-dd HH:mm:ss", null);
                                string 結束日期 = reader["UDENDDT2"].ToString().Trim();
                                string 結束時間 = reader["UDENDTM"].ToString().Trim();
                                日期時間 = $"{結束日期} {結束時間.Substring(0, 2)}:{結束時間.Substring(2, 2)}:00";
                                DateTime 結束日期時間 = DateTime.ParseExact(日期時間, "yyyy-MM-dd HH:mm:ss", null);
                                medCpoeClass medCpoeClass = new medCpoeClass
                                {
                                    GUID = Guid.NewGuid().ToString(),
                                    藥局 = medCarInfoClass.藥局,
                                    護理站 = medCarInfoClass.護理站,
                                    床號 = medCarInfoClass.床號,
                                    Master_GUID = medCarInfoClass.GUID,
                                    更新時間 = updateTime,
                                    住院號 = reader["UDCASENO"].ToString().Trim(),
                                    序號 = reader["UDORDSEQ"].ToString().Trim(),
                                    開始時間 = 開始日期時間.ToDateTimeString_6(),
                                    結束時間 = 結束日期時間.ToDateTimeString_6(),
                                    藥碼 = reader["UDDRGNO"].ToString().Trim(),
                                    頻次代碼 = reader["UDFREQN"].ToString().Trim(),
                                    頻次屬性 = reader["UDFRQATR"].ToString().Trim(),
                                    藥品名 = reader["UDDRGNAM"].ToString().Trim(),
                                    途徑 = reader["UDROUTE"].ToString().Trim(),
                                    數量 = reader["UDLQNTY"].ToString().Trim(),
                                    劑量 = reader["UDDOSAGE"].ToString().Trim(),
                                    單位 = reader["UDDUNIT"].ToString().Trim(),
                                    期限 = reader["UDDURAT"].ToString().Trim(),
                                    自動包藥機 = reader["UDDSPMF"].ToString().Trim(),
                                    化癌分類 = reader["UDCHEMO"].ToString().Trim(),
                                    自購 = reader["UDSELF"].ToString().Trim(),
                                    血液製劑註記 = reader["UDALBUMI"].ToString().Trim(),
                                    處方醫師 = reader["UDORSIGN"].ToString().Trim(),
                                    處方醫師姓名 = reader["UDSIGNAM"].ToString().Trim(),
                                    操作人員 = reader["UDLUSER"].ToString().Trim(),
                                    藥局代碼 = reader["UDLRXID"].ToString().Trim(),
                                    大瓶點滴 = reader["UDCNT02"].ToString().Trim(),
                                    LKFLAG = reader["UDBRFNM"].ToString().Trim(),
                                    排序 = reader["UDRANK"].ToString().Trim(),
                                    判讀藥師代碼 = reader["PHARNUM"].ToString().Trim(),
                                    判讀FLAG = reader["FLAG"].ToString().Trim(),
                                    勿磨 = reader["UDNGT"].ToString().Trim(),
                                    抗生素等級 = reader["UDANTICG"].ToString().Trim(),
                                    重複用藥 = reader["UDSAMEDG"].ToString().Trim(),
                                    配藥天數 = reader["UDDSPDY"].ToString().Trim(),
                                    交互作用 = reader["UDDDI"].ToString().Trim(),
                                    交互作用等級 = reader["UDDDIC"].ToString().Trim()
                                };
                                if (reader["UDSTATUS"].ToString().Trim() == "80") medCpoeClass.狀態 = "DC";
                                if (reader["UDSTATUS"].ToString().Trim() == "30") medCpoeClass.狀態 = "New";
                                //if (medCpoeClass.藥局代碼 == "UB01") medCpoeClass.藥局名稱 = "中正樓總藥局";
                                //if (medCpoeClass.藥局代碼 == "UB18") medCpoeClass.藥局名稱 = "中正樓十三樓藥局";
                                //if (medCpoeClass.藥局代碼 == "UA05") medCpoeClass.藥局名稱 = "思源樓思源藥局";
                                //if (medCpoeClass.藥局代碼 == "ERS1") medCpoeClass.藥局名稱 = "中正樓急診藥局";
                                //if (medCpoeClass.藥局代碼 == "UBAA") medCpoeClass.藥局名稱 = "中正樓配方機藥局";
                                //if (medCpoeClass.藥局代碼 == "UATP") medCpoeClass.藥局名稱 = "中正樓TPN藥局";
                                //if (medCpoeClass.藥局代碼 == "EW01") medCpoeClass.藥局名稱 = "思源樓神經再生藥局";
                                //if (medCpoeClass.藥局代碼 == "UBTP") medCpoeClass.藥局名稱 = "中正樓臨床試驗藥局";
                                //if (medCpoeClass.藥局代碼 == "UC02") medCpoeClass.藥局名稱 = "長青樓藥局";
                                if (string.IsNullOrWhiteSpace(medCpoeClass.狀態)) medCpoeClass.狀態 = "New";
                                if (medCpoeClass.藥局代碼 == "" || medCpoeClass.藥局代碼 == "UC02") prescription.Add(medCpoeClass);
                            }
                        }
                    }
                }
                return prescription;
            }
        }
        private List<medCpoeRecClass> ExecuteUDPDPORD(List<medCarInfoClass> medCarInfoClasses)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string procName = $"{DB2_schema}.UDPDPORD";
                List<medCpoeRecClass> medCpoeRecClasses = new List<medCpoeRecClass>();
                foreach (var medCarInfoClass in medCarInfoClasses)
                {
                    if (medCarInfoClass.住院號.StringIsEmpty()) continue;
                    string time = DateTime.Now.ToString("HHmm");

                    using (DB2Command cmd = MyDb2Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = procName;
                        cmd.Parameters.Add("@TCASENO", DB2Type.VarChar, 8).Value = medCarInfoClass.住院號;
                        cmd.Parameters.Add("@TNURSTA", DB2Type.VarChar, 4).Value = medCarInfoClass.護理站;
                        cmd.Parameters.Add("@TTIME1", DB2Type.VarChar, 4).Value = "0000";
                        cmd.Parameters.Add("@TTIME2", DB2Type.VarChar, 4).Value = time;
                        DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                        try
                        {
                            using (DB2DataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string 開始日期 = reader["UDBGNDT2"].ToString().Trim();
                                    string 開始時間 = reader["UDBGNTM"].ToString().Trim();
                                    string 日期時間 = $"{開始日期} {開始時間.Substring(0, 2)}:{開始時間.Substring(2, 2)}:00";
                                    DateTime 開始日期時間 = DateTime.ParseExact(日期時間, "yyyy-MM-dd HH:mm:ss", null);
                                    string 結束日期 = reader["UDENDDT2"].ToString().Trim();
                                    string 結束時間 = reader["UDENDTM"].ToString().Trim();
                                    日期時間 = $"{結束日期} {結束時間.Substring(0, 2)}:{結束時間.Substring(2, 2)}:00";
                                    DateTime 結束日期時間 = DateTime.ParseExact(日期時間, "yyyy-MM-dd HH:mm:ss", null);
                                    medCpoeRecClass medCpoeRecClass = new medCpoeRecClass
                                    {
                                        GUID = Guid.NewGuid().ToString(),
                                        Master_GUID = medCarInfoClass.GUID,
                                        藥局 = medCarInfoClass.藥局,
                                        護理站 = medCarInfoClass.護理站,
                                        床號 = medCarInfoClass.床號,
                                        住院號 = reader["UDCASENO"].ToString().Trim(),
                                        序號 = reader["UDORDSEQ"].ToString().Trim(),
                                        開始時間 = 開始日期時間.ToDateTimeString_6(),
                                        結束時間 = 結束日期時間.ToDateTimeString_6(),
                                        藥碼 = reader["UDDRGNO"].ToString().Trim(),
                                        藥品名 = reader["UDDRGNAM"].ToString().Trim(),
                                        數量 = reader["UDLQNTY"].ToString().Trim(),
                                        劑量 = reader["UDDOSAGE"].ToString().Trim(),
                                        單位 = reader["UDDUNIT"].ToString().Trim(),
                                        處方醫師 = reader["UDORSIGN"].ToString().Trim(),
                                        處方醫師姓名 = reader["UDSIGNAM"].ToString().Trim(),
                                        操作人員 = reader["UDLUSER"].ToString().Trim(),
                                        藥局代碼 = reader["UDLRXID"].ToString().Trim(),
                                        大瓶點滴 = reader["UDCNT02"].ToString().Trim(),
                                        頻次代碼 = reader["UDFREQN"].ToString().Trim(),
                                        頻次屬性 = reader["UDFRQATR"].ToString().Trim(),

                                    };
                                    if (reader["UDSTATUS"].ToString().Trim() == "80") medCpoeRecClass.狀態 = "DC";
                                    if (reader["UDSTATUS"].ToString().Trim() == "30") medCpoeRecClass.狀態 = "New";
                                    if (medCpoeRecClass.狀態 == "DC") medCpoeRecClass.更新時間 = medCpoeRecClass.結束時間;
                                    if (medCpoeRecClass.狀態 == "New") medCpoeRecClass.更新時間 = medCpoeRecClass.開始時間;
                                    if (string.IsNullOrWhiteSpace(medCpoeRecClass.狀態))
                                    {
                                        DateTime startday = medCpoeRecClass.開始時間.StringToDateTime().Date;
                                        DateTime endday = medCpoeRecClass.結束時間.StringToDateTime().Date;
                                        DateTime today = DateTime.Now.Date;
                                        if (startday == today)
                                        {
                                            medCpoeRecClass.狀態 = "New";
                                            medCpoeRecClass.更新時間 = medCpoeRecClass.開始時間;
                                        }
                                        if (endday == today)
                                        {
                                            medCpoeRecClass.狀態 = "DC";
                                            medCpoeRecClass.更新時間 = medCpoeRecClass.結束時間;
                                        }
                                    }
                                    medCpoeRecClasses.Add(medCpoeRecClass);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"DB2Exception: {ex.Message}");
                        }

                    }
                }
                return medCpoeRecClasses;
            }

        }
        private List<medInfoClass> ExecuteUDPDPHLP(List<string> code)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string SP = "UDPDPHLP";
                string procName = $"{DB2_schema}.{SP}";
                List<medInfoClass> medInfoClasses = new List<medInfoClass>();
                for (int i = 0; i < code.Count; i++)
                {
                    using (DB2Command cmd = MyDb2Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = procName;
                        cmd.Parameters.Add("@UDDRGNO", DB2Type.VarChar, 5).Value = code[i];
                        DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                        DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);
                        using (DB2DataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string update_time = reader["DHUPDT"].ToString().Trim();
                                DateTime dateTime = DateTime.Parse(update_time);
                                medInfoClass medInfoClass = new medInfoClass
                                {
                                    序號 = reader["DHSEQNO"].ToString().Trim(),
                                    分類號 = reader["DHCHARNO"].ToString().Trim(),
                                    類別名 = reader["DHCHARNM"].ToString().Trim(),
                                    藥品通名 = reader["DHGNAME"].ToString().Trim(),
                                    藥品商品名 = reader["DHTNAME"].ToString().Trim(),
                                    藥品分類 = reader["DHRXCLAS"].ToString().Trim(),
                                    藥品治療分類 = reader["DHTXCLAS"].ToString().Trim(),
                                    適應症 = reader["DHINDICA"].ToString().Trim(),
                                    用法劑量 = reader["DHADMIN"].ToString().Trim(),
                                    備註 = reader["DHNOTE"].ToString().Trim(),
                                    藥碼 = code[i],
                                    仿單 = $"https://www7.vghtpe.gov.tw/api/find-package-insert-by-udCode?udCode={code[i]}",
                                    更新時間 = dateTime.ToDateTimeString(),                                   
                                };
                                medInfoClasses.Add(medInfoClass);
                            }
                        }
                    }
                }
                return medInfoClasses;
            }
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
                        cmd.Parameters.Add("@TDRGNO", DB2Type.VarChar, 5).Value = medInfoClass.藥碼;
                        DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                        DB2Parameter RETMSG = cmd.Parameters.Add("@RETMSG", DB2Type.VarChar, 60);
                        using (DB2DataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medInfoClass.藥品名 = reader["UDARNAME"].ToString().Trim();
                                medInfoClass.售價 = reader["UDWCOST"].ToString().Trim();
                                medInfoClass.健保價 = reader["UDPRICE"].ToString().Trim();
                                medInfoClass.頻次代碼 = reader["UDFREQN"].ToString().Trim();
                                medInfoClass.劑量 = reader["UDCMDOSA"].ToString().Trim();
                                
                            }                           
                        }
                    }                  
                }
                return medInfoClasses;
            }
        }
        private List<medInfoClass> ExecuteDRUGSPEC(List<medInfoClass> medInfoClasses)
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string SP = "VGHLNXVG.DRUGSPEC";
                string procName = $"{SP}";
                string today = DateTime.Today.ToDateString();
                foreach (var medInfoClass in medInfoClasses)
                {
                    string SPEC = "";
                    using (DB2Command cmd = MyDb2Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = procName;
                        cmd.Parameters.Add("@DRUGNO", DB2Type.VarChar, 5).Value = medInfoClass.藥碼;
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
                    medInfoClass.健保規範 = SPEC;
                }
                return medInfoClasses;
            }
        }

        private string age(string birthday)
        {
            int birthYear = birthday.Substring(0, 4).StringToInt32();
            int birthMon = birthday.Substring(4, 2).StringToInt32();
            int birthDay = birthday.Substring(6, 2).StringToInt32();

            DateTime today = DateTime.Now;
            int todayYear = today.Year;
            int todayMon = today.Month;
            int todayDay = today.Day;

            int ageYears = todayYear - birthYear;
            int ageMonths = todayMon - birthMon;

            if (ageMonths < 0 || (ageMonths == 0 && todayDay < birthDay))
            {
                ageYears--;
                ageMonths += 12;
            }

            if (todayDay < birthDay)
            {
                ageMonths--;
                if (ageMonths < 0)
                {
                    ageYears--;
                    ageMonths += 12;
                }
            }
            string ages = $"{ageYears}歲{ageMonths}月";

            return ages;
        }
        private (string dieaseCode, string dieaseName) disease(diseaseClass diseaseClass)
        {
            string dieaseCode = "";
            string dieaseName = "";

            if (!string.IsNullOrWhiteSpace(diseaseClass.國際疾病分類代碼1)) dieaseCode += diseaseClass.國際疾病分類代碼1;
            if (!string.IsNullOrWhiteSpace(diseaseClass.國際疾病分類代碼2)) dieaseCode += $";{diseaseClass.國際疾病分類代碼2}";
            if (!string.IsNullOrWhiteSpace(diseaseClass.國際疾病分類代碼3)) dieaseCode += $";{diseaseClass.國際疾病分類代碼3}";
            if (!string.IsNullOrWhiteSpace(diseaseClass.國際疾病分類代碼4)) dieaseCode += $";{diseaseClass.國際疾病分類代碼4}";
            if (!string.IsNullOrWhiteSpace(diseaseClass.疾病說明1)) dieaseName += diseaseClass.疾病說明1;
            if (!string.IsNullOrWhiteSpace(diseaseClass.疾病說明2)) dieaseName += $";{diseaseClass.疾病說明2}";
            if (!string.IsNullOrWhiteSpace(diseaseClass.疾病說明3)) dieaseName += $";{diseaseClass.疾病說明3}";
            if (!string.IsNullOrWhiteSpace(diseaseClass.疾病說明4)) dieaseName += $";{diseaseClass.疾病說明4}";
            return (dieaseCode, dieaseName);
        }
        private medCarInfoClass abnormal(medCarInfoClass medCarInfoClasses)
        {
            List<string> abnormalList = new List<string>();
            double 白蛋白 = medCarInfoClasses.白蛋白.StringToDouble();
            double 肌酸酐 = medCarInfoClasses.肌酸酐.StringToDouble();
            double 估算腎小球過濾率 = medCarInfoClasses.估算腎小球過濾率.StringToDouble();
            double 丙氨酸氨基轉移酶 = medCarInfoClasses.丙氨酸氨基轉移酶.StringToDouble();
            double 鉀離子 = medCarInfoClasses.鉀離子.StringToDouble();
            double 鈣離子 = medCarInfoClasses.鈣離子.StringToDouble();
            double 總膽紅素 = medCarInfoClasses.總膽紅素.StringToDouble();
            double 鈉離子 = medCarInfoClasses.鈉離子.StringToDouble();
            double 白血球 = medCarInfoClasses.白血球.StringToDouble();
            double 血紅素 = medCarInfoClasses.血紅素.StringToDouble();
            double 血小板 = medCarInfoClasses.血小板.StringToDouble();
            double 國際標準化比率 = medCarInfoClasses.國際標準化比率.StringToDouble();


            if (白蛋白 < 3.7 || 白蛋白 > 5.3) abnormalList.Add("alb");
            if (肌酸酐 < 0.5 || 肌酸酐 > 0.9) abnormalList.Add("scr");
            if (估算腎小球過濾率 <= 60) abnormalList.Add("egfr");
            if (丙氨酸氨基轉移酶 < 33) abnormalList.Add("alt");
            if (鉀離子 <= 3.5 || 鉀離子 >= 5.1) abnormalList.Add("k");
            if (鈣離子 <= 8.6 || 鈣離子 >= 10.0) abnormalList.Add("ca");
            if (總膽紅素 < 1.2) abnormalList.Add("tb");
            if (鈉離子 <= 136 || 鈉離子 >= 145) abnormalList.Add("na");
            if (白血球 <= 4180 || 白血球 >= 9380) abnormalList.Add("wbc");
            if (血紅素 <= 10.9 || 血紅素 >= 15.6) abnormalList.Add("hgb");
            if (血小板 <= 145000.0 || 血小板 >= 383000) abnormalList.Add("plt");
            if (國際標準化比率 < 0.82 || 國際標準化比率 > 1.15) abnormalList.Add("inr");

            string[] abnormalArray = abnormalList.ToArray();
            string abnormal = string.Join(";", abnormalArray);
            medCarInfoClasses.檢驗數值異常 = abnormal;
            return medCarInfoClasses;
        }
        private string ReplaceInvalidCharacters(string input)
        {
            char replacementChar = '?';
            var output = new StringBuilder();

            foreach (char c in input)
            {
                if (char.IsSurrogate(c) || c > '\uFFFF')
                {
                    output.Append(replacementChar); // 替換成 ?
                }
                else
                {
                    output.Append(c); // 保留原始字元
                }
            }

            return output.ToString();
        }
        
    }
}
