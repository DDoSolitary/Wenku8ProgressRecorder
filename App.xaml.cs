using System.Text;
using System.Windows;

namespace Wenku8ProgressRecorder {
	public partial class App : Application {
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
	}
}
