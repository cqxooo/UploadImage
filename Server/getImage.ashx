<%@ WebHandler Language="C#" Class="getImage" %>

using System;
using System.Web;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class getImage : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {
        try
        {
            if (context.Request.ContentLength > 0)
            {

                using (FileStream fs = File.Create(AppDomain.CurrentDomain.BaseDirectory+ "upload/" + context.Request["FileName"].ToString()))
                {
                    fs.Write(Convert.FromBase64String(context.Request["UploadFile"].ToString()), 0, Convert.FromBase64String(context.Request["UploadFile"].ToString()).Length);
                    fs.Flush();
                    fs.Close();
                }
                //成功
                context.Response.Write("上传成功");
            }
            else
            {
                //文件不存在
                context.Response.Write("失败:没有收到任何文件");
            }

        }
        catch (Exception ex)
        {
            //上传失败
            context.Response.Write("fail:" + ex.Message);
        }
    }
    private void SaveFile(Stream stream, FileStream fs)
    {
        try
        {
            byte[] buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                fs.Write(buffer, 0, bytesRead);
            }
        }
        catch (Exception ex)
        {

        }
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}