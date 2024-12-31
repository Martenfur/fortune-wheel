using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Monofoxe.Engine;
using Monofoxe.Engine.Cameras;
using Monofoxe.Engine.Drawing;
using Monofoxe.Engine.EC;
using Monofoxe.Engine.Resources;
using Monofoxe.Engine.SceneSystem;
using Monofoxe.Engine.Utils;
using System;
using System.Collections.Generic;

namespace FortuneWheel
{
	public class Wheel : Entity
	{
		public bool NeverStopOnOwnNumber = true;
		public const float SlideSpeed = 75;
		public const float MinSlideSpeed = 10;

		private Vector2 _position;

		private Angle _rotation;
		private Angle _oldRotation;

		private double _rotationDelta;


		private double _angularSpeed;
		private List<double> _angularSpeedHistory = new List<double>();
		private const int _speedHistorySize = 5;

		private Camera2D _camera;

		private double _friction = 5;

		private float _radius;

		private Vector2 _shadowOffset = new Vector2(4, 4);

		private bool _canRemoveNumber = false;
		private const double _speedTreshhold = 300;

		private Color[] _colors =
		{
			new Color(244,243,230),
			new Color(232,48,76),
			new Color(178,229,146),
			new Color(98,164,119),
			new Color(62,59,101),
		};

		private Color[] _bgColors =
		{
			new Color(244,243,230),
			new Color(244,243,230),
			new Color(62,59,101),
			new Color(244,243,230),
			new Color(244,243,230),
		};

		private Animation _stopperAnimation = new Animation()
		{
			Speed = 10,
			Easing = new Easing((a) => Math.Sin(a * Math.PI))
		};
		private SoundEffect _tick;
		private SoundEffect _bell;

		public Wheel(Layer layer, Vector2 position, float radius, Camera2D camera) : base(layer)
		{
			_position = position;
			_radius = radius;
			_camera = camera;

			_tick = ResourceHub.GetResource<SoundEffect>("Sounds", "tick");
			_bell = ResourceHub.GetResource<SoundEffect>("Sounds", "bell");
		}


		public bool CanRemoveNumber => _canRemoveNumber && State.Numbers.Count > 2 && _angularSpeed == 0;


		public int GetCurrentNumber()
		{
			var arc = 360f / State.Numbers.Count;
			return State.Numbers[(int)((_rotation * -1 - 90).DegreesF / arc)];
		}

		public int RemoveNumber()
		{
			if (CanRemoveNumber)
			{
				var number = GetCurrentNumber();
				State.RemoveNumber(number);
				_canRemoveNumber = false;

				return number;
			}

			return -1;
		}

		bool _grabbed = false;

		int _tickCooldown = 0;
		public override void Update()
		{
			base.Update();
			_tickCooldown -= 1;

			if ((_position - _camera.GetRelativeMousePosition()).Length() <= _radius + 100)
			{
				if (Input.CheckButtonPress(Buttons.MouseLeft))
				{
					_rotationDelta = _rotation.Difference(
						new Angle(_position, _camera.GetRelativeMousePosition())
					);
					_canRemoveNumber = false;
					_grabbed = true;
				}
			}

			if (Input.CheckButton(Buttons.MouseLeft) && _grabbed)
			{
				_rotation = new Angle(_position, _camera.GetRelativeMousePosition()) + _rotationDelta;
			}
			else
			{
				_rotation += TimeKeeper.Global.Time(_angularSpeed);
			}

			if (Input.CheckButtonRelease(Buttons.MouseLeft) && _grabbed)
			{
				_grabbed = false;
				_angularSpeed = GetAverageSpeed() * 100;
				if (Math.Abs(_angularSpeed) > _speedTreshhold)
				{
					_canRemoveNumber = true;
				}
			}


			var friction = _friction + Math.Abs(_angularSpeed) * 1f;

			if (_angularSpeed != 0 && Math.Abs(_angularSpeed) < TimeKeeper.Global.Time(friction))
			{
				_angularSpeed = 0;
				if (_canRemoveNumber)
				{
					_bell.Play();
				}
			}

			var sameOwner = State.GetOwner(GetCurrentNumber()) == RollState.RollingUser;
			var slidePast = (Math.Abs(_angularSpeed) < SlideSpeed && sameOwner);


			if (!NeverStopOnOwnNumber || !slidePast)
			{
				if (_angularSpeed > 0)
				{
					_angularSpeed -= TimeKeeper.Global.Time(friction);
				}
				if (_angularSpeed < 0)
				{
					_angularSpeed += TimeKeeper.Global.Time(friction);
				}
			}
			else
			{
				// Edge case to make sure we absolutely do not stop on our own number.
				if (Math.Abs(_angularSpeed) < TimeKeeper.Global.Time(friction) * 2)
				{
					_angularSpeed = TimeKeeper.Global.Time(friction) * 2 * Math.Sign(GetAverageSpeed());
				}
				// Accelerating is speed is too low.
				if (Math.Abs(_angularSpeed) < MinSlideSpeed)
				{
					if (_angularSpeed > 0)
					{
						_angularSpeed += TimeKeeper.Global.Time(friction);
					}
					if (_angularSpeed < 0)
					{
						_angularSpeed -= TimeKeeper.Global.Time(friction);
					}
				}
			}

			LogRotation();

			var number = GetCurrentNumber();
			if (_oldNumber != number)
			{
				_stopperAnimation.Stop();
				_stopperAnimation.Start();
				if (_tickCooldown <= 0 && _angularSpeed != 0)
				{
					_tick.Play();
					_tickCooldown = 3;
				}
			}
			_oldNumber = number;

			_stopperAnimation.Update();
			var stopperAmplitude = MathHelper.Clamp((float)_angularSpeed / 500f, 0.1f, 1);
			_stopperAngle = new Angle(90 - 30 * stopperAmplitude * _stopperAnimation.Progress * Math.Sign(GetAverageSpeed()));
		}
		private int _oldNumber = 0;


		private void LogRotation()
		{
			_angularSpeedHistory.Add(_rotation.Difference(_oldRotation));
			if (_angularSpeedHistory.Count > _speedHistorySize)
			{
				_angularSpeedHistory.RemoveAt(0);
			}
			_oldRotation = _rotation;
		}

		private double GetAverageSpeed()
		{
			var sum = 0.0;
			foreach (var a in _angularSpeedHistory)
			{
				sum += a;
			}
			return sum / _angularSpeedHistory.Count;
		}

		float _targetR;
		float _r;
		public override void Draw()
		{
			base.Draw();

			_targetR = 100 + (float)Math.Abs(GetAverageSpeed()) * 10;

			_r -= (_r - _targetR) / 4f;

			for (var i = 8; i >= 0; i -= 1)
			{
				var colorIndex = i;
				while (colorIndex >= _colors.Length)
				{
					colorIndex -= _colors.Length;
				}

				GraphicsMgr.CurrentColor = _bgColors[colorIndex];
				CircleShape.Draw(_position, _radius + _r * i * i, false);
			}


			GraphicsMgr.CurrentColor = _colors[4];
			CircleShape.Draw(_position + _shadowOffset * 2, _radius, false);

			var arc = new Angle(360f / State.Numbers.Count);

			for (var i = 0; i < State.Numbers.Count; i += 1)
			{
				var colorIndex = i;
				while (colorIndex >= _colors.Length)
				{
					colorIndex -= _colors.Length;
				}

				if (State.Numbers[i] == GetCurrentNumber())
				{
					DrawPie(arc, _rotation + arc * i, _radius - 16, _colors[colorIndex], State.Numbers[i], colorIndex == 0);
				}
				else
				{
					DrawPie(arc, _rotation + arc * i, _radius, _colors[colorIndex], State.Numbers[i], colorIndex == 0);
				}
			}

			DrawStopper();
		}


		private Angle _stopperAngle = Angle.Down;
		private void DrawStopper()
		{
			GraphicsMgr.CurrentColor = Color.Black;
			var stopperPosition = _position - Vector2.UnitY * _radius + _shadowOffset;

			TriangleShape.Draw(
				stopperPosition + (_stopperAngle - 90).ToVector2() * 16,
				stopperPosition + _stopperAngle.ToVector2() * 80,
				stopperPosition + (_stopperAngle + 90).ToVector2() * 16,
				false
			);
			TriangleShape.Draw(
				 stopperPosition + (_stopperAngle + 90).ToVector2() * 16,
				 stopperPosition - _stopperAngle.ToVector2() * 16,
				 stopperPosition + (_stopperAngle - 90).ToVector2() * 16,
				 false
			);
			stopperPosition -= _shadowOffset;

			GraphicsMgr.CurrentColor = _colors[1];
			TriangleShape.Draw(
				stopperPosition + (_stopperAngle - 90).ToVector2() * 16,
				stopperPosition + _stopperAngle.ToVector2() * 80,
				stopperPosition + (_stopperAngle + 90).ToVector2() * 16,
				false
			);
			TriangleShape.Draw(
				 stopperPosition + (_stopperAngle + 90).ToVector2() * 16,
				 stopperPosition - _stopperAngle.ToVector2() * 16,
				 stopperPosition + (_stopperAngle - 90).ToVector2() * 16,
				 false
			);
		}

		TriangleFanPrimitive _pie = new TriangleFanPrimitive(32);

		private float GetTextScale()
		{
			var textWidth = Text.CurrentFont.MeasureStringWidth("99");
			var availableWidth = (_radius * 0.8f * MathHelper.TwoPi) / State.Numbers.Count;
			return Math.Min(1, availableWidth / textWidth * 0.75f);
		}

		private void DrawPie(Angle arc, Angle rotation, float radius, Color color, int number, bool invert)
		{
			var vertexCount = (int)(arc.Degrees / 2);

			var vertices = new Vertex[vertexCount + 2];

			vertices[0] = new Vertex(_position, color);

			var arcStep = arc / vertexCount;
			for (var i = 0; i <= vertexCount; i += 1)
			{
				var v = new Vertex(
					_position + (rotation + arcStep * i).ToVector2() * _radius, color);
				vertices[i + 1] = v;
			}

			_pie.Vertices = vertices;
			_pie.Draw();

			Text.VerAlign = TextAlign.Center;
			Text.HorAlign = TextAlign.Center;
			var textRotation = rotation + arc / 2;

			if (invert)
			{
				GraphicsMgr.CurrentColor = _colors[1];
			}
			else
			{
				GraphicsMgr.CurrentColor = Color.Black;
			}
			Text.Draw(
				number + "",
				_position + textRotation.ToVector2() * (radius * 0.8f) + _shadowOffset,
				Vector2.One * GetTextScale(),
				Vector2.Zero,
				textRotation * -1 + Angle.Up
			);
			CircleShape.Draw(_position + textRotation.ToVector2() * (radius - 64) + _shadowOffset, 8, false);

			if (invert)
			{
				GraphicsMgr.CurrentColor = _colors[4];
			}
			else
			{
				GraphicsMgr.CurrentColor = _colors[0];
			}
			Text.Draw(
				number + "",
				_position + textRotation.ToVector2() * (radius * 0.8f),
				Vector2.One * GetTextScale(),
				Vector2.Zero,
				textRotation * -1 + Angle.Up
			);

			CircleShape.Draw(_position + textRotation.ToVector2() * (radius - 64), 8, false);
		}

	}
}
