using System;
using UnityEngine;
using UnityEngine.UI;

namespace DimraethMinimap;

public static class Draw
{
	public struct Band(float inner, float outer, Color col)
	{
		public float Inner = inner;

		public float Outer = outer;

		public Color Col = col;
	}

	public static readonly Color Cream = new Color(0.96f, 0.92f, 0.8f, 1f);

	public static readonly Color NorthRed = new Color(0.93f, 0.36f, 0.3f, 1f);

	public static readonly Color Iron = new Color(0.42f, 0.4f, 0.37f, 1f);

	public static readonly Color Leather = new Color(0.16f, 0.11f, 0.08f, 1f);

	private static Sprite _border;

	private static Sprite _compassBadge;

	private static readonly Sprite[] _letters = new Sprite[4];

	private static Sprite _circleMask;

	private static Sprite _defaultCircle;

	private static Sprite _defaultArrow;

	public static Sprite CircleMask()
	{
		if (_circleMask != null) return _circleMask;
		_circleMask = Bands(256, new Band[1]
		{
			new Band(0f, 0.965f, Color.white)
		});
		return _circleMask;
	}

	public static Sprite DefaultCircle()
	{
		if (_defaultCircle != null) return _defaultCircle;
		_defaultCircle = Circle(64, 0f, new Color(1f, 0.9f, 0.3f, 1f));
		return _defaultCircle;
	}

	public static Sprite DefaultArrow()
	{
		if (_defaultArrow != null) return _defaultArrow;
		_defaultArrow = Arrow(32);
		return _defaultArrow;
	}

	public static Sprite Bands(int size, Band[] bands)
	{
		Texture2D texture2D = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
		texture2D.wrapMode = TextureWrapMode.Clamp;
		texture2D.filterMode = FilterMode.Bilinear;
		float num = (float)size * 0.5f;
		float num2 = num - 0.5f;
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float num3 = (float)j - num2;
				float num4 = (float)i - num2;
				float num5 = Mathf.Sqrt(num3 * num3 + num4 * num4);
				float num6 = 0f;
				float num7 = 0f;
				float num8 = 0f;
				float num9 = 0f;
				for (int k = 0; k < bands.Length; k++)
				{
					Band band = bands[k];
					float num10 = Mathf.Clamp01(band.Outer * num - num5 + 0.5f) * Mathf.Clamp01(num5 - band.Inner * num + 0.5f);
					float num11 = band.Col.a * num10;
					if (!(num11 <= 0f))
					{
						float num12 = num11 + num9 * (1f - num11);
						num6 = (band.Col.r * num11 + num6 * num9 * (1f - num11)) / num12;
						num7 = (band.Col.g * num11 + num7 * num9 * (1f - num11)) / num12;
						num8 = (band.Col.b * num11 + num8 * num9 * (1f - num11)) / num12;
						num9 = num12;
					}
				}
				texture2D.SetPixel(j, i, new Color(num6, num7, num8, num9));
			}
		}
		texture2D.Apply();
		return MakeSprite(texture2D, size);
	}

	public static Sprite Circle(int size, float innerFrac, Color color)
	{
		return Bands(size, new Band[1]
		{
			new Band(innerFrac, 1f, color)
		});
	}

	public static Sprite Letter(char ch)
	{
		int num = "NESW".IndexOf(ch);
		if (num >= 0 && _letters[num] != null)
		{
			return _letters[num];
		}
		Sprite sprite = BuildLetter(ch);
		if (num >= 0)
		{
			_letters[num] = sprite;
		}
		return sprite;
	}

	private static Sprite BuildLetter(char ch)
	{
		float num = 7f;
		float[][] strokes = GetStrokes(ch);
		Texture2D texture2D = new Texture2D(128, 128, TextureFormat.RGBA32, mipChain: true);
		texture2D.wrapMode = TextureWrapMode.Clamp;
		texture2D.filterMode = FilterMode.Bilinear;
		for (int i = 0; i < 128; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				float px = (float)j + 0.5f;
				float py = (float)i + 0.5f;
				float num2 = 9999f;
				foreach (float[] array in strokes)
				{
					for (int l = 0; l + 3 < array.Length; l += 2)
					{
						float num3 = DistToSegment(px, py, array[l] * 128f, array[l + 1] * 128f, array[l + 2] * 128f, array[l + 3] * 128f);
						if (num3 < num2)
						{
							num2 = num3;
						}
					}
				}
				float a = Mathf.Clamp01(num - num2 + 0.5f);
				texture2D.SetPixel(j, i, new Color(1f, 1f, 1f, a));
			}
		}
		texture2D.Apply();
		return MakeSprite(texture2D, 128);
	}

	private static float[][] GetStrokes(char ch)
	{
		return ch switch
		{
			'N' => new float[1][] { new float[8] { 0.26f, 0.2f, 0.26f, 0.8f, 0.74f, 0.2f, 0.74f, 0.8f } }, 
			'E' => new float[2][]
			{
				new float[8] { 0.72f, 0.8f, 0.3f, 0.8f, 0.3f, 0.2f, 0.72f, 0.2f },
				new float[4] { 0.3f, 0.5f, 0.65f, 0.5f }
			}, 
			'S' => new float[1][] { new float[24]
			{
				0.72f, 0.7f, 0.64f, 0.8f, 0.36f, 0.8f, 0.28f, 0.7f, 0.28f, 0.6f,
				0.36f, 0.5f, 0.64f, 0.5f, 0.72f, 0.4f, 0.72f, 0.3f, 0.64f, 0.2f,
				0.36f, 0.2f, 0.28f, 0.3f
			} }, 
			'W' => new float[1][] { new float[10] { 0.16f, 0.8f, 0.33f, 0.2f, 0.5f, 0.62f, 0.67f, 0.2f, 0.84f, 0.8f } }, 
			_ => new float[0][], 
		};
	}

	private static float DistToSegment(float px, float py, float ax, float ay, float bx, float by)
	{
		float num = bx - ax;
		float num2 = by - ay;
		float num3 = px - ax;
		float num4 = py - ay;
		float num5 = num * num + num2 * num2;
		float num6 = ((num5 > 0f) ? Mathf.Clamp01((num3 * num + num4 * num2) / num5) : 0f);
		float num7 = px - (ax + num6 * num);
		float num8 = py - (ay + num6 * num2);
		return Mathf.Sqrt(num7 * num7 + num8 * num8);
	}

	private static Sprite MakeSprite(Texture2D tex, int size)
	{
		tex.hideFlags = HideFlags.HideAndDontSave;
		Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
		sprite.hideFlags = HideFlags.HideAndDontSave;
		return sprite;
	}

	public static Sprite CompassBadge()
	{
		if (_compassBadge != null)
		{
			return _compassBadge;
		}
		_compassBadge = Bands(128, new Band[3]
		{
			new Band(0f, 1f, new Color(0.07f, 0.05f, 0.03f, 1f)),
			new Band(0f, 0.9f, Iron),
			new Band(0f, 0.74f, Leather)
		});
		return _compassBadge;
	}

	public static void ApplyLayer(Image img, Sprite sprite, float scale, Vector2 pos, Color color)
	{
		if (!(img == null))
		{
			if (img.sprite != sprite)
			{
				img.sprite = sprite;
			}
			img.color = color;
			RectTransform rectTransform = img.rectTransform;
			rectTransform.localScale = new Vector3(scale, scale, 1f);
			rectTransform.anchoredPosition = pos;
		}
	}

	public static Sprite WoodBorder(int size)
	{
		if (_border != null)
		{
			return _border;
		}
		Texture2D texture2D = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true);
		texture2D.wrapMode = TextureWrapMode.Clamp;
		texture2D.filterMode = FilterMode.Trilinear;
		float num = (float)size * 0.5f;
		float num2 = num - 0.5f;
		float num3 = 0.042f * num;
		float num4 = 0.9425f * num;
		float[] array = new float[4];
		float[] array2 = new float[4];
		for (int i = 0; i < 4; i++)
		{
			float f = (45f + 90f * (float)i) * ((float)Math.PI / 180f);
			array[i] = num2 + Mathf.Cos(f) * num4;
			array2[i] = num2 + Mathf.Sin(f) * num4;
		}
		Color b = new Color(0.07f, 0.05f, 0.03f, 1f);
		for (int j = 0; j < size; j++)
		{
			for (int k = 0; k < size; k++)
			{
				float num5 = (float)k - num2;
				float num6 = (float)j - num2;
				float num7 = Mathf.Sqrt(num5 * num5 + num6 * num6);
				float num8 = num7 / num;
				Color color = new Color(0f, 0f, 0f, 0f);
				if (num8 < 0.885f)
				{
					float num9 = Mathf.Clamp01((num8 - 0.825f) / 0.06f);
					color = new Color(0.04f, 0.03f, 0.02f, 0.45f * num9 * num9);
				}
				else
				{
					float num10 = Mathf.Clamp01((num8 - 0.885f) / 0.11500001f);
					float num11 = Noise((float)k * 0.035f, (float)j * 0.035f);
					float num12 = Noise((float)k * 0.3f, (float)j * 0.3f);
					float num13 = 0.5f + 0.5f * Mathf.Sin(num8 * 260f + num11 * 7f);
					float num14 = 0.78f + 0.24f * num13 + 0.2f * (num11 - 0.5f) + 0.1f * (num12 - 0.5f);
					Color color2 = new Color(0.43f * num14, 0.29f * num14, 0.17f * num14, 1f);
					float num15 = Mathf.Clamp01(1f - Mathf.Abs(num10 - 0.28f) / 0.08f) * 0.1f;
					color2 = new Color(color2.r + num15, color2.g + num15, color2.b + num15 * 0.6f, 1f);
					float num16 = Mathf.Clamp01(1f - Mathf.Min(num10, 1f - num10) / 0.1f);
					color2 = Color.Lerp(color2, b, num16 * 0.8f);
					color2.a = Mathf.Clamp01(num - num7 + 0.5f);
					color = color2;
				}
				if (num8 > 0.85f && num8 < 1.05f)
				{
					for (int l = 0; l < 4; l++)
					{
						float num17 = (float)k - array[l];
						float num18 = (float)j - array2[l];
						float num19 = Mathf.Sqrt(num17 * num17 + num18 * num18);
						float num20 = Mathf.Clamp01(num3 - num19 + 0.5f);
						if (!(num20 <= 0f))
						{
							float t = Mathf.Clamp01((num19 - num3 * 0.6f) / (num3 * 0.4f));
							Color color3 = Color.Lerp(new Color(0.4f, 0.38f, 0.35f, 1f), new Color(0.1f, 0.09f, 0.08f, 1f), t);
							float num21 = (float)k - (array[l] - num3 * 0.3f);
							float num22 = (float)j - (array2[l] + num3 * 0.3f);
							float num23 = Mathf.Clamp01(1f - Mathf.Sqrt(num21 * num21 + num22 * num22) / (num3 * 0.45f)) * 0.45f;
							color3 = new Color(color3.r + num23, color3.g + num23, color3.b + num23, num20);
							color = Over(color3, color);
						}
					}
				}
				texture2D.SetPixel(k, j, color);
			}
		}
		texture2D.Apply();
		_border = MakeSprite(texture2D, size);
		return _border;
	}

	private static Color Over(Color top, Color bottom)
	{
		float num = top.a + bottom.a * (1f - top.a);
		if (num <= 0f)
		{
			return new Color(0f, 0f, 0f, 0f);
		}
		float num2 = bottom.a * (1f - top.a);
		return new Color((top.r * top.a + bottom.r * num2) / num, (top.g * top.a + bottom.g * num2) / num, (top.b * top.a + bottom.b * num2) / num, num);
	}

	private static float Hash(int x, int y)
	{
		int num = x * 374761393 + y * 668265263;
		int num2 = (num ^ (num >> 13)) * 1274126177;
		return (float)((num2 ^ (num2 >> 16)) & 0xFFFFFF) / 16777215f;
	}

	private static float Noise(float x, float y)
	{
		int num = Mathf.FloorToInt(x);
		int num2 = Mathf.FloorToInt(y);
		float num3 = x - (float)num;
		float num4 = y - (float)num2;
		float t = num3 * num3 * (3f - 2f * num3);
		float t2 = num4 * num4 * (3f - 2f * num4);
		float a = Hash(num, num2);
		float b = Hash(num + 1, num2);
		float a2 = Hash(num, num2 + 1);
		return Mathf.Lerp(b: Mathf.Lerp(a2, Hash(num + 1, num2 + 1), t), a: Mathf.Lerp(a, b, t), t: t2);
	}

	public static Sprite Arrow(int size)
	{
		Texture2D texture2D = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
		texture2D.wrapMode = TextureWrapMode.Clamp;
		Color color = new Color(1f, 0.9f, 0.3f, 1f);
		Color color2 = new Color(0f, 0f, 0f, 0f);
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float num = ((float)j + 0.5f) / (float)size;
				float num2 = ((float)i + 0.5f) / (float)size;
				bool flag = num2 >= 0.05f && num2 <= 0.95f && Mathf.Abs(num - 0.5f) <= (0.95f - num2) * 0.39f;
				texture2D.SetPixel(j, i, flag ? color : color2);
			}
		}
		texture2D.Apply();
		return MakeSprite(texture2D, size);
	}
}
