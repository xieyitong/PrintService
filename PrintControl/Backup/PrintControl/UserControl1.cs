/*
 * 由SharpDevelop创建。
 * 用户： Simon
 * 日期: 2015-8-25
 * 时间: 1:37
 * 
 * 要改变这种模板请点击 工具|选项|代码编写|编辑标准头文件
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.Win32;
using System.IO;
using System.Collections;
using O2S.Components.PDFRender4NET;
using O2S.Components.PDFRender4NET.Printing;
using System.Diagnostics;
namespace PrintControl
{
	/// <summary>
	/// Description of UserControl1.
	/// </summary>
	[Guid("B8B75347-9781-4A31-8319-058CE5F873FD")]
    public class PrinterControl : IObjectSafety
	{
		public PrinterControl()
		{
		}

        public string PrintEngine = "exe";
		
		public string SilentPrint(string Url, string PostData,string cookieStr,string printerParams,string charset)
		{
			string fileName = null;
			try {
				Stream stream = HttpUtils.SendRequest(Url,PostData,charset,cookieStr);
				
				//创建临时目录
				string temp = System.Environment.GetEnvironmentVariable("TEMP"); 
				fileName = Guid.NewGuid().ToString();
				fileName = temp + "\\"+fileName+".pdf";
				saveFile(stream,fileName);
				
				//读取PDF文件
                if (PrintEngine.Equals("exe"))
                {
                    string exePath = Assembly.GetExecutingAssembly().Location ;
                    string exeFile = new FileInfo(exePath).DirectoryName + "\\FR.exe";
                   // MessageBox.Show(exeFile);
                    Process p = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.CreateNoWindow = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.UseShellExecute = true;
                    startInfo.FileName = exeFile;
                    //startInfo.Verb = "FR.exe";
                    string cmd = "/p  \"" + fileName + "\" \"" + GetPrinter() + "\"";
                    
                    startInfo.Arguments = cmd;
                    p.StartInfo = startInfo;
                    p.Start();
                    p.WaitForExit();

                }
                else
                {
                    PDFFile pdf = PDFFile.Open(fileName);
                    // Create a default printer settings to print on the default printer.
                    PrinterSettings settings = new PrinterSettings();
                    PDFPrintSettings pdfPrintSettings = new PDFPrintSettings(settings);
                    pdfPrintSettings.PageScaling = PageScaling.FitToPrinterMargins;
                    pdf.Print(pdfPrintSettings);
                    pdf.Dispose();
                }

			} catch (Exception ex) {
                return "{status:'error',msg:'" + ex.Message + "'}";
			} finally {
				try {
					if(fileName!=null){
						new FileInfo(fileName).Delete();
					}
				} catch (Exception ee) {
				}
			}
			return "{status:'success'}";
		}
		
		public string[] GetPrinters()
        {
			ICollection col = PrinterSettings.InstalledPrinters;
			string[] printers = new string[col.Count];
			col.CopyTo(printers,0);
			return printers;
        }

        public string GetPrinter()
        {
			return  new PrinterSettings().PrinterName;
        }
        
        private PrinterSettings FindPrinterSettings(string printerName){
        	if(printerName == null || printerName.Length<1){
        		return new PrinterSettings();
        	}else{
        		return new PrinterSettings();
        	}
        	
        	
        }


        private static void saveFile(Stream stream, string fileName)
        {
            byte[] buf = new byte[1024];
            int len = -1;
            FileStream file = new FileStream(fileName, FileMode.OpenOrCreate);
            while ((len = stream.Read(buf, 0, buf.Length)) > 0)
            {
                file.Write(buf, 0, len);
            }
            file.Flush();
            file.Close();
        }
        
		#region 写注册表

        public void WriteDataToRegistry()
        {
            RegistryKey rootKeyStair, rootKeyTwo, rootKeyThree, rootKeyFour, rootKeyFive;
            rootKeyStair = Registry.ClassesRoot.CreateSubKey("CLSID");
            rootKeyTwo = rootKeyStair.CreateSubKey("{B8B75347-9781-4A31-8319-058CE5F873FD}");    //GUID和类上面的一样
            rootKeyThree = rootKeyTwo.CreateSubKey("Implemented Categories");
            rootKeyFour = rootKeyThree.CreateSubKey("{6AEFCD84-FACB-43A7-8CB0-B0ED3F491164}");
            rootKeyFive = rootKeyThree.CreateSubKey("{6AEFCD84-FACB-43A7-8CB0-B0ED3F491164}");


        }
        #endregion


        #region IObjectSafety 成员

        public void GetInterfacceSafyOptions(Int32 riid, out Int32 pdwSupportedOptions, out Int32 pdwEnabledOptions)
        {
            // TODO:  添加 WebCamControl.GetInterfacceSafyOptions 实现 
            pdwSupportedOptions = 1;
            pdwEnabledOptions = 2;
        }

        public void SetInterfaceSafetyOptions(Int32 riid, Int32 dwOptionsSetMask, Int32 dwEnabledOptions)
        {
            // TODO:  添加 WebCamControl.SetInterfaceSafetyOptions 实现             
        }

        #endregion 
	}
    
    
    
    
    [Guid("21541D4D-20CC-4a7b-B93D-BC50D7CCB487"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IObjectSafety
    {
        // methods 
        void GetInterfacceSafyOptions(
            System.Int32 riid,
            out System.Int32 pdwSupportedOptions,
            out System.Int32 pdwEnabledOptions);
        void SetInterfaceSafetyOptions(
            System.Int32 riid,
            System.Int32 dwOptionsSetMask,
            System.Int32 dwEnabledOptions);
    } 
}