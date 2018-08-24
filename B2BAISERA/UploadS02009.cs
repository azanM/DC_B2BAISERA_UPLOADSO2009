using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using B2BAISERA.Log;
using B2BAISERA.Models.Providers;
using B2BAISERA.Properties;
using System.Net;
using B2BAISERA.Models;
using System.Security.Principal;
using B2BAISERA.Models.EFServer;
using System.Diagnostics;

namespace B2BAISERA
{
    public partial class UploadS02009 : Form
    {
        private static string fileType = "S02009";
        private bool acknowledge;
        private string ticketNo = "";
        private string message = "";

        public UploadS02009()
        {
            InitializeComponent();
        }

        private void UploadS02009_Load(object sender, EventArgs e)
        {
            LogEvent logEvent = new LogEvent();
            TransactionProvider transactionProvider = new TransactionProvider();
            List<CUSTOM_S02009_TEMP_HS> tempHS = new List<CUSTOM_S02009_TEMP_HS>();
            List<CUSTOM_S02009_TEMP_IS> tempIS = new List<CUSTOM_S02009_TEMP_IS>();
            List<CUSTOM_S02009_TEMP_HS> tempHSISChecked = new List<CUSTOM_S02009_TEMP_HS>();
            List<CUSTOM_S02009_TEMP_IS> tempHSISChecked2 = new List<CUSTOM_S02009_TEMP_IS>();
            try
            {
                transactionProvider.DeleteAllTempHSIS();
                do
                {
                    tempHS = transactionProvider.CreatePOSeraToAI_HS();
                    tempIS = transactionProvider.CreatePOSeraToAI_IS();

                    tempHSISChecked = transactionProvider.CheckingHistoryHSIS(tempHS, tempIS);
                    tempHSISChecked2 = transactionProvider.CheckingHistoryHSIS2(tempHS, tempIS);
                    transactionProvider.DeleteAllTempHSIS();

                    if (tempHSISChecked.Count > 0 && tempHSISChecked2.Count > 0)
                    {
                        LoginAuthentication();
                        if (acknowledge == false || ticketNo == string.Empty)
                        {
                            //close
                        }
                        else Upload(tempHSISChecked, tempHSISChecked2);
                       

                        tempHS = new List<CUSTOM_S02009_TEMP_HS>();
                        tempIS = new List<CUSTOM_S02009_TEMP_IS>();
                        tempHSISChecked = new List<CUSTOM_S02009_TEMP_HS>();
                        tempHSISChecked2 = new List<CUSTOM_S02009_TEMP_IS>();

                       

                        tempHSISChecked = transactionProvider.CheckingHistoryHSIS(tempHS, tempIS);
                        tempHSISChecked2 = transactionProvider.CheckingHistoryHSIS2(tempHS, tempIS);

                        transactionProvider.DeleteAllTempHSIS();

                    }
                } while (tempHSISChecked.Count > 0 && tempHSISChecked2.Count > 0);

                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", "Upload Finish.", fileType, "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
            }
            catch (Exception ex)
            {
                LblResult.Text = ex.Message;
                LblAcknowledge.Text = "";
                LblTicketNo.Text = "";
                LblMessage.Text = "";

                //logevent login failed
                logEvent.WriteDBLog("", "UploadS02009_Load", false, "", ex.Message, fileType, "SERA");
                transactionProvider.DeleteAllTempHSIS();

               
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                //end
            }
        }

        private void LoginAuthentication()
        {
            LogEvent logEvent = new LogEvent();
            TransactionProvider transactionProvider = new TransactionProvider();
            try
            {
                using (WsDev.B2BAIWebServiceDMZ wsB2B = new WsDev.B2BAIWebServiceDMZ())
                {
                    var User = transactionProvider.GetUser("SERA", "SERA", "B2BAITAG");
                    if (User != null)
                    { 
                        var loginReq = new WsDev.LoginRequest();
                        loginReq.UserName = User.UserCode;
                        loginReq.Password = User.PassCode;
                        loginReq.ClientTag = User.ClientTag;

                        //WebProxy myProxy = new WebProxy(Resources.WebProxyAddress, true);
                        //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);

                        //wsB2B.Proxy = myProxy;

                        var wsResult = wsB2B.LoginAuthentication(loginReq);
                        acknowledge = wsResult.Acknowledge;
                        ticketNo = wsResult.TicketNo;
                        message = wsResult.Message;
                    }

                    LblResult.Text = "Service Result = ";
                    LblAcknowledge.Text = "Acknowledge : " + acknowledge;
                    LblTicketNo.Text = "TicketNo : " + ticketNo;
                    LblMessage.Text = "Message :" + message;

                    //logevent login succeded
                    logEvent.WriteDBLog("B2BAIWebServiceDMZ", "LoginAuthentication", acknowledge, ticketNo, message, fileType, "SERA");
                    
                    
                }
            }
            catch (Exception ex)
            {
                LblResult.Text = ex.Message;
                LblAcknowledge.Text = "";
                LblTicketNo.Text = "";
                LblMessage.Text = "";

                //logevent login failed
                logEvent.WriteDBLog("B2BAIWebServiceDMZ", "LoginAuthentication", acknowledge, ticketNo, "webservice message : " + message + ". exception message : " + ex.Message, fileType, "SERA");

               
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                //end
            }            
        }

        private void Upload(List<CUSTOM_S02009_TEMP_HS> tempHSISChecked, List<CUSTOM_S02009_TEMP_IS> tempHSISChecked2)
        {
            LogEvent logEvent = new LogEvent();
            TransactionProvider transactionProvider = new TransactionProvider();
            TransactionViewModel transaction = null;
            WsDev.TransactionData[] transactionDataArray = null;
            List<S02009HSViewModel> transactionDataDetailHS = new List<S02009HSViewModel>();
            List<S02009ISViewModel> transactionDataDetailIS = new List<S02009ISViewModel>();
            List<string> arrHSIS = null;
            try
            {
                

                //4.INSERT INTO LOG TRANSACTION HEADER DETAIL + DELETE TEMP
                var intResult = transactionProvider.InsertLogTransaction(tempHSISChecked, tempHSISChecked2);

                //5.GET DATA FROM LOG TRANSACTION HEADER DETAIL 
                if (intResult != 0)
                {
                    //a.GET TRANSACTION 
                    transaction = transactionProvider.GetTransaction();

                    //b.GET TRANSACTION DATA
                    if (transaction != null)
                    {
                        //transactionData = transactionProvider.GetTransactionData(transaction.ID);
                        transactionDataArray = transactionProvider.GetTransactionDataArray(transaction.ID);

                        //c.GET TRANSACTIONDATA DETAIL / HS-IS
                        for (int i = 0; i < transactionDataArray.Count(); i++)
                        {
                            var DataDetailHS = transactionProvider.GetTransactionDataDetailHS(transactionDataArray[i].ID);
                            var DataDetailIS = transactionProvider.GetTransactionDataDetailIS(transactionDataArray[i].ID);
                            for (int j = 0; j < DataDetailHS.Count; j++)
                            {
                                transactionDataDetailHS.Add(DataDetailHS[j]);
                                //masukan ke array
                                arrHSIS = new List<string>();
                                arrHSIS.Add(transactionProvider.ConcateStringHS(DataDetailHS[j]));

                                for (int k = 0; k < DataDetailIS.Count; k++)
                                {
                                    if (DataDetailHS[j].PONumber == DataDetailIS[k].PONumber)
                                    {
                                        transactionDataDetailIS.Add(DataDetailIS[k]);
                                        //masukan ke array
                                        arrHSIS.Add(transactionProvider.ConcateStringIS(DataDetailIS[k]));
                                    }
                                }
                                //masukan ke transactionDataArray.
                                transactionDataArray[i].Data = arrHSIS.ToArray();
                                transactionDataArray[i].DataLength = arrHSIS.Count;
                            }
                        }
                        //6.SEND TO WEB SERVICE
                        using (WsDev.B2BAIWebServiceDMZ wsB2B = new WsDev.B2BAIWebServiceDMZ())
                        {
                            WsDev.UploadRequest uploadRequest = new WsDev.UploadRequest();
                            var lastTicketNo = transactionProvider.GetLastTicketNo(fileType);
                            uploadRequest.TicketNo = lastTicketNo; //from session ticketNo login
                            uploadRequest.ClientTag = Resources.ClientTag;
                            uploadRequest.transactionData = transactionDataArray;

                            //WebProxy myProxy = new WebProxy(Resources.WebProxyAddress, true);
                            //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);

                            //wsB2B.Proxy = myProxy;

                            var wsResult = wsB2B.UploadDocument(uploadRequest);
                            acknowledge = wsResult.Acknowledge;
                            ticketNo = wsResult.TicketNo;
                            message = wsResult.Message;
                        }
                    }

                    
                    else if (transaction == null)
                    {
                        logEvent.WriteDBLog("", "UploadS02009_Load", false, "", "transaction == null", "s02009", "SERA");
                        Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
                    }

                }
                else if (intResult == 0)
                {
                    //delete temp table 
                    transactionProvider.DeleteAllTempHSIS();
                    acknowledge = false;
                    ticketNo = "";
                    message = "No Data Upload.";
                }

                LblResult.Text = "Service Result = ";
                LblAcknowledge.Text = "Acknowledge : " + acknowledge;
                LblTicketNo.Text = "TicketNo : " + ticketNo;
                LblMessage.Text = "Message :" + message;

                //logevent login succeded
                logEvent.WriteDBLog("B2BAIWebServiceDMZ", "UploadDocumentS02009", acknowledge, ticketNo, message, fileType, "SERA");                
            }
            catch (Exception ex)
            {
                //delete temp table 
                transactionProvider.DeleteAllTempHSIS();

                LblResult.Text = ex.Message;
                LblAcknowledge.Text = "";
                LblTicketNo.Text = "";
                LblMessage.Text = "";

                //logevent login failed
                
                logEvent.WriteDBLog("B2BAIWebServiceDMZ", "UploadDocumentS02009", acknowledge, ticketNo, "webservice message : " + message + ". exception message : " + ex.Message, fileType, "SERA");

                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02009.exe");
            }
        }
    }
}
