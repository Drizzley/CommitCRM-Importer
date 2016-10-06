﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RepairShoprCore
{
    public class RepairShoprUtils
    {
        public static string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Repairshopr");
        public static string path = folderPath + "\\" + string.Format("RepairshoprLog_{0}{1}.Html", DateTime.Today.Date.ToString("dd/MM/yyyy").Replace("/", "_"), string.Empty);//Path.Combine(folderPath, "KarmaCRMLog.Html");
        private static readonly object Obj = new object();
        public static LoginResponse LoginResponse = null;
        public static string globalURl = string.Empty;
        static int sufix = 1;

        public static void LogWriteLineinHTML(string msg, MessageSource source, string exception, messageType msgType)
        {
            string errorPath = string.Empty;

            lock (Obj)
            {
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                string header = "<html><title>CommitCRM integration with Repairshopr </title><head><head><body><div align='center'><h1> CommitCRM integration with Repairshopr </h1></div><table border=\"1\" style=\"width:100%\"><tr><th>Date Time</th><th>Source</th><th>Log Level </th><th>Message</th><th>Exception </th><tr>";
                string message = null;//string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3} </td><td>{4}</td></tr>", DateTime.Now, source, msgType, msg, exception);


                if (msgType == messageType.Error)
                {
                    message = string.Format("<tr><td><font color='red'><b>{0}</b></font></td><td><font color='red'><b>{1}</b></font></td><td><font color='red'><b>{2}</b></font></td><td><font color='red'><b>{3}</b></font> </td><td><font color='red'><b>{4}</b></font></td></tr>", DateTime.Now, source, msgType, msg, exception);
                    errorPath = folderPath + "\\" + string.Format("RepairshoprErrorLog_{0}{1}.Html", DateTime.Today.Date.ToString("dd/MM/yyyy").Replace("/", "_"), string.Empty);

                }
                else if (source == MessageSource.Complete)
                {
                    message = string.Format("<tr><td><font color='Green'><b>{0}</b></font></td><td><font color='Green'><b>{1}</b></font></td><td><font color='Green'><b>{2}</b></font></td><td><font color='Green'><b>{3}</b></font> </td><td><font color='Green'><b>{4}</b></font></td></tr>", DateTime.Now, source, msgType, msg, exception);

                }
                else
                {
                    message = string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3} </td><td>{4}</td></tr>", DateTime.Now, source, msgType, msg, exception);

                }


                if (File.Exists(path))
                {
                    FileInfo fl = new FileInfo(path);
                    long size = fl.Length;
                    if (ConvertBytesToMegabytes(size) > 3)
                    {
                        errorPath = folderPath + "\\" + string.Format("RepairshoprErrorLog_{0}_{1}.Html", DateTime.Today.Date.ToString("dd/MM/yyyy").Replace("/", "_"), sufix);//Path.Combine(folderPath, "KarmaCRMLog.Html");
                        path = folderPath + "\\" + string.Format("RepairshoprLog_{0}_{1}.Html", DateTime.Today.Date.ToString("dd/MM/yyyy").Replace("/", "_"), sufix);//Path.Combine(folderPath, "KarmaCRMLog.Html");
                        sufix++;
                    }
                }

                if (!File.Exists(path))
                {
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        //Creating file with new Content.
                        sw.WriteLine(string.Format("{0}", header));
                        sw.WriteLine(string.Format("{0}", message));
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine(string.Format("{0}", message));
                    }
                }


                if (msgType == messageType.Error)
                {
                    if (!File.Exists(errorPath))
                    {
                        using (StreamWriter sw = File.CreateText(errorPath))
                        {
                            //Creating file with new Content.
                            sw.WriteLine(string.Format("{0}", header));
                            sw.WriteLine(string.Format("{0}", message));
                        }
                    }
                    else
                    {
                        using (StreamWriter sw = File.AppendText(errorPath))
                        {
                            sw.WriteLine(string.Format("{0}", message));
                        }
                    }
                }
            }
        }

        static double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }

        public static LoginResponse GetLoginResquest(string username, string password)
        {
            globalURl = ConfigurationManager.AppSettings.Get("HostName");
            using (WebClient myWebClient = new WebClient())
            {
                try
                {
                    NameValueCollection myNameValueCollection = new NameValueCollection();
                    myNameValueCollection.Add("email", username);
                    myNameValueCollection.Add("password", password);
                    string url = string.Format("http://admin.{0}/api/v1/sign_in", globalURl.Trim());
                    var responseArray = myWebClient.UploadValues(url, "POST", myNameValueCollection);
                    string jsonResult = Encoding.ASCII.GetString(responseArray);
                    LoginResponse = JsonConvert.DeserializeObject<LoginResponse>(jsonResult);
                    if (LoginResponse != null)
                    {
                        RepairShoprUtils.LogWriteLineinHTML("Successfully Login in RepairShopr", MessageSource.Login, "", messageType.Information);
                        return LoginResponse;
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                    {
                        if ((ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized)
                        {
                            RepairShoprUtils.LogWriteLineinHTML("Failed to Authenticate given Username and Password", MessageSource.Login, ex.Message, messageType.Error);
                        }
                    }
                    else
                    {
                        RepairShoprUtils.LogWriteLineinHTML("Unable to Login in RepairShopr", MessageSource.Login, ex.Message, messageType.Error);
                    }
                }
            }
            return null;
        }

        public static Customer ExportCustomer(NameValueCollection myNameValueCollection)
        {
            using (WebClient myWebClient = new WebClient())
            {
                try
                {
                    RepairShoprUtils.LogWriteLineinHTML("Sending new Customer Parameter to RepairShopr ", MessageSource.Customer, "", messageType.Information);
                    //string urls = "http://example.repairshopr.com/api/v1/customers.json?api_key=123123";
                    string urls = string.Format("http://{0}.{1}/api/v1/customers.json?api_key={2}", LoginResponse.Subdomain.Trim(), globalURl.Trim(), LoginResponse.UserToken.Trim());
                    var responseArray = myWebClient.UploadValues(urls, "POST", myNameValueCollection);

                    var jsonResult = Encoding.ASCII.GetString(responseArray);
                    RepairShoprUtils.LogWriteLineinHTML(string.Format("Server Response for Customer : {0} ", jsonResult), MessageSource.Customer, "", messageType.Information);

                    var res = JsonConvert.DeserializeObject<CustomerRoot>(jsonResult);
                    if (res.success == false && res.status != "unprocessable_entity" && res.customer == null)
                    {

                        var obj = JObject.Parse(jsonResult);
                        var email = (string)obj["params"]["email"];
                        if (string.IsNullOrEmpty(email))
                            return null;
                        string url = string.Format("http://{0}.{1}/api/v1/customers/autocomplete?api_key={2}&query={3}", LoginResponse.Subdomain.Trim(), globalURl.Trim(), LoginResponse.UserToken.Trim(), email);
                        var result = myWebClient.DownloadString(url);
                        var rootCustomer = JsonConvert.DeserializeObject<CustomerListRoot>(result);
                        if (rootCustomer != null)
                        {
                            if (rootCustomer.customers != null && rootCustomer.customers.Count > 0)
                                return rootCustomer.customers[0];
                        }
                        else
                        {
                            string bodyText = string.Join(" ", res.message);
                            RepairShoprUtils.LogWriteLineinHTML("Failed to Create New Customer in RepairShopr. " + bodyText, MessageSource.Customer, res.params_.email, messageType.Error);
                        }
                    }
                    else
                    {
                        var customerData = JsonConvert.DeserializeObject<CustomerRoot>(jsonResult);
                        if (customerData != null && customerData.customer != null)
                        {
                            return customerData.customer;
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        if ((int)((HttpWebResponse)ex.Response).StatusCode == 429)
                        {
                            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                            return ExportCustomer(myNameValueCollection);
                        }

                        using (Stream data = ex.Response.GetResponseStream())
                        using (var reader = new StreamReader(data))
                        {
                            string bodyText = reader.ReadToEnd();
                            RepairShoprUtils.LogWriteLineinHTML("Failed to Create New Customer in RepairShopr. " + bodyText, MessageSource.Customer, ex.Message, messageType.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    RepairShoprUtils.LogWriteLineinHTML("Failed to Create New Customer in RepairShopr. " + ex.Message, MessageSource.Customer, ex.Message, messageType.Error);
                }
            }
            return null;
        }

        public static ContactResponse ExportContact(NameValueCollection myNameValueCollection)
        {
            using (WebClient myWebClient = new WebClient())
            {
                try
                {
                    RepairShoprUtils.LogWriteLineinHTML("Sending new Contact Parameter to RepairShopr ", MessageSource.Contact, "", messageType.Information);
                    string urls = string.Format("http://{0}.{1}/api/v1/contacts.json?api_key={2}", LoginResponse.Subdomain.Trim(), globalURl.Trim(), LoginResponse.UserToken.Trim());
                    var responseArray = myWebClient.UploadValues(urls, "POST", myNameValueCollection);
                    string jsonResult = Encoding.ASCII.GetString(responseArray);
                    RepairShoprUtils.LogWriteLineinHTML(string.Format("Server Response for Contact : {0} ", jsonResult), MessageSource.Customer, "", messageType.Information);
                    var contactData = JsonConvert.DeserializeObject<ContactResponse>(jsonResult);

                    if (string.IsNullOrEmpty(contactData.Errors))
                    {
                        RepairShoprUtils.LogWriteLineinHTML("Successfully Created new Contact to RepairShopr ", MessageSource.Contact, "", messageType.Information);
                        return contactData;
                    }
                    else
                    {
                        RepairShoprUtils.LogWriteLineinHTML("Failed to Create New Customer in RepairShopr. " + contactData.Errors, MessageSource.Contact, contactData.Record, messageType.Error);
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        if ((int)((HttpWebResponse)ex.Response).StatusCode == 429)
                        {
                            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                            return ExportContact(myNameValueCollection);
                        }

                        using (Stream data = ex.Response.GetResponseStream())
                        using (var reader = new StreamReader(data))
                        {
                            string bodyText = reader.ReadToEnd();
                            RepairShoprUtils.LogWriteLineinHTML("Failed to Create New Contact in RepairShopr . " + bodyText, MessageSource.Contact, ex.Message, messageType.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    RepairShoprUtils.LogWriteLineinHTML("Failed to Create New Contact in RepairShopr . " + ex.Message, MessageSource.Contact, ex.Message, messageType.Error);
                }
            }
            return null;
        }

        public static Ticket ExportTicket(NameValueCollection myNameValueCollection)
        {
            using (WebClient myWebClient = new WebClient())
            {
                try
                {
                    RepairShoprUtils.LogWriteLineinHTML("Sending new Ticket Parameter to RepairShopr ", MessageSource.Ticket, "", messageType.Information);
                    string urls = string.Format("http://{0}.{1}/api/v1/tickets.json?api_key={2}", LoginResponse.Subdomain.Trim(), globalURl.Trim(), LoginResponse.UserToken.Trim());
                    var responseArray = myWebClient.UploadValues(urls, "POST", myNameValueCollection);
                    string jsonResult = Encoding.ASCII.GetString(responseArray);
                    RepairShoprUtils.LogWriteLineinHTML(string.Format("Server Response for Ticket : {0} ", jsonResult), MessageSource.Customer, "", messageType.Information);
                    var ticketData = JsonConvert.DeserializeObject<TicketRoot>(jsonResult);
                    if (ticketData != null && ticketData.ticket != null)
                        return ticketData.ticket;
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        if ((int)((HttpWebResponse)ex.Response).StatusCode == 429)
                        {
                            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                            return ExportTicket(myNameValueCollection);
                        }

                        using (Stream data = ex.Response.GetResponseStream())
                        using (var reader = new StreamReader(data))
                        {
                            string bodyText = reader.ReadToEnd();
                            RepairShoprUtils.LogWriteLineinHTML("Failed to Create New Ticket in RepairShopr . " + bodyText, MessageSource.Ticket, ex.StackTrace, messageType.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    RepairShoprUtils.LogWriteLineinHTML("Failed to Create New Ticket in RepairShopr . " + ex.Message, MessageSource.Ticket, ex.StackTrace, messageType.Error);
                }
            }
            return null;
        }
    }

    public enum messageType
    {
        Information,
        Error,
        Warning
    }

    public enum MessageSource
    {
        Customer,
        Ticket,
        Login,
        Initialization,
        Complete,
        Contact
    }
}