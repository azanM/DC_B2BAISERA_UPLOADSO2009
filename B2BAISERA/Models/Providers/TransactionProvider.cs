using System;
using System.Collections.Generic;
using System.Linq;
//using B2BAISERA.Models.DataAccess;
//using B2BAISERA.Helper;
//using B2BAISERA.Logic;
using Microsoft.Practices.Unity;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Common;
using B2BAISERA.Models.EFServer;
using B2BAISERA.Helper;
using System.Data.EntityClient;
using System.Data;
using B2BAISERA.B2BAIWsDMZ;
using System.Globalization;
using B2BAISERA.Log;
using System.Diagnostics;

namespace B2BAISERA.Models.Providers
{
    public class TransactionProvider : DataAccessBase
    {
        public TransactionProvider()
            : base()
        {
        }

        public TransactionProvider(EProcEntities context)
            : base(context)
        {
        }

        #region MAIN
        //B2BAISERAEntities ctx = new B2BAISERAEntities(Repository.ConnectionStringEF);

        //public User GetUser(string userName, string password, string clientTag)
        //{
        //    var User = (from o in ctx.Users
        //                where o.UserName == userName && o.Password == password && o.ClientTag == clientTag
        //                select o).FirstOrDefault();

        //    return User;
        //}

        public CUSTOM_USER GetUser(string userName, string password, string clientTag)
        {
            var user = (from o in entities.CUSTOM_USER
                        where o.UserName == userName && o.Password == password && o.ClientTag == clientTag
                        select o).FirstOrDefault();

            return user;
        }

        public string GetLastTicketNo(string fileType)
        {
            var result = "";
            try
            {
                var query = (entities.CUSTOM_LOG
                    .Where(log => (log.Acknowledge == true) && (log.FileType == fileType))
                    .Select(log => new LogViewModel
                    {
                        ID = log.ID,
                        WebServiceName = log.WebServiceName,
                        MethodName = log.MethodName,
                        TicketNo = log.TicketNo,
                        Message = log.Message
                    })
                    ).OrderByDescending(log => log.ID).FirstOrDefault();

                result = query != null ? Convert.ToString(query.TicketNo) : "";
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        #endregion

        #region UPLOAD

        #region S02009
        public List<CUSTOM_S02009_TEMP_HS> CreatePOSeraToAI_HS()
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02009_TEMP_HS> listTempHS = new List<CUSTOM_S02009_TEMP_HS>();
            try
            {
                listTempHS = entities.sp_CreatePOSeraS02009ToAI_HS().ToList();
                return listTempHS;
            }
            catch (Exception ex)
            {
               
                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.StackTrace, "S02009", "SERA");
                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.InnerException.Message, "S02009", "SERA");
                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.InnerException.StackTrace, "S02009", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                //end
                throw ex;
            }
        }

        public List<CUSTOM_S02009_TEMP_IS> CreatePOSeraToAI_IS()
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02009_TEMP_IS> listTempIS = new List<CUSTOM_S02009_TEMP_IS>();
            try
            {
                listTempIS = entities.sp_CreatePOSeras02009ToAI_IS().ToList();
                return listTempIS;
            }
            catch (Exception ex)
            {
              
                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.Message + "error get data for insert CUSTOM_S02009_TEMP_IS", "S02009", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                //end
                throw ex;
            }
        }

        public int InsertLogTransaction(List<CUSTOM_S02009_TEMP_HS> listTempHS, List<CUSTOM_S02009_TEMP_IS> listTempIS)
        {
            LogEvent logEvent = new LogEvent();
            int result = 0;
            try
            {
                if (listTempHS.Count > 0)
                {
                    //insert into CUSTOM_TRANSACTION
                    CUSTOM_TRANSACTION transaction = new CUSTOM_TRANSACTION();
                    transaction.TicketNo = "";
                    transaction.ClientTag = "";
                    EntityHelper.SetAuditForInsert(transaction, "SERA");
                    entities.CUSTOM_TRANSACTION.AddObject(transaction);

                    var countListTempHS = listTempHS.Count;
                    var countListTempIS = listTempIS.Count;

                    for (int i = 0; i < listTempHS.Count; i++)
                    {
                        //insert into CUSTOM_TRANSACTIONDATA
                        CUSTOM_TRANSACTIONDATA transactionData = new CUSTOM_TRANSACTIONDATA();
                        transactionData.CUSTOM_TRANSACTION = transaction;
                        transactionData.TransGUID = Guid.NewGuid().ToString();
                        transactionData.DocumentNumber = listTempHS[i].PONumber;
                        transactionData.FileType = "S02009";
                        transactionData.IPAddress = "118.97.80.12"; //IP ADDRESS KOMP SERVER, dan HARUS TERDAFTAR DI DB AI
                        transactionData.DestinationUser = "AI";
                        transactionData.Key1 = listTempHS[i].CompanyCodeAI;
                        transactionData.Key2 = listTempHS[i].KodeCabangAI;
                        //transactionData.Key1 = "0003";
                        //transactionData.Key2 = "D004";
                        transactionData.Key3 = "";
                        transactionData.DataLength = null;
                        transactionData.RowStatus = "";
                        EntityHelper.SetAuditForInsert(transactionData, "SERA");
                        entities.CUSTOM_TRANSACTIONDATA.AddObject(transactionData);

                        //CHECK IF DATA HS BY PONUMBER SUDAH ADA, DELETE DULU BY ID, supaya tidak redundant ponumber nya
                        var poNumb = listTempHS[i].PONumber;
                        var query = (from o in entities.CUSTOM_S02009_HS
                                     where o.PONumber == poNumb
                                     select o).ToList();
                        if (query.Count > 0)
                        {
                            for (int d = 0; d < query.Count; d++)
                            {
                                //delete
                                var delID = query[d].ID;
                                CUSTOM_S02009_HS delHS = entities.CUSTOM_S02009_HS.Single(o => o.ID == delID);
                                entities.CUSTOM_S02009_HS.DeleteObject(delHS);


                            }
                        }

                        //insert into CUSTOM_S02009_HS
                        CUSTOM_S02009_HS DataDetailHS = new CUSTOM_S02009_HS();
                        DataDetailHS.CUSTOM_TRANSACTIONDATA = transactionData;
                        DataDetailHS.PONumber = listTempHS[i].PONumber;
                        DataDetailHS.VersionPOSERA = listTempHS[i].VersionPOSERA;
                        DataDetailHS.DataVersion = listTempHS[i].DataVersion;

                        

                        //start add identitas penambahan row custom_s02009_hs 
                        DataDetailHS.dibuatOleh = "system";
                        DataDetailHS.dibuatTanggal = DateTime.Now;
                        DataDetailHS.diubahOleh = "system";
                        DataDetailHS.diubahTanggal = DateTime.Now;
                        //end
                        entities.CUSTOM_S02009_HS.AddObject(DataDetailHS);

                        //build HS separator
                        var strHS = ConcateStringHS(listTempHS[i]);

                        //insert into CUSTOM_TRANSACTIONDATADETAIL for HS
                        CUSTOM_TRANSACTIONDATADETAIL transactionDataDetail = new CUSTOM_TRANSACTIONDATADETAIL();
                        transactionDataDetail.CUSTOM_TRANSACTIONDATA = transactionData;
                        transactionDataDetail.Data = strHS;
                        transactionDataDetail.dibuatOleh = "system";
                        transactionDataDetail.dibuatTanggal = DateTime.Now;
                        transactionDataDetail.diubahOleh = "system";
                        transactionDataDetail.diubahTanggal = DateTime.Now;
                        //end
                        entities.CUSTOM_TRANSACTIONDATADETAIL.AddObject(transactionDataDetail);

                        if (listTempIS != null)
                        {
                            for (int j = 0; j < countListTempIS; j++)
                            {
                                if (listTempIS[j].PONumber == listTempHS[i].PONumber)
                                {
                                    //CHECK IF DATA IS BY PONUMBER SUDAH ADA, DELETE DULU BY ID
                                    var poNumbIS = listTempIS[j].PONumber;
                                    var queryIS = (from o in entities.CUSTOM_S02009_IS
                                                   where o.PONumber == poNumbIS
                                                   select o).ToList();
                                    if (queryIS.Count > 0)
                                    {
                                        for (int d = 0; d < queryIS.Count; d++)
                                        {
                                            //delete
                                            var delIDIS = queryIS[d].ID;
                                            CUSTOM_S02009_IS delIS = entities.CUSTOM_S02009_IS.Single(o => o.ID == delIDIS);
                                            entities.CUSTOM_S02009_IS.DeleteObject(delIS);
                                        }
                                    }

                                    //insert into CUSTOM_S02009_IS
                                    CUSTOM_S02009_IS DataDetailIS = new CUSTOM_S02009_IS();
                                    DataDetailIS.CUSTOM_TRANSACTIONDATA = transactionData;
                                    DataDetailIS.PONumber = listTempIS[j].PONumber;
                                    DataDetailIS.VersionPOSERA = listTempIS[j].VersionPOSERA;
                                    DataDetailIS.DataVersionAI = listTempIS[j].DataVersionAI;
                                    DataDetailIS.ChassisNumberByVendor = listTempIS[j].ChassisNumberByVendor;
                                    DataDetailIS.TGLGRSAP = listTempIS[j].TGLGRSAP;
                                    DataDetailIS.dibuatOleh = "system";
                                    DataDetailIS.dibuatTanggal = DateTime.Now;
                                    DataDetailIS.diubahOleh = "system";
                                    DataDetailIS.diubahTanggal = DateTime.Now;
                                    //end
                                    entities.CUSTOM_S02009_IS.AddObject(DataDetailIS);

                                    //build IS separator
                                    var strIS = ConcateStringIS(listTempIS[j]);

                                    //insert into CUSTOM_TRANSACTIONDATADETAIL for IS
                                    transactionDataDetail = new CUSTOM_TRANSACTIONDATADETAIL();
                                    transactionDataDetail.CUSTOM_TRANSACTIONDATA = transactionData;
                                    transactionDataDetail.Data = strIS;

                                   
                                    transactionDataDetail.dibuatOleh = "system";
                                    transactionDataDetail.dibuatTanggal = DateTime.Now;
                                    transactionDataDetail.diubahOleh = "system";
                                    transactionDataDetail.diubahTanggal = DateTime.Now;
                                    //end

                                    entities.CUSTOM_TRANSACTIONDATADETAIL.AddObject(transactionDataDetail);     
                                }
                            }
                        }
                    }
                    entities.SaveChanges();
                    //delete temp table 
                    for (int y = 0; y < countListTempHS; y++)
                    {
                        DeleteTempHS(listTempHS[y]);
                    }
                    for (int z = 0; z < countListTempIS; z++)
                    {
                        DeleteTempIS(listTempIS[z]);
                    }
                    result = 1;
                }
            }
            catch (Exception ex)
            {
                
                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.Message, "S02009", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                //end
                throw ex;
            }
            return result;
        }

        public TransactionViewModel GetTransaction()
        {
            LogEvent logEvent = new LogEvent();
            TransactionViewModel transaction = null;
            try
            {
                DateTime dateNow = DateTime.Now.Date;
                transaction = (from h in entities.CUSTOM_TRANSACTION
                               join d in entities.CUSTOM_TRANSACTIONDATA
                               on h.ID equals d.TransactionID
                               where d.FileType == "S02009" && h.CreatedWhen >= dateNow
                               select new TransactionViewModel()
                               {
                                   ID = h.ID
                               }).OrderByDescending(z => z.ID).FirstOrDefault();
            }
            catch (Exception ex)
            {

                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.Message, "S02009", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                //end
                throw ex;
            }
            return transaction;
        }

        public List<TransactionDataViewModel> GetTransactionData(int? transactionID)
        {
            List<TransactionDataViewModel> transactionData = null;
            try
            {
                transactionData = (entities.CUSTOM_TRANSACTIONDATA
                                   .Where(o => o.TransactionID == transactionID)
                                   .Select(o => new TransactionDataViewModel
                                   {
                                       ID = o.ID,
                                       TransactionID = o.TransactionID,
                                       TransGUID = o.TransGUID,
                                       DocumentNumber = o.DocumentNumber,
                                       FileType = o.FileType,
                                       IPAddress = o.IPAddress,
                                       DestinationUser = o.DestinationUser,
                                       Key1 = o.Key1,
                                       Key2 = o.Key2,
                                       Key3 = o.Key3,
                                       DataLength = o.DataLength,
                                       TransStatus = o.RowStatus,
                                       CreatedWho = o.CreatedWho,
                                       CreatedWhen = o.CreatedWhen,
                                       ChangedWho = o.ChangedWho,
                                       ChangedWhen = o.ChangedWhen
                                   }).ToList());
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return transactionData;
        }

        public WsDev.TransactionData[] GetTransactionDataArray(int? transactionID)
        {
            LogEvent logEvent = new LogEvent();
            WsDev.TransactionData[] transactionData = null;
            try
            {
                transactionData = (from o in entities.CUSTOM_TRANSACTIONDATA
                                  
                                   where o.TransactionID == transactionID
                                   select new WsDev.TransactionData
                                   {
                                       ID = o.ID,
                                       TransGUID = o.TransGUID,
                                       DocumentNumber = o.DocumentNumber,
                                       FileType = o.FileType,
                                       IPAddress = o.IPAddress,
                                       DestinationUser = o.DestinationUser,
                                       Key1 = o.Key1,
                                       Key2 = o.Key2,
                                       Key3 = o.Key3,
                                     
                                   }).ToArray();
            }
            catch (Exception ex)
            {

                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.Message, "S02009", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                //end
                throw ex;
            }
            return transactionData;
        }

        private string[] GetArrayOfStringData()
        {
            string[] arr = new string[100];
            List<string> list = new List<string>(arr);

            string[] arrData1 = new string[] 
            { 
                "HS|F-30C|A001CUA13000999|782725A9-5F3E-4362-A010-3A08BAB1DD11|20130620|20130620|T0A0|20130620|IDR|1|06|A001PQA13000131",
                "IS|01|7006038032||275000|K0||Z000|A001|20130620||||A001PUA13000114|A001PQA13000131/7006038032/Agustari Wira |1030000000||||1080202000||7006038032|||Agustari Wira |Jl KAYA RAYA|JAKARTA UTARA|/AGRAGUST5504//|||||275000|",
                "IS|50|2140322000||25000|K2|||A001||20130620|||A001PUA13000114|A001PUA13000114/7006038032/Agustari Wira |2140322000||||2140322000||||||||/AGRAGUST5504//|||||25000|",
                "IS|50|3000202001||250000|K0|||A001||20130620|||A001PUA13000114|A001PQA13000131/7006038032/Agustari Wira |3000202001||910-A0-001||3000202001|15601-BZ010|7006038032|09|1,00||||/AGRAGUST5504//|||||250000|02"
            };
            return arrData1;
        }

        public List<S02009HSViewModel> GetTransactionDataDetailHS(int? transactionDataID)
        {
            LogEvent logEvent = new LogEvent();
            List<S02009HSViewModel> dataHS = null;
            try
            {
                dataHS = (entities.CUSTOM_S02009_HS
                          .Where(o => o.TransactionDataID == transactionDataID)
                          .Select(o => new S02009HSViewModel
                          {
                              ID = o.ID,
                              TransactionDataID = o.TransactionDataID,
                              PONumber = o.PONumber,
                             VersionPOSERA =o.VersionPOSERA,
                             DataVersion =o.DataVersion

                          }).ToList());
            }
            catch (Exception ex)
            {
               
                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.Message, "S02009", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                //end
                throw ex;
            }
            return dataHS;
        }

        public List<S02009ISViewModel> GetTransactionDataDetailIS(int? transactionDataID)
        {
            LogEvent logEvent = new LogEvent();
            List<S02009ISViewModel> dataIS = null;
            try
            {
                dataIS = (entities.CUSTOM_S02009_IS
                          .Where(o => o.TransactionDataID == transactionDataID)
                          .Select(o => new S02009ISViewModel
                          {
                              ID = o.ID,
                              TransactionDataID = (int)(!o.TransactionDataID.HasValue ? 0 : o.TransactionDataID),
                              PONumber = o.PONumber,
                              VersionPOSERA =o.VersionPOSERA,
                              DataVersion =o.DataVersionAI,
                              ChassisNumberByVendor = o.ChassisNumberByVendor,
                              GRDATE =o.TGLGRSAP
                          }).ToList());
            }
            catch (Exception ex)
            {
                
                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.Message, "S02009", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                //end
                throw ex;
            }
            return dataIS;
        }

        private string ConcateStringHS(CUSTOM_S02009_TEMP_HS tempHS)
        {
            StringBuilder strHS = new StringBuilder(1000);
            strHS.Append("HS|");
            strHS.Append(tempHS.PONumber);
            strHS.Append("|");
           
            strHS.Append(tempHS.VersionPOSERA == null ? "" : tempHS.VersionPOSERA.ToString());
            strHS.Append("|");
            strHS.Append(tempHS.DataVersion == null ? "" : tempHS.DataVersion.ToString());
            

            return strHS.ToString();
        }

        public string ConcateStringHS(S02009HSViewModel HS)
        {
            StringBuilder strHS = new StringBuilder(1000);
            strHS.Append("HS|");
            strHS.Append(HS.PONumber);
            strHS.Append("|");

            strHS.Append(HS.VersionPOSERA == null ? "" : HS.VersionPOSERA.ToString());
            strHS.Append("|");
            strHS.Append(HS.DataVersion == null ? "" : HS.DataVersion.ToString());


            return strHS.ToString();
        }

        private string ConcateStringIS(CUSTOM_S02009_TEMP_IS tempIS)
        {
            StringBuilder strIS = new StringBuilder(1000);
            strIS.Append("IS|");
            strIS.Append(tempIS.PONumber);
            strIS.Append("|");
           
            strIS.Append(tempIS.VersionPOSERA == null ? "" : Convert.ToString(tempIS.VersionPOSERA));
            strIS.Append("|");
            strIS.Append(tempIS.DataVersionAI == null ? "" : Convert.ToString(tempIS.DataVersionAI));
            strIS.Append("|");
            strIS.Append(tempIS.ChassisNumberByVendor);
            strIS.Append("|");
            strIS.Append(tempIS.TGLGRSAP == null ? "19000101" : string.Format("{0:yyyyMMddhhmmss}", tempIS.TGLGRSAP));
            return strIS.ToString();
        }

        public string ConcateStringIS(S02009ISViewModel IS)
        {
            StringBuilder strIS = new StringBuilder(1000);
            strIS.Append("IS|");
            strIS.Append(IS.PONumber);
            strIS.Append("|");

            strIS.Append(IS.VersionPOSERA == null ? "" : Convert.ToString(IS.VersionPOSERA));
            strIS.Append("|");
            strIS.Append(IS.DataVersion == null ? "" : Convert.ToString(IS.DataVersion));
            strIS.Append("|");
            strIS.Append(IS.ChassisNumberByVendor);
            strIS.Append("|");
            strIS.Append(IS.GRDATE == null ? "19000101" : string.Format("{0:yyyyMMddhhmmss}", IS.GRDATE));
            return strIS.ToString();
        }

        public int DeleteTempHS(CUSTOM_S02009_TEMP_HS tempHS)
        {
            int result = 1;
            try
            {
                EntityCommand cmd = new EntityCommand("EProcEntities.sp_DeleteTempHS_s02009", entityConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("PONUMBER", DbType.String).Value = tempHS.PONumber;
                OpenConnection();
                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                result = 0;

                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        private int DeleteTempIS(CUSTOM_S02009_TEMP_IS tempIS)
        {
            int result = 1;
            try
            {
                EntityCommand cmd = new EntityCommand("EProcEntities.sp_DeleteTemps02009IS", entityConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("PONUMBER", DbType.String).Value = tempIS.PONumber;
                OpenConnection();
                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                result = 0;

                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public bool EqualS02009HS(S02009HSViewModel item1, S02009HSViewModel item2)
        {
            //jika value sama return true, jika value beda return false
            if (item1 == null && item2 == null)
                return true;
            else if ((item1 != null && item2 == null) || (item1 == null && item2 != null))
                return false;

            var PONumber1 = !string.IsNullOrEmpty(item1.PONumber) ? item1.PONumber : "";
           
          
            var PONumber2 = !string.IsNullOrEmpty(item2.PONumber) ? item2.PONumber : "";
           


            return PONumber1.Equals(PONumber2);
        }

        public bool EqualS02009IS(S02009ISViewModel item1, S02009ISViewModel item2)
        {
            //jika value sama return true, jika value beda return false
            if (item1 == null && item2 == null)
                return true;
            else if ((item1 != null && item2 == null) || (item1 == null && item2 != null))
                return false;

            var PONumber1 = !string.IsNullOrEmpty(item1.PONumber) ? item1.PONumber : "";
            var ChassisNumberByVendor1 = !string.IsNullOrEmpty(item1.ChassisNumberByVendor) ? item1.ChassisNumberByVendor : "";
            var GRDATE1 = item1.GRDATE != null ? item1.GRDATE : Convert.ToDateTime("01/01/1900");

            var PONumber2 = !string.IsNullOrEmpty(item2.PONumber) ? item2.PONumber : "";
            var ChassisNumberByVendor2 = !string.IsNullOrEmpty(item2.ChassisNumberByVendor) ? item2.ChassisNumberByVendor : "";
            var GRDATE2 = item2.GRDATE != null ? item2.GRDATE : Convert.ToDateTime("01/01/1900");


            return PONumber1.Equals(PONumber2) &&
                ChassisNumberByVendor1.Equals(ChassisNumberByVendor2) &&
                GRDATE1.Equals(GRDATE2);
        }

        public List<CUSTOM_S02009_TEMP_HS> CheckingHistoryHSIS(List<CUSTOM_S02009_TEMP_HS> tempHS, List<CUSTOM_S02009_TEMP_IS> tempIS)
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02009_TEMP_HS> listDataHS = new List<CUSTOM_S02009_TEMP_HS>();
            List<CUSTOM_S02009_TEMP_IS> listDataIS = new List<CUSTOM_S02009_TEMP_IS>();
            List<S02009HSISViewModel> listDataHSIS = new List<S02009HSISViewModel>();
            try
            {
                if (tempHS.Count > 0)
                {
                    listDataHS = tempHS;
                    var existingRowHS = (from o in tempHS
                                         where entities.CUSTOM_S02009_IS.Any(e => o.PONumber == e.PONumber)
                                         select new S02009HSViewModel
                                         {
                                             PONumber = o.PONumber,
                                             VersionPOSERA = o.VersionPOSERA,
                                             DataVersion = o.DataVersion
                                         }).ToList();

                    for (int i = 0; i < existingRowHS.Count; i++)
                    {
                        string existPoNumber = existingRowHS[i].PONumber;
                        var q = (from o in entities.CUSTOM_S02009_HS
                                 where o.PONumber == existPoNumber
                                 select new S02009HSViewModel
                                 {
                                     PONumber = o.PONumber,
                                     VersionPOSERA = o.VersionPOSERA,
                                     DataVersion = o.DataVersion
                                 }).OrderByDescending(o => o.DataVersion).SingleOrDefault();

                        var compareHS = EqualS02009HS(existingRowHS[i], q);

                        // jika di HS sama persis tiap fieldnya, cek di IS nya
                        if (compareHS)
                        {
                            //compareIS
                            if (tempIS.Count > 0)
                            {
                                listDataIS = tempIS;
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02009_IS.Any(e => o.PONumber == e.PONumber)
                                                        && o.PONumber == existPoNumber
                                                     select new S02009ISViewModel
                                                     {
                                                         PONumber = o.PONumber,
                                                         VersionPOSERA = o.VersionPOSERA,
                                                         DataVersion = o.DataVersionAI,
                                                         ChassisNumberByVendor = o.ChassisNumberByVendor,
                                                         GRDATE =o.TGLGRSAP
                                                     }).ToList();

                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].PONumber == existingRowHS[i].PONumber)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumber;
                                        var z = (from o in entities.CUSTOM_S02009_IS
                                                 where o.PONumber == existPoNumberIS
                                                 select new S02009ISViewModel
                                                 {
                                                     PONumber = o.PONumber,
                                                   
                                                     VersionPOSERA = o.VersionPOSERA,
                                                     DataVersion = o.DataVersionAI,
                                                     ChassisNumberByVendor = o.ChassisNumberByVendor,
                                                     GRDATE = o.TGLGRSAP
                                                 }).OrderByDescending(o => o.DataVersion).SingleOrDefault();
                                        var compareIS = EqualS02009IS(existingRowIS[j], z);

                                        //jika di HS dan IS sama persis tiap fieldnya, maka delete row di list
                                        if (compareIS)
                                        {
                                            //remove row di HS dan IS
                                            var rowDelHS = (from o in listDataHS
                                                            where o.PONumber == existingRowHS[i].PONumber
                                                            select o).SingleOrDefault();
                                            listDataHS.Remove(rowDelHS);

                                         
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);
                                        }

                                        //jika di HS sama persis tapi di IS ada yag berubah, maka update row di list IS saja
                                        else if (!compareIS)
                                        {
                                            //NEW SIT
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02009_TEMP_HS row = new CUSTOM_S02009_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            row.DataVersion = existingRowHS[i].DataVersion;

                                            listDataHS.Add(row);
                                          
                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02009_TEMP_IS rowIS = new CUSTOM_S02009_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            rowIS.DataVersionAI = existingRowHS[i].DataVersion;
                                           
                                            rowIS.ChassisNumberByVendor = existingRowIS[j].ChassisNumberByVendor;
                                            rowIS.TGLGRSAP = existingRowIS[j].GRDATE;
                                            listDataIS.Add(rowIS);
                                        }
                                    }
                                }
                            }
                        }
                        //jika di HS ada yg berubah di salah satu field atau lebih, maka cek di IS nya 
                        else if (!compareHS)
                        {
                            //compareIS
                            if (tempIS.Count > 0)
                            {
                                listDataIS = tempIS;
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02009_IS.Any(e => o.PONumber == e.PONumber)
                                                        && o.PONumber == existPoNumber
                                                     select new S02009ISViewModel
                                                     {
                                                         PONumber = o.PONumber,
                                                      
                                                         VersionPOSERA = o.VersionPOSERA,
                                                         DataVersion = o.DataVersionAI,
                                                         ChassisNumberByVendor = o.ChassisNumberByVendor,
                                                         GRDATE = o.TGLGRSAP
                                                     }).ToList();

                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].PONumber == existingRowHS[i].PONumber)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumber;
                                        var z = (from o in entities.CUSTOM_S02009_IS
                                                 where o.PONumber == existPoNumberIS
                                                 select new S02009ISViewModel
                                                 {
                                                     PONumber = o.PONumber,
                                                   
                                                     VersionPOSERA = o.VersionPOSERA,
                                                     DataVersion = o.DataVersionAI,
                                                     ChassisNumberByVendor = o.ChassisNumberByVendor,
                                                     GRDATE = o.TGLGRSAP
                                                 }).OrderByDescending(o => o.DataVersion).SingleOrDefault();
                                        var compareIS = EqualS02009IS(existingRowIS[j], z);

                                        //jika di HS ada yg berubah tapi di IS sama persis tiap fieldnya, maka update row di list HS saja
                                        if (compareIS)
                                        {
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);
                                          
                                            //add new row HS
                                            CUSTOM_S02009_TEMP_HS row = new CUSTOM_S02009_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            row.DataVersion = existingRowHS[i].DataVersion;
                                            //NEW SIT
                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02009_TEMP_IS rowIS = new CUSTOM_S02009_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            rowIS.DataVersionAI = existingRowHS[i].DataVersion;
                                            //rowIS.DataVersion = z.DataVersion != null ? z.DataVersion + 1 : existingRowIS[j].DataVersion != null ? existingRowIS[j].DataVersion : 1;
                                            rowIS.ChassisNumberByVendor = existingRowIS[j].ChassisNumberByVendor;
                                            rowIS.TGLGRSAP = existingRowIS[j].GRDATE;
                                            listDataIS.Add(rowIS);
                                            //END: NEW SIT
                                        }

                                        //jika di HS ada yg berubah dan di IS pun ada yag berubah, maka update row di list HS dan IS
                                        else if (!compareIS)
                                        {
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02009_TEMP_HS row = new CUSTOM_S02009_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            row.DataVersion = existingRowHS[i].DataVersion;
                                            listDataHS.Add(row);

                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02009_TEMP_IS rowIS = new CUSTOM_S02009_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            rowIS.DataVersionAI = existingRowHS[i].DataVersion;
                                            //rowIS.DataVersion = z.DataVersion != null ? z.DataVersion + 1 : existingRowIS[j].DataVersion != null ? existingRowIS[j].DataVersion : 1;
                                            rowIS.ChassisNumberByVendor = existingRowIS[j].ChassisNumberByVendor;
                                            rowIS.TGLGRSAP = existingRowIS[j].GRDATE;
                                            listDataIS.Add(rowIS);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               
                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.Message, "S02009", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                //end
                throw ex;
            }
            return listDataHS;
        }

        public List<CUSTOM_S02009_TEMP_IS> CheckingHistoryHSIS2(List<CUSTOM_S02009_TEMP_HS> tempHS, List<CUSTOM_S02009_TEMP_IS> tempIS)
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02009_TEMP_HS> listDataHS = new List<CUSTOM_S02009_TEMP_HS>();
            List<CUSTOM_S02009_TEMP_IS> listDataIS = new List<CUSTOM_S02009_TEMP_IS>();
            List<S02009HSISViewModel> listDataHSIS = new List<S02009HSISViewModel>();
            try
            {
                if (tempHS.Count > 0)
                {
                    listDataHS = tempHS;
                    listDataIS = tempIS;
                    var existingRowHS = (from o in tempHS
                                         where entities.CUSTOM_S02009_HS.Any(e => o.PONumber == e.PONumber)
                                         select new S02009HSViewModel
                                         {
                                             PONumber = o.PONumber,
                                             VersionPOSERA = o.VersionPOSERA,
                                             DataVersion = o.DataVersion
                                         }).ToList();
                    for (int i = 0; i < existingRowHS.Count; i++)
                    {
                        string existPoNumber = existingRowHS[i].PONumber;
                        var q = (from o in entities.CUSTOM_S02009_HS
                                 where o.PONumber == existPoNumber
                                 select new S02009HSViewModel
                                 {
                                     PONumber = o.PONumber,
                                     VersionPOSERA = o.VersionPOSERA,
                                     DataVersion = o.DataVersion
                                 }).OrderByDescending(o => o.DataVersion).SingleOrDefault();

                        var compareHS = EqualS02009HS(existingRowHS[i], q);

                        // jika di HS sama persis tiap fieldnya, cek di IS nya
                        if (compareHS)
                        {
                            //compareIS
                            if (tempIS.Count > 0)
                            {
                                listDataIS = tempIS;
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02009_IS.Any(e => o.PONumber == e.PONumber)
                                                     select new S02009ISViewModel
                                                     {
                                                         PONumber = o.PONumber,
                                                       
                                                         VersionPOSERA = o.VersionPOSERA,
                                                         DataVersion = o.DataVersionAI,
                                                         ChassisNumberByVendor = o.ChassisNumberByVendor,
                                                         GRDATE = o.TGLGRSAP
                                                     }).ToList();

                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].PONumber == existingRowHS[i].PONumber)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumber;
                                        var z = (from o in entities.CUSTOM_S02009_IS
                                                 where o.PONumber == existPoNumberIS
                                                 select new S02009ISViewModel
                                                 {
                                                     PONumber = o.PONumber,
                                                     VersionPOSERA = o.VersionPOSERA,
                                                     DataVersion = o.DataVersionAI,
                                                     ChassisNumberByVendor = o.ChassisNumberByVendor,
                                                     GRDATE = o.TGLGRSAP
                                                 }).OrderByDescending(o => o.DataVersion).SingleOrDefault();
                                        var compareIS = EqualS02009IS(existingRowIS[j], z);

                                        //jika di HS dan IS sama persis tiap fieldnya, maka delete row di list
                                        if (compareIS)
                                        {
                                            //remove row di HS dan IS
                                            var rowDelHS = (from o in listDataHS
                                                            where o.PONumber == existingRowHS[i].PONumber
                                                            select o).SingleOrDefault();
                                            listDataHS.Remove(rowDelHS);

                                            //DeleteTempHSByPoNumber(existingRowHS[i].PONumber);

                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //DeleteTempISByPoNumber(existingRowIS[i].PONumber);
                                        }

                                        //jika di HS sama persis tapi di IS ada yag berubah, maka update row di list IS saja
                                        else if (!compareIS)
                                        {
                                            //NEW SIT
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02009_TEMP_HS row = new CUSTOM_S02009_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            row.DataVersion = existingRowHS[i].DataVersion;
                                            listDataHS.Add(row);
                                            //END: NEW SIT

                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02009_TEMP_IS rowIS = new CUSTOM_S02009_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            rowIS.DataVersionAI = existingRowHS[i].DataVersion;
                                            //rowIS.DataVersion = z.DataVersion != null ? z.DataVersion + 1 : existingRowIS[j].DataVersion != null ? existingRowIS[j].DataVersion : 1;
                                            rowIS.ChassisNumberByVendor = existingRowIS[j].ChassisNumberByVendor;
                                            rowIS.TGLGRSAP = existingRowIS[j].GRDATE;
                                            listDataIS.Add(rowIS);
                                        }
                                    }
                                }
                            }
                        }
                        //jika di HS ada yg berubah di salah satu field atau lebih, maka cek di IS nya 
                        else if (!compareHS)
                        {
                            //compareIS
                            if (tempIS.Count > 0)
                            {
                                //listDataIS = tempIS;
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02009_IS.Any(e => o.PONumber == e.PONumber)
                                                     select new S02009ISViewModel
                                                     {
                                                         PONumber = o.PONumber,
                                                       
                                                         VersionPOSERA = o.VersionPOSERA,
                                                         DataVersion = o.DataVersionAI,
                                                         ChassisNumberByVendor = o.ChassisNumberByVendor,
                                                         GRDATE = o.TGLGRSAP
                                                     }).ToList();

                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].PONumber == existingRowHS[i].PONumber)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumber;
                                        var z = (from o in entities.CUSTOM_S02009_IS
                                                 where o.PONumber == existPoNumberIS
                                                 select new S02009ISViewModel
                                                 {
                                                     PONumber = o.PONumber,
                                                    
                                                     VersionPOSERA = o.VersionPOSERA,
                                                     DataVersion = o.DataVersionAI,
                                                     ChassisNumberByVendor = o.ChassisNumberByVendor,
                                                     GRDATE = o.TGLGRSAP
                                                 }).OrderByDescending(o => o.DataVersion).SingleOrDefault();
                                        var compareIS = EqualS02009IS(existingRowIS[j], z);

                                        //jika di HS ada yg berubah tapi di IS sama persis tiap fieldnya, maka update row di list HS saja
                                        if (compareIS)
                                        {
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02009_TEMP_HS row = new CUSTOM_S02009_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            row.DataVersion = existingRowHS[i].DataVersion;
                                            listDataHS.Add(row);

                                            //NEW SIT
                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02009_TEMP_IS rowIS = new CUSTOM_S02009_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            rowIS.DataVersionAI = existingRowHS[i].DataVersion;
                                            rowIS.ChassisNumberByVendor = existingRowIS[j].ChassisNumberByVendor;
                                            rowIS.TGLGRSAP = existingRowIS[j].GRDATE;
                                            listDataIS.Add(rowIS);
                                            //END: NEW SIT
                                        }

                                        //jika di HS ada yg berubah dan di IS pun ada yag berubah, maka update row di list HS dan IS
                                        else if (!compareIS)
                                        {
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.PONumber == existingRowHS[i].PONumber
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02009_TEMP_HS row = new CUSTOM_S02009_TEMP_HS();
                                            row.PONumber = existingRowHS[i].PONumber;
                                            row.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            row.DataVersion = existingRowHS[i].DataVersion;
                                            listDataHS.Add(row);

                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumber == existingRowIS[j].PONumber
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            CUSTOM_S02009_TEMP_IS rowIS = new CUSTOM_S02009_TEMP_IS();
                                            rowIS.PONumber = existingRowIS[j].PONumber;
                                            rowIS.VersionPOSERA = q.VersionPOSERA != null ? q.VersionPOSERA + 1 : existingRowHS[i].VersionPOSERA != null ? existingRowHS[i].VersionPOSERA : 1;
                                            rowIS.DataVersionAI = existingRowHS[i].DataVersion;
                                            rowIS.TGLGRSAP = existingRowIS[j].GRDATE;
                                            listDataIS.Add(rowIS);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               
                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.Message, "S02009", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                //end
                throw ex;
            }
            return listDataIS;
        }

        public int DeleteAllTempHSIS()
        {
            int result = 1;
            try
            {
                EntityCommand cmd = new EntityCommand("EProcEntities.sp_DeleteAllTempHSISS02009", entityConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                OpenConnection();
                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                result = 0;

                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

       
        #endregion

        #endregion

    }
}