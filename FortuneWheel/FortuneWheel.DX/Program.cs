using Monofoxe.Engine.WindowsDX;
using System;

namespace FortuneWheel.DX
{
	public static class Program
	{
		[STAThread]
		static void Main()
		{
			MonofoxePlatform.Init();

			using (var game = new Game1())
			{
				game.Run();
			}
		}
	}
}
