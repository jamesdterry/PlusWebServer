using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections;
using PlusComponents.PlusWebServer;

namespace TestPlusWebServer
{
  class Program
  {
    static WebServer theWebServer;

    static void Main(string[] args)
    {
  		// Start Web Server
  		theWebServer = new WebServer();
  		theWebServer.DocumentRoot = Directory.GetCurrentDirectory();
  		WebServer.CgiCallbackDelegate wcb = new WebServer.CgiCallbackDelegate(ourCgiCallback);
  		theWebServer.CgiCallback = wcb;
  		theWebServer.Port = 80;
  		theWebServer.Start();

      for (;;) {
          Thread.Sleep(1000);
      }

    }

    static public string ourCgiCallback(string request, ref string mime_type)
	  {
	    // Parse Request
	    string cgi = request;
	    Hashtable arglist = new Hashtable();
	    int qoff = cgi.IndexOf('?');
	    if (qoff != -1) {
	      cgi = cgi.Substring(0, qoff);
	      char[] amp = { '&' };
  	    string[] args = null;
	      args = request.Substring(qoff+1).Split(amp);
	      foreach (string s in args) {
	        string[] arg_parts;
	        char[] sep = { '=' };
	        arg_parts = s.Split(sep);
	        if (arg_parts.Length == 1) {
	          arglist.Add(arg_parts[0], null);
	        } else if (arg_parts.Length == 2) {
	          arglist.Add(arg_parts[0], arg_parts[1]);
	        }
	      }
	    }
	    if (cgi == "/test.cgi") {
	      return TestCgi(arglist);
	    }

	    string html = "<HTML><BODY>Unknown CGI</BODY></HTML>";	  
	    return html;
	  }
	  
	  static private string TestCgi(Hashtable arglist)
	  {
	    string html;
	    
      html = "<HTML><HEAD>";
      html += "<TITLE>Test Page</TITLE>";
      html += "</HEAD>\n";
      html += "<BODY>\n";
      html += "</BODY>\n";
      html += "<H1>Test CGI Arguments:</H1>\n";
      html += "<UL>\n";

      foreach (DictionaryEntry entry in arglist) {
	      html +=  "<LI>" + entry.Key + " = " + entry.Value + "</LI>";
	    }

      html += "</UL></HTML>\n";

      return html;
    }

  }
}
