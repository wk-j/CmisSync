using System;
using System.Collections.Generic;
using System.Text;
using DotCMIS.Client;
using System.IO;
using CmisSync.Lib.Database;

namespace CmisSync.Lib.Sync
{
    public partial class CmisRepo : RepoBase
    {
        /// <summary>
        /// Synchronization by comparizon with local database.
        /// </summary>
        public partial class SynchronizedFolder
        {
            public void ApplyLocalChanges(string rootFolder)
            {
                Logger.Debug("Checking for local changes");

                var deletedFolders = new List<string>();
                var deletedFiles = new List<string>();
                var modifiedFiles = new List<string>();
                var addedFolders = new List<string>();
                var addedFiles = new List<string>();

                activityListener.ActivityStarted();

                // Crawl through all entries in the database, and record the ones that have changed on the filesystem.
                // Check for deleted folders.
                var folders = database.GetLocalFolders();
                foreach(string folder in folders)
                {
                    if (!Directory.Exists(Utils.PathCombine(rootFolder, folder)))
                    {
                        deletedFolders.Add(folder);
                    }
                }
                var files = database.GetChecksummedFiles();
                foreach (ChecksummedFile file in files)
                {
                    // Check for deleted files.
                    if (File.Exists(Path.Combine(rootFolder, file.RelativePath)))
                    {
                        // Check for modified files.
                        if(file.HasChanged(rootFolder))
                        {
                            modifiedFiles.Add(file.RelativePath);
                        }
                    }
                    else
                    {
                        deletedFiles.Add(file.RelativePath);
                    }
                }

                // Check for added folders and files.
                // TODO performance improvement: To reduce the number of database requests, count files and folders, and skip this step if equal to the numbers of database rows.
                FindNewLocalObjects(rootFolder, ref addedFolders, ref addedFiles);

                // TODO: Try to make sense of related changes, for instance renamed folders.

                // TODO: Check local metadata modification cache.

                Logger.Debug("Applying " +
                    (deletedFolders.Count + deletedFiles.Count + modifiedFiles.Count + addedFolders.Count + addedFiles.Count)
                    + " changes.");

                // Apply changes to the server.

                // Apply: Deleted folders.
                foreach(string deletedFolder in deletedFolders)
                {
                    SyncItem deletedItem = SyncItemFactory.CreateFromLocalPath(deletedFolder, true, repoInfo, database);
                    IFolder deletedIFolder = (IFolder)session.GetObjectByPath(deletedItem.RemotePath);
                    DeleteRemoteFolder(deletedIFolder, deletedItem, Utils.UpperFolderLocal(deletedItem.LocalPath));
                }

                // Apply: Deleted files.
                foreach (string deletedFile in deletedFiles)
                {
                    SyncItem deletedItem = SyncItemFactory.CreateFromLocalPath(deletedFile, true, repoInfo, database);
                    IDocument deletedDocument = (IDocument)session.GetObjectByPath(deletedItem.RemotePath);
                    DeleteRemoteDocument(deletedDocument, deletedItem);
                }

                // Apply: Modified files.
                foreach (string modifiedFile in modifiedFiles)
                {
                    SyncItem modifiedItem = SyncItemFactory.CreateFromLocalPath(modifiedFile, true, repoInfo, database);
                    IDocument modifiedDocument = (IDocument)session.GetObjectByPath(modifiedItem.RemotePath);
                    UpdateFile(modifiedItem.LocalPath, modifiedDocument);
                }

                // Apply: Added folders.
                foreach (string addedFolder in addedFolders)
                {
                    string destinationFolderPath = Path.GetDirectoryName(addedFolder);
                    SyncItem folderItem = SyncItemFactory.CreateFromLocalPath(destinationFolderPath, true, repoInfo, database);

                    IFolder destinationFolder = (IFolder)session.GetObjectByPath(folderItem.RemotePath);
                    UploadFolderRecursively(destinationFolder, addedFolder);
                }

                // Apply: Added files.
                foreach (string addedFile in addedFiles)
                {
                    string destinationFolderPath = Path.GetDirectoryName(addedFile);
                    SyncItem folderItem = SyncItemFactory.CreateFromLocalPath(destinationFolderPath, true, repoInfo, database);

                    IFolder destinationFolder = (IFolder)session.GetObjectByPath(folderItem.RemotePath);
                    UploadFile(addedFile, destinationFolder);
                }

                Logger.Debug("Finished applying local changes.");
                activityListener.ActivityStopped();
            }

            public void FindNewLocalObjects(string folder, ref List<string> addedFolders, ref List<string> addedFiles)
            {
                // Check files in this folder.
                string[] files;
                try
                {
                    files = Directory.GetFiles(folder);
                }
                catch (Exception e)
                {
                    Logger.Warn("Could not get the files list from folder: " + folder, e);
                    return;
                }

                foreach (string file in files)
                {
                    // Check whether this file is present in database.
                    string filePath = Path.Combine(folder, file);
                    if ( ! database.ContainsLocalFile(filePath))
                    {
                        addedFiles.Add(filePath);
                    }
                }

                // Check folders and recurse.
                string[] subFolders;
                try
                {
                    subFolders = Directory.GetDirectories(folder);
                }
                catch (Exception e)
                {
                    Logger.Warn("Could not get the folders list from folder: " + folder, e);
                    return;
                }

                foreach (string subFolder in subFolders)
                {
                    // Check whether this sub-folder is present in database.
                    string folderPath = Path.Combine(folder, subFolder);
                    if (!database.ContainsLocalPath(folderPath))
                    {
                        addedFolders.Add(folderPath);
                    }

                    // Recurse.
                    FindNewLocalObjects(folderPath, ref addedFolders, ref addedFiles);
                }
            }
        }
    }
}