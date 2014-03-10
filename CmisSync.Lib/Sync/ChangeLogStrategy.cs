using DotCMIS.Client;
using DotCMIS.Enums;
using System.IO;
using System.Linq;


namespace CmisSync.Lib.Sync
{
    public partial class CmisRepo : RepoBase
    {
        /// <summary>
        /// Synchronization with a particular CMIS folder.
        /// </summary>
        public partial class SynchronizedFolder
        {
            /// <summary>
            /// Synchronize using the ChangeLog feature of CMIS.
            /// Not all CMIS servers support this feature, so sometimes CrawlStrategy is used instead.
            /// </summary>
            private void ChangeLogSync(IFolder remoteFolder)
            {
                // Get last ChangeLog token on server side.
                session.Clear(); // Needed because DotCMIS keeps token in cache.
                string lastTokenOnServer = session.Binding.GetRepositoryService().GetRepositoryInfo(session.RepositoryInfo.Id, null).LatestChangeLogToken;

                // Get last ChangeLog token that had been saved on client side.
                // TODO catch exception invalidArgument which means that changelog has been truncated and this token is not found anymore.
                string lastTokenOnClient = database.GetChangeLogToken();

                if (lastTokenOnClient == null)
                {
                    // Token is null, which means no sync has ever happened yet, so just copy everything.
                    RecursiveFolderCopy(remoteFolder, repoinfo.TargetDirectory);

                    // Update ChangeLog token.
                    Logger.Info("Sync | Updating ChangeLog token: " + lastTokenOnServer);
                    database.SetChangeLogToken(lastTokenOnServer);
                }
                else
                {
                    // If there are remote changes, apply them.
                    if (lastTokenOnServer.Equals(lastTokenOnClient))
                    {
                        Logger.Info("Sync | No changes on server, ChangeLog token: " + lastTokenOnServer);
                    }
                    else
                    {
                        // Check which files/folders have changed.
                        int maxNumItems = 1000;
                        IChangeEvents changes = session.GetContentChanges(lastTokenOnClient, true, maxNumItems);

                        // Replicate each change to the local side.
                        foreach (IChangeEvent change in changes.ChangeEventList)
                        {
                            ApplyRemoteChange(change);
                        }

                        // Save ChangeLog token locally.
                        // TODO only if successful
                        Logger.Info("Sync | Updating ChangeLog token: " + lastTokenOnServer);
                        database.SetChangeLogToken(lastTokenOnServer);
                    }

                    // Upload local changes by comparing with database.
                    // (or let ChangeMonitor do it, and run CrawlStrategy once in a while to make sure?)
                    // TODO
                }
            }


            /// <summary>
            /// Apply a remote change.
            /// </summary>
            private void ApplyRemoteChange(IChangeEvent change)
            {
                Logger.Info("Sync | Change type:" + change.ChangeType.ToString() + " id:" + change.ObjectId);
                IFolder remoteFolder;
                IDocument remoteDocument;
                switch (change.ChangeType)
                {
                    // Case when an object has been created.
                    case ChangeType.Created:
                        // It seems that all created objects receive a subsequent update (at least with Alfresco), so even without implementing this case creation works.
                        Logger.Warn("Sync | Not applied because change not implemented:" + change.ChangeType);
                        break;

                    // Case when an object has been updated.
                    case ChangeType.Updated:
                        ICmisObject cmisObject = session.GetObject(change.ObjectId);
                        if (null != (remoteDocument = cmisObject as IDocument))
                        {
                            if (remoteDocument.Paths.Count == 0)
                            {
                                Logger.Info("Sync | Change in non-fileable object: " + remoteDocument.ContentStreamFileName + " (" + remoteDocument.ContentStreamMimeType + ")");
                                break;
                            }
                            string remoteDocumentPath = remoteDocument.Paths.First();
                            if (!remoteDocumentPath.StartsWith(remoteFolderPath))
                            {
                                Logger.Info("Sync | Change in unrelated object: " + remoteDocumentPath);
                                break; // The change is not under the folder we care about.
                            }
                            string relativePath = remoteDocumentPath.Substring(remoteFolderPath.Length + 1);
                            string relativeFolderPath = Path.GetDirectoryName(relativePath);
                            relativeFolderPath = relativeFolderPath.Replace('/', '\\'); // TODO OS-specific separator
                            string localFolderPath = Path.Combine(repoinfo.TargetDirectory, relativeFolderPath);
                            DownloadFile(remoteDocument, localFolderPath);
                        }
                        else if (null != (remoteFolder = cmisObject as IFolder))
                        {
                            string localFolder = Path.Combine(repoinfo.TargetDirectory, remoteFolder.Path);
                            if(!this.repoinfo.isPathIgnored(remoteFolder.Path))
                                RecursiveFolderCopy(remoteFolder, localFolder);
                        }
                        break;

                    // Case when an object has been deleted.
                    case ChangeType.Deleted:
                        Logger.Info("Sync | Deleting locally because deleted on remote server: " + change.ObjectId);

                        string id = change.ObjectId;

                        // Possible bug in Alfresco, see http://stackoverflow.com/q/22294589
                        if (id.Contains("workspace://SpacesStore/"))
                        {
                            id = id.Substring("workspace://SpacesStore/".Length);
                        }

                        // In the local database, find and remove the file/folder that has this id.
                        string path = database.RemoveId(id);

                        if (path == null)
                        {
                            Logger.Error("Sync | Not deleting because no file/folder found with this object id: " + change.ObjectId);
                        }

                        // Delete the file/folder from the local filesystem.
                        /*remoteFolderPath = 
                        try
                        {
                            Logger.Info("Removing remotely deleted folder: " + folderPath);
                            Directory.Delete(folderPath, true);
                        }
                        catch (Exception e)
                        {
                            ProcessRecoverableException("Could not delete tree:" + folderPath, e);
                            return false;
                        }*/
                        
                        break;

                    // Case when access control or security policy has changed.
                    case ChangeType.Security:
                        // TODO
                        Logger.Warn("Sync | Not applied because change not implemented: " + change.ChangeType);
                        break;

                    default:
                        Logger.Warn("Sync | Not applied because change not implemented: " + change.ChangeType);
                        break;
                }
            }
        }
    }
}
