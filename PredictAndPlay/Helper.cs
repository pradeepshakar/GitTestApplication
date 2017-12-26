using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictAndPlay
{
    public class Helper
    {
        public static void WriteLog(string message)
        {
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"C:\Pradeep\Projects\PLog.txt", true))
            {
                file.WriteLine(message);
            }
        }

        public static void WriteError(Exception ex)
        {
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"C:\Pradeep\Projects\PError.txt", true))
            {
                file.WriteLine(ex.Message);
                file.WriteLine(ex.StackTrace);
            }
        }
    }
}
