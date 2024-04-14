using floppy.src;

public class Program
{
	static void Main(string[] args)
	{
		if (args.Length == 0)
		{
			Console.WriteLine("You need to provide a file name.");
			return;
		}

		

		Floppy fp = new Floppy(args[0]);

		fp.Run();
	}
}