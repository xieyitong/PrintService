﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Printing;
using System.Linq;
namespace SinoPrintForm
{
    class LocalPrinter
    {
        private static PrintDocument fPrintDocument = new PrintDocument();
        //获取本机默认打印机名称
        public static String DefaultPrinter()
        {
            return fPrintDocument.PrinterSettings.PrinterName;
        }
        public static List<String> GetLocalPrinters()
        {
            List<String> fPrinters = new List<String>();
            fPrinters.Add(DefaultPrinter()); //默认打印机始终出现在列表的第一项
            foreach (String fPrinterName in PrinterSettings.InstalledPrinters)
            {
                if (!fPrinters.Contains(fPrinterName))
                {
                    fPrinters.Add(fPrinterName);
                }
            }
            return fPrinters;
        }

        public static string GetPrinterByName(string printerName)
        {
            //List<String> list = LocalPrinter.GetLocalPrinters();
            //foreach(var item in list)
            //{
            //    if (item.Contains(printerName))
            //    {
            //        return item;
            //    }
            //}
           return LocalPrinter.GetLocalPrinters().FirstOrDefault(x => x == printerName);
        }

    }
}
