using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace BingPaper
{

	public partial class Form1 : Form
	{
		private int currentIndex = 0;
		List<BingImage> bingImages = new List<BingImage>();

		public Form1(string[] args)
		{
			InitializeComponent();

			if (args != null && args.Length == 1 && args[0] == "set")
			{
				GetWallpapers();
				SetWallpaper();
				Application.Exit();
				return;
			}
		}

		private const int SPI_SETDESKWALLPAPER = 20;
		private const int SPIF_UPDATEINIFILE = 0x01;
		private const int SPIF_SENDWININICHANGE = 0x02;

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

		private void Form1_Load(object sender, EventArgs e)
		{
			GetWallpapers();
		}

		private void GetWallpapers(string region = "en-ww")
		{
			try
			{
				string address = "http://www.bing.com/hpimagearchive.aspx?format=xml&idx=0&n=20&mbl=1&mkt=" + region;
				string text = "";

				using (WebClient webClient = new WebClient())
					text = webClient.DownloadString(address);

				var doc = XDocument.Parse(text).Descendants("image");
				foreach (var n in doc)
				{
					bingImages.Add(new BingImage()
						{
							Copyright = Convert(n.Descendants("copyright").FirstOrDefault().Value), 
							ImageUrl = n.Descendants("url").FirstOrDefault().Value
						});

				}

				ShowBingImage(0);
			}
			catch (Exception)
			{
				if (MessageBox.Show("An error occurred getting the current Bing background.\r\nPlease try again later.", "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
					GetWallpapers(region);
			}
		}

		private void SetWallpaper()
		{
			if (File.Exists(System.IO.Path.GetTempPath() + "\\bingwallpaper.jpg"))
				SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, System.IO.Path.GetTempPath() + "\\bingwallpaper.jpg", SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
		}
		
		private void buttonOk_Click(object sender, EventArgs e)
		{
			SetWallpaper();
			this.Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			GetWallpapers(comboBox1.Text);
		}

		private void ShowBingImage(int index)
		{
			if (index < 0 || index > bingImages.Count - 1)
				return;

			using (WebClient webClient = new WebClient())
			{
				((NameValueCollection)webClient.Headers).Add("User-agent", "BingDesktop");
				webClient.DownloadFile("http://www.bing.com" + bingImages[index].ImageUrl + "_1920x1200.jpg", Path.GetTempPath() + "\\bingwallpaper.jpg");
			}

			pictureBox1.ImageLocation = Path.GetTempPath() + "\\bingwallpaper.jpg";

			var note =  bingImages[index].Copyright;
			if (note.Contains("("))
				note = note.Insert(note.IndexOf("("), Environment.NewLine);

			label1.Text = note; 
			toolTip1.SetToolTip(pictureBox1, note);
		}

		private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.X < pictureBox1.Width/2)
			{
				//currentIndex = currentIndex + 1;
				currentIndex++;
				if (currentIndex > bingImages.Count - 1)
					currentIndex--;
				ShowBingImage(currentIndex);
			}
			else
			{
				currentIndex--;
				if (currentIndex < 0)
					currentIndex = 0;
				ShowBingImage(currentIndex);
			}
		}


		private string Convert(string input)
		{
			byte[] data = Encoding.Default.GetBytes(input);
			return Encoding.UTF8.GetString(data);
		}

	}

	internal class BingImage
	{
		public string Copyright { get; set; }
		public string ImageUrl { get; set; }
	}

}
