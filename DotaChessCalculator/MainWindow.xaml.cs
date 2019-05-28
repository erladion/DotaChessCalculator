using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using Tesseract;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HelperFunctions;

namespace DotaChessCalculator
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : System.Windows.Window
	{
		private const int BASEMMR = 250;
		private const int RANKSIZE = 80;

		TesseractEngine engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);

		public enum Placement
		{
			First = 80,
			Second = 64,
			Third = 48,
			Fourth = 32,
			Fifth = -33,
			Sixth = -49,
			Seventh = -65,
			Eigth = -81
		};

		public enum PlacementToNumber
		{
			First = 1,
			Second = 2,
			Third = 3,
			Fourth = 4,
			Fifth = 5,
			Sixth = 6,
			Seventh = 7,
			Eigth = 8
		}

		public enum Rank
		{
			pawn1 = BASEMMR,
			pawn2 = pawn1 + BASEMMR + RANKSIZE / 2,
			pawn3 = pawn2 + RANKSIZE,
			pawn4 = pawn3 + RANKSIZE,
			pawn5 = pawn4 + RANKSIZE,
			pawn6 = pawn5 + RANKSIZE,
			pawn7 = pawn6 + RANKSIZE,
			pawn8 = pawn7 + RANKSIZE,
			pawn9 = pawn8 + RANKSIZE,
			knight1 = pawn9 + RANKSIZE,
			knight2 = knight1 + RANKSIZE,
			knight3 = knight2 + RANKSIZE,
			knight4 = knight3 + RANKSIZE,
			knight5 = knight4 + RANKSIZE,
			knight6 = knight5 + RANKSIZE,
			knight7 = knight6 + RANKSIZE,
			knight8 = knight7 + RANKSIZE,
			knight9 = knight8 + RANKSIZE,
			bishop1 = knight9 + RANKSIZE,
			bishop2 = bishop1 + RANKSIZE,
			bishop3 = bishop2 + RANKSIZE,
			bishop4 = bishop3 + RANKSIZE,
			bishop5 = bishop4 + RANKSIZE,
			bishop6 = bishop5 + RANKSIZE,
			bishop7 = bishop6 + RANKSIZE,
			bishop8 = bishop7 + RANKSIZE,
			bishop9 = bishop8 + RANKSIZE,
			rook1 = bishop9 + RANKSIZE,
			rook2 = rook1 + RANKSIZE,
			rook3 = rook2 + RANKSIZE,
			rook4 = rook3 + RANKSIZE,
			rook5 = rook4 + RANKSIZE,
			rook6 = rook5 + RANKSIZE,
			rook7 = rook6 + RANKSIZE,
			rook8 = rook7 + RANKSIZE,
			rook9 = rook8 + RANKSIZE,
			unranked,
		};

		public MainWindow()
		{
			InitializeComponent();
#if DEBUG
			AllocConsole();
			TextWriterTraceListener writer = new TextWriterTraceListener(System.Console.Out);
			Debug.Listeners.Add(writer);
#endif
			// Add options for all the comboboxes
			foreach (var control in ComboBoxGrid.Children)
			{
				foreach (var item in Enum.GetValues(typeof(Rank)))
				{
					((ComboBox)control).Items.Add(item);
				}
			}

			foreach (var item in Enum.GetValues(typeof(Rank)))
			{
				CurrentRank.Items.Add(item);
			}
		}

#if DEBUG
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();
#endif

		private double CalculateAvgMMR(UIElementCollection collection)
		{
			int numberOfUnranked = 0;
			int total = 0;
			foreach (var control in collection)
			{
				if (((ComboBox)control).SelectedValue.ToString() == "unranked")
				{
					numberOfUnranked++;
					continue;
				}
				total += (int)Enum.Parse(typeof(Rank), ((ComboBox)control).SelectedValue.ToString());
			}

			double avgMMR = total / (8 - numberOfUnranked);

			double closestRankValue = Math.Ceiling(avgMMR / 80) * 80;
			double diff = int.MaxValue;
			string closestRank = "";
			foreach (var item in Enum.GetValues(typeof(Rank)))
			{
				double currentDiff = Math.Abs((int)Enum.Parse(typeof(Rank), item.ToString()) - closestRankValue);
				if (currentDiff < diff)
				{
					diff = currentDiff;
					closestRank = item.ToString();
				}
			}

			InformationBlock.Text += $"Approximated avg mmr in this game: {avgMMR} ({closestRank})\n";

			return avgMMR;
		}

		private double CalculateMMRChange(double avg, double current, int placement)
		{
			int placementNumber = (int)Enum.Parse(typeof(PlacementToNumber), Enum.Parse(typeof(Placement), placement.ToString()).ToString());
			double v = ((avg - current) * .15 + placement);

			if (placementNumber == 1 && v < 15)
			{
				v = 15;
			}
			if (placementNumber == 8 && v > -15)
			{
				v = -15;
			}

			return v;
		}

		private void CalculateButton_Click(object sender, RoutedEventArgs e)
		{
			InformationBlock.Text = "";
			double avgMMR = CalculateAvgMMR(ComboBoxGrid.Children);
			double currentMMR = (int)Enum.Parse(typeof(Rank), ((ComboBox)CurrentRank).SelectedValue.ToString());
			int placement = 0;
			double mmrChange = CalculateMMRChange(avgMMR, currentMMR, placement);

			List<Tuple<double, int>> l = new List<Tuple<double, int>>();
			foreach (var item in Enum.GetValues(typeof(Placement)))
			{
				mmrChange = CalculateMMRChange(avgMMR, currentMMR, (int)Enum.Parse(typeof(Placement), item.ToString()));

				int s = (int)Enum.Parse(typeof(PlacementToNumber), item.ToString());

				l.Add(new Tuple<double, int>(mmrChange, s));
			}

			l.Sort(new ChangeTupleComparer());

			foreach (var item in l)
			{
				InformationBlock.Text += $"{item.Item2.ToString()} Estimated mmr change: {item.Item1} \n";
			}
		}

		class ChangeTupleComparer : IComparer<Tuple<double, int> >
		{
			public int Compare(Tuple<double, int> a, Tuple<double, int> b)
			{
				if (a.Item2 == b.Item2)
				{
					return 0;
				}
				else if (a.Item2 < b.Item2)
				{
					return -1;
				}
				else
				{
					return 1;
				}
			}
		}

		private void UpdateComboBoxes(List<string> comboBoxInfo)
		{
			int counter = 0;
			foreach (var item in ComboBoxGrid.Children)
			{
				foreach (var it in ((ComboBox)item).Items)
				{
					if (counter < comboBoxInfo.Count && it.ToString() == comboBoxInfo[counter])
					{
						((ComboBox)item).SelectedItem = it;
						counter++;
						break;
					}
				}
			}
		}

		private string ProcessBitmap(Bitmap bm)
		{
			int contrast = 10;
			while (contrast <= 100)
			{
				Bitmap b = bm;

				b = (Bitmap)ImageUtility.IncreaseContrast(b, contrast);
				// TODO: Create a replacement for these 2 functions.
				ImageUtility.MakeGrayscale(b);
				ImageUtility.InverseGrayscale(b);
				// Increase size of the images so that the text have a increased pixel size for better accuracy.
				// According to https://groups.google.com/forum/#!msg/tesseract-ocr/Wdh_JJwnw94/24JHDYQbBQAJ
				// the ideal pixel height of letters are to be around 30-33.
				b = (Bitmap)ImageUtility.Resize(b, bm.Width * 2, bm.Height * 2, false);

				using (Tesseract.Page page = engine.Process(b))
				{
					string text = page.GetText();
					text = text.ToLower();
					// TODO: Fix this regex so it does not have to match with exactly bishop or knight etc, but instead just a general one.
					Match t = Regex.Match(text, @"(((bishop)|(knight)|(rook)|(pawn))[-–—][0-9])");
					Debug.WriteLine(text);
					if (t.Success)
					{
						Debug.WriteLine(t.Value);
						return t.Value; 
					}
					contrast += 10;
				}
			}
			return "";
		}

		private void Screenshot_Button_Click(object sender, RoutedEventArgs e)
		{
			string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			
			Bitmap bm = ImageUtility.CaptureScreen();
			List<Bitmap> rankList = ImageUtility.ExtractRanks(bm);

			List<string> ranks = new List<string>();
			for (int i = 0; i < rankList.Count; i++)
			{
				ranks.Add(RemoveUnwantedChars(ProcessBitmap(rankList[i])));
			}

			UpdateComboBoxes(ranks);
		}

		// Removes Space, Regular -, EN DASH, EM DASH
		private string RemoveUnwantedChars(string s) => s.Replace(" ", "").Replace("-", "").Replace("–", "").Replace("—", "");

		//
		// Hooked to every combobox, so we get dynamic updates of the avg mmr
		//
		private void Rank_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			InformationBlock.Text = "";

			double totalValue = 0;
			int counter = 0;
			foreach (var item in ComboBoxGrid.Children)
			{
				var t = ((ComboBox)item).SelectedIndex;
				if (t != -1) {
					int v = (int)Enum.Parse(typeof(Rank), ((ComboBox)item).SelectedValue.ToString());

					totalValue += v;
					counter++;
				}
			}
			double avg = totalValue / counter;

			InformationBlock.Text += $"Avg rank: {avg}\n";
		}
	}
}
