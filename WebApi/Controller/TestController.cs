﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using System.Data;
using System.Configuration;
using Basic;
namespace DB2VM
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
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
       

            String MyDb2ConnectionString = $"server={DB2_server};database={DB2_database};userid={DB2_userid};password={DB2_password};";
            DB2Connection MyDb2Connection = new DB2Connection(MyDb2ConnectionString);
            try
            {
                MyDb2Connection.Open();
            }
            catch
            {
                return $"DB2 Connecting failed! , {MyDb2ConnectionString}";
            }
            MyDb2Connection.Close();
            MyDb2Connection.Dispose();
            return $"DB2 Connecting sucess! , {MyDb2ConnectionString}";


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
                cmd.CommandText = "SELECT * FROM VGHLNXVG.DIMAUTRN where DIMDATE >='20240820' with ur;";
                cmd.ExecuteReader();
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
