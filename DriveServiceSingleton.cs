using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace Wenku8ProgressRecorder {
	static class DriveServiceSingleton {
		private const string Credentials = "{\"installed\":{\"client_id\":\"958774091541-2phrrvrhu4ue271h1v2lkj0i6jpcr2lv.apps.googleusercontent.com\",\"project_id\":\"quickstart-1567244662205\",\"auth_uri\":\"https://accounts.google.com/o/oauth2/auth\",\"token_uri\":\"https://oauth2.googleapis.com/token\",\"auth_provider_x509_cert_url\":\"https://www.googleapis.com/oauth2/v1/certs\",\"client_secret\":\"IxSopMB561co3sxo9C-QlF7p\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\",\"http://localhost\"]}}";
		private const string TokenPath = "gdrive-token";
		private const string AppName = "Wenku8 Progress Recorder";
		private static readonly string[] Scopes = { DriveService.Scope.DriveAppdata };

		private static DriveService service;
		public static async Task<DriveService> GetServiceAsync() {
			if (service == null) {
				var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(new MemoryStream(Encoding.UTF8.GetBytes(Credentials))).Secrets,
					Scopes, "user", CancellationToken.None,
					new FileDataStore(TokenPath, true)
				);
				service = new DriveService(new BaseClientService.Initializer() {
					HttpClientInitializer = credential,
					ApplicationName = AppName
				});
			}
			return service;
		}
	}
}
