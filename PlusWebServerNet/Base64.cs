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

namespace PlusComponents.PlusWebServer
{
	/// <summary>
	/// Summary description for Base64.
	/// </summary>
	public class Base64
	{
		public Base64()
		{
		}

    static string base64Alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    static public string Decode(string input)
    {
      int v1, v2, v3, v4;
      string output = "";

      for (int i=0; i<input.Length; i+=4) {
        v1 = v2 = v3 = v4 = 0;
        char c = input[i];    
        if (c != '=') {
          v1 = base64Alpha.IndexOf(c);
          c = input[i+1];
          if (c != '=') {
            v2 = base64Alpha.IndexOf(c);
            c = input[i+2];
            if (c != '=') {
              v3 = base64Alpha.IndexOf(c);
              c = input[i+3];
              if (c != '=') {
                v4 = base64Alpha.IndexOf(c);
              }
            }
          }
        }
      
        byte b;

        b = (byte)((v1 << 2) + ((v2 & 0x30) >> 4));
        if (b == 0) break;
        output += (char) b;

        b = (byte)(((v2 & 0xF) << 4) + ((v3 & 0x3C) >> 2));
        if (b == 0) break;
        output += (char) b;

        b = (byte)(((v3 & 0x3) << 6) + (v4));
        if (b == 0) break;
        output += (char) b;
      }

      return output;
    }
	}
}
