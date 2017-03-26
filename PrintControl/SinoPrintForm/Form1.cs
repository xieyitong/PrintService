using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using log4net;
using System.IO;
using System.Net;
using PrintControl;
using System.Drawing.Printing;
using System.Web;
using System.Web.Script.Serialization;

namespace SinoPrintForm
{
    public partial class Form1 : Form
    {

        private Thread thread = null;
        private int port = 18999;
        private static readonly ILog log = LogManager.GetLogger(typeof(Form1));
        private string sysIniPath = System.Windows.Forms.Application.StartupPath + "\\sys.ini";
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




        public Form1()
        {
            InitializeComponent();
            Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 系统退出事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否确认退出程序？", "退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                // 关闭所有的线程
                this.Dispose();
                this.Close();
                System.Environment.Exit(0);
            }

        }
        /// <summary>
        /// 打印机设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 打印机设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // PrintDialog printDialog = new PrintDialog();
            printDialog1.Document = printDocument;

            //printDialog1.ShowDialog();
            if (DialogResult.OK == printDialog1.ShowDialog()) //如果确认，将会覆盖所有的打印参数设置
            {
                //页面设置对话框（可以不使用，其实PrintDialog对话框已提供页面设置）
                PageSetupDialog psd = new PageSetupDialog();
                psd.Document = printDocument;
                psd.ShowDialog();
            }
            Externs.SetDefaultPrinter(printDocument.PrinterSettings.PrinterName);
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
                            var printUrl = parm["url"].ToString().Trim();
                            callback = parm["callback"].ToString().Trim();
                            log.Info(printUrl + ":   " + printUrl);
                            var postData = parm["postData"].ToString().Trim();
                            log.Info(postData + ":   " + postData);
                            var cookieStr = parm["cookieStr"].ToString().Trim();
                            log.Info(cookieStr + ":   " + cookieStr);
                            var printerParams = parm["printerParams"].ToString().Trim();
                            log.Info(printerParams + ":   " + printerParams);
                            var charset = parm["charset"].ToString().Trim();
                            log.Info(charset + ":   " + charset);
                            reportTemplate = parm["_report"].ToString().Trim();
                            log.Info(reportTemplate + ":   " + reportTemplate);
                            SetPrint(sysIniPath, reportTemplate);
                            PrinterSettings settings = new PrinterSettings();
                            //settings.DefaultPageSettings.PaperSize.Kind= System.Drawing.Printing.PaperKind.Custom; 
                            //1.设置打印机名称
                            settings.PrinterName = INIHepler.INIGetStringValue(sysIniPath, reportTemplate, reportTemplate, null);
                            //2.设置打印机打印方向
                            settings.DefaultPageSettings.Landscape = Convert.ToBoolean(INIHepler.INIGetStringValue(sysIniPath, reportTemplate, "SetLandscape", null));
                            //3.设置纸张

                            //settings.DefaultPageSettings.PaperSize.PaperName = INIHepler.INIGetStringValue(sysIniPath, reportTemplate, "SetPaperName", null);


                            //纸张高度
                            var SetPaperHeight = INIHepler.INIGetStringValue(sysIniPath, reportTemplate, "SetPaperHeight", 0 + "");
                            //纸张宽度
                            var SetPaperWidth = INIHepler.INIGetStringValue(sysIniPath, reportTemplate, "SetPaperWidth", 0 + "");

                            PaperSize paperSize = new PaperSize(reportTemplate, Convert.ToInt32(SetPaperWidth), Convert.ToInt32(SetPaperHeight));
                            settings.DefaultPageSettings.PaperSize = paperSize;



                            //4下边距
                            var SetMarginsBottom = INIHepler.INIGetStringValue(sysIniPath, reportTemplate, "SetMarginsBottom", 0 + "");
                            //5上边距
                            var SetMarginsTop = INIHepler.INIGetStringValue(sysIniPath, reportTemplate, "SetMarginsTop", 0 + "");
                            //6左边距
                            var SetMarginsLeft = INIHepler.INIGetStringValue(sysIniPath, reportTemplate, "SetMarginsLeft", 0 + "");
                            //7右边距
                            var SetMarginsRight = INIHepler.INIGetStringValue(sysIniPath, reportTemplate, "SetMarginsRight", 0 + "");
                            //8------设置边距
                            Margins margins = new Margins();
                            margins.Bottom = Convert.ToInt32(SetMarginsBottom);
                            margins.Top = Convert.ToInt32(SetMarginsTop);
                            margins.Left = Convert.ToInt32(SetMarginsLeft);
                            margins.Right = Convert.ToInt32(SetMarginsRight);
                            settings.DefaultPageSettings.Margins = margins;
                            var result = printerControl.SilentPrint(printUrl, postData, cookieStr, printerParams, charset, settings);
                            log.Info("本地打印服务调用打印操作：" + result);
                            using (StreamWriter writer = new StreamWriter(ctx.Response.OutputStream))
                            {
                                // writer.WriteLine(result);
                                writeJS(writer, result.ToString(), callback);
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
                var printName = INIHepler.INIGetStringValue(sysIniPath, reportTemplate, reportTemplate, null);
                if (string.IsNullOrEmpty(printName) || string.IsNullOrEmpty(LocalPrinter.GetPrinterByName(printName)))
                {
                    PrintDocument printDocument = new PrintDocument();
                    //设置打印机，记录设置信息
                    PrintDialog printDialog = new PrintDialog();
                    printDialog.Document = printDocument;
                    printDialog.UseEXDialog = true;
                    printDocument.OriginAtMargins = true;
                    //printDocument.DefaultPageSettings.PaperSize.Kind = System.Drawing.Printing.PaperKind.Custom; 
                    this.Invoke(new Action(() =>
                    {
                        var result = printDialog.ShowDialog();
                        if (DialogResult.OK == result) //如果确认，将会覆盖所有的打印参数设置
                        {
                            //页面设置对话框（可以不使用，其实PrintDialog对话框已提供页面设置）
                            PageSetupDialog psd = new PageSetupDialog();
                            psd.Document = printDocument;
                            psd.ShowDialog();
                        }
                        //1设置默认打印机
                        Externs.SetDefaultPrinter(printDocument.PrinterSettings.PrinterName);
                        //2设置打印机名称
                        INIHepler.INIWriteValue(sysIniPath, reportTemplate, reportTemplate, printDocument.PrinterSettings.PrinterName);
                        //3设置打印机打印方向
                        INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetLandscape", printDocument.DefaultPageSettings.Landscape.ToString());
                        //4设置纸张名称 A3等
                        INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetPaperName", printDocument.DefaultPageSettings.PaperSize.PaperName);
                        //5设置下边距
                        INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetMarginsBottom", printDocument.DefaultPageSettings.Margins.Bottom.ToString());
                        //6设置上边距
                        INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetMarginsTop", printDocument.DefaultPageSettings.Margins.Top.ToString());
                        //7设置左边距
                        INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetMarginsLeft", printDocument.DefaultPageSettings.Margins.Left.ToString());
                        //8设置右边距
                        INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetMarginsRight", printDocument.DefaultPageSettings.Margins.Right.ToString());
                        //9设置纸张高度
                        INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetPaperHeight", printDocument.DefaultPageSettings.PaperSize.Height.ToString());
                        //10设置纸张宽度
                        INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetPaperWidth", printDocument.DefaultPageSettings.PaperSize.Width.ToString());
                    }));

                }

            }
            else //不存在打印设置
            {


                //3.不存在进行打印设置，并执行打印操作，记录设置信息
                PrintDocument printDocument = new PrintDocument();
                PrintDialog printDialog = new PrintDialog();
                printDialog.Document = printDocument;
                printDialog.UseEXDialog = true;
                printDocument.OriginAtMargins = true;
                // printDocument.DefaultPageSettings.PaperSize.Kind = System.Drawing.Printing.PaperKind.Custom; 


                this.Invoke(new Action(() =>
                {
                    var result = printDialog.ShowDialog();
                    if (DialogResult.OK == result) //如果确认，将会覆盖所有的打印参数设置
                    {
                        //页面设置对话框（可以不使用，其实PrintDialog对话框已提供页面设置）
                        PageSetupDialog psd = new PageSetupDialog();
                        psd.Document = printDocument;
                        psd.ShowDialog();
                    }

                }));
                //1设置默认打印机
                Externs.SetDefaultPrinter(printDocument.PrinterSettings.PrinterName);
                //2设置打印机名称
                INIHepler.INIWriteValue(sysIniPath, reportTemplate, reportTemplate, printDocument.PrinterSettings.PrinterName);
                //3设置打印机打印方向
                INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetLandscape", printDocument.DefaultPageSettings.Landscape.ToString());

                //4设置纸张名称 A3等
                INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetPaperName", reportTemplate);
                //5设置下边距
                INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetMarginsBottom", printDocument.DefaultPageSettings.Margins.Bottom.ToString());
                //6设置上边距
                INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetMarginsTop", printDocument.DefaultPageSettings.Margins.Top.ToString());
                //7设置左边距
                INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetMarginsLeft", printDocument.DefaultPageSettings.Margins.Left.ToString());
                //8设置右边距
                INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetMarginsRight", printDocument.DefaultPageSettings.Margins.Right.ToString());
                //9设置纸张高度
                INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetPaperHeight", printDocument.DefaultPageSettings.PaperSize.Height.ToString());
                //10设置纸张宽度
                INIHepler.INIWriteValue(sysIniPath, reportTemplate, "SetPaperWidth", printDocument.DefaultPageSettings.PaperSize.Width.ToString());
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

        private void 清除设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (MessageBox.Show("是否确认清除打印机配置信息？", "退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                if (File.Exists(sysIniPath)) //存在
                {
                    FileInfo fi = new FileInfo(sysIniPath);
                    if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                        fi.Attributes = FileAttributes.Normal;
                    File.Delete(sysIniPath);
                }
            }
        }





    }
}
