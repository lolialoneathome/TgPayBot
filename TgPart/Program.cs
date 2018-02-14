using System;

namespace TgPart
{
    class Program
    {
        static void Main(string[] args)
        {
            SQLiteConnection.CreateFile("MyDatabase.sqlite");
        }

    }
}
