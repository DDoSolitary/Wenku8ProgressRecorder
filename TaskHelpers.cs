using System;
using System.Threading.Tasks;
using System.Windows;

namespace Wenku8ProgressRecorder {
	static class TaskHelpers {
		static void ShowError(Exception e, Window window, bool exit) {
			window.Dispatcher.Invoke(() => {
				MessageBox.Show(window, e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				if (exit) Application.Current.Shutdown(1);
			});
		}

		public static void ShowIfError(Task task, Window window, bool exit) {
			if (!task.IsCompletedSuccessfully) {
				ShowError(task.Exception, window, exit);
			}
		}

		public static void ShowIfError<T>(Task<T> task, Window window, bool exit) {
			if (!task.IsCompletedSuccessfully) {
				ShowError(task.Exception, window, exit);
			}
		}
	}
}
