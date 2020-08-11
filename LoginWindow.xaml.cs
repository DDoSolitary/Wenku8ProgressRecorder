using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Wenku8ProgressRecorder {
	public partial class LoginWindow : Window {
		private Data data;
		private bool credentialChanged;

		public LoginWindow() {
			this.InitializeComponent();
		}

		private void Window_ContentRendered(object sender, EventArgs e) {
			DataProvider.GetDataAsync().ContinueWith(t => {
				TaskHelpers.ShowIfError(t, this, true);
				this.data = t.Result;
				this.Dispatcher.Invoke(() => {
					this.UsernameBox.Text = this.data.Username;
					this.PasswordBox.Password = this.data.Password;
					this.IsEnabled = true;
				});
				this.credentialChanged = false;
			});
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			this.IsEnabled = false;
			var client = new HttpClient() { BaseAddress = new Uri("http://www.wenku8.net/") };
			var loginTask = Task.Run(async () => {
				var loginRes = await client.PostAsync("login.php", new FormUrlEncodedContent(new[] {
					KeyValuePair.Create("username", this.data.Username),
					KeyValuePair.Create("password", this.data.Password),
					KeyValuePair.Create("action", "login"),
					KeyValuePair.Create("usecookie", "0")
				}));
				if (!loginRes.Headers.Any(h => h.Key == "Set-Cookie" && h.Value.Any(c => c.Contains("jieqiUserInfo")))) {
					throw new Exception("Login failed.");
				}

			});
			var saveTask = credentialChanged ? DataProvider.SaveDataAsync() : Task.CompletedTask;
			Task.WhenAll(loginTask, saveTask).ContinueWith(t => {
				TaskHelpers.ShowIfError(t, this, true);
				this.Dispatcher.Invoke(() => {
					new MainWindow(client).Show();
					this.Close();
				});
			});
		}

		private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e) {
			data.Username = ((TextBox)sender).Text;
			credentialChanged = true;
		}

		private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
			data.Password = ((PasswordBox)sender).Password;
			credentialChanged = true;
		}
	}
}
