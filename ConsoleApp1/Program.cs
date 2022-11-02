using Ronin.Union;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("yay");
            
            //Datetime test2 = 4.3f;
            //Datetime test3 = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

            //int? test4 = test;

        }
    }

    [Union<int, float, DateOnly>]
    public partial class Datetime { }
}