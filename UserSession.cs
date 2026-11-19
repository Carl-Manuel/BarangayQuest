using System;
using System.Collections.Generic;
using System.Text;

namespace BarangayQuest
{
    public static class UserSession
    {
        public static int UserID { get; set; }
        public static int WorkerID { get; set; }
        public static string FullName { get; set; } = "";
        public static string Username { get; set; } = "";
        public static string BarangayIDNumber { get; set; } = "";
        public static int Points { get; set; }

        public static void Clear()
        {
            UserID = 0;
            WorkerID = 0;
            FullName = "";
            Username = "";
            BarangayIDNumber = "";
            Points = 0;
        }
    }
}
