using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Net;

namespace OnlineVideos.Sites
{
    public class DownloadedVideoUtil : SiteUtilBase, IFilter
    {
        public Dictionary<string, DownloadInfo> currentDownloads = new Dictionary<string, DownloadInfo>();
        string lastSort = "date";

        // keep a reference of all Categories ever created and reuse them, to get them selected when returning to the category view
        Dictionary<string, Category> cachedCategories = new Dictionary<string, Category>();

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Category cat = null;
            // add a category for all files
            if (!cachedCategories.TryGetValue(Translation.All, out cat))
            {
                cat = new RssLink() { Name = Translation.All, Url = OnlineVideoSettings.Instance.DownloadDir };
                cachedCategories.Add(cat.Name, cat);
            }
            Settings.Categories.Add(cat);

            if (currentDownloads.Count > 0)
            {
                // add a category for all downloads in progress
                if (!cachedCategories.TryGetValue(Translation.Downloading, out cat))
                {
                    cat = new Category() { Name = Translation.Downloading, Description = Translation.DownloadingDescription };
                    cachedCategories.Add(cat.Name, cat);
                }
                Settings.Categories.Add(cat);
            }

            foreach (string aDir in Directory.GetDirectories(OnlineVideoSettings.Instance.DownloadDir))
            {
                SiteUtilBase util = null;
                if (OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(Path.GetFileName(aDir), out util))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(aDir);
                    if (dirInfo.GetFiles().Length == 0)
                    {
                        try { Directory.Delete(aDir); } catch {} // try to delete empty directories
                    }
                    else
                    {
                        SiteSettings aSite = util.Settings;
                        if (aSite.IsEnabled &&
                           (!aSite.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                        {
                            if (!cachedCategories.TryGetValue(aSite.Name + " - " + Translation.DownloadedVideos, out cat))
                            {
                                cat = new RssLink();
                                cat.Name = aSite.Name + " - " + Translation.DownloadedVideos;
                                cat.Description = aSite.Description;
                                ((RssLink)cat).Url = aDir;
                                cat.Thumb = Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Icons\" + aSite.Name + ".png");
                                cachedCategories.Add(cat.Name, cat);
                            }
                            Settings.Categories.Add(cat);
                        }
                    }
                }
            }

            // need to always get the categories, because when adding new fav video from a new site, a removing the last one for a site, the categories must be refreshed 
            Settings.DynamicCategoriesDiscovered = false;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            return getVideoList(category is RssLink ? (category as RssLink).Url : null, "*", category.Name == Translation.All);
        }        

        List<VideoInfo> getVideoList(string path, string search, bool recursive)
        {
            List<VideoInfo> loVideoInfoList = new List<VideoInfo>();
            if (!(string.IsNullOrEmpty(path)))
            {
                FileInfo[] files = new DirectoryInfo(path).GetFiles(search, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                foreach (FileInfo file in files)
                {
                    if (isPossibleVideo(file.Name) && PassesAgeCheck(file.FullName))
                    {
                        VideoInfo loVideoInfo = new VideoInfo();
                        loVideoInfo.VideoUrl = file.FullName;
                        loVideoInfo.ImageUrl = file.FullName.Substring(0, file.FullName.LastIndexOf(".")) + ".jpg";
                        loVideoInfo.Length = file.LastWriteTime.ToString("g", OnlineVideoSettings.Instance.Locale);
                        loVideoInfo.Title = file.Name;
                        loVideoInfo.Description = string.Format("{0} MB", (file.Length / 1024 / 1024).ToString("N0"));
                        loVideoInfo.Other = file;
                        loVideoInfoList.Add(loVideoInfo);
                    }
                }

                switch (lastSort)
                {
                    case "name":
                        loVideoInfoList.Sort((Comparison<VideoInfo>)delegate(VideoInfo v1, VideoInfo v2) 
                        { 
                            return v1.Title.CompareTo(v2.Title); 
                        });
                        break;
                    case "date":
                        loVideoInfoList.Sort((Comparison<VideoInfo>)delegate(VideoInfo v1, VideoInfo v2) 
                        {
                            return (v2.Other as FileInfo).LastWriteTime.CompareTo((v1.Other as FileInfo).LastWriteTime); 
                        });
                        break;
                    case "size":
                        loVideoInfoList.Sort((Comparison<VideoInfo>)delegate(VideoInfo v1, VideoInfo v2)
                        {
                            return (v2.Other as FileInfo).Length.CompareTo((v1.Other as FileInfo).Length);
                        });
                        break;
                }
            }
            else
            {
                lock (currentDownloads)
                {
                    foreach (DownloadInfo di in currentDownloads.Values)
                    {
                        if (PassesAgeCheck(di.LocalFile))
                        {
                            string progressInfo = (di.PercentComplete != 0 || di.KbTotal != 0 || di.KbDownloaded != 0) ?
                                string.Format(" | {0}% / {1} KB - {2} KB/sec", di.PercentComplete, di.KbTotal > 0 ? di.KbTotal.ToString("n0") : di.KbDownloaded.ToString("n0"), (int)(di.KbDownloaded / (DateTime.Now - di.Start).TotalSeconds)) : "";

                            VideoInfo loVideoInfo = new VideoInfo();
                            loVideoInfo.Title = di.Title;
                            loVideoInfo.ImageUrl = di.ThumbFile;
                            loVideoInfo.Length = di.Start.ToString("HH:mm:ss") + progressInfo;
                            loVideoInfo.Description = string.Format("{0}\n{1}", di.Url, di.LocalFile, progressInfo);
                            loVideoInfo.Other = di;
                            loVideoInfoList.Add(loVideoInfo);
                        }
                    }
                }
            }
            return loVideoInfoList;
        }

        public override List<string> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<string> options = new List<string>();
            if (selectedItem != null)
            {
                if (selectedCategory is RssLink)
                {
                    options.Add(Translation.Delete);
                    options.Add(Translation.DeleteAll);
                }
                else
                {
                    options.Add(Translation.Cancel);
                }
            }
            return options;
        }

        public override bool ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, string choice)
        {
            if (choice == Translation.Delete)
            {
                if (System.IO.File.Exists(selectedItem.ImageUrl)) System.IO.File.Delete(selectedItem.ImageUrl);
                if (System.IO.File.Exists(selectedItem.VideoUrl)) System.IO.File.Delete(selectedItem.VideoUrl);
            }
            else if (choice == Translation.DeleteAll)
            {
                FileInfo[] files = new DirectoryInfo((selectedCategory as RssLink).Url).GetFiles("*", selectedCategory.Name == Translation.All ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                foreach (FileInfo file in files)
                {
                    if (isPossibleVideo(file.Name))
                    {
                        try
                        {
                            string imageFileName = file.FullName.Substring(0, file.FullName.LastIndexOf(".")) + ".jpg";
                            if (System.IO.File.Exists(imageFileName)) System.IO.File.Delete(imageFileName);
                            System.IO.File.Delete(file.FullName);
                        }
                        catch { } // file might be locked (e.g. still downloading)
                    }
                }
            }
            else if (choice == Translation.Cancel)
            {
                ((IDownloader)(selectedItem.Other as DownloadInfo).Downloader).CancelAsync();
            }
            return true;
        }

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            query = query.Replace(' ', '*');
            if (!query.StartsWith("*")) query = "*" + query;
            if (!query.EndsWith("*")) query += "*";
            return getVideoList(OnlineVideoSettings.Instance.DownloadDir, query, true);
        }

        #endregion

        #region IFilter Member

        public List<VideoInfo> filterVideoList(Category category, int maxResult, string orderBy, string timeFrame)
        {
            lastSort = orderBy;
            return getVideoList(category);
        }

        public List<VideoInfo> filterSearchResultList(string query, int maxResult, string orderBy, string timeFrame)
        {
            lastSort = orderBy;
            return Search(query);
        }

        public List<VideoInfo> filterSearchResultList(string query, string category, int maxResult, string orderBy, string timeFrame)
        {
            return null;
        }

        public List<int> getResultSteps()
        {
            return new List<int>();
        }

        public Dictionary<string, string> getOrderbyList()
        {
            Dictionary<string, string> options = new Dictionary<string, string>();
            options.Add(Translation.Date, "date");
            options.Add(Translation.Name, "name");
            options.Add(Translation.Size, "size");
            return options;
        }

        public Dictionary<string, string> getTimeFrameList()
        {
            return new Dictionary<string,string>();
        }

        #endregion

        bool PassesAgeCheck(string fullFileName)
        {
            if (!OnlineVideoSettings.Instance.UseAgeConfirmation) return true;
            if (OnlineVideoSettings.Instance.UseAgeConfirmation && OnlineVideoSettings.Instance.AgeConfirmed) return true;

            try
            {
                // try to find out what site this video belongs to
                string siteName = Path.GetDirectoryName(fullFileName);
                siteName = siteName.Substring(siteName.LastIndexOf('\\') + 1);
                SiteUtilBase util = null;
                if (OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(siteName, out util))
                {
                    return !util.Settings.ConfirmAge;
                }
            }
            catch { }
            return true;
        }
    }
}