using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Monofoxe.Engine;
using Monofoxe.Engine.Cameras;
using Monofoxe.Engine.Drawing;
using Monofoxe.Engine.EC;
using Monofoxe.Engine.Resources;
using Monofoxe.Engine.SceneSystem;
using Monofoxe.Engine.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace FortuneWheel
{
	public class GameController : Entity
	{
		public Camera2D Camera;

		public GameController() : base(SceneMgr.GetScene("default")["default"])
		{
			Layer.DepthSorting = true;
			Depth = -999;
			GameMgr.MaxGameSpeed = 60;
			GameMgr.MinGameSpeed = 60; // Fixing framerate on 60.

			Camera = new Camera2D(GameMgr.WindowManager.ScreenWidth, GameMgr.WindowManager.ScreenHeight);
			Camera.BackgroundColor = Color.White;

			GameMgr.WindowManager.CanvasSize = Camera.Size;
			GameMgr.WindowManager.Window.AllowUserResizing = false;
			GameMgr.WindowManager.Window.IsBorderless = true;
			GameMgr.WindowManager.ApplyChanges();
			GameMgr.WindowManager.CenterWindow();
			GameMgr.WindowManager.CanvasMode = CanvasMode.Fill;
			GameMgr.WindowManager.SetFullScreen(false);


			GraphicsMgr.VertexBatch.SamplerState = SamplerState.PointClamp;

			Text.CurrentFont = ResourceHub.GetResource<IFont>("Fonts", "Arial");

			CircleShape.CircleVerticesCount = 64;

			_wheel = new Wheel(LoadNumbers(), Layer, Vector2.One * Camera.Size.Y / 2, 800, Camera);
		}

		public override void Update()
		{
			base.Update();

			if (Input.CheckButtonPress(Buttons.Escape))
			{
				GameMgr.ExitGame();
			}
		}


		public override void Draw()
		{
			base.Draw();


			Text.VerAlign = TextAlign.Center;
			Text.HorAlign = TextAlign.Center;

			Text.CurrentFont = ResourceHub.GetResource<IFont>("Fonts", "ArialBig");

			var textPos = new Vector2(Camera.Size.X - Camera.Size.Y / 2 + 600, Camera.Size.Y * 0.5f);
			var buttonSize = Text.CurrentFont.MeasureString("CLAIM") + Vector2.One * 32;
			var buttonPos = new Vector2(Camera.Size.X - 64 - buttonSize.X / 2, Camera.Size.Y - 64 - buttonSize.Y / 2);

			GraphicsMgr.CurrentColor = new Color(62, 59, 101);
			Text.Draw(_wheel.GetCurrentNumber().ToString(), textPos + Vector2.One * 8);
			GraphicsMgr.CurrentColor = new Color(178, 229, 146);
			Text.Draw(_wheel.GetCurrentNumber().ToString(), textPos);

			if (_wheel.CanRemoveNumber)
			{

				GraphicsMgr.CurrentColor = new Color(65, 64, 109);
				//for (var i = 1; i < 9; i += 1)
				//RectangleShape.DrawBySize(buttonPos + Vector2.One * i, buttonSize, false);
				GraphicsMgr.CurrentColor = new Color(179, 230, 255);
				//RectangleShape.DrawBySize(buttonPos, buttonSize, false);

				GraphicsMgr.CurrentColor = new Color(62, 59, 101);
				Text.Draw("CLAIM", buttonPos + Vector2.One * 8);
				GraphicsMgr.CurrentColor = new Color(178, 229, 146);
				Text.Draw("CLAIM", buttonPos);

				var state = TouchPanel.GetState();


				if (state.Count > 0 && state[0].State != TouchLocationState.Released && GameMath.PointInRectangleBySize(state[0].Position, buttonPos, buttonSize))
				{
					ResourceHub.GetResource<SoundEffect>("Sounds", "click").Play();
					_wheel.RemoveNumber();
					SaveNumbers(_wheel.Numbers);
				}
			}

			Text.CurrentFont = ResourceHub.GetResource<IFont>("Fonts", "Arial");
		}


		private void SaveNumbers(List<int> numbers)
		{
			var path = Environment.CurrentDirectory + "/numbers.txt";
			var lines = new List<string>();

			foreach (var number in numbers)
			{
				lines.Add(number.ToString());
			}
			File.WriteAllLines(path, lines.ToArray());
		}

		private List<int> LoadNumbers()
		{
			var path = Environment.CurrentDirectory + "/numbers.txt";
			if (!File.Exists(path))
			{
				return GenerateNumbers();
			}
			var lines = File.ReadAllLines(path);

			var numbers = new List<int>();

			foreach (var line in lines)
			{
				numbers.Add(int.Parse(line));
			}

			return numbers;
		}

		private int _numbersCount = 40;
		private Wheel _wheel;

		private List<int> GenerateNumbers()
		{
			var rng = new RandomExt();
			return rng.GetListWithoutRepeats(_numbersCount, 1, _numbersCount + 1);
		}
	}
}