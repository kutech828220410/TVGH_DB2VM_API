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
using System.Collections.Concurrent;
using MySql.Data.MySqlClient;
using System.IO;
using OfficeOpenXml;
using MyOffice;






// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DB2VM_API.Controller._API_VM調劑系統
{
    [Route("api/[controller]")]
    [ApiController]
    public class med_cart : ControllerBase
    {
        static private string API01 = "http://127.0.0.1:4433";
        static private MySqlSslMode SSLMode = MySqlSslMode.None;
        private static string Message = "---------------------------------------------------------------------------";
        //公藥、外圍藥清單
        private List<string> medcode = new List<string>() { "80096", "80105", "80104", "80090", "80067", "02279", "04843" }; 
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
            try
            {
                returnData.Method = "med_cart/get_bed_list_by_cart";
                MyTimerBasic myTimerBasic = new MyTimerBasic();
                List<medCarInfoClass> medCarInfoClasses = new List<medCarInfoClass>();
                List<medCarInfoClass> bedList = new List<medCarInfoClass>();
                string 藥局 = returnData.ValueAry[0];
                string 護理站 = returnData.ValueAry[1];
                DateTime now = DateTime.Now;
                if (now.TimeOfDay < new TimeSpan(15, 0, 0)) 
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


                    
                    bedList = ExecuteUDPDPPF1(藥局, 護理站);
                    //List<medCarInfoClass> bedListInfo = ExecuteUDPDPPF0(bedList);
                    //List<medCarInfoClass> out_medCarInfoClass = medCarInfoClass.update_med_carinfo(API01, bedList);
                    //string url = $"{API01}/api/med_cart/update_med_carinfo";
                    //returnData rreturnData = new returnData();
                    //returnData.Data = bedList;
                    //string json_in = returnData.JsonSerializationt();
                    //string json_out = Net.WEBApiPostJson(url, json_in);
                    //returnData = json_out.JsonDeserializet<returnData>();
                    //if (returnData == null) return null;
                    //if (returnData.Code != 200) return returnData.JsonSerializationt(true);
                    //List<medCarInfoClass> out_medCarInfoClass = returnData.Data.ObjToClass<List<medCarInfoClass>>();



                    //(string Server, string DB, string UserName, string Password, uint Port) = GetServerInfo("Main", "網頁", "藥檔資料");
                    //string API = GetServerAPI("Main", "網頁", "API01");

                    List<medCarInfoClass> medCart_sql_add = new List<medCarInfoClass>();
                    List<medCarInfoClass> medCart_sql_replace = new List<medCarInfoClass>();
                    List<medCarInfoClass> medCart_sql_delete = new List<medCarInfoClass>();


                    SQLControl sQLControl_med_carInfo = new SQLControl("10.107.3.147", "dbvm", "med_carInfo", "user", "66437068", 3309, SSLMode);
                    SQLControl sQLControl_med_cpoe = new SQLControl("10.107.3.147", "dbvm", "med_cpoe", "user", "66437068", 3309, SSLMode);

                    DateTime lestweek = DateTime.Now.AddDays(-30);
                    DateTime yesterday = DateTime.Now.AddDays(-0);
                    string starttime = lestweek.GetStartDate().ToDateString();
                    string endtime = yesterday.GetEndDate().ToDateString();
                    sQLControl_med_carInfo.DeleteByBetween(null, (int)enum_med_carInfo.更新時間, starttime, endtime);

                    List<medCarInfoClass> input_medCarInfo = bedList;

                    if (input_medCarInfo == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"傳入Data資料異常";
                        return returnData.JsonSerializationt();
                    }

                    List<object[]> list_med_carInfo = sQLControl_med_carInfo.GetRowsByDefult(null, (int)enum_med_carInfo.藥局, 藥局);
                    List<medCarInfoClass> sql_medCar = list_med_carInfo.SQLToClass<medCarInfoClass, enum_med_carInfo>();
                    List<medCarInfoClass> medCarInfo = sql_medCar.Where(temp => temp.護理站 == 護理站).ToList();
                    Dictionary<string, List<medCarInfoClass>> medCarInfoDictBedNum = medCarInfoClass.ToDictByBedNum(medCarInfo);

                    ConcurrentBag<medCarInfoClass> localList_add = new ConcurrentBag<medCarInfoClass>();
                    ConcurrentBag<medCarInfoClass> localList_delete = new ConcurrentBag<medCarInfoClass>();
                    ConcurrentBag<medCarInfoClass> localList_replace = new ConcurrentBag<medCarInfoClass>();
                    Parallel.ForEach(input_medCarInfo, new ParallelOptions { MaxDegreeOfParallelism = 10 }, medCarInfoClass =>
                    {
                        medCarInfoClass targetPatient = new medCarInfoClass();

                        string 床號 = medCarInfoClass.床號;
                        if (medCarInfoClass.GetDictByBedNum(medCarInfoDictBedNum, 床號).Count != 0)
                        {
                            targetPatient = medCarInfoClass.GetDictByBedNum(medCarInfoDictBedNum, 床號)[0];
                        }

                        if (targetPatient.GUID.StringIsEmpty() == true)
                        {
                            medCarInfoClass.GUID = Guid.NewGuid().ToString();
                            localList_add.Add(medCarInfoClass);
                        }
                        else
                        {
                            if (medCarInfoClass.病歷號 != targetPatient.病歷號)
                            {
                                medCarInfoClass.GUID = Guid.NewGuid().ToString();
                                medCarInfoClass.異動 = "Y";
                                localList_add.Add(medCarInfoClass);
                                localList_delete.Add(targetPatient);
                            }
                            else
                            {
                                medCarInfoClass.GUID = targetPatient.GUID;
                                medCarInfoClass.調劑狀態 = targetPatient.調劑狀態;
                                localList_replace.Add(medCarInfoClass);
                            }
                        }
                    });
                    lock (medCart_sql_add) medCart_sql_add.AddRange(localList_add);
                    lock (medCart_sql_replace) medCart_sql_replace.AddRange(localList_replace);
                    lock (medCart_sql_delete) medCart_sql_delete.AddRange(localList_delete);

                    List<object[]> list_medCart_add = new List<object[]>();
                    List<object[]> list_medCart_replace = new List<object[]>();
                    List<object[]> list_medCart_delete = new List<object[]>();
                    list_medCart_add = medCart_sql_add.ClassToSQL<medCarInfoClass, enum_med_carInfo>();
                    list_medCart_replace = medCart_sql_replace.ClassToSQL<medCarInfoClass, enum_med_carInfo>();
                    list_medCart_delete = medCart_sql_delete.ClassToSQL<medCarInfoClass, enum_med_carInfo>();

                    if (list_medCart_add.Count > 0) sQLControl_med_carInfo.AddRows(null, list_medCart_add);
                    if (list_medCart_replace.Count > 0) sQLControl_med_carInfo.UpdateByDefulteExtra(null, list_medCart_replace);
                    if (list_medCart_delete.Count > 0)
                    {
                        sQLControl_med_carInfo.DeleteExtra(null, list_medCart_delete);
                        List<object[]> list_med_cpoe = sQLControl_med_cpoe.GetRowsByDefult(null, (int)enum_med_cpoe.藥局, 藥局);
                        List<medCpoeClass> sql_medCpoe = list_med_cpoe.SQLToClass<medCpoeClass, enum_med_cpoe>();
                        Dictionary<string, List<medCpoeClass>> medCpoeDict = medCpoeClass.ToDictByMasterGUID(sql_medCpoe);
                        //List<medCpoeClass> filterCpoe = new List<medCpoeClass>();
                        //for (int i = 0; medCart_sql_delete.Count > 0; i++)
                        //{
                        //    List<medCpoeClass> result = medCpoeClass.SortDictByMasterGUID(medCpoeDict, medCart_sql_delete[i].GUID);
                        //    filterCpoe.AddRange(result);
                        //}
                        List<medCpoeClass> filterCpoe = sql_medCpoe
                            .Where(cpoe => medCart_sql_delete.Any(medCart => medCart.GUID == cpoe.Master_GUID)).ToList();
                        List<object[]> list_medCpoe_delete = filterCpoe.ClassToSQL<medCpoeClass, enum_med_cpoe>();
                        if (list_medCpoe_delete.Count > 0) sQLControl_med_cpoe.DeleteExtra(null, list_medCpoe_delete);
                    }

                    List<object[]> list_bedList = sQLControl_med_carInfo.GetRowsByDefult(null, (int)enum_med_carInfo.藥局, 藥局);
                    List<medCarInfoClass> bedListt = list_bedList.SQLToClass<medCarInfoClass, enum_med_carInfo>();
                    medCarInfoClasses = bedListt.Where(temp => temp.護理站 == 護理站).ToList();
                    medCarInfoClasses.Sort(new medCarInfoClass.ICP_By_bedNum());

                    if (medCarInfoClasses == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"out_medCarInfoClass 無資料";
                        return returnData.JsonSerializationt(true);
                    }
                }
                else
                {
                    SQLControl sQLControl_med_carInfo = new SQLControl("10.107.3.147", "dbvm", "med_carInfo", "user", "66437068", 3309, SSLMode);
                    List<object[]> list_bedList = sQLControl_med_carInfo.GetRowsByDefult(null, (int)enum_med_carInfo.藥局, 藥局);
                    List<medCarInfoClass> bedListt = list_bedList.SQLToClass<medCarInfoClass, enum_med_carInfo>();
                    medCarInfoClasses = bedListt.Where(temp => temp.護理站 == 護理站).ToList();
                    medCarInfoClasses.Sort(new medCarInfoClass.ICP_By_bedNum());

                    if (medCarInfoClasses == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"out_medCarInfoClass 無資料";
                        return returnData.JsonSerializationt(true);
                    }
                }


                
                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = medCarInfoClasses;
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
                medCarInfoClass out_medCarInfoClass = new medCarInfoClass();
                string 藥局 = "";
                string 護理站 = "";
                string 床號 = "";
                string str_result_temp = "";
                DateTime now = DateTime.Now;
                if (now.TimeOfDay < new TimeSpan(15, 0, 0))
                {
                    List<Task> tasks = new List<Task>();
                    if (returnData.ValueAry == null || returnData.ValueAry.Count != 1)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"returnData.ValueAry 內容應為[\"GUID\"]";
                        return returnData.JsonSerializationt(true);
                    }
                    medCarInfoClass targetPatient = medCarInfoClass.get_patient_by_GUID_brief(API01, returnData.ValueAry);
                    str_result_temp += $"取得病人住院號, {myTimerBasic}ms \n";
                    if (targetPatient == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = "無對應的病人資料";
                        return returnData.JsonSerializationt(true);
                    }
                    藥局 = targetPatient.藥局;
                    護理站 = targetPatient.護理站;
                    床號 = targetPatient.床號;
                    List<medCarInfoClass> medCarInfoClasses = new List<medCarInfoClass> { targetPatient };
                    List<medCarInfoClass> bedListInfo = new List<medCarInfoClass>();
                    List<medCpoeClass> bedListCpoe = new List<medCpoeClass>();
                    tasks.Add(Task.Run(new Action(delegate
                    {
                        bedListInfo = ExecuteUDPDPPF0(medCarInfoClasses);
                        str_result_temp += $"取得病人個人資料, {myTimerBasic}ms \n";

                    })));
                    tasks.Add(Task.Run(new Action(delegate
                    {
                        bedListCpoe = ExecuteUDPDPDSP(medCarInfoClasses);
                        str_result_temp += $"取得病人處方, {myTimerBasic}ms \n";

                    })));


                    //tasks.Clear();
                    if (bedListCpoe.Count == 0) medCarInfoClasses[0].調劑狀態 = "Y";
                    //tasks.Add(Task.Run(new Action(delegate
                    //{
                    //    medCarInfoClass.update_med_carinfo(API01, medCarInfoClasses);
                    //    str_result_temp += $"更新med_carinfo, {myTimerBasic}ms \n";

                    //})));
                    //tasks.Add(Task.Run(new Action(delegate
                    //{
                    //    medCpoeClass.update_med_cpoe(API01, bedListCpoe);
                    //    str_result_temp += $"更新med_cpoe, {myTimerBasic}ms \n";

                    //})));
                    Task.WhenAll(tasks).Wait();

                    medCarInfoClass.update_med_carinfo(API01, bedListInfo);
                    str_result_temp += $"更新med_carinfo, {myTimerBasic}ms \n";

                    medCpoeClass.update_med_cpoe(API01, bedListCpoe);
                    str_result_temp += $"更新med_cpoe, {myTimerBasic}ms \n";

                    out_medCarInfoClass = medCarInfoClass.get_patient_by_GUID(API01, returnData.Value, returnData.ValueAry);
                    str_result_temp += $"取得病人所有資訊, {myTimerBasic}ms \n";
                }
                else
                {
                    out_medCarInfoClass = medCarInfoClass.get_patient_by_GUID(API01, returnData.Value, returnData.ValueAry);
                }


                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = out_medCarInfoClass;
                returnData.Result = $"取得{藥局} {護理站} 第{床號}病床資訊 {str_result_temp}";
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
                List<medQtyClass> get_med_qty = new List<medQtyClass>();
                DateTime now = DateTime.Now;
                string 藥局 = "";
                string 護理站 = "";
                if (now.TimeOfDay < new TimeSpan(15, 0, 0))
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

                    藥局 = returnData.ValueAry[0];
                    護理站 = returnData.ValueAry[1];

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

                    get_med_qty = medCpoeClass.get_med_qty(API01, returnData.Value, returnData.ValueAry);
                    if (get_med_qty == null)
                    {
                        returnData.Code = 200;
                        returnData.Result = $"無藥品處方資料";
                        return returnData.JsonSerializationt(true);
                    }
                }
                else
                {
                    get_med_qty = medCpoeClass.get_med_qty(API01, returnData.Value, returnData.ValueAry);
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
                DateTime now = DateTime.Now;
                string 藥局 = "";
                string 護理站 = "";
                string 床號 = "";
                List<medCarInfoClass> get_patient = new List<medCarInfoClass>();
                if (now.TimeOfDay < new TimeSpan(15, 0, 0))
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
                    get_patient = medCpoeRecClass.get_medChange_by_GUID(API01, returnData.ValueAry);

                    藥局 = bedList[0].藥局;
                    護理站 = bedList[0].護理站;
                    床號 = bedList[0].床號;
                }
                else
                {
                    get_patient = medCpoeRecClass.get_medChange_by_GUID(API01, returnData.ValueAry);
                }
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

                //List<medCarInfoClass> bedList = ExecuteUDPDPPF1(藥局, 護理站);
                //List<medCarInfoClass> bedListInfo = ExecuteUDPDPPF0(bedList);
                //List<medCarInfoClass> update_medCarInfoClass = medCarInfoClass.update_med_carinfo(API01, bedListInfo);
                //List<medCpoeClass> bedListCpoe = ExecuteUDPDPDSP(update_medCarInfoClass);
                //medCpoeClass.update_med_cpoe(API01, bedListCpoe);

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
        [HttpGet("get_all")]
        public string get_all()
        {
            returnData returnData = new returnData();
            string 藥局 = "UC02";
            string[] list = { "C039", "C049", "C059", "C069", "C079", "C089", "C099", "C109", "C119", "C129", "BMT" };

            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
                List<medCpoeClass> update_medCpoeClass = new List<medCpoeClass>();
                List<medCarInfoClass> bedList = new List<medCarInfoClass>();
                string 護理站 = "";
                for (int i = 0; i < list.Length; i++)
                {
                    護理站 = list[i];
                    List<medCarInfoClass> bedList_first = ExecuteUDPDPPF1(藥局, 護理站);
                    List<medCarInfoClass> bedListInfo = ExecuteUDPDPPF0(bedList_first);
                    returnData returnData_medCarInfoClass = medCarInfoClass.update_med_carinfo(API01, bedListInfo);
                    if(returnData_medCarInfoClass.Code != 200)
                    {
                        Logger.Log("update_med_carinfo", returnData_medCarInfoClass.JsonSerializationt());
                        Logger.Log("update_med_carinfo", Message);
                        returnData_medCarInfoClass.Data = bedListInfo;
                        return returnData_medCarInfoClass.JsonSerializationt(true);
                    }
                    List<medCarInfoClass> update_medCarInfoClass = returnData_medCarInfoClass.Data.ObjToClass<List<medCarInfoClass>>();

                    List<medCpoeClass> bedListCpoe = ExecuteUDPDPDSP(update_medCarInfoClass);
                    returnData returnData_medCpoeClass = medCpoeClass.update_med_cpoe(API01, bedListCpoe);
                    if(returnData_medCpoeClass.Code != 200)
                    {
                        Logger.Log("update_med_cpoe", returnData_medCpoeClass.JsonSerializationt());
                        Logger.Log("update_med_cpoe", Message);
                        returnData_medCpoeClass.Data = bedListCpoe;
                        return returnData_medCpoeClass.JsonSerializationt(true);
                    }

                }
                
                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Result = $"取得 {藥局} 病床及處方資訊完成";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception:{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        [HttpGet("get_all_db")]
        public string get_all_db()
        {
            returnData returnData = new returnData();
            string 藥局 = "UC02";
            string[] list = { "C039", "C049", "C059", "C069", "C079", "C089", "C099", "C109", "C119", "C129", "BMT" };

            MyTimerBasic myTimerBasic = new MyTimerBasic();
            //try
            //{
            List<medCpoeClass> update_medCpoeClass = new List<medCpoeClass>();
            List<medCarInfoClass> bedList = new List<medCarInfoClass>();
            string 護理站 = "";
            for (int i = 0; i < list.Length; i++)
            {
                護理站 = list[i];
                List<medCarInfoClass> bedList_first = ExecuteUDPDPPF1(藥局, 護理站);
                List<medCarInfoClass> bedListInfo = ExecuteUDPDPPF0(bedList_first);

                //update_med_carinfo
                List<medCarInfoClass> medCart_sql_add = new List<medCarInfoClass>();
                List<medCarInfoClass> medCart_sql_replace = new List<medCarInfoClass>();
                List<medCarInfoClass> medCart_sql_delete = new List<medCarInfoClass>();

                SQLControl sQLControl_med_carInfo = new SQLControl("127.0.0.1", "dbvm", "med_carInfo", "user", "66437068", 3306, SSLMode);
                SQLControl sQLControl_med_cpoe = new SQLControl("127.0.0.1", "dbvm", "med_cpoe", "user", "66437068", 3306, SSLMode);

                DateTime lestweek = DateTime.Now.AddDays(-30);
                DateTime yesterday = DateTime.Now.AddDays(-0);
                string starttime = lestweek.GetStartDate().ToDateString();
                string endtime = yesterday.GetEndDate().ToDateString();
                sQLControl_med_carInfo.DeleteByBetween(null, (int)enum_med_carInfo.更新時間, starttime, endtime);

                List<medCarInfoClass> input_medCarInfo = bedListInfo;

                if (input_medCarInfo == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"input_medCarInfo傳入Data資料異常";
                    return returnData.JsonSerializationt();
                }
                //string 藥局 = input_medCarInfo[0].藥局;
                //string 護理站 = input_medCarInfo[0].護理站;

                List<object[]> list_med_carInfo = sQLControl_med_carInfo.GetRowsByDefult(null, (int)enum_med_carInfo.藥局, 藥局);
                List<medCarInfoClass> sql_medCar = list_med_carInfo.SQLToClass<medCarInfoClass, enum_med_carInfo>();
                List<medCarInfoClass> medCarInfo = sql_medCar.Where(temp => temp.護理站 == 護理站).ToList();
                Dictionary<string, List<medCarInfoClass>> medCarInfoDictBedNum = medCarInfoClass.ToDictByBedNum(medCarInfo);



                List<Task> tasks = new List<Task>();

                foreach (medCarInfoClass medCarInfoClass in input_medCarInfo)
                {
                    tasks.Add(Task.Run(new Action(delegate
                    {
                        medCarInfoClass targetPatient = new medCarInfoClass();

                        string 床號 = medCarInfoClass.床號;
                        if (medCarInfoClass.GetDictByBedNum(medCarInfoDictBedNum, 床號).Count != 0)
                        {
                            targetPatient = medCarInfoClass.GetDictByBedNum(medCarInfoDictBedNum, 床號)[0];
                        }
                        if (targetPatient.GUID.StringIsEmpty() == true)
                        {
                            medCarInfoClass.GUID = Guid.NewGuid().ToString();
                            medCart_sql_add.LockAdd(medCarInfoClass);
                        }
                        else
                        {
                            if (medCarInfoClass.PRI_KEY != targetPatient.PRI_KEY)
                            {
                                medCarInfoClass.GUID = Guid.NewGuid().ToString();
                                medCarInfoClass.異動 = "Y";
                                medCart_sql_add.LockAdd(medCarInfoClass);
                                medCart_sql_delete.LockAdd(targetPatient);
                            }
                            else
                            {
                                medCarInfoClass.GUID = targetPatient.GUID;
                                medCarInfoClass.調劑狀態 = targetPatient.調劑狀態;
                                medCart_sql_replace.LockAdd(medCarInfoClass);
                            }
                        }
                    })));
                }
                Task.WhenAll(tasks).Wait();




                List<object[]> list_medCart_add = new List<object[]>();
                List<object[]> list_medCart_replace = new List<object[]>();
                List<object[]> list_medCart_delete = new List<object[]>();
                list_medCart_add = medCart_sql_add.ClassToSQL<medCarInfoClass, enum_med_carInfo>();
                list_medCart_replace = medCart_sql_replace.ClassToSQL<medCarInfoClass, enum_med_carInfo>();
                list_medCart_delete = medCart_sql_delete.ClassToSQL<medCarInfoClass, enum_med_carInfo>();

                if (list_medCart_add.Count > 0) sQLControl_med_carInfo.AddRows(null, list_medCart_add);
                if (list_medCart_replace.Count > 0) sQLControl_med_carInfo.UpdateByDefulteExtra(null, list_medCart_replace);
                if (list_medCart_delete.Count > 0)
                {
                    sQLControl_med_carInfo.DeleteExtra(null, list_medCart_delete);
                    List<object[]> list_med_cpoe_1 = sQLControl_med_cpoe.GetRowsByDefult(null, (int)enum_med_cpoe.藥局, 藥局);
                    List<medCpoeClass> sql_medCpoe_1 = list_med_cpoe_1.SQLToClass<medCpoeClass, enum_med_cpoe>();
                    List<medCpoeClass> filterCpoe = sql_medCpoe_1
                        .Where(cpoe => medCart_sql_delete.Any(medCart => medCart.GUID == cpoe.Master_GUID)).ToList();
                    List<object[]> list_medCpoe_delete_1 = filterCpoe.ClassToSQL<medCpoeClass, enum_med_cpoe>();
                    if (list_medCpoe_delete_1.Count > 0) sQLControl_med_cpoe.DeleteExtra(null, list_medCpoe_delete_1);
                }

                List<object[]> list_bedList = sQLControl_med_carInfo.GetRowsByDefult(null, (int)enum_med_carInfo.藥局, 藥局);
                bedList = list_bedList.SQLToClass<medCarInfoClass, enum_med_carInfo>();
                List<medCarInfoClass> medCarInfoClasses = bedList.Where(temp => temp.護理站 == 護理站).ToList();
                medCarInfoClasses.Sort(new medCarInfoClass.ICP_By_bedNum());

                List<medCpoeClass> bedListCpoe = ExecuteUDPDPDSP(medCarInfoClasses);

                //update_med_cpoe
                sQLControl_med_cpoe.DeleteByBetween(null, (int)enum_med_cpoe.更新時間, starttime, endtime);

                List<medCpoeClass> input_medCpoe = bedListCpoe;
                if (input_medCpoe == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"input_medCpoe傳入Data資料異常";
                    return returnData.JsonSerializationt();
                }

                //string 藥局 = input_medCpoe[0].藥局;
                //string 護理站 = input_medCpoe[0].護理站;

                list_med_carInfo = sQLControl_med_carInfo.GetRowsByDefult(null, (int)enum_med_carInfo.護理站, 護理站);
                List<object[]> list_med_cpoe = sQLControl_med_cpoe.GetRowsByDefult(null, (int)enum_med_cpoe.護理站, 護理站);

                List<medCarInfoClass> sql_medCarInfo = list_med_carInfo.SQLToClass<medCarInfoClass, enum_med_carInfo>();
                List<medCpoeClass> sql_medCpoe = list_med_cpoe.SQLToClass<medCpoeClass, enum_med_cpoe>();

                List<medCpoeClass> medCpoe_sql_add = new List<medCpoeClass>();
                List<medCpoeClass> medCpoe_sql_replace = new List<medCpoeClass>();
                List<medCpoeClass> medCpoe_sql_delete_buf = new List<medCpoeClass>();
                List<medCpoeClass> medCpoe_sql_delete = new List<medCpoeClass>();
                List<medCarInfoClass> update_medCarInfo = new List<medCarInfoClass>();

                Dictionary<string, List<medCarInfoClass>> medCarInfoDict = medCarInfoClass.ToDictByGUID(sql_medCarInfo);
                Dictionary<string, List<medCpoeClass>> sqlMedCpoeDict = medCpoeClass.ToDictByMasterGUID(sql_medCpoe);
                Dictionary<string, List<medCpoeClass>> inputMedCpoeDict = medCpoeClass.ToDictByMasterGUID(input_medCpoe);

                foreach (string GUID in medCarInfoDict.Keys)
                {
                    List<medCpoeClass> medCpoeClasses_old = medCpoeClass.GetByMasterGUID(sqlMedCpoeDict, GUID);
                    List<medCpoeClass> medCpoeClasses_new = medCpoeClass.GetByMasterGUID(inputMedCpoeDict, GUID);
                    medCarInfoClasses = medCarInfoClass.GetDictByGUID(medCarInfoDict, GUID);

                    if (medCpoeClasses_old.Count == 0 && medCpoeClasses_new.Count == 0)
                    {
                        medCarInfoClasses[0].調劑狀態 = "已調劑";
                        continue;
                    }
                    List<medCpoeClass> onlyInOld = medCpoeClasses_old.Where(oldItem => !medCpoeClasses_new.Any(newItem => newItem.PRI_KEY == oldItem.PRI_KEY)).ToList(); //DC
                    List<medCpoeClass> onlyInNew = medCpoeClasses_new.Where(newItem => !medCpoeClasses_old.Any(oldItem => oldItem.PRI_KEY == newItem.PRI_KEY)).ToList(); //NEW
                    for (int k = 0; k< onlyInOld.Count; k++)
                    {
                        if (onlyInOld[k].調劑狀態.StringIsEmpty() && onlyInOld[k].狀態.StringIsEmpty())
                        {
                            onlyInOld[k].調劑異動 = "Y";
                            medCpoe_sql_delete.Add(onlyInOld[k]);
                        }
                        else
                        {
                            //找出onlyInOld有沒有和onlyInNew一樣的
                            for (int j = 0; j < onlyInNew.Count; j++)
                            {
                                if (onlyInOld[k].藥碼 == onlyInNew[j].藥碼 &&
                                    onlyInOld[k].途徑 == onlyInNew[j].途徑 &&
                                    onlyInOld[k].頻次 == onlyInNew[j].頻次)
                                {
                                    medCpoe_sql_delete.Add(onlyInOld[k]);
                                    onlyInNew[j].調劑狀態 = onlyInOld[k].調劑狀態;
                                    onlyInOld[k].調劑異動 = "Y";
                                    break;
                                }
                            }
                        }
                    }
                    foreach (var oldItem in onlyInOld.Where(o => o.調劑異動.StringIsEmpty()))
                    {
                        double 數量 = oldItem.數量.StringToInt32() * -1;
                        oldItem.數量 = 數量.ToString();
                        oldItem.劑量 = "--";
                        oldItem.頻次 = "--";
                        oldItem.途徑 = "--";
                        oldItem.單位 = "--";
                        oldItem.調劑狀態 = "";
                        oldItem.狀態 = "DC";
                        oldItem.調劑異動 = "Y";
                        medCpoe_sql_replace.Add(oldItem);
                    }
                    DateTime 調劑時間 = medCarInfoClasses[0].調劑時間.StringToDateTime();
                    DateTime 現在時間 = DateTime.Now;
                    if (調劑時間 != DateTime.MaxValue && 現在時間 > 調劑時間)
                    {
                        foreach (var item in onlyInNew)
                        {
                            item.狀態 = "New";
                            item.調劑異動 = "Y";
                        }
                    }
                    medCpoe_sql_add.AddRange(onlyInNew);
                }

                List<object[]> list_medCpoe_add = medCpoe_sql_add.ClassToSQL<medCpoeClass, enum_med_cpoe>();
                List<object[]> list_medCpoe_replace = medCpoe_sql_replace.ClassToSQL<medCpoeClass, enum_med_cpoe>();
                List<object[]> list_medCpoe_delete = medCpoe_sql_delete.ClassToSQL<medCpoeClass, enum_med_cpoe>();
                list_medCart_add = update_medCarInfo.ClassToSQL<medCarInfoClass, enum_med_carInfo>();


                if (list_medCpoe_add.Count > 0) sQLControl_med_cpoe.AddRows(null, list_medCpoe_add);
                if (list_medCpoe_replace.Count > 0) sQLControl_med_cpoe.UpdateByDefulteExtra(null, list_medCpoe_replace);
                if (list_medCpoe_delete.Count > 0) sQLControl_med_cpoe.DeleteExtra(null, list_medCpoe_delete);
                if (list_medCart_add.Count > 0) sQLControl_med_carInfo.UpdateByDefulteExtra(null, list_medCart_add);

            }

            returnData.Code = 200;
            returnData.TimeTaken = $"{myTimerBasic}";
            returnData.Result = $"取得 {藥局} 病床及處方資訊完成";
            return returnData.JsonSerializationt(true);
            
        }
        [HttpGet("get_medChange")]
        public string get_medChange()
        {
            returnData returnData = new returnData();
            string 藥局 = "UC02";
            string[] list = { "C039", "C049", "C059", "C069", "C079", "C089", "C099", "C109", "C119", "C129", "BMT" };
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
                string 護理站 = "";
                List<medCarInfoClass> bedList = new List<medCarInfoClass>();
                for (int i = 0; i < list.Length; i++)
                {
                    護理站 = list[i];
                    List<string> value = new List<string>() { 藥局, 護理站 };
                    bedList = medCarInfoClass.get_bed_list_by_cart(API01, value);
                    List<medCpoeRecClass> medCpoe_change = ExecuteUDPDPORD(bedList);
                    List<medCpoeRecClass> update_medCpoe_change = medCpoeRecClass.update_med_CpoeRec(API01, medCpoe_change);
                    returnData.Data = medCpoe_change;
                    returnData.Result = $"取得{藥局} {護理站} 處方異動資料共{medCpoe_change.Count}筆";
                    //Logger.Log("get_medChange", returnData.JsonSerializationt());
                    //Logger.Log("get_medChange", Message);
                }
                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception:{ex.Message}";
                Logger.Log("get_medChange", returnData.JsonSerializationt());
                Logger.Log("get_medChange", Message);
                return returnData.JsonSerializationt(true);
            }
        }
        [HttpGet("get_bedStatus")]
        public string get_bedStatus()
        {
            returnData returnData = new returnData();
            string 藥局 = "UC02";
            //string[] list = { "C039" };
            string[] list = { "C039", "C049", "C059", "C069", "C079", "C089", "C099", "C109", "C119", "C129", "BMT" };

            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
                string 護理站 = "";
                List<medCarInfoClass> bedList = new List<medCarInfoClass>();
                for (int i = 0; i < list.Length; i++)
                {
                    護理站 = list[i];
                    List<string> value = new List<string>() { 藥局, 護理站 };
                    bedList = medCarInfoClass.get_bed_list_by_cart(API01, value);
                    List<bedStatusClass> bedStatus = ExecuteUDPDPBED(bedList);
                    bedStatusClass.update_med_CpoeRec(API01, bedStatus);
                    returnData.Data = bedStatus;
                    returnData.Result = $"取得{藥局} {護理站} 處方異動資料共{bedStatus.Count}筆";
                    //Logger.Log("get_medChange", returnData.JsonSerializationt());
                    //Logger.Log("get_medChange", Message);
                }
                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception:{ex.Message}";
                Logger.Log("get_medChange", returnData.JsonSerializationt());
                Logger.Log("get_medChange", Message);
                return returnData.JsonSerializationt(true);
            }
        }
        [HttpGet("get_medInfo")]
        public string get_medInfo_by_cart()
        {
            returnData returnData = new returnData();
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
      
                
                //List<medCarInfoClass> bedList = ExecuteUDPDPPF1(藥局, nurst_list[i]);
                List<medCpoeClass> bedListCpoe = medCpoeClass.get_medCpoe(API01);
                List<string> code = bedListCpoe.GroupBy(temp => temp.藥碼).Select(group => group.Key).ToList();
                List<string> result = code.SelectMany(code => code.Split(",")).ToList();
                List<medInfoClass> medInfoClass_1 = ExecuteUDPDPHLP(result);
                List<medInfoClass> medInfoClass_2 = ExecuteUDPDPDRG(medInfoClass_1);
                medInfoClass.update_med_info(API01, medInfoClass_2);

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
        //[HttpGet("get_medInfo")]
        //public string get_medInfo([FromBody] returnData returnData)
        //{
        //    MyTimerBasic myTimerBasic = new MyTimerBasic();
        //    try
        //    {              
        //        List<medClass> medClasses = medClass.get_med_cloud(API01);
        //        List<string> code = medClasses.Select(temp => temp.藥品碼).ToList();
        //        List<medInfoClass> medInfoClass_1 = ExecuteUDPDPHLP(code);
        //        List<medInfoClass> medInfoClass_2 = ExecuteUDPDPDRG(medInfoClass_1);
        //        List<medInfoClass> medInfoClass_3 = ExecuteDRUGSPEC(medInfoClass_2);
        //        medInfoClass.update_med_info(API01, medInfoClass_3);             
              
        //        returnData.Code = 200;
        //        returnData.TimeTaken = $"{myTimerBasic}";
        //        returnData.Data = "";
        //        returnData.Result = $"更新藥品資訊成功";
        //        return returnData.JsonSerializationt(true);
        //    }
        //    catch (Exception ex)
        //    {
        //        returnData.Code = -200;
        //        returnData.Result = $"Exception:{ex.Message}";
        //        return returnData.JsonSerializationt(true);
        //    }
        //}
      
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
                                GUID = Guid.NewGuid().ToString(),
                                藥局 = phar,
                                PRI_KEY = reader["HISTNUM"].ToString().Trim(),
                                更新時間 = DateTime.Now.ToDateTimeString(),
                                調劑時間 = DateTime.MinValue.ToDateTimeString(),
                                護理站 = reader["HNURSTA"].ToString().Trim(),
                                床號 = reader["HBEDNO"].ToString().Trim(),
                                病歷號 = reader["HISTNUM"].ToString().Trim(),
                                住院號 = reader["PCASENO"].ToString().Trim(),
                                姓名 = ReplaceInvalidCharacters(reader["PNAMEC"].ToString()).Trim()
                            };
                            if (medCarInfoClass.姓名.StringIsEmpty() == false) medCarInfoClass.占床狀態 = "已佔床";
                            //if (!string.IsNullOrWhiteSpace(medCarInfoClass.姓名)) medCarInfoClass.占床狀態 = "已佔床";
                            medCarInfoClasses.Add(medCarInfoClass);
                        }
                        return medCarInfoClasses;
                    }
                }
            }
        }
        private List<medCarInfoClass> ExecuteUDPDPPF0( List<medCarInfoClass> medCarInfoClasses)
        {
            DB2Connection MyDb2Connection = GetDB2Connection();
            try
            {
                MyDb2Connection.Open();
                return ExecuteUDPDPPF0(MyDb2Connection, medCarInfoClasses);
            }
            catch(Exception e)
            {
                Logger.Log($"Exception : {e.Message}");
                return null;
            }
            finally
            {
                MyDb2Connection.Close();
                MyDb2Connection.Dispose();
            }
        
        }
        private List<medCarInfoClass> ExecuteUDPDPPF0(DB2Connection MyDb2Connection, List<medCarInfoClass> medCarInfoClasses)
        {
            Logger.LogAddLine("ExecuteUDPDPPF0");
            MyTimerBasic myTimerBasic = new MyTimerBasic();
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
                        Logger.Log("Execute_SP", $"[UDPDPPF0]cmd.ExecuteReader() , {myTimerBasic}");
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
                        Logger.Log("Execute_SP", $"[UDPDPPF0]get result dictionary , {myTimerBasic}");

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
                                    //if (!string.IsNullOrWhiteSpace(value)) medCarInfoClass.年齡 = age(value);
                                    if (value.StringIsEmpty() == false) medCarInfoClass.年齡 = age(value);
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

                        Logger.Log("Execute_SP", $"[UDPDPPF0]get result done , {myTimerBasic}");

                    }
                }
            }
            Logger.LogAddLine("Execute_SP");
            return medCarInfoClasses;
        }
        private List<medCpoeClass> ExecuteUDPDPDSP(List<medCarInfoClass> medCarInfoClasses)
        {
            DB2Connection MyDb2Connection = GetDB2Connection();
            try
            {
                MyDb2Connection.Open();
                return ExecuteUDPDPDSP(MyDb2Connection, medCarInfoClasses);
            }
            catch (Exception e)
            {
                Logger.Log($"Exception : {e.Message}");
                return null;
            }
            finally
            {
                MyDb2Connection.Close();
                MyDb2Connection.Dispose();
            }

        }
        private List<medCpoeClass> ExecuteUDPDPDSP(DB2Connection MyDb2Connection ,List<medCarInfoClass> medCarInfoClasses)
        {
            Logger.LogAddLine("Execute_SP");
            MyTimerBasic myTimerBasic = new MyTimerBasic();
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
                    Logger.Log("Execute_SP", $"[UDPDPDSP]cmd.ExecuteReader() , {myTimerBasic}");

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = procName;
                    cmd.Parameters.Add("@TCASENO", DB2Type.VarChar, 8).Value = medCarInfoClass.住院號;
                    cmd.Parameters.Add("@TNURSTA", DB2Type.VarChar, 4).Value = medCarInfoClass.護理站;
                    cmd.Parameters.Add("@TTIME", DB2Type.VarChar, 4).Value = time;
                    DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                    using (DB2DataReader reader = cmd.ExecuteReader())
                    {
                        Logger.Log("Execute_SP", $"[UDPDPDSP]get result dictionary , {myTimerBasic}");

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
                                姓名 = medCarInfoClass.姓名,
                                PRI_KEY = reader["UDORDSEQ"].ToString().Trim(),                           
                                Master_GUID = medCarInfoClass.GUID,
                                更新時間 = updateTime,
                                住院號 = reader["UDCASENO"].ToString().Trim(),
                                病歷號 = medCarInfoClass.病歷號,
                                序號 = reader["UDORDSEQ"].ToString().Trim(),
                                開始時間 = 開始日期時間.ToDateTimeString_6(),
                                結束時間 = 結束日期時間.ToDateTimeString_6(),
                                藥碼 = reader["UDDRGNO"].ToString().Trim(),
                                頻次 = reader["UDFREQN"].ToString().Trim(),
                                //頻次屬性 = reader["UDFRQATR"].ToString().Trim(),
                                藥品名 = reader["UDDRGNAM"].ToString().Trim(),
                                途徑 = reader["UDROUTE"].ToString().Trim(),
                                數量 = reader["UDLQNTY"].ToString().Trim().StringToInt32().ToString(),
                                劑量 = reader["UDDOSAGE"].ToString().Trim(),
                                單位 = reader["UDDUNIT"].ToString().Trim(),
                                //期限 = reader["UDDURAT"].ToString().Trim(),
                                //自動包藥機 = reader["UDDSPMF"].ToString().Trim(),
                                //化癌分類 = reader["UDCHEMO"].ToString().Trim(),
                                自購 = reader["UDSELF"].ToString().Trim(),
                                //血液製劑註記 = reader["UDALBUMI"].ToString().Trim(),
                                處方醫師 = reader["UDORSIGN"].ToString().Trim(),
                                處方醫師姓名 = reader["UDSIGNAM"].ToString().Trim(),
                                操作人員 = reader["UDLUSER"].ToString().Trim(),
                                藥局代碼 = reader["UDLRXID"].ToString().Trim(),
                                大瓶點滴 = reader["UDCNT02"].ToString().Trim(),
                                LKFLAG = reader["UDBRFNM"].ToString().Trim(),
                                排序 = reader["UDRANK"].ToString().Trim(),
                                //判讀藥師代碼 = reader["PHARNUM"].ToString().Trim(),
                                //判讀FLAG = reader["FLAG"].ToString().Trim(),
                                勿磨 = reader["UDNGT"].ToString().Trim(),
                                //抗生素等級 = reader["UDANTICG"].ToString().Trim(),
                                重複用藥 = reader["UDSAMEDG"].ToString().Trim(),
                                //配藥天數 = reader["UDDSPDY"].ToString().Trim(),
                                //交互作用 = reader["UDDDI"].ToString().Trim(),
                                //交互作用等級 = reader["UDDDIC"].ToString().Trim()
                            };
                            if (medCpoeClass.劑量.StartsWith("X"))
                            {
                                if (medCpoeClass.劑量.Length == 2 || medCpoeClass.劑量.Length == 3)
                                {
                                    continue;
                                }
                            }
                            if (medCpoeClass.藥碼.Length == 5 && medCpoeClass.藥碼.StartsWith("8"))
                            {
                                medCpoeClass.公藥 = "Y";
                                medCpoeClass.調劑狀態 = "Y";
                            }
                            else
                            {
                                bool flag_公藥 = pubMed(medCpoeClass.藥碼);
                                if (flag_公藥)
                                {
                                    medCpoeClass.公藥 = "Y";
                                    medCpoeClass.調劑狀態 = "Y";
                                }
                            } 
                            if (iceMed(medCpoeClass.藥碼)) medCpoeClass.冷儲 = "Y";
                            if (medCpoeClass.藥品名.ToLower().Contains(" cap ") || medCpoeClass.藥品名.ToLower().Contains(" tab ")) medCpoeClass.口服 = "Y";
                            if (medCpoeClass.途徑 == "IVA" || medCpoeClass.途徑 == "IVD") medCpoeClass.針劑 = "Y";
                            if (medCpoeClass.途徑 == "PO") medCpoeClass.大瓶點滴 = "";                    
                            if (medCpoeClass.藥局代碼 == "" || medCpoeClass.藥局代碼 == "UC02" ) prescription.Add(medCpoeClass);

                        }
                    }
                }
            }
            Logger.Log("Execute_SP", $"[UDPDPDSP]get result done , {myTimerBasic}");

            return prescription;
        }
        private List<medCpoeRecClass> ExecuteUDPDPORD(List<medCarInfoClass> medCarInfoClasses) //處方歷程
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
                                    if (medCpoeRecClass.狀態.StringIsEmpty())
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
                            Logger.Log("ExecuteUDPDPORD", $"DB2Exception: {ex.Message}");
                        }

                    }
                }
                return medCpoeRecClasses;
            }

        }
        private List<bedStatusClass> ExecuteUDPDPBED(List<medCarInfoClass> medCarInfoClasses) //轉床紀錄
        {
            using (DB2Connection MyDb2Connection = GetDB2Connection())
            {
                MyDb2Connection.Open();
                string procName = $"{DB2_schema}.UDPDPBED";
                List<bedStatusClass> bedStatusClasses = new List<bedStatusClass>();
                foreach (var medCarInfoClass in medCarInfoClasses)
                {
                    if (medCarInfoClass.住院號.StringIsEmpty()) continue;
                    string time = DateTime.Now.ToString("HHmm");

                    using (DB2Command cmd = MyDb2Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = procName;
                        cmd.Parameters.Add("@TCASENO", DB2Type.VarChar, 8).Value = medCarInfoClass.住院號;
                        cmd.Parameters.Add("@TTIME1", DB2Type.VarChar, 4).Value = "0000";
                        cmd.Parameters.Add("@TTIME2", DB2Type.VarChar, 4).Value = time;
                        DB2Parameter RET = cmd.Parameters.Add("@RET", DB2Type.Integer);
                        try
                        {
                            using (DB2DataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {                 
                                    string 轉床 = reader["MOVETIME"].ToString().Trim();
                                    string 時間 = $"{轉床.Substring(0, 4)}-{轉床.Substring(4, 2)}-{轉床.Substring(6, 2)} {轉床.Substring(8, 2)}:{轉床.Substring(10, 2)}:{轉床.Substring(12, 2)}";
                                    string 轉床前護理站 = reader["STATION_OLD"].ToString().Trim();
                                    string 轉床前床號 = reader["BED_OLD"].ToString().Trim();
                                    string 轉床後護理站 = reader["STATION_NEW"].ToString().Trim();
                                    string 轉床後床號 = reader["BED_NEW"].ToString().Trim();
                                    bedStatusClass bedStatusClass = new bedStatusClass
                                    {
                                        GUID = Guid.NewGuid().ToString(),
                                        Master_GUID = medCarInfoClass.GUID,
                                        PRI_KEY = reader["MOVETIME"].ToString().Trim(), //轉床時間
                                        轉床時間 = 時間,
                                        姓名 = medCarInfoClass.姓名,
                                        住院號 = reader["UDCASENO"].ToString().Trim(),
                                        病歷號 = reader["UDHISTNO"].ToString().Trim(),
                                        轉床前護理站床號 = $"{轉床前護理站}-{轉床前床號}",
                                        轉床後護理站床號 = $"{轉床後護理站}-{轉床後床號}"
                                    };
                                    if (bedStatusClass.轉床前護理站床號 == "X000-0") bedStatusClass.狀態 = "轉入";
                                    if (bedStatusClass.轉床後護理站床號 == "X000-0") bedStatusClass.狀態 = "轉出";
                                    if (bedStatusClass.轉床前護理站床號 != "X000-0" && bedStatusClass.轉床後護理站床號 != "X000-0") bedStatusClass.狀態 = "轉床";



                                    bedStatusClasses.Add(bedStatusClass);

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"DB2Exception: {ex.Message}");
                            Logger.Log("ExecuteUDPDPORD", $"DB2Exception: {ex.Message}");
                        }

                    }
                }
                return bedStatusClasses;
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
        private (string Server, string DB, string UserName, string Password, uint Port) GetServerInfo(string Name, string Type, string Content)
        {
            List<sys_serverSettingClass> serverSettingClasses = sys_serverSettingClassMethod.WebApiGet("http://127.0.0.1:4433");
            sys_serverSettingClass serverSettingClass = serverSettingClasses.MyFind(Name, Type, Content).FirstOrDefault();
            if (serverSettingClass == null)
            {
                throw new Exception("找無Server資料");
            }
            return (serverSettingClass.Server, serverSettingClass.DBName, serverSettingClass.User, serverSettingClass.Password, (uint)serverSettingClass.Port.StringToInt32());
        }
        private string GetServerAPI(string Name, string Type, string Content)
        {
            List<sys_serverSettingClass> serverSettingClasses = sys_serverSettingClassMethod.WebApiGet("http://127.0.0.1:4433");
            sys_serverSettingClass serverSettingClass = serverSettingClasses.MyFind(Name, Type, Content).FirstOrDefault();
            if (serverSettingClass == null)
            {
                throw new Exception("找無Server資料");
            }
            return serverSettingClass.Server;
        }

        private bool pubMed(string code)
        {
            List<string> listPubMed = new List<string>() { "01970" };
            if (listPubMed.Contains(code))
            {
                return true;
            }
            else
            {
                return false;
            }           
        }
        private bool iceMed(string code)
        {
            //List<string> listIceMed = new List<string>() { "01990","03852","03809" };
            List<string> listIceMed = new List<string>
            {
                "02096", "05792", "00053", "04881", "03175", "04868", "05400", "05872",
                "05758", "03809", "04425", "05439", "01989", "01990", "01754", "00081",
                "03852", "02713", "05851", "00282", "05653", "01909", "01668", "03637",
                "03636", "06021", "03190", "05743", "01855", "05596", "06200"
            };
            if (listIceMed.Contains(code))
            {
                return true;
            }
            else
            {
                return false;
            }        
        }

        //public static byte[] ReadFile(string filePath)
        //{
        //    if (!System.IO.File.Exists(filePath))
        //    {
        //        throw new FileNotFoundException("檔案不存在", filePath);
        //    }
        //    return System.IO.File.ReadAllBytes(filePath);
        //}
        public enum enum_medManage
        {
            項次,
            藥品碼         
        }



    }
}
