using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudSoundMetroV2.DataAccessLayer
{
    public class AzureAccess
    {
        private const string connectionString =
            "DefaultEndpointsProtocol=https;AccountName=cloudmp3;AccountKey=gHwhRUYX9xNAJIEjcWAG8RayTX//ir8NPNWWAK/BxUQNa85JQizZQ6Emn9ucoxez+M0pa/2q499t4SGQ+ksX+Q==";
        private const string containerName = "cloudmp3";
        private const string blobStorageUri = "https://cloudmp3.blob.core.windows.net";
        public bool isCompleted = false;

        private string localMp3Directory = "C:/Users/Public/Music/CloudMp3";

        private CloudStorageAccount _account;
        private CloudBlobClient _client;
        private CloudBlobContainer _container;
        private StorageCredentials _accountCredentials;
        private StorageCredentials _permissions;

        public AzureAccess()
        {
            _accountCredentials = new StorageCredentials("cloudmp3", "gHwhRUYX9xNAJIEjcWAG8RayTX//ir8NPNWWAK/BxUQNa85JQizZQ6Emn9ucoxez+M0pa/2q499t4SGQ+ksX+Q==");
            _account = new CloudStorageAccount(_accountCredentials, true);
            _client = _account.CreateCloudBlobClient();
            _container = _client.GetContainerReference(containerName);
            _container.CreateIfNotExists();

            setPermissions(_container);

            _permissions = new StorageCredentials(_container.GetSharedAccessSignature(new SharedAccessBlobPolicy(), "policy"));
        }

        private void setPermissions(CloudBlobContainer container)
        {
            container.GetPermissions().SharedAccessPolicies.Clear();
            BlobContainerPermissions blobPermissions = new BlobContainerPermissions();
            blobPermissions.SharedAccessPolicies.Add("policy", new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-10),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write |
                SharedAccessBlobPermissions.List
            });

            blobPermissions.PublicAccess = BlobContainerPublicAccessType.Off;
            container.SetPermissions(blobPermissions);
        }

        public void UploadSong(string filePath, int userID)
        {
            using (var context = new CloudMp3Context())
            {
                using (var tran = context.Database.BeginTransaction())
                {
                    try
                    {
                        isCompleted = false;
                        string fileName = Path.GetFileName(filePath);
                        TagLib.File tagFile = TagLib.File.Create(filePath);
                        CloudBlockBlob blockBlob = _container.GetBlockBlobReference(fileName);
                        var user = (from u in context.Users
                                    where u.U_Id == userID
                                    select u).SingleOrDefault();
                        string s = "";
                        if (tagFile.Tag.Title == null)
                        {
                            s = fileName.Replace(".mp3", "");
                        }
                        else
                        {
                            s = tagFile.Tag.Title;
                        }
                        Song newSong = new Song()
                        {
                            S_Artist = tagFile.Tag.FirstPerformer,
                            S_Length = (int)tagFile.Properties.Duration.TotalMilliseconds,
                            S_Title = s,
                            S_OwnerId = userID,
                            S_Path = blobStorageUri + blockBlob.Uri.AbsolutePath
                        };
                        Console.WriteLine(newSong.S_Path);
                        user.Songs.Add(newSong);

                        using (var fileStream = System.IO.File.OpenRead(filePath))
                        {
                            blockBlob.UploadFromStream(fileStream);
                            isCompleted = true;
                        }
                        isCompleted = false;
                        context.SaveChanges();
                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.Rollback();
                    }
                }
            }
        }

        public string GetSaS()
        {
            return _container.GetSharedAccessSignature(new SharedAccessBlobPolicy(), "policy");
        }

        public void DownloadSong(string filePath)
        {
            CloudBlockBlob blockBlob = _container.GetBlockBlobReference(filePath);
            string writePath = String.Format("{0}/{1}", localMp3Directory, filePath);
            isCompleted = false;
            using (var fileStream = System.IO.File.OpenWrite(writePath))
            {

                blockBlob.DownloadToStream(fileStream);
                isCompleted = true;

            }
        }

        public ObservableCollection<string> GetCloudSongs()
        {
            ObservableCollection<string> cloudSongs = new ObservableCollection<string>();

            foreach (IListBlobItem blob in _container.ListBlobs(null, false))
            {
                cloudSongs.Add(blob.Uri.ToString());
            }
            return cloudSongs;
        }
    }
}
