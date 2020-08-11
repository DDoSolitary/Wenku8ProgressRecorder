using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Http;
using HtmlAgilityPack;
using System.ComponentModel;

namespace Wenku8ProgressRecorder {
	public partial class MainWindow : Window {
		private readonly HttpClient client;

		public MainWindow(HttpClient client) {
			this.client = client;
			this.LoadListsAsync().ContinueWith(t => TaskHelpers.ShowIfError(t, this, false));
			this.InitializeComponent();
		}

		private async Task<HtmlDocument> GetDocumentAsync(string url) {
			var getRes = await client.GetAsync(url);
			getRes.EnsureSuccessStatusCode();
			var doc = new HtmlDocument();
			doc.Load(await getRes.Content.ReadAsStreamAsync(), Encoding.GetEncoding("GBK"));
			return doc;
		}

		private async Task<BookInfo> LoadBookAsync(string bookName, string bookUrl) {
			var idBegin = bookUrl.IndexOf('=') + 1;
			var idEnd = bookUrl.IndexOf('&');
			var bookId = int.Parse(bookUrl.Substring(idBegin, idEnd - idBegin));
			var doc = await this.GetDocumentAsync($"novel/{bookId / 1000}/{bookId}/index.htm");
			var volumeList = new List<VolumeInfo>();
			var book = new BookInfo { Name = bookName, Volumes = volumeList };
			List<ChapterInfo> currentChapterList = null;
			VolumeInfo currentVolume = null;
			foreach (var row in doc.DocumentNode.SelectNodes("//table//tr")) {
				var cells = row.SelectNodes("td");
				if (cells.Count == 1) {
					currentChapterList = new List<ChapterInfo>();
					currentVolume = new VolumeInfo() {
						Parent = book,
						Name = cells[0].InnerText,
						Chapters = currentChapterList
					};
					volumeList.Add(currentVolume);
				} else {
					foreach (var cell in cells) {
						var chapterLink = cell.SelectSingleNode("a");
						if (chapterLink != null) {
							currentChapterList.Add(new ChapterInfo() {
								Parent = currentVolume,
								Name = chapterLink.InnerText,
								Id = chapterLink.GetAttributeValue("href", null)
							});
						}
					}
				}
			}
			return book;
		}

		private async Task LoadListsAsync() {
			Dispatcher.Invoke(() => this.IsEnabled = false);
			var doc = await this.GetDocumentAsync("modules/article/bookcase.php");
			var bookTasks = doc.DocumentNode.SelectNodes("//form[@id='checkform']//tr/td[2]/a")
				.Select(async node => await this.LoadBookAsync(node.InnerText, node.GetAttributeValue("href", null)));
			var books = await Task.WhenAll(bookTasks);
			Dispatcher.Invoke(() => {
				this.BookList.ItemsSource = books;
				this.IsEnabled = true;
			});
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e) {
			this.LoadListsAsync().ContinueWith(t => TaskHelpers.ShowIfError(t, this, false));
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e) {
			this.IsEnabled = false;
			DataProvider.SaveDataAsync().ContinueWith(t => {
				TaskHelpers.ShowIfError(t, this, false);
				this.Dispatcher.Invoke(() => {
					this.IsEnabled = true;
					if (t.IsCompletedSuccessfully) this.SaveButton.IsEnabled = false;
				});
			});
		}

		private void CheckBox_Click(object sender, RoutedEventArgs e) {
			this.SaveButton.IsEnabled = true;
		}
	}

	public abstract class BookPartInfo : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		public BookPartInfo Parent { get; set; }
		public string Name { get; set; }
		public abstract bool? IsRead { get; set; }

		public override string ToString() => Name;
		protected void NotifyIsReadChanged() {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRead)));
			Parent?.NotifyIsReadChanged();
		}
	}

	public class BookInfo : BookPartInfo {
		public IEnumerable<VolumeInfo> Volumes { get; set; }
		public override bool? IsRead {
			get {
				var totalCount = this.Volumes.Count();
				var readCount = this.Volumes.Count(volume => volume.IsRead == true);
				if (readCount == totalCount) return true;
				if (readCount == 0) return false;
				return null;
			}
			set {
				foreach (var volume in this.Volumes) {
					volume.IsRead = value;
				}
				this.NotifyIsReadChanged();
			}
		}
	}

	public class VolumeInfo : BookPartInfo {
		public IEnumerable<ChapterInfo> Chapters { get; set; }
		public override bool? IsRead {
			get {
				var totalCount = this.Chapters.Count();
				var readCount = this.Chapters.Count(volume => volume.IsRead == true);
				if (readCount == totalCount) return true;
				if (readCount == 0) return false;
				return null;
			}
			set {
				foreach (var volume in this.Chapters) {
					volume.IsRead = value;
				}
				this.NotifyIsReadChanged();
			}
		}
	}

	public class ChapterInfo : BookPartInfo {
		public string Id { get; set; }
		public override bool? IsRead {
			get => DataProvider.GetDataAsync().Result.ReadList.Contains(Id);
			set {
				var list = DataProvider.GetDataAsync().Result.ReadList;
				if (value == true) list.Add(Id);
				else if (value == false) list.Remove(Id);
				this.NotifyIsReadChanged();
			}
		}
	}
}
