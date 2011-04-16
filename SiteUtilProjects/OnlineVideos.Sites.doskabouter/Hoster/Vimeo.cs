﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class Vimeo : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Vimeo";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"{""cached_timestamp"":[^,]*,""source"":""cache"",""signature"":""(?<signature>[^""]*)"",""timestamp"":(?<timestamp>[^,]*),""referrer"":null,""vimeo_url"":""vimeo\.com"",""player_url"":""player\.vimeo\.com"",""cdn_url"":""a\.vimeocdn\.com"",""cookie_domain"":""\.vimeo\.com""},""video"":{""id"":(?<id>[^,]*),""title");
                if (n.Success)
                {
                    string vidUrl = String.Format(@"http://player.vimeo.com/play_redirect?clip_id={0}&sig={1}&time={2}&codecs=H264,VP8,VP6",
                        n.Groups["id"].Value, n.Groups["signature"].Value, n.Groups["timestamp"].Value);
                    return SiteUtilBase.GetRedirectedUrl(vidUrl);
                }
            }
            return String.Empty;
        }
    }
}