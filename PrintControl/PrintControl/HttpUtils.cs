/*
 * 由SharpDevelop创建。
 * 用户： Simon
 * 日期: 2015-8-25
 * 时间: 2:02
 * 
 * 要改变这种模板请点击 工具|选项|代码编写|编辑标准头文件
 */
using System;
using System.Net;
using System.Text;
using System.Collections.Generic; 
using System.IO;

namespace PrintControl
{
	/// <summary>
	/// Description of HttpUtils.
	/// </summary>
	public class HttpUtils
	{
		public HttpUtils()
		{
			
		}
		
		
		public static Stream SendRequest(string Url, string PostData,string charset,string cookieStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
//            request.Timeout = 30*1000;
			//提交请求方式
            request.Method = "POST";
            //保持连接
            request.KeepAlive = true;
            //上面的http头看情况而定，但是下面俩必须加  
            request.ContentType = "application/x-www-form-urlencoded";
            //禁止重定向
            request.AllowAutoRedirect = false;
            request.ContentLength = Encoding.UTF8.GetByteCount(PostData);
            //初始化Cookie
            if(IsNotEmpty(cookieStr)){
            	request.Headers.Add("Cookie",cookieStr);
            }
            if(IsNotEmpty(PostData)){
                byte[] bytes = Encoding.GetEncoding(charset).GetBytes(PostData);
                request.ContentLength = bytes.Length;
                //初始化请求数据流
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
                requestStream.Close();
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();           
        }
		
		private static IDictionary<string,string> Parse(string str){
			IDictionary<string,string> parameters = new Dictionary<string ,string>();
			if(str != null && str.Length>0){
				string[] groups = str.Split(';');
		 	foreach (string group in groups) {
		 		string[] g = group.Split('=');
		 		parameters.Add(g[0],g[1]);
		 	}
			}
			
			return parameters;
		}
		
		private static bool IsNotEmpty(string str){
			return str != null && str.Length >0;
		}
	}
}
