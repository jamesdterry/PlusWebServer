//
// Plus Web Server
// (c) 2013 James Terry
//

/*
    This file is part of the Plus Web Server.

    Foobar is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Foobar is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PlusComponents.PlusWebServer
{
	/// <summary>
	/// Web Server
	/// </summary>
	public class WebServer
	{
	  // Delegate
	  public delegate string CgiCallbackDelegate(string request, ref string mime_type);
	  public delegate string CgiPostCallbackDelegate(string request, string[] headerLines, string post_data, ref string mime_type);
	  
	  // Properties
	  public int Port
	  {
	    get { return myPort; }
	    set { myPort = value; }
	  }
	  
	  public string DefaultDocument
	  {
	    get { return myDefaultDocument; }
	    set { myDefaultDocument = value; }
	  }
	  
	  public string DocumentRoot
	  {
	    get { return myDocumentRoot; }
	    set { 
	      myDocumentRoot = value;
	      if (!myDocumentRoot.EndsWith("\\")) myDocumentRoot += "\\";
	    }
	  }
	  
	  public CgiCallbackDelegate CgiCallback
	  {
	    get { return myCgiCallback; }
	    set { myCgiCallback = value; }
	  }
	  
	  public CgiPostCallbackDelegate CgiPostCallback
	  {
	    get { return myCgiPostCallback; }
	    set { myCgiPostCallback = value; }
	  }
	  
	  public StringDictionary MimeTypes; 
    public ArrayList ProtectedDirs;
    public StringDictionary UsersPasswords;

	  // Class Members
	  private int myPort;
	  private string myDefaultDocument;
	  private string myDocumentRoot;
	  private CgiCallbackDelegate myCgiCallback;
	  private CgiPostCallbackDelegate myCgiPostCallback;
	  
	  //
	  private Thread myThread;
 		private TcpListener myListener;

    // Constructor
		public WebServer()
		{
		  myPort = 80;
		  myDefaultDocument = "index.html";
		  myDocumentRoot = "c:\\";
		  MimeTypes = new StringDictionary();
      ProtectedDirs = new ArrayList();
      UsersPasswords = new StringDictionary();
		  
      MimeTypes.Add("gif", "image/gif");
      MimeTypes.Add("jpg", "image/jpg");
      MimeTypes.Add("htm", "text/html");
      MimeTypes.Add("html", "text/html");
      MimeTypes.Add("txt", "text/plain");
      MimeTypes.Add("css", "text/css");
      MimeTypes.Add("class", "application/x-java");
      MimeTypes.Add("swf", "application/x-shockwave-flash");
		}
		
		// Class Methods
		public void Start()
		{
  		myThread = new Thread(new ThreadStart(HttpTask));
  		myThread.Start();
		}
		
		public void Stop()
		{
		  if (myThread.IsAlive) {
		    myListener.Stop();
		    myThread.Abort();
		  }
		}

    public void AddProtectedDir(string dir)
    {
      ProtectedDirs.Add(dir);
    }

    public void AddUserPassword(string user, string password)
    {
      UsersPasswords.Add(user, password);
    }
		
		private void HttpTask()
		{
			IPAddress ipAddress = Dns.Resolve("localhost").AddressList[0];
			myListener = new TcpListener(/*ipAddress,*/ myPort);
			myListener.Start();
			
			while (true) {
				// Accept a new connection
				Socket mySocket = myListener.AcceptSocket();
				if (mySocket.Connected) {
				  WorkerThread wt = new WorkerThread();
				  wt.mySocket = mySocket;
				  wt.myWebServer = this;
  		    Thread th = new Thread(new ThreadStart(wt.HandleHttpRequest));
  		    th.Start();
				} else {
				  mySocket.Close();
				}  
			}
		}		
	}
	
	class WorkerThread {
	  // Properties
	  public Socket mySocket;
	  public WebServer myWebServer;
	  
	  private enum RequestType { GET_REQUEST, POST_REQUEST };
	  
		public void HandleHttpRequest()
		{
		  Try2HandleHttpRequest();
		  mySocket.Close();
		}
		
		public void Try2HandleHttpRequest()
		{
		  int bufferSize;
		  int bufferOffset;
		  byte[] buffer;

      bufferOffset = 0;
      bufferSize = 2048;
      buffer = new byte[bufferSize];
		  
      string post_data;

		  for (;;) {
		    int cnt = 0;
		    try {
          cnt = mySocket.Receive(buffer, bufferOffset, bufferSize-bufferOffset, SocketFlags.None);
        } catch { }
        if (cnt == 0) {
          MessageReturn(400, "Invalid");
          return;
        }
        bufferOffset += cnt;

        if (ValidRequest(buffer, bufferOffset, out post_data)) break;

        if ((bufferSize-bufferOffset) < 1024) {
          // Resize Buffer
          byte[] prev_buffer = buffer;
          bufferSize += 2048;
          buffer = new byte[bufferSize];
          Array.Copy(prev_buffer, 0, buffer, 0, bufferOffset);
        }      
      }
      
      // Ok, we have the request      
  	  // Convert to string
  	  string sBuffer = Encoding.ASCII.GetString(buffer, 0, bufferOffset);
  	  
  	  // Split into lines
  	  char[] seps = { '\r', '\n' };
  	  string[] headerLines = sBuffer.Split(seps);  
  	  if (headerLines.Length == 0) {
  	    MessageReturn(400, "Invalid");
  	    return;
  	  }
  	  
  	  char [] seps2 = { ' ' };
  	  string[] reqLineParts = headerLines[0].Split(seps2);
  	  if (reqLineParts.Length < 2) {
  	    MessageReturn(400, "Invalid");
  	    return;
  	  }
  	  
  	  RequestType Request;
      bool HeadOnly = false;
      string sFile;
  	  if (reqLineParts[0] == "GET") {
  	    Request = RequestType.GET_REQUEST;
  	    sFile = reqLineParts[1];
  	  } else if (reqLineParts[0] == "HEAD") {      
  	    Request = RequestType.GET_REQUEST;
  	    sFile = reqLineParts[1];
  	    HeadOnly = true;
  	  } else if (reqLineParts[0] == "POST") {      
  	    Request = RequestType.POST_REQUEST;
  	    sFile = reqLineParts[1];
  	  } else {
  	    MessageReturn(405, "Unknown Request Method");
  	    return;
  	  }
  	  
      // It is default?
      if (sFile == "/") {
        // It's the Default
        sFile = myWebServer.DefaultDocument;
      }
      
      // Is this a directory that needs Authorization?
      string ddir = Path.GetDirectoryName(sFile);
      if (myWebServer.ProtectedDirs.Contains(ddir)) {
        string auth = FindHeader(headerLines, "Authorization");
        if ((auth == null) || (auth.Length==0)) {
  	      MessageReturnExtra(401, "Authorization Required", "WWW-Authenticate: Basic realm=\"Scheduler\"\r\n");
  	      return;
        }
        if (!CheckAuthorization(auth)) {
  	      MessageReturnExtra(401, "Authorization Required", "WWW-Authenticate: Basic realm=\"Scheduler\"\r\n");
  	      return;
        }
      }

      // Is this a conditional GET?
      string if_mod_header;
      if_mod_header = FindHeader(headerLines, "If-Modified-Since");
      
      // Send File 
      if (Request == RequestType.GET_REQUEST) {
        SendFile(sFile, HeadOnly, if_mod_header);
      } else {
        HandlePost(sFile, headerLines, post_data);
      }
	  }

    private bool CheckAuthorization(string auth)
    {
      char[] sc = { ':' };

      if (auth.Length < 7) return false;
      if (auth.Substring(0, 7) != " Basic ") return false;

      string up_str = Base64.Decode(auth.Substring(7));
      string[] up = up_str.Split(sc);
      if (up.Length != 2) return false;

      if (myWebServer.UsersPasswords.ContainsKey(up[0])) {
        return myWebServer.UsersPasswords[up[0]] == up[1];
      }

      return false;      
    }

    private void HandlePost(string sfile, string[] headerLines, string post_data)
    {
      int ei = sfile.LastIndexOf('.');
      if (ei == -1) {
        MessageReturn(404, "Extension not found");
        return;
      }
      string extension = sfile.Substring(ei+1);
      
      // cgi?
      if (extension != "cgi") {
        MessageReturn(404, "Invalid Extension");
        return;
      }

      if (myWebServer.CgiPostCallback == null) {
        MessageReturn(404, "File not found");
        return;
      }
      
      string mime_type = "text/html";
      string html = myWebServer.CgiPostCallback(sfile, headerLines, post_data, ref mime_type);
      
      if (!SendHeader(html.Length, mime_type)) {
		    return;
		  }
      SendToBrowser(html);
    }
    
    private void SendFile(string sfile, bool HeadOnly, string if_mod_header)
    {
      // Grab Extension
      string noArgs = sfile;
      if (sfile.IndexOf("?") != -1) {
        noArgs = sfile.Substring(0, sfile.IndexOf("?"));
      }
      int ei = noArgs.LastIndexOf('.');
      if (ei == -1) {
        MessageReturn(404, "Extension not found");
        return;
      }
      string extension = noArgs.Substring(ei+1);
      
      // cgi?
      if (extension == "cgi") {
        HandleCgi(sfile);
        return;
      }
      
      // Lookup Mime type
      string MimeType = "";
      if (myWebServer.MimeTypes.ContainsKey(extension)) {
        MimeType = myWebServer.MimeTypes[extension];
      }
      
      string fullPath = myWebServer.DocumentRoot;
      fullPath += sfile;
      
  		if (!File.Exists(fullPath)) {
        MessageReturn(404, "File not found");
        return;
      }

  		FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
  		
      // Check for If-Modified-Since
      
      // Send Header
      if (!SendHeader(fs.Length, MimeType)) {
		    fs.Close();
		    return;
		  }
		  
		  // Send File
			BinaryReader reader = new BinaryReader(fs);
			byte[] bytes = new byte[fs.Length];
 		  reader.Read(bytes, 0, bytes.Length);
      SendToBrowser(bytes);
      
      reader.Close();
      fs.Close();
    }

    private void HandleCgi(string sfile)
    {
      if (myWebServer.CgiCallback == null) {
        MessageReturn(404, "File not found");
        return;
      }
      
      string mime_type = "text/html";
      string html = myWebServer.CgiCallback(sfile, ref mime_type);
      
      if (!SendHeader(html.Length, mime_type)) {
		    return;
		  }
      SendToBrowser(html);
    }
    
  	private bool ValidRequest(byte[] buffer, int bufferOffset, out string post_data)
  	{
      post_data = "";
      ArrayList LineOffsets = new ArrayList();

  	  // Convert to string
  	  string sBuffer = Encoding.ASCII.GetString(buffer, 0, bufferOffset);

      int prev_line_len = -1;
      int last_line_pos = 0;
      for (;;) {
        LineOffsets.Add(last_line_pos);
        int next_line_pos = sBuffer.IndexOf("\r\n", last_line_pos);
        if (next_line_pos == -1) return false;
        next_line_pos += 2;
        if (next_line_pos > sBuffer.Length) return false;
        prev_line_len = next_line_pos - last_line_pos;
        last_line_pos = next_line_pos;
        if (prev_line_len == 2) break;
      }

      if (sBuffer.Substring(0, 3).ToUpper() == "GET") return true;
      if (sBuffer.Substring(0, 4).ToUpper() == "HEAD") return true;

      if (sBuffer.Substring(0, 4).ToUpper() != "POST") return false;

      // Post, require a Length
      foreach (int offset in LineOffsets) {
        int end_pos = sBuffer.IndexOf("\r\n", offset);
        string line = sBuffer.Substring(offset, end_pos - offset);
        if (line.Length > 16) {
          if (line.Substring(0, 16) == "Content-Length: ") {
            int line_length = 0;
            try {
              line_length = Convert.ToInt32(line.Substring(16));
            } catch { }
            // Enough Data?
            if (sBuffer.Length >= (last_line_pos + line_length)) {
              post_data = sBuffer.Substring(last_line_pos);
              return true;
            } else {
              return false;
            }
          }
        }
      }

      return false;
	  }
	  
	  private string FindHeader(string[] headerLines, string arg)
	  {
	    foreach(string s in headerLines) {
	      if (s.Length > arg.Length) {
	        if (s.Substring(0, arg.Length) == arg) {
	          return s.Substring(arg.Length+1);
	        }
	      }
	    }
	    
	    return "";
	  }
	  
	  private bool SendHeader(long FileSize, string Mime)
	  {
      SendToBrowser("HTTP/1.0 200 OK\r\n");
      // Date
      SendToBrowser("Server: Plus Components.NET/1.0\r\n");
      SendToBrowser("MIME-version: 1.0\r\n");
      SendToBrowser("Content-type: ");
      if (Mime.Length == 0) {
        SendToBrowser("text/html\r\n");
      } else {
        SendToBrowser(Mime);
        SendToBrowser("\r\n");
      }
      // Last Mod
      string cl = "Content-length: " + FileSize.ToString() + "\r\n\r\n";
      SendToBrowser(cl);
      
      return true;
	  }
	  
	  private void MessageReturn(int status_code, string explain)
	  {
      string html = "<HEAD><TITLE>" + status_code.ToString() + " ";
      html += explain + "</TITLE></HEAD>\n<BODY><H1>Server Error: ";
      html += status_code.ToString() + " " + explain + "</H1></BODY>\r\n";

      string header = "HTTP/1.0 " + status_code.ToString() + " " + explain + "\r\n";
      SendToBrowser(header);
      string cl = "Content-length: " + html.Length.ToString() + "\r\n\r\n";
      SendToBrowser(cl);
      SendToBrowser(html);
	  }

	  private void MessageReturnExtra(int status_code, string explain, string extra_header)
	  {
      string html = "<HEAD><TITLE>" + status_code.ToString() + " ";
      html += explain + "</TITLE></HEAD>\n<BODY><H1>Server Error: ";
      html += status_code.ToString() + " " + explain + "</H1></BODY>\r\n";

      string header = "HTTP/1.0 " + status_code.ToString() + " " + explain + "\r\n";
      SendToBrowser(header);
      SendToBrowser(extra_header);
      string cl = "Content-length: " + html.Length.ToString() + "\r\n\r\n";
      SendToBrowser(cl);
      SendToBrowser(html);
	  }

    public void SendToBrowser(string s)
    {
      SendToBrowser(Encoding.ASCII.GetBytes(s));
    }
    
		public void SendToBrowser(byte[] bSendData)
		{	
			try {
				if (mySocket.Connected) {
					mySocket.Send(bSendData, bSendData.Length, 0);
			  }
			}
			catch (Exception e)
			{
				// Console.WriteLine("Error Occurred : {0} ", e );						
			}
		}		
	}	
}
