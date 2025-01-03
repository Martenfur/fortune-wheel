﻿using Microsoft.Xna.Framework;
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
using System.Diagnostics;
using System.IO;
using System.Runtime;

namespace FortuneWheel
{
	public class GameController : Entity
	{
		public Camera2D Camera;
		private Wheel _wheel;

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

			CircleShape.CircleVerticesCount = 128;

			string json;

			if (!File.Exists(State.StateFileName))
			{
				json = File.ReadAllText(State.DefaultStateFileName);
			}
			else
			{ 
				json = File.ReadAllText(State.StateFileName);
			}

			State.ParseState(json);
			_wheel = new Wheel(Layer, Vector2.One * Camera.Size.Y / 2, 800, Camera);
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

			DrawWinnersList();
			DrawModeButtons();

			Text.VerAlign = TextAlign.Center;
			Text.HorAlign = TextAlign.Center;
			Text.CurrentFont = ResourceHub.GetResource<IFont>("Fonts", "ArialBig");
			
			var textPos = new Vector2(Camera.Size.X - Camera.Size.Y / 2 + 600, Camera.Size.Y * 0.5f);
			var buttonSize = Text.CurrentFont.MeasureString("CLAIM") + Vector2.One * 32;
			var buttonPos = new Vector2(Camera.Size.X - 64 - buttonSize.X / 2, Camera.Size.Y - 64 - buttonSize.Y / 2);

			DrawLargeFancyText(_wheel.GetCurrentNumber().ToString(), textPos);

			if (_wheel.CanRemoveNumber)
			{
				DrawLargeFancyText("CLAIM", buttonPos);

				GraphicsMgr.CurrentColor = Color.Red;
				RectangleShape.DrawBySize(buttonPos, buttonSize, true);

				var state = TouchPanel.GetState();
				var touchPress = state.Count > 0 && state[0].State != TouchLocationState.Released && GameMath.PointInRectangleBySize(state[0].Position, buttonPos, buttonSize);
				var mousePress = Input.CheckButtonRelease(Buttons.MouseLeft) && GameMath.PointInRectangleBySize(Camera.GetRelativeMousePosition(), buttonPos, buttonSize);

				if (touchPress || mousePress)
				{
					ResourceHub.GetResource<SoundEffect>("Sounds", "click").Play();
					_wheel.RemoveNumbers();
					State.SaveState();
				}
			}

			Text.CurrentFont = ResourceHub.GetResource<IFont>("Fonts", "Arial");
		}

		private void DrawWinnersList()
		{
			Text.VerAlign = TextAlign.Center;
			Text.HorAlign = TextAlign.Left;
			var textPos = new Vector2(Camera.Size.X - Camera.Size.Y / 2 + 0, Camera.Size.Y * 0.15f);
			for (var i = 0; i < SpinState.RollingUsers.Length; i += 1)
			{
				var text = SpinState.RollingUsers[i];

				if (_wheel.PickedNumbers.Count > i)
				{
					text += " gets [" + _wheel.PickedNumbers[i] + "] from " + State.GetOwner(_wheel.PickedNumbers[i]);
				}

				DrawFancyText(text, textPos + Vector2.UnitY * 80 * i);
			}
		}

		private void DrawModeButtons()
		{
			Text.CurrentFont = ResourceHub.GetResource<IFont>("Fonts", "ArialBig");

			var buttonSize = Text.CurrentFont.MeasureString("A") + Vector2.One * 64;
			var buttonPos = new Vector2(Camera.Size.X - 64 - buttonSize.X / 2 - buttonSize.X * 2, buttonSize.Y / 2);

			DrawLargeFancyText("A", buttonPos);

			var state = TouchPanel.GetState();
			var touchPress = state.Count > 0 && state[0].State != TouchLocationState.Released && GameMath.PointInRectangleBySize(state[0].Position, buttonPos, buttonSize);
			var mousePress = Input.CheckButtonRelease(Buttons.MouseLeft) && GameMath.PointInRectangleBySize(Camera.GetRelativeMousePosition(), buttonPos, buttonSize);

			if (touchPress || mousePress)
			{
				SpinState.RollingUsers = new[] { "Foxe" };
			}



			buttonPos = new Vector2(Camera.Size.X - 64 - buttonSize.X / 2, buttonSize.Y / 2);

			DrawLargeFancyText("B", buttonPos);

			state = TouchPanel.GetState();
			touchPress = state.Count > 0 && state[0].State != TouchLocationState.Released && GameMath.PointInRectangleBySize(state[0].Position, buttonPos, buttonSize);
			mousePress = Input.CheckButtonRelease(Buttons.MouseLeft) && GameMath.PointInRectangleBySize(Camera.GetRelativeMousePosition(), buttonPos, buttonSize);

			if (touchPress || mousePress)
			{
				SpinState.RollingUsers = new[] { "Kity" };
			}
		}

		private void DrawFancyText(string text, Vector2 pos)
		{
			Text.CurrentFont = ResourceHub.GetResource<IFont>("Fonts", "Arial");
			GraphicsMgr.CurrentColor = new Color(62, 59, 101);
			Text.Draw(text, pos + Vector2.One * 4);
			GraphicsMgr.CurrentColor = new Color(178, 229, 146);
			Text.Draw(text, pos);
		}

		private void DrawLargeFancyText(string text, Vector2 pos)
		{
			Text.CurrentFont = ResourceHub.GetResource<IFont>("Fonts", "ArialBig");
			GraphicsMgr.CurrentColor = new Color(62, 59, 101);
			Text.Draw(text, pos + Vector2.One * 8);
			GraphicsMgr.CurrentColor = new Color(178, 229, 146);
			Text.Draw(text, pos);
		}

	}
}