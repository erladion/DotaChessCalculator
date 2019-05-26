using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using ImageProcessor;
using ImageProcessor.Imaging.Formats;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace HelperFunctions
{
	public static class ImageUtility
	{
		/// <summary>
		/// Converts a Bitmap to a byte[].
		/// </summary>
		/// <param name="bm"></param>
		/// <returns></returns>
		public static byte[] BitmapToByteArray(Bitmap bm)
		{
			MemoryStream s = new MemoryStream();
			bm.Save(s, ImageFormat.Png);

			return s.ToArray();
		}

		/// <summary>
		/// Increases the contrast of the given image.
		/// </summary>
		/// <param name="path">Original image</param>
		/// <param name="contrast">Contrast %</param>
		/// <returns></returns>
		public static Image IncreaseContrast(string path, int contrast)
		{
			byte[] photoBytes = File.ReadAllBytes(path);

			ISupportedImageFormat f = new PngFormat();

			using (MemoryStream inS = new MemoryStream(photoBytes))
			{
				using (MemoryStream oS = new MemoryStream())
				{
					using (ImageFactory iF = new ImageFactory(preserveExifData: true))
					{
						iF.Load(inS).Contrast(contrast).Format(f).Save(oS);
					}

					return Image.FromStream(oS);
				}
			}
		}

		/// <summary>
		/// Increases the contrast of the given image.
		/// </summary>
		/// <param name="bm">Original image</param>
		/// <param name="contrast">Contrast %</param>
		/// <returns></returns>
		public static Image IncreaseContrast(Bitmap bm, int contrast)
		{
			ISupportedImageFormat f = new PngFormat();			

			using (MemoryStream inS = new MemoryStream(BitmapToByteArray(bm)))
			{
				using (MemoryStream oS = new MemoryStream())
				{
					using (ImageFactory iF = new ImageFactory(preserveExifData: true))
					{
						iF.Load(inS).Contrast(contrast).Format(f).Save(oS);
					}

					return Image.FromStream(oS);
				}
			}
		}

		/// <summary>
		/// Change the size of the given image
		/// </summary>
		/// <param name="image"></param>
		/// <param name="newWidth"></param>
		/// <param name="maxHeight"></param>
		/// <param name="onlyResizeIfWider"></param>
		/// <returns></returns>
		public static Image Resize(Image image, int newWidth, int maxHeight, bool onlyResizeIfWider)
		{
			if (onlyResizeIfWider && image.Width <= newWidth) newWidth = image.Width;

			var newHeight = image.Height * newWidth / image.Width;
			if (newHeight > maxHeight)
			{
				// Resize with height instead  
				newWidth = image.Width * maxHeight / image.Height;
				newHeight = maxHeight;
			}

			Bitmap res = new Bitmap(newWidth, newHeight);

			using (Graphics graphic = Graphics.FromImage(res))
			{
				graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphic.SmoothingMode = SmoothingMode.HighQuality;
				graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphic.CompositingQuality = CompositingQuality.HighQuality;
				graphic.DrawImage(image, 0, 0, newWidth, newHeight);
			}

			return res;
		}

		public static Bitmap CutImage(Bitmap bm, int startX, int startY, int width, int height)
		{
			Bitmap bmn = new Bitmap(width, height);

			int startYHeight = (startY == 0 ? 0 : bm.Height - startY);

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					bmn.SetPixel(x, y, bm.GetPixel(startX + x, startY + y));
				}
			}
			return bmn;
		}

		/// <summary>
		/// Set the background to being black based on the threshold while leaving the letters with their shading.
		/// </summary>
		/// <param name="bm"></param>
		/// <param name="threshold"></param>
		/// <returns></returns>
		public static Bitmap MakeBackgroundBlack(Bitmap bm, int threshold)
		{
			for (int x = 0; x < bm.Width; x++)
			{
				for (int y = 0; y < bm.Height; y++)
				{
					Color c = bm.GetPixel(x, y);
					if (c.R < threshold)
					{
						bm.SetPixel(x, y, Color.FromArgb(0,0,0));
					}
				}
			}

			return bm;
		}

		/// <summary>
		/// Takes a screenshot of the current primary screen.
		/// </summary>
		/// <returns>A bitmap containing the screenshot</returns>
		public static Bitmap CaptureScreen()
		{
			double width = SystemParameters.MaximizedPrimaryScreenWidth;
			double height = SystemParameters.MaximizedPrimaryScreenHeight;
			Bitmap target = new Bitmap((int)width, (int)height);
			using (Graphics g = Graphics.FromImage(target))
			{
				g.CopyFromScreen(0, 0, 0, 0, target.Size);
			}
			return target;
		}

		/// <summary>
		/// Makes the pixels in a Bitmap only white if they are truly white, otherwise they become black
		/// </summary>
		/// <param name="bm"></param>
		/// <returns></returns>
		public static Bitmap HardBlackAndWhite(Bitmap bm)
		{
			for (int y = 0; y < bm.Height; y++)
			{
				for (int x = 0; x < bm.Width; x++)
				{
					Color c = bm.GetPixel(x, y);

					Color nC = (c.R != 255 && c.G != 255 && c.B != 255 ? Color.FromArgb(0, 0, 0) : Color.FromArgb(255, 255, 255));
					bm.SetPixel(x, y, nC);
				}
			}
			return bm;
		}

		/// <summary>
		/// Makes the pixels in a Bitmap black or white
		/// </summary>
		/// <param name="bm"></param>
		/// <returns></returns>
		public static Bitmap MakeBlackAndWhite(Bitmap bm)
		{
			for (int y = 0; y < bm.Height; y++)
			{
				for (int x = 0; x < bm.Width; x++)
				{
					Color c = bm.GetPixel(x, y);

					bm.SetPixel(x, y, (Luminosity(c.R, c.G, c.B) < 128 ? Color.FromArgb(0, 0, 0) : Color.FromArgb(255, 255, 255)));
				}
			}

			return bm;
		}

		public static double Luminosity(int R, int G, int B) => (0.2126 * R + 0.7152 * G + 0.0722 * B);

		public static double Average(int R, int G, int B) => (R + G + B) / 3;

		/// <summary>
		/// Makes an image grayscale pixel by pixel
		/// </summary>
		/// <param name="bm"></param>
		/// <returns></returns>
		public static Bitmap MakeGrayscale(Bitmap bm)
		{
			for (int y = 0; y < bm.Height; y++)
			{
				for (int x = 0; x < bm.Width; x++)
				{
					Color c = bm.GetPixel(x, y);

					int avg = (int)Average(c.R, c.G, c.B);

					bm.SetPixel(x, y, Color.FromArgb(c.A, avg, avg, avg));
				}
			}
			return bm;
		}

		public static void Grayscale(Bitmap bm)
		{
			unsafe
			{
				BitmapData bmd = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, bm.PixelFormat);

				int bytesPerPixel = Image.GetPixelFormatSize(bm.PixelFormat) / 8;
				int height = bmd.Height;
				int widthInBytes = bmd.Width * bytesPerPixel;

				byte* ptr = (byte*)bmd.Scan0;

				Parallel.For(0, height, y =>
				{
					byte* line = ptr + (y * bmd.Stride);

					for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
					{
						int B = line[x];
						int G = line[x + 1];
						int R = line[x + 2];

						int gray = (int)Luminosity(R,G,B);

						line[x] = (byte)gray;
						line[x + 1] = (byte)gray;
						line[x + 2] = (byte)gray;
					}
				});
				bm.UnlockBits(bmd);
			}
		}

		/// <summary>
		/// Makes an image black and white depending on a given threshold.
		/// </summary>
		/// <param name="bm"></param>
		/// <param name="threshold"></param>
		/// <returns></returns>
		public static Bitmap BlackAndWhiteThreshold (Bitmap bm, int threshold)
		{
			for (int x = 0; x < bm.Width; x++)
			{
				for (int y = 0; y < bm.Height; y++)
				{
					Color c = bm.GetPixel(x, y);

					Color cn = ((Average(c.R, c.G, c.B) >= threshold) ? Color.FromArgb(255, 255, 255) : Color.FromArgb(0,0,0));

					bm.SetPixel(x, y, cn);
				}
			}
			return bm;
		}

		public static Bitmap GrayscaleToBNW(Bitmap bm)
		{
			double sum = 0;
			for (int x = 0; x < bm.Width; x++)
			{
				for (int y = 0; y < bm.Height; y++)
				{
					Color c = bm.GetPixel(x, y);

					sum += Luminosity(c.R, c.G, c.B);
				}
			}

			double threshold = sum / (bm.Width * bm.Height);

			for (int x = 0; x < bm.Width; x++)
			{
				for (int y = 0; y < bm.Height; y++)
				{
					Color c = bm.GetPixel(x, y);

					bm.SetPixel(x, y, (Luminosity(c.R, c.G, c.B) < threshold ? Color.FromArgb(0, 0, 0) : Color.FromArgb(255, 255, 255)));
				}
			}

			return bm;
		}

		public static List<Bitmap> ExtractRanks(Bitmap bm)
		{
			int screenWidth = (int)SystemParameters.MaximizedPrimaryScreenWidth;
			int screenHeight = (int)SystemParameters.MaximizedPrimaryScreenHeight;

			int width = (int)Math.Round((screenWidth * 0.035)/5)*5;
			int height = (int)Math.Ceiling((screenHeight * 0.03)/5)*5;
			int spacing = (int)Math.Round((screenHeight * 0.0972)/5)*5;

			// Original
			//int width = 65;
			//int height = 25;
			//int spacing = 105;
			//int start = 165;

			// Improved
			//int width = 70;
			//int height = 35;
			//int spacing = 105;
			//int start = 160;

			List<Bitmap> res = new List<Bitmap>(8);

			for (int i = 0; i < res.Capacity; i++)
			{
				Bitmap bmn = new Bitmap(width, height);
				for (int h = 0; h < height; h++)
				{
					for (int w = 0; w < width; w++)
					{
						bmn.SetPixel(w, h, bm.GetPixel(1850 + w, 160 + spacing * i + h));
					}
				}
				res.Add(bmn);
			}

			return res;
		}
	}
}
