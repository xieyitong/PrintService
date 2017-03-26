using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.Web;
using PrintControl;
using log4net;
using System.Windows.Forms;
using System.Drawing.Printing;
namespace SinoPrintForm
{
    public class SerialPortHttpService
    {

        private Thread thread = null;
        private int port = 18999;
        private static readonly ILog log = LogManager.GetLogger(typeof(SerialPortHttpService));
        private string sysIniPath = System.Windows.Forms.Application.StartupPath + "\\sys.ini";
        private static SerialPortHttpService self = null;

        public static SerialPortHttpService Instance()
        {
            if (self == null)
            {
                self = new SerialPortHttpService();
            }
            return self;
        }
        private SerialPortHttpService()
        {

        }

        public void Start()
        {
            thread = new Thread(startHttpThread);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            log.Info("本地打印服务已经启动……");
        }
        public void Stop()
        {
            if (thread != null)
            {
                thread.Abort();
                thread = null;
            }
        }
        private void startHttpThread()
        {
            using (HttpListener listerner = new HttpListener())
            {
                listerner.AuthenticationSchemes = AuthenticationSchemes.Anonymous;//指定身份验证 Anonymous匿名访问
                listerner.Prefixes.Add("http://127.0.0.1:" + port + "/report/");

                listerner.Start();

                log.Info("WebServer Start Successed.......");
                PrinterControl printerControl = new PrinterControl();
                var reportTemplate = string.Empty;
                while (true)
                {
                    //等待请求连接
                    //没有请求则GetContext处于阻塞状态

                    HttpListenerContext ctx = listerner.GetContext();
                    string callback = string.Empty;
                    try
                    {
                        log.Info("开始处理请求…… ");
                        Uri url = ctx.Request.Url;
                        ctx.Response.StatusCode = 200;//设置返回给客服端http状态代码
                        ctx.Response.ContentType = "application/json";
                        Dictionary<string, string> parm = getData(ctx);
                        if (parm.Count > 0)
                        {
                            //var printUrl = parm["url"].ToString().Trim();
                            //callback = parm["callback"].ToString().Trim();
                            //log.Info(printUrl + ":   " + printUrl);
                            //var postData = parm["postData"].ToString().Trim();
                            //log.Info(postData + ":   " + postData);
                            //var cookieStr = parm["cookieStr"].ToString().Trim();
                            //log.Info(cookieStr + ":   " + cookieStr);
                            //var printerParams = parm["printerParams"].ToString().Trim();
                            //log.Info(printerParams + ":   " + printerParams);
                            //var charset = parm["charset"].ToString().Trim();
                            //log.Info(charset + ":   " + charset);
                            reportTemplate = "AsnList";


                            SetPrint(sysIniPath, reportTemplate);
                           // var result = printerControl.SilentPrint(printUrl, postData, cookieStr, printerParams, charset);
                            //log.Info("本地打印服务调用打印操作：" + result);
                            using (StreamWriter writer = new StreamWriter(ctx.Response.OutputStream))
                            {
                                // writer.WriteLine(result);
                              //  writeJS(writer, result.ToString(), callback);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        if (ctx != null)
                        {
                            ctx.Response.StatusCode = 500;//设置返回给客服端http状态代码
                            using (StreamWriter writer = new StreamWriter(ctx.Response.OutputStream))
                            {

                                writeJS(writer, "{status:'error',msg:'" + e.StackTrace + "'}", callback);
                                log.Info("调用本地打印服务出现异常" + e.StackTrace + "*******" + e.Message + "*******" + e.InnerException.StackTrace);

                            }
                        }
                    }

                }
            }
        }
        private void SetPrint(string sysiniPath, string reportTemplate)
        {
            //1.判断sysini配置文件是否存在
            if (File.Exists(sysIniPath)) //存在
            {
                //2.是否存在指定模板的打印机
                var printName = INIHepler.INIGetStringValue(sysIniPath, "SysSetting", reportTemplate, null);
                if (string.IsNullOrEmpty(printName) || string.IsNullOrEmpty(LocalPrinter.GetPrinterByName(printName)))
                {
                    PrintDocument printDocument = new PrintDocument();
                    //设置打印机，记录设置信息
                    PrintDialog printDialog = new PrintDialog();
                    printDialog.Document = printDocument;
                   // printDialog.UseEXDialog = true;
              
                    if (DialogResult.OK == printDialog.ShowDialog()) //如果确认，将会覆盖所有的打印参数设置
                    {
                        //页面设置对话框（可以不使用，其实PrintDialog对话框已提供页面设置）
                        PageSetupDialog psd = new PageSetupDialog();
                        psd.Document = printDocument;
                        psd.ShowDialog();
                    }
                    Externs.SetDefaultPrinter(printDocument.PrinterSettings.PrinterName);
                    INIHepler.INIWriteValue(sysIniPath, "SysSetting", reportTemplate, printDocument.PrinterSettings.PrinterName);
                }

            }
            else
            {
                //3.不存在进行打印设置，并执行打印操作，记录设置信息
                PrintDocument printDocument = new PrintDocument();
                 PrintDialog printDialog = new PrintDialog();
                 printDialog.Document = printDocument;
                // printDialog.UseEXDialog = true;
              
               
                 if (DialogResult.OK == printDialog.ShowDialog()) //如果确认，将会覆盖所有的打印参数设置
                {
                    //页面设置对话框（可以不使用，其实PrintDialog对话框已提供页面设置）
                    PageSetupDialog psd = new PageSetupDialog();
                    psd.Document = printDocument;
                    psd.ShowDialog();
                }
                Externs.SetDefaultPrinter(printDocument.PrinterSettings.PrinterName);
                INIHepler.INIWriteValue(sysIniPath, "SysSetting", reportTemplate, printDocument.PrinterSettings.PrinterName);
            }

        }
       
        private void writeJS(StreamWriter writer, String json, String callback)
        {
            if (callback == null || callback.Length < 1)
            {
                writer.Write(json);
            }
            else
            {
                writer.Write(callback);
                writer.Write("(");
                writer.Write(json);
                writer.Write(")");
            }
        }

        public Dictionary<string, string> getData(System.Net.HttpListenerContext ctx)
        {
            var request = ctx.Request;
            if (request.HttpMethod == "GET")
            {
                return getData(ctx, DataType.Get);
            }
            else
            {
                return getData(ctx, DataType.Post);
            }
        }

        public Dictionary<string, string> getData(System.Net.HttpListenerContext ctx, DataType type)
        {
            var rets = new Dictionary<string, string>();
            var request = ctx.Request;
            switch (type)
            {
                case DataType.Post:
                    if (request.HttpMethod == "POST")
                    {
                        string rawData;
                        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                        {
                            rawData = reader.ReadToEnd();
                        }
                        string[] rawParams = rawData.Split('&');
                        foreach (string param in rawParams)
                        {
                            string[] kvPair = param.Split('=');
                            string key = kvPair[0];
                            string value = HttpUtility.UrlDecode(kvPair[1]);
                            rets[key] = value;
                        }
                    }
                    break;
                case DataType.Get:
                    if (request.HttpMethod == "GET")
                    {
                        string[] keys = request.QueryString.AllKeys;
                        foreach (string key in keys)
                        {
                            rets[key] = request.QueryString[key];
                        }
                    }
                    break;
            }
            return rets;
        }

        /// <summary>
        /// 数据提交方式
        /// </summary>
        public enum DataType
        {
            Post,
            Get,
        }


    }
}
