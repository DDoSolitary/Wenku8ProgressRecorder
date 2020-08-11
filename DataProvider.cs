using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Google.Apis.Download;
using Google.Apis.Upload;

namespace Wenku8ProgressRecorder {
	static class DataProvider {
		private const string DataFileName = "data.xml";
		private const string DataMime = "text/xml";
		private const string DriveSpace = "appDataFolder";

		private static Data data;

		private static async Task<string> GetFileIdAsync() {
			var listReq = (await DriveServiceSingleton.GetServiceAsync()).Files.List();
			listReq.Q = $"name='{DataFileName}'";
			listReq.Spaces = DriveSpace;
			var list = await listReq.ExecuteAsync();
			return list.Files.Count > 0 ? list.Files[0].Id : null;
		}

		private static async Task<Data> DownloadDataAsync(string id) {
			await using var dataStream = new MemoryStream();
			var res = await (await DriveServiceSingleton.GetServiceAsync())
				.Files.Get(id).DownloadAsync(dataStream);
			if (res.Status == DownloadStatus.Failed) throw res.Exception;
			dataStream.Position = 0;
			return (Data)new XmlSerializer(typeof(Data)).Deserialize(dataStream);
		}

		public static async Task<Data> GetDataAsync() {
			if (data == null) {
				var id = await GetFileIdAsync();
				data = id == null ? new Data() : await DownloadDataAsync(id);
			}
			return data;
		}

		public static async Task SaveDataAsync() {
			var localData = await GetDataAsync();
			var id = await GetFileIdAsync();
			var service = await DriveServiceSingleton.GetServiceAsync();
			var dataStream = new MemoryStream();
			new XmlSerializer(typeof(Data)).Serialize(dataStream, localData);
			dataStream.Position = 0;
			var file = new Google.Apis.Drive.v3.Data.File() { Name = DataFileName };
			IUploadProgress res;
			if (id == null) {
				file.Parents = new[] { DriveSpace };
				res = await service.Files.Create(file, dataStream, DataMime).UploadAsync();
			} else {
				res = await service.Files.Update(file, id, dataStream, DataMime).UploadAsync();
			}
			if (res.Status == UploadStatus.Failed) throw res.Exception;
		}
	}
}
