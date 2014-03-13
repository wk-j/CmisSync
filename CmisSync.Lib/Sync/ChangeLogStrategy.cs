using DotCMIS.Client;
using DotCMIS.Enums;
using System.IO;
using System.Linq;
using System;
using CmisSync.Lib.Cmis;
using DotCMIS.Exceptions;


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
            private void ChangeLogSync(IFolder remoteFolder, string localFolder)
            {
                // Get last ChangeLog token on server side.
                string lastTokenOnServer = CmisUtils.GetChangeLogToken(session);

                // Get last ChangeLog token that had been saved on client side.
                // TODO catch exception invalidArgument which means that changelog has been truncated and this token is not found anymore.
                string lastTokenOnClient = database.GetChangeLogToken();

                if (lastTokenOnClient == null)
                {
                    // Token is null, which means no sync has ever happened yet, so just copy everything.
                    RecursiveFolderCopy(remoteFolder, repoinfo.TargetDirectory); // TODO use localFolder instead of repoinfo.TargetDirectory ?

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
                            ApplyRemoteChange(change, localFolder);
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
            private void ApplyRemoteChange(IChangeEvent change, string repoFolder)
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
                        ICmisObject cmisObject = null;
                        try
                        {
                            cmisObject = session.GetObject(change.ObjectId);
                        }
                        catch (CmisObjectNotFoundException e)
                        {
                            Logger.Warn("Sync | Object can not be updated because it can not be found anymore. Deleting it locally: " + change.ObjectId);
                            Delete(change.ObjectId);
                            break;
                        }
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
                            string localFolderPath = Path.Combine(repoinfo.TargetDirectory, relativeFolderPath); // TODO use localFolder instead of repoinfo.TargetDirectory ?
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
                        Delete(change.ObjectId);
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


            private void Delete(string objectId)
            {
                Logger.Info("Sync | Deleting locally because deleted on remote server: " + objectId);

                string id = objectId;

                // Possible bug in Alfresco, see http://stackoverflow.com/q/22294589
                if (id.Contains("workspace://SpacesStore/"))
                {
                    id = id.Substring("workspace://SpacesStore/".Length);
                }

                // In the local database, find and remove the file/folder that has this id.
                string path = database.RemoveId(id);

                if (path == null)
                {
                    Logger.Error("Sync | Not deleting because no file/folder found with this object id: " + objectId);
                }
                else
                {
                    // Delete the file/folder from the local filesystem.
                    //string relativeFolderPath = Path.GetDirectoryName(remoteDocumentPath);
                    string unescapedPath = path.Replace('/', '\\'); // TODO OS-specific separator
                    unescapedPath = unescapedPath.Substring(repoinfo.Name.Length + 1);
                    string absolutePath = Path.Combine(repoinfo.TargetDirectory, unescapedPath); // TODO use localFolder instead of repoinfo.TargetDirectory ?

                    if (Utils.IsDirectory(absolutePath))
                    {
                        try
                        {
                            Logger.Info("Removing remotely deleted folder recursively: " + absolutePath);
                            Directory.Delete(absolutePath, true);
                        }
                        catch (Exception e)
                        {
                            ProcessRecoverableException("Could not delete folder recursively:" + absolutePath, e);
                        }
                    }
                    else
                    {
                        try
                        {
                            Logger.Info("Removing remotely deleted file: " + absolutePath);
                            File.Delete(absolutePath);
                        }
                        catch (Exception e)
                        {
                            ProcessRecoverableException("Could not delete file:" + absolutePath, e);
                        }
                    }
                }
            }
        }
    }
}
