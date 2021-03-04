using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAI.RestAPI
{
    public class RestApiClient
    {
        public static string POST(string URL, string ContentType, string bodyData = "")
        {
            string response = string.Empty;
            WebRequest request = WebRequest.Create(URL);
            request.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(bodyData);

            request.ContentType = ContentType;
            request.ContentLength = byteArray.Length;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse();
            var httpResponse2 = httpResponse;
            using (var streamReader = new StreamReader(httpResponse2.GetResponseStream()))
            {
                response = streamReader.ReadToEnd();
            }
            return response;
        }

        public static string POST1(string URL, string ContentType, string bodyData = "")
        {
            string response = string.Empty;
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(URL);
            httpWebRequest.ContentType = ContentType;
            httpWebRequest.Method = "POST";
            using (var streamWriter = new
            StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(bodyData);
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                response = streamReader.ReadToEnd();
            }

            return response;
        }
    }
}
