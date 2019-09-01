using System;
using System.Collections.Generic;

namespace Wenku8ProgressRecorder {
	[Serializable]
	public class Data {
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public HashSet<string> ReadList { get; set; } = new HashSet<string>();

	}
}
